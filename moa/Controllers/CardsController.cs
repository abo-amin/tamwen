using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using moa.Data;
using moa.Models;
using moa.Services;

namespace moa.Controllers;

[Authorize]
public class CardsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPinHasher _pinHasher;
    private readonly ICurrentStoreProvider _currentStoreProvider;

    public CardsController(AppDbContext db, IPinHasher pinHasher, ICurrentStoreProvider currentStoreProvider)
    {
        _db = db;
        _pinHasher = pinHasher;
        _currentStoreProvider = currentStoreProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var storeId = _currentStoreProvider.GetStoreId();
        var groups = await _db.CardGroups
            .AsNoTracking()
            .Where(g => g.StoreId == storeId)
            .OrderBy(g => g.GroupName)
            .Select(g => new SelectListItem { Value = g.CardGroupId.ToString(), Text = g.GroupName })
            .ToListAsync();

        return View(new CardCreateViewModel
        {
            MembersCount = 1,
            PricePerPerson = 50m,
            CardGroups = groups
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CardCreateViewModel model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        model.CardNumber = string.IsNullOrWhiteSpace(model.CardNumber) ? null : model.CardNumber.Trim();
        model.Pin = string.IsNullOrWhiteSpace(model.Pin) ? null : model.Pin.Trim();

        if (model.Pin is not null && !System.Text.RegularExpressions.Regex.IsMatch(model.Pin, "^\\d{4}$"))
        {
            ModelState.AddModelError(nameof(CardCreateViewModel.Pin), "الرقم السري يجب أن يكون 4 أرقام");
        }

        if (model.CardGroupId is null && string.IsNullOrWhiteSpace(model.Phone))
        {
            ModelState.AddModelError(nameof(CardCreateViewModel.Phone), "رقم التليفون مطلوب للبطاقة بدون مجموعة");
        }

        if (!ModelState.IsValid)
        {
            model.CardGroups = await _db.CardGroups
                .AsNoTracking()
                .Where(g => g.StoreId == storeId)
                .OrderBy(g => g.GroupName)
                .Select(g => new SelectListItem { Value = g.CardGroupId.ToString(), Text = g.GroupName })
                .ToListAsync();
            return View(model);
        }

        var cardNumber = model.CardNumber ?? await GenerateUniqueCardNumber();
        var pin = model.Pin ?? GeneratePin();

        var cardExists = await _db.RationCards.AnyAsync(c => c.CardNumber == cardNumber);
        if (cardExists)
        {
            ModelState.AddModelError(nameof(CardCreateViewModel.CardNumber), "رقم البطاقة موجود بالفعل");
            model.CardGroups = await _db.CardGroups
                .AsNoTracking()
                .Where(g => g.StoreId == storeId)
                .OrderBy(g => g.GroupName)
                .Select(g => new SelectListItem { Value = g.CardGroupId.ToString(), Text = g.GroupName })
                .ToListAsync();
            return View(model);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        int? validatedGroupId = null;
        if (model.CardGroupId is not null)
        {
            var groupOk = await _db.CardGroups.AnyAsync(g => g.StoreId == storeId && g.CardGroupId == model.CardGroupId);
            if (!groupOk)
            {
                ModelState.AddModelError(nameof(CardCreateViewModel.CardGroupId), "المجموعة غير صحيحة");
                model.CardGroups = await _db.CardGroups
                    .AsNoTracking()
                    .Where(g => g.StoreId == storeId)
                    .OrderBy(g => g.GroupName)
                    .Select(g => new SelectListItem { Value = g.CardGroupId.ToString(), Text = g.GroupName })
                    .ToListAsync();
                return View(model);
            }

            validatedGroupId = model.CardGroupId;
        }

        var customer = new Customer
        {
            FullName = model.CustomerName,
            Phone = validatedGroupId is null ? model.Phone : null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        var (hash, salt) = _pinHasher.HashPin(pin);

        var card = new RationCard
        {
            CustomerId = customer.CustomerId,
            CardNumber = cardNumber,
            MembersCount = model.MembersCount,
            PricePerPerson = model.PricePerPerson,
            CardGroupId = validatedGroupId,
            PinHash = hash,
            PinSalt = salt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.RationCards.Add(card);
        await _db.SaveChangesAsync();

        // Auto-open monthly balance based on members count and price per person
        var now = DateTime.UtcNow;
        var yearMonth = now.Year * 100 + now.Month;
        var openAt = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var openingBalance = ComputeOpeningBalance(model.MembersCount, model.PricePerPerson);
        _db.CardMonthlyBalances.Add(new CardMonthlyBalance
        {
            CardId = card.CardId,
            YearMonth = yearMonth,
            MembersCountSnapshot = model.MembersCount,
            OpeningBalance = openingBalance,
            OpenAt = openAt,
            CloseAt = null,
            Notes = null,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Assign customer to current store (single-store now; multi-store later)

        var hasCurrentAssignment = await _db.CustomerStoreAssignments.AnyAsync(a => a.CustomerId == customer.CustomerId && a.EndAt == null);
        if (!hasCurrentAssignment)
        {
            _db.CustomerStoreAssignments.Add(new CustomerStoreAssignment
            {
                CustomerId = customer.CustomerId,
                StoreId = storeId,
                StartAt = DateTime.UtcNow,
                EndAt = null,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        await tx.CommitAsync();

        return RedirectToAction(nameof(Details), new { id = card.CardId });
    }

    private async Task<string> GenerateUniqueCardNumber()
    {
        for (var i = 0; i < 20; i++)
        {
            var candidate = GenerateCardNumber();
            var exists = await _db.RationCards.AnyAsync(c => c.CardNumber == candidate);
            if (!exists) return candidate;
        }

        return $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
    }

    private static string GenerateCardNumber()
        => Random.Shared.NextInt64(100000000000, 999999999999).ToString();

    private static string GeneratePin()
        => Random.Shared.Next(0, 10000).ToString("D4");

    private static string NormalizeName(string? name)
        => (name ?? string.Empty).Trim();

    [HttpGet]
    public async Task<IActionResult> BatchCreate(int? storeId, int? groupId)
    {
        var currentStoreId = _currentStoreProvider.GetStoreId();
        var effectiveStoreId = storeId ?? currentStoreId;

        var ownerId = await _db.Stores
            .AsNoTracking()
            .Where(s => s.StoreId == currentStoreId)
            .Select(s => s.OwnerId)
            .FirstOrDefaultAsync();

        var storesList = await _db.Stores
            .AsNoTracking()
            .Where(s => s.OwnerId == ownerId && s.IsActive)
            .OrderBy(s => s.StoreName)
            .Select(s => new SelectListItem { Value = s.StoreId.ToString(), Text = s.StoreName })
            .ToListAsync();

        var storeInfo = await _db.Stores
            .AsNoTracking()
            .Where(s => s.StoreId == effectiveStoreId)
            .Select(s => new { s.StoreName, s.OwnerId })
            .FirstOrDefaultAsync();

        if (storeInfo is null || storeInfo.OwnerId != ownerId)
        {
            return NotFound();
        }

        var groupsList = await _db.CardGroups
            .AsNoTracking()
            .Where(g => g.StoreId == effectiveStoreId)
            .OrderBy(g => g.GroupName)
            .Select(g => new SelectListItem { Value = g.CardGroupId.ToString(), Text = g.GroupName })
            .ToListAsync();

        var query = _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .Join(
                _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == effectiveStoreId),
                card => card.CustomerId,
                a => a.CustomerId,
                (card, a) => card);

        if (groupId.HasValue)
        {
            query = query.Where(c => c.CardGroupId == groupId.Value);
        }

        var existingCards = await query
            .OrderBy(c => c.Customer.FullName)
            .Select(c => new CardBatchExistingItemVm
            {
                CustomerName = c.Customer.FullName,
                MembersCount = c.MembersCount
            })
            .ToListAsync();

        return View(new CardBatchCreateViewModel
        {
            StoreId = effectiveStoreId,
            StoreName = storeInfo.StoreName,
            Stores = storesList,
            CardGroupId = groupId,
            CardGroups = groupsList,
            ExistingCards = existingCards,
            Rows = Enumerable.Range(0, 20).Select(_ => new CardBatchCreateRowVm()).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchCreate(CardBatchCreateViewModel model)
    {
        var currentStoreId = _currentStoreProvider.GetStoreId();

        var ownerId = await _db.Stores
            .AsNoTracking()
            .Where(s => s.StoreId == currentStoreId)
            .Select(s => s.OwnerId)
            .FirstOrDefaultAsync();

        var storeInfo = await _db.Stores
            .AsNoTracking()
            .Where(s => s.StoreId == model.StoreId)
            .Select(s => new { s.StoreName, s.OwnerId })
            .FirstOrDefaultAsync();

        if (storeInfo is null || storeInfo.OwnerId != ownerId)
        {
            return NotFound();
        }

        model.StoreName = storeInfo.StoreName;
        model.Rows ??= new List<CardBatchCreateRowVm>();

        if (model.Rows.Count < 20)
        {
            model.Rows.AddRange(Enumerable.Range(0, 20 - model.Rows.Count).Select(_ => new CardBatchCreateRowVm()));
        }
        else if (model.Rows.Count > 20)
        {
            model.Rows = model.Rows.Take(20).ToList();
        }

        var existingPairsQuery = _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .Join(
                _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == model.StoreId),
                card => card.CustomerId,
                a => a.CustomerId,
                (card, a) => card);

        if (model.CardGroupId.HasValue)
        {
            existingPairsQuery = existingPairsQuery.Where(c => c.CardGroupId == model.CardGroupId.Value);
        }

        var existingPairs = await existingPairsQuery
            .Select(c => new { c.Customer.FullName, c.MembersCount })
            .ToListAsync();

        var existingPairsSet = existingPairs
            .Select(x => (Name: NormalizeName(x.FullName), x.MembersCount))
            .Where(x => x.Name != "")
            .ToHashSet();

        var batchPairsSet = new HashSet<(string Name, int MembersCount)>();

        for (var i = 0; i < model.Rows.Count; i++)
        {
            var row = model.Rows[i];

            row.CustomerName = string.IsNullOrWhiteSpace(row.CustomerName) ? null : row.CustomerName.Trim();

            if (string.IsNullOrWhiteSpace(row.CustomerName))
            {
                continue;
            }

            if (row.MembersCount < 1 || row.MembersCount > 50)
            {
                ModelState.AddModelError($"Rows[{i}].MembersCount", "عدد الأفراد يجب أن يكون بين 1 و 50");
            }

            var normalized = NormalizeName(row.CustomerName);
            var pair = (normalized, row.MembersCount);

            // Check against existing database cards
            if (existingPairsSet.Contains(pair) && !row.IsConfirmedDuplicate)
            {
                ModelState.AddModelError($"Rows[{i}].CustomerName", "هذا الاسم موجود بالفعل. هل تريد إضافته مرة أخرى؟ (قم بالتأكيد من المربع الجانبي)");
            }

            // Check against other rows in the same batch
            if (batchPairsSet.Contains(pair) && !row.IsConfirmedDuplicate)
            {
                ModelState.AddModelError($"Rows[{i}].CustomerName", "هذا الاسم مكرر داخل الدفعة. هل تريد إضافته مرة أخرى؟ (قم بالتأكيد من المربع الجانبي)");
            }

            batchPairsSet.Add(pair);
        }

        var toCreate = model.Rows
            .Select((r, idx) => new { Row = r, Index = idx })
            .Where(x => !string.IsNullOrWhiteSpace(x.Row.CustomerName))
            .ToList();

        if (toCreate.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "من فضلك أدخل على الأقل بطاقة واحدة");
        }

        if (!ModelState.IsValid)
        {
            model.Stores = await _db.Stores
                .AsNoTracking()
                .Where(s => s.OwnerId == ownerId && s.IsActive)
                .OrderBy(s => s.StoreName)
                .Select(s => new SelectListItem { Value = s.StoreId.ToString(), Text = s.StoreName })
                .ToListAsync();

            model.CardGroups = await _db.CardGroups
                .AsNoTracking()
                .Where(g => g.StoreId == model.StoreId)
                .OrderBy(g => g.GroupName)
                .Select(g => new SelectListItem { Value = g.CardGroupId.ToString(), Text = g.GroupName })
                .ToListAsync();

            var existingCardsQuery = _db.RationCards
                .AsNoTracking()
                .Include(c => c.Customer)
                .Join(
                    _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == model.StoreId),
                    card => card.CustomerId,
                    a => a.CustomerId,
                    (card, a) => card);

            if (model.CardGroupId.HasValue)
            {
                existingCardsQuery = existingCardsQuery.Where(c => c.CardGroupId == model.CardGroupId.Value);
            }

            model.ExistingCards = await existingCardsQuery
                .OrderBy(c => c.Customer.FullName)
                .Select(c => new CardBatchExistingItemVm
                {
                    CustomerName = c.Customer.FullName,
                    MembersCount = c.MembersCount
                })
                .ToListAsync();

            return View(model);
        }

        var createdCount = 0;

        await using var tx = await _db.Database.BeginTransactionAsync();

        foreach (var x in toCreate)
        {
            var name = x.Row.CustomerName!.Trim();
            var normalized = NormalizeName(name);
            var pair = (normalized, x.Row.MembersCount);

            // We only skip if it's a duplicate AND not confirmed.
            // If it's a duplicate but confirmed, we proceed.
            if (existingPairsSet.Contains(pair) && !x.Row.IsConfirmedDuplicate)
            {
                continue;
            }

            var customer = new Customer
            {
                FullName = name,
                Phone = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            var cardNumber = await GenerateUniqueCardNumber();
            var pin = GeneratePin();
            var (hash, salt) = _pinHasher.HashPin(pin);

            var card = new RationCard
            {
                CustomerId = customer.CustomerId,
                CardNumber = cardNumber,
                MembersCount = x.Row.MembersCount,
                PricePerPerson = 50m,
                CardGroupId = model.CardGroupId,
                PinHash = hash,
                PinSalt = salt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.RationCards.Add(card);
            await _db.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var yearMonth = now.Year * 100 + now.Month;
            var openAt = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var openingBalance = ComputeOpeningBalance(card.MembersCount, card.PricePerPerson);
            _db.CardMonthlyBalances.Add(new CardMonthlyBalance
            {
                CardId = card.CardId,
                YearMonth = yearMonth,
                MembersCountSnapshot = card.MembersCount,
                OpeningBalance = openingBalance,
                OpenAt = openAt,
                CloseAt = null,
                Notes = null,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var hasCurrentAssignment = await _db.CustomerStoreAssignments.AnyAsync(a => a.CustomerId == customer.CustomerId && a.StoreId == model.StoreId && a.EndAt == null);
            if (!hasCurrentAssignment)
            {
                _db.CustomerStoreAssignments.Add(new CustomerStoreAssignment
                {
                    CustomerId = customer.CustomerId,
                    StoreId = model.StoreId,
                    StartAt = DateTime.UtcNow,
                    EndAt = null,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            existingPairsSet.Add(pair);
            createdCount++;
        }

        await tx.CommitAsync();

        TempData["Success"] = $"تم إضافة {createdCount} بطاقة بنجاح";
        return RedirectToAction(nameof(BatchCreate), new { storeId = model.StoreId, groupId = model.CardGroupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CleanupGroupDuplicates(int storeId, int groupId)
    {
        var currentStoreId = _currentStoreProvider.GetStoreId();
        var ownerId = await _db.Stores
            .AsNoTracking()
            .Where(s => s.StoreId == currentStoreId)
            .Select(s => s.OwnerId)
            .FirstOrDefaultAsync();

        var storeInfo = await _db.Stores
            .AsNoTracking()
            .Where(s => s.StoreId == storeId)
            .Select(s => new { s.StoreId, s.OwnerId })
            .FirstOrDefaultAsync();

        if (storeInfo is null || storeInfo.OwnerId != ownerId)
        {
            return Forbid();
        }

        var groupExists = await _db.CardGroups.AnyAsync(g => g.CardGroupId == groupId && g.StoreId == storeId);
        if (!groupExists)
        {
            return NotFound();
        }

        // Get all cards in this group
        var cards = await _db.RationCards
            .Where(c => c.CardGroupId == groupId)
            .Select(c => new
            {
                c.CardId,
                c.CustomerId,
                c.MembersCount,
                c.Customer.FullName
            })
            .ToListAsync();

        // Group by Normalized Name + MembersCount
        var duplicateGroups = cards
            .GroupBy(c => new { Name = (c.FullName ?? "").Trim(), c.MembersCount })
            .Where(g => g.Count() > 1)
            .ToList();

        var removedCount = 0;
        var skippedCount = 0;

        foreach (var cardGroup in duplicateGroups)
        {
            // Keep the first one (oldest)
            var toProcess = cardGroup.OrderBy(c => c.CardId).Skip(1).ToList();

            foreach (var cardInfo in toProcess)
            {
                // Check if card has ANY withdrawal activity
                var hasActivity = await _db.WithdrawalReceipts.AnyAsync(r => r.CardId == cardInfo.CardId);
                if (hasActivity)
                {
                    skippedCount++;
                    continue;
                }

                // Safe to delete
                await using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    // 1. Delete Monthly Balances
                    var balances = await _db.CardMonthlyBalances.Where(b => b.CardId == cardInfo.CardId).ToListAsync();
                    _db.CardMonthlyBalances.RemoveRange(balances);

                    // 2. Delete Store Assignments for this customer
                    var assignments = await _db.CustomerStoreAssignments.Where(a => a.CustomerId == cardInfo.CustomerId).ToListAsync();
                    _db.CustomerStoreAssignments.RemoveRange(assignments);

                    // 3. Delete the Card
                    var card = await _db.RationCards.FindAsync(cardInfo.CardId);
                    if (card != null) _db.RationCards.Remove(card);

                    // 4. Delete the Customer
                    var customer = await _db.Customers.FindAsync(cardInfo.CustomerId);
                    if (customer != null) _db.Customers.Remove(customer);

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                    removedCount++;
                }
                catch
                {
                    await tx.RollbackAsync();
                    skippedCount++;
                }
            }
        }

        if (removedCount > 0)
        {
            TempData["Success"] = $"تم تنظيف {removedCount} بطاقة مكررة بنجاح." + (skippedCount > 0 ? $" (تم تخطي {skippedCount} بطاقة لوجود عمليات صرف عليها)" : "");
        }
        else if (skippedCount > 0)
        {
            TempData["Info"] = $"لم يتم حذف أي بطاقات. هناك {skippedCount} بطاقة مكررة مرتبطة بعمليات صرف حالية ولا يمكن حذفها.";
        }
        else
        {
            TempData["Info"] = "لا توجد أسماء مكررة في هذه المجموعة حالياً.";
        }

        return RedirectToAction(nameof(BatchCreate), new { storeId, groupId });
    }

    [HttpGet]
    public IActionResult Search()
    {
        return View(new CardSearchViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> SearchResults(string? name)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var query = _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .Join(
                _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == storeId),
                card => card.CustomerId,
                a => a.CustomerId,
                (card, a) => card);

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(c => c.Customer.FullName.Contains(name));
        }

        var results = await query
            .OrderBy(c => c.Customer.FullName)
            .Select(c => new CardSearchResultItem
            {
                CardId = c.CardId,
                CustomerName = c.Customer.FullName,
                CardNumber = c.CardNumber,
                MembersCount = c.MembersCount
            })
            .Take(50)
            .ToListAsync();

        return View("Search", new CardSearchViewModel
        {
            Name = name,
            Results = results
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var card = await _db.RationCards
            .Include(c => c.Customer)
            .Include(c => c.CardGroup)
            .FirstOrDefaultAsync(c => c.CardId == id);

        if (card is null)
        {
            return NotFound();
        }

        var assignmentOk = await _db.CustomerStoreAssignments.AnyAsync(a => a.CustomerId == card.CustomerId && a.StoreId == storeId && a.EndAt == null);
        if (!assignmentOk)
        {
            return Forbid();
        }

        var setting = await _db.StoreSettings.AsNoTracking().FirstOrDefaultAsync(s => s.StoreId == storeId);
        var threshold = setting?.NearEmptyThresholdAmount ?? 0m;
        var allowEarly = setting?.AllowEarlyOpenNextMonth ?? true;

        var balances = await _db.CardMonthlyBalances
            .AsNoTracking()
            .Where(b => b.CardId == card.CardId)
            .OrderBy(b => b.YearMonth)
            .Select(b => new
            {
                b.CardMonthlyBalanceId,
                b.YearMonth,
                b.OpeningBalance
            })
            .ToListAsync();

        var balanceIds = balances.Select(b => b.CardMonthlyBalanceId).ToList();

        var spentByBalance = await _db.WithdrawalAllocations
            .AsNoTracking()
            .Where(a => balanceIds.Contains(a.CardMonthlyBalanceId))
            .GroupBy(a => a.CardMonthlyBalanceId)
            .Select(g => new { CardMonthlyBalanceId = g.Key, Spent = g.Sum(x => x.AllocatedAmount) })
            .ToDictionaryAsync(x => x.CardMonthlyBalanceId, x => x.Spent);

        var rows = balances.Select(b =>
        {
            spentByBalance.TryGetValue(b.CardMonthlyBalanceId, out var spent);
            var remaining = b.OpeningBalance - spent;
            return new CardBalanceRow
            {
                YearMonth = b.YearMonth,
                OpeningBalance = b.OpeningBalance,
                Spent = spent,
                Remaining = remaining
            };
        }).ToList();

        var oldestWithRemaining = rows.FirstOrDefault(r => r.Remaining > 0);
        var nearEmpty = oldestWithRemaining is not null && oldestWithRemaining.Remaining <= threshold;

        var nextYearMonth = GetNextYearMonth(oldestWithRemaining?.YearMonth);

        return View(new CardDetailsViewModel
        {
            CardId = card.CardId,
            CustomerName = card.Customer.FullName,
            CardNumber = card.CardNumber,
            MembersCount = card.MembersCount,
            Phone = card.Customer.Phone,
            CardGroupId = card.CardGroupId,
            NearEmptyThresholdAmount = threshold,
            AllowEarlyOpenNextMonth = allowEarly,
            Balances = rows,
            CanOpenNextMonth = allowEarly && nearEmpty,
            NextYearMonth = nextYearMonth,
            NextOpeningBalance = ComputeOpeningBalance(card.MembersCount, card.PricePerPerson)
        });
    }

    [HttpGet]
    public async Task<IActionResult> SetPhone(int id)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var card = await _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.CardId == id);

        if (card is null)
        {
            return NotFound();
        }

        var assignmentOk = await _db.CustomerStoreAssignments.AnyAsync(a => a.CustomerId == card.CustomerId && a.StoreId == storeId && a.EndAt == null);
        if (!assignmentOk)
        {
            return Forbid();
        }

        if (card.CardGroupId is not null)
        {
            return BadRequest("Card already belongs to a group");
        }

        return View(new CardSetPhoneViewModel
        {
            CardId = card.CardId,
            CustomerName = card.Customer.FullName,
            CardNumber = card.CardNumber,
            Phone = card.Customer.Phone ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPhone(CardSetPhoneViewModel model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

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

        if (card.CardGroupId is not null)
        {
            return BadRequest("Card already belongs to a group");
        }

        if (!ModelState.IsValid)
        {
            model.CustomerName = card.Customer.FullName;
            model.CardNumber = card.CardNumber;
            return View(model);
        }

        card.Customer.Phone = model.Phone;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = card.CardId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenNextMonth(CardDetailsViewModel model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

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

        var setting = await _db.StoreSettings.AsNoTracking().FirstOrDefaultAsync(s => s.StoreId == storeId);
        if (setting is null)
        {
            return BadRequest("Store settings not found");
        }

        var balances = await _db.CardMonthlyBalances
            .AsNoTracking()
            .Where(b => b.CardId == card.CardId)
            .OrderBy(b => b.YearMonth)
            .Select(b => new { b.CardMonthlyBalanceId, b.YearMonth, b.OpeningBalance })
            .ToListAsync();

        var balanceIds = balances.Select(b => b.CardMonthlyBalanceId).ToList();
        var spentByBalance = await _db.WithdrawalAllocations
            .AsNoTracking()
            .Where(a => balanceIds.Contains(a.CardMonthlyBalanceId))
            .GroupBy(a => a.CardMonthlyBalanceId)
            .Select(g => new { CardMonthlyBalanceId = g.Key, Spent = g.Sum(x => x.AllocatedAmount) })
            .ToDictionaryAsync(x => x.CardMonthlyBalanceId, x => x.Spent);

        var rows = balances.Select(b =>
        {
            spentByBalance.TryGetValue(b.CardMonthlyBalanceId, out var spent);
            return new CardBalanceRow
            {
                YearMonth = b.YearMonth,
                OpeningBalance = b.OpeningBalance,
                Spent = spent,
                Remaining = b.OpeningBalance - spent
            };
        }).ToList();

        var oldestWithRemaining = rows.FirstOrDefault(r => r.Remaining > 0);
        if (oldestWithRemaining is null)
        {
            oldestWithRemaining = rows.LastOrDefault();
        }

        var canOpen = setting.AllowEarlyOpenNextMonth && oldestWithRemaining is not null && oldestWithRemaining.Remaining <= setting.NearEmptyThresholdAmount;
        if (!canOpen)
        {
            return BadRequest("Not allowed to open next month yet");
        }

        var exists = await _db.CardMonthlyBalances.AnyAsync(b => b.CardId == card.CardId && b.YearMonth == model.NextYearMonth);
        if (exists)
        {
            return RedirectToAction(nameof(Details), new { id = card.CardId });
        }

        var computedNextOpening = ComputeOpeningBalance(card.MembersCount, card.PricePerPerson);

        _db.CardMonthlyBalances.Add(new CardMonthlyBalance
        {
            CardId = card.CardId,
            YearMonth = model.NextYearMonth,
            MembersCountSnapshot = card.MembersCount,
            OpeningBalance = computedNextOpening,
            OpenAt = DateTime.UtcNow,
            CloseAt = null,
            Notes = null,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = card.CardId });
    }

    private static int GetNextYearMonth(int? yearMonth)
    {
        if (yearMonth is null) return GetCurrentYearMonth();

        var y = yearMonth.Value / 100;
        var m = yearMonth.Value % 100;
        if (m >= 12)
        {
            return (y + 1) * 100 + 1;
        }

        return y * 100 + (m + 1);
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
