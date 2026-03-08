using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moa.Data;
using moa.Models;
using moa.Services;

namespace moa.Controllers;

[Authorize]
public class CardGroupsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentStoreProvider _currentStoreProvider;
    private readonly IMonthlyBalanceService _monthlyBalanceService;

    public CardGroupsController(AppDbContext db, ICurrentStoreProvider currentStoreProvider, IMonthlyBalanceService monthlyBalanceService)
    {
        _db = db;
        _currentStoreProvider = currentStoreProvider;
        _monthlyBalanceService = monthlyBalanceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var storeId = _currentStoreProvider.GetStoreId();
        var nowMonth = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        await _monthlyBalanceService.EnsureBalancesForMonthAsync(storeId, nowMonth);

        var groups = await _db.CardGroups
            .AsNoTracking()
            .Where(g => g.StoreId == storeId)
            .OrderBy(g => g.GroupName)
            .Select(g => new CardGroupListItemVm
            {
                CardGroupId = g.CardGroupId,
                GroupName = g.GroupName,
                CardsCount = _db.RationCards.Count(c => c.CardGroupId == g.CardGroupId)
            })
            .ToListAsync();

        var cards = await _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .Join(
                _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == storeId),
                card => card.CustomerId,
                a => a.CustomerId,
                (card, a) => card)
            .OrderBy(c => c.Customer.FullName)
            .Select(c => new CardGroupCardItemVm
            {
                CardId = c.CardId,
                CardNumber = c.CardNumber,
                CustomerName = c.Customer.FullName,
                Phone = c.Customer.Phone,
                CardGroupId = c.CardGroupId
            })
            .ToListAsync();

        return View(new CardGroupsIndexViewModel
        {
            Groups = groups,
            Cards = cards
        });
    }

    // ── صفحة تفاصيل المجموعة - تُولَّد تلقائيًا لكل مجموعة عبر /CardGroups/Details/{id} ──
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var storeId = _currentStoreProvider.GetStoreId();
        var nowMonth = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        await _monthlyBalanceService.EnsureBalancesForMonthAsync(storeId, nowMonth);

        var group = await _db.CardGroups
            .AsNoTracking()
            .Where(g => g.CardGroupId == id && g.StoreId == storeId)
            .Select(g => new CardGroupDetailsVm
            {
                CardGroupId = g.CardGroupId,
                GroupName   = g.GroupName,
                CreatedAt   = g.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (group is null) return NotFound();

        var cards = await _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .Where(c => c.CardGroupId == id)
            .OrderBy(c => c.Customer.FullName)
            .Select(c => new CardGroupCardItemVm
            {
                CardId       = c.CardId,
                CardNumber   = c.CardNumber,
                CustomerName = c.Customer.FullName,
                Phone        = c.Customer.Phone,
                CardGroupId  = c.CardGroupId
            })
            .ToListAsync();

        var cardIds = cards.Select(c => c.CardId).ToList();
        nowMonth = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        var balances = await _db.CardMonthlyBalances
            .AsNoTracking()
            .Where(b => b.YearMonth == nowMonth && cardIds.Contains(b.CardId))
            .ToDictionaryAsync(b => b.CardId, b => b.IsSwipedInMachine);

        foreach (var c in cards)
        {
            balances.TryGetValue(c.CardId, out var swiped);
            c.IsSwipedInMachine = swiped;
        }

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfNextMonth = startOfMonth.AddMonths(1);

        var withdrawnCardIds = await _db.WithdrawalReceipts
            .AsNoTracking()
            .Where(r => r.StoreId == storeId && r.WithdrawnAt >= startOfMonth && r.WithdrawnAt < startOfNextMonth && cardIds.Contains(r.CardId))
            .Select(r => r.CardId)
            .Distinct()
            .ToListAsync();

        var withdrawnSet = new HashSet<int>(withdrawnCardIds);

        foreach (var c in cards)
        {
            c.HasWithdrawnThisMonth = withdrawnSet.Contains(c.CardId);
        }

        group.Cards = cards;

        return View(group);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CardGroupsIndexViewModel model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
        }

        var name = (model.NewGroupName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return RedirectToAction(nameof(Index));
        }

        var exists = await _db.CardGroups.AnyAsync(g => g.StoreId == storeId && g.GroupName == name);
        if (exists)
        {
            return RedirectToAction(nameof(Index));
        }

        _db.CardGroups.Add(new CardGroup
        {
            StoreId = storeId,
            GroupName = name,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(CardGroupDeleteVm model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var g = await _db.CardGroups.FirstOrDefaultAsync(x => x.CardGroupId == model.CardGroupId && x.StoreId == storeId);
        if (g is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var cards = await _db.RationCards.Where(c => c.CardGroupId == g.CardGroupId).ToListAsync();
        foreach (var c in cards)
        {
            c.CardGroupId = null;
        }

        _db.CardGroups.Remove(g);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(CardGroupAssignVm model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var card = await _db.RationCards
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.CardId == model.CardId);

        if (card is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var assignmentOk = await _db.CustomerStoreAssignments.AnyAsync(a => a.CustomerId == card.CustomerId && a.StoreId == storeId && a.EndAt == null);
        if (!assignmentOk)
        {
            return Forbid();
        }

        var groupOk = await _db.CardGroups.AnyAsync(g => g.CardGroupId == model.CardGroupId && g.StoreId == storeId);
        if (!groupOk)
        {
            return RedirectToAction(nameof(Index));
        }

        card.CardGroupId = model.CardGroupId;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unassign(int cardId)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var card = await _db.RationCards
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.CardId == cardId);

        if (card is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var assignmentOk = await _db.CustomerStoreAssignments.AnyAsync(a => a.CustomerId == card.CustomerId && a.StoreId == storeId && a.EndAt == null);
        if (!assignmentOk)
        {
            return Forbid();
        }

        card.CardGroupId = null;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSwipeStatus(int cardId, bool isSwiped)
    {
        var storeId = _currentStoreProvider.GetStoreId();
        var now = DateTime.UtcNow;
        var yearMonth = now.Year * 100 + now.Month;

        var card = await _db.RationCards
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.CardId == cardId);

        if (card == null) return NotFound();

        var hasAssignment = await _db.CustomerStoreAssignments.AnyAsync(a => a.CustomerId == card.CustomerId && a.StoreId == storeId && a.EndAt == null);
        if (!hasAssignment) return Forbid();

        var balance = await _db.CardMonthlyBalances
            .FirstOrDefaultAsync(b => b.CardId == cardId && b.YearMonth == yearMonth);

        if (balance == null)
        {
            return BadRequest("لا يوجد رصيد مفتوح لهذا الشهر لهذه البطاقة");
        }

        balance.IsSwipedInMachine = isSwiped;
        await _db.SaveChangesAsync();

        return Ok();
    }
}
