using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moa.Data;
using moa.Models;
using moa.Services;

namespace moa.Controllers;

[Authorize]
public class MonthlyBalancesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentStoreProvider _currentStoreProvider;
    private readonly IMonthlyBalanceService _monthlyBalanceService;

    public MonthlyBalancesController(AppDbContext db, ICurrentStoreProvider currentStoreProvider, IMonthlyBalanceService monthlyBalanceService)
    {
        _db = db;
        _currentStoreProvider = currentStoreProvider;
        _monthlyBalanceService = monthlyBalanceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? yearMonth, string? search)
    {
        var storeId = _currentStoreProvider.GetStoreId();
        var ym = yearMonth ?? GetCurrentYearMonth();

        await _monthlyBalanceService.EnsureBalancesForMonthAsync(storeId, ym);

        var cardsQuery = _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .Join(
                _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == storeId),
                card => card.CustomerId,
                a => a.CustomerId,
                (card, a) => card)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            cardsQuery = cardsQuery.Where(c => c.Customer.FullName.Contains(search) || c.CardNumber.Contains(search));
        }

        var cards = await cardsQuery
            .OrderBy(c => c.Customer.FullName)
            .Select(c => new { c.CardId, c.CardNumber, CustomerName = c.Customer.FullName, c.MembersCount })
            .Take(100)
            .ToListAsync();

        var cardIds = cards.Select(c => c.CardId).ToList();

        var existingBalances = await _db.CardMonthlyBalances
            .AsNoTracking()
            .Where(b => b.YearMonth == ym && cardIds.Contains(b.CardId))
            .ToDictionaryAsync(b => b.CardId, b => b.OpeningBalance);

        var vm = new MonthlyBalancesIndexViewModel
        {
            YearMonth = ym,
            Search = search,
            Items = cards.Select(c => new MonthlyBalanceListItemVm
            {
                CardId = c.CardId,
                CardNumber = c.CardNumber,
                CustomerName = c.CustomerName,
                MembersCount = c.MembersCount,
                OpeningBalance = existingBalances.TryGetValue(c.CardId, out var bal) ? bal : null
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int cardId, int yearMonth)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var card = await _db.RationCards
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.CardId == cardId);

        if (card is null)
        {
            return NotFound();
        }

        var assignmentOk = await _db.CustomerStoreAssignments.AnyAsync(a => a.CustomerId == card.CustomerId && a.StoreId == storeId && a.EndAt == null);
        if (!assignmentOk)
        {
            return Forbid();
        }

        var existing = await _db.CardMonthlyBalances
            .FirstOrDefaultAsync(b => b.CardId == cardId && b.YearMonth == yearMonth);

        var membersSnapshot = existing?.MembersCountSnapshot ?? card.MembersCount;
        var computedOpening = ComputeOpeningBalance(membersSnapshot, card.PricePerPerson);

        var vm = new MonthlyBalanceEditViewModel
        {
            CardId = card.CardId,
            YearMonth = yearMonth,
            CustomerName = card.Customer.FullName,
            CardNumber = card.CardNumber,
            MembersCountSnapshot = membersSnapshot,
            OpeningBalance = existing?.OpeningBalance ?? computedOpening,
            OpenAt = existing?.OpenAt ?? DateTime.UtcNow
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MonthlyBalanceEditViewModel model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var card = await _db.RationCards
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.CardId == model.CardId);

        if (card is null)
        {
            return NotFound();
        }

        var assignmentOk = await _db.CustomerStoreAssignments.AnyAsync(a => a.CustomerId == card.CustomerId && a.StoreId == storeId && a.EndAt == null);
        if (!assignmentOk)
        {
            return Forbid();
        }

        var existing = await _db.CardMonthlyBalances
            .FirstOrDefaultAsync(b => b.CardId == model.CardId && b.YearMonth == model.YearMonth);

        if (existing is null)
        {
            _db.CardMonthlyBalances.Add(new CardMonthlyBalance
            {
                CardId = model.CardId,
                YearMonth = model.YearMonth,
                MembersCountSnapshot = model.MembersCountSnapshot,
                OpeningBalance = model.OpeningBalance,
                OpenAt = model.OpenAt,
                CloseAt = null,
                Notes = null,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.MembersCountSnapshot = model.MembersCountSnapshot;
            existing.OpeningBalance = model.OpeningBalance;
            existing.OpenAt = model.OpenAt;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { yearMonth = model.YearMonth });
    }

    private static int GetCurrentYearMonth()
    {
        var now = DateTime.UtcNow;
        return now.Year * 100 + now.Month;
    }

    private static decimal ComputeOpeningBalance(int membersCount, decimal pricePerPerson)
    {
        var fullCount = Math.Min(membersCount, 4);
        var halfCount = Math.Max(membersCount - 4, 0);
        return (fullCount + (halfCount * 0.5m)) * pricePerPerson;
    }
}
