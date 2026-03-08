using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using moa.Data;
using moa.Models;
using moa.Services;

namespace moa.Controllers;

[Authorize]
public class ReceiptsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentStoreProvider _currentStoreProvider;
    private readonly IMonthlyBalanceService _monthlyBalanceService;

    public ReceiptsController(AppDbContext db, ICurrentStoreProvider currentStoreProvider, IMonthlyBalanceService monthlyBalanceService)
    {
        _db = db;
        _currentStoreProvider = currentStoreProvider;
        _monthlyBalanceService = monthlyBalanceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ReceiptListFilter filter)
    {
        return RedirectToAction("Index", "Logs", new
        {
            Tab = UiTabs.Withdrawals,
            WithdrawalsFilter = filter
        });
    }

    [HttpGet]
    public async Task<IActionResult> CardLookup(string? q)
    {
        var storeId = _currentStoreProvider.GetStoreId();
        var query = (q ?? string.Empty).Trim();

        var cardsQuery = _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .Join(
                _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == storeId),
                card => card.CustomerId,
                a => a.CustomerId,
                (card, a) => card)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            cardsQuery = cardsQuery.Where(c => c.Customer.FullName.Contains(query) || c.CardNumber.Contains(query));
        }

        var results = await cardsQuery
            .OrderBy(c => c.Customer.FullName)
            .Select(c => new { c.CardId, c.CardNumber, CustomerName = c.Customer.FullName })
            .Take(50)
            .ToListAsync();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var ym = GetCurrentYearMonth();
        await _monthlyBalanceService.EnsureBalancesForMonthAsync(storeId, ym);

        var groups = await _db.CardGroups
            .AsNoTracking()
            .Where(g => g.StoreId == storeId)
            .OrderBy(g => g.GroupName)
            .Select(g => new { g.CardGroupId, g.GroupName })
            .ToListAsync();

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && p.IsActive)
            .OrderBy(p => p.ProductName)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.UnitName,
                UnitPrice = _db.StoreProductMonthlyPrices
                    .Where(x => x.StoreId == storeId && x.ProductId == p.ProductId && x.YearMonth == ym)
                    .Select(x => (decimal?)x.UnitPrice)
                    .FirstOrDefault(),
                QuantityOnHand = _db.StoreProductInventories
                    .Where(i => i.StoreId == storeId && i.ProductId == p.ProductId)
                    .Select(i => (decimal?)i.QuantityOnHand)
                    .FirstOrDefault() ?? 0m,
                LastRestockQuantity = _db.StoreProductInventories
                    .Where(i => i.StoreId == storeId && i.ProductId == p.ProductId)
                    .Select(i => (decimal?)i.LastRestockQuantity)
                    .FirstOrDefault() ?? 0m
            })
            .ToListAsync();

        // Load all assigned cards for instant search
        var allCards = await _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .Join(
                _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == storeId),
                card => card.CustomerId,
                a => a.CustomerId,
                (card, a) => card)
            .OrderBy(c => c.Customer.FullName)
            .Select(c => new 
            { 
                c.CardId, 
                c.CardNumber, 
                CustomerName = c.Customer.FullName, 
                c.CardGroupId,
                Phone = c.Customer.Phone,
                c.MembersCount
            })
            .ToListAsync();

        var cardIds = allCards.Select(c => c.CardId).ToList();
        var nowMonth = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        var swipeBalances = await _db.CardMonthlyBalances
            .AsNoTracking()
            .Where(b => b.YearMonth == nowMonth && cardIds.Contains(b.CardId))
            .ToDictionaryAsync(b => b.CardId, b => b.IsSwipedInMachine);

        var cardListWithSwipe = allCards.Select(c => {
            swipeBalances.TryGetValue(c.CardId, out var swiped);
            return new { 
                c.CardId, 
                c.CardNumber, 
                c.CustomerName, 
                c.CardGroupId, 
                c.Phone, 
                c.MembersCount, 
                IsSwipedInMachine = swiped 
            };
        }).ToList();

        var grouped = groups
            .Select(g => new
            {
                GroupId = (int?)g.CardGroupId,
                GroupName = g.GroupName,
                Cards = cardListWithSwipe.Where(c => c.CardGroupId == g.CardGroupId).ToList()
            })
            .ToList();

        grouped.Add(new
        {
            GroupId = (int?)null,
            GroupName = "بدون مجموعة",
            Cards = cardListWithSwipe.Where(c => c.CardGroupId == null).ToList()
        });

        ViewBag.Products = products;
        ViewBag.CardGroups = grouped;

        return View(new ReceiptCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReceiptCreateViewModel model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var ym = GetCurrentYearMonth();
        await _monthlyBalanceService.EnsureBalancesForMonthAsync(storeId, ym);

        var groups = await _db.CardGroups
            .AsNoTracking()
            .Where(g => g.StoreId == storeId)
            .OrderBy(g => g.GroupName)
            .Select(g => new { g.CardGroupId, g.GroupName })
            .ToListAsync();

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && p.IsActive)
            .OrderBy(p => p.ProductName)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.UnitName,
                UnitPrice = _db.StoreProductMonthlyPrices
                    .Where(x => x.StoreId == storeId && x.ProductId == p.ProductId && x.YearMonth == ym)
                    .Select(x => (decimal?)x.UnitPrice)
                    .FirstOrDefault(),
                QuantityOnHand = _db.StoreProductInventories
                    .Where(i => i.StoreId == storeId && i.ProductId == p.ProductId)
                    .Select(i => (decimal?)i.QuantityOnHand)
                    .FirstOrDefault() ?? 0m,
                LastRestockQuantity = _db.StoreProductInventories
                    .Where(i => i.StoreId == storeId && i.ProductId == p.ProductId)
                    .Select(i => (decimal?)i.LastRestockQuantity)
                    .FirstOrDefault() ?? 0m
            })
            .ToListAsync();

        var allCards = await _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .Join(
                _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == storeId),
                card => card.CustomerId,
                a => a.CustomerId,
                (card, a) => card)
            .OrderBy(c => c.Customer.FullName)
            .Select(c => new 
            { 
                c.CardId, 
                c.CardNumber, 
                CustomerName = c.Customer.FullName, 
                c.CardGroupId,
                Phone = c.Customer.Phone,
                c.MembersCount
            })
            .ToListAsync();

        var cardIds = allCards.Select(c => c.CardId).ToList();
        var swipeBalances = await _db.CardMonthlyBalances
            .AsNoTracking()
            .Where(b => b.YearMonth == ym && cardIds.Contains(b.CardId))
            .ToDictionaryAsync(b => b.CardId, b => b.IsSwipedInMachine);

        var cardListWithSwipe = allCards.Select(c => {
            swipeBalances.TryGetValue(c.CardId, out var swiped);
            return new { 
                c.CardId, 
                c.CardNumber, 
                c.CustomerName, 
                c.CardGroupId, 
                c.Phone, 
                c.MembersCount, 
                IsSwipedInMachine = swiped 
            };
        }).ToList();

        var grouped = groups
            .Select(g => new
            {
                GroupId = (int?)g.CardGroupId,
                GroupName = g.GroupName,
                Cards = cardListWithSwipe.Where(c => c.CardGroupId == g.CardGroupId).ToList()
            })
            .ToList();

        grouped.Add(new
        {
            GroupId = (int?)null,
            GroupName = "بدون مجموعة",
            Cards = cardListWithSwipe.Where(c => c.CardGroupId == null).ToList()
        });

        ViewBag.Products = products;
        ViewBag.CardGroups = grouped;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var items = (model.Items ?? new List<ReceiptItemInput>())
            .Where(i => i.ProductId.HasValue && i.Quantity.HasValue && i.Quantity.Value > 0)
            .Select(i => new { ProductId = i.ProductId!.Value, Quantity = i.Quantity!.Value })
            .ToList();

        if (items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "اختر منتج واحد على الأقل");
            return View(model);
        }

        var itemProductIds = items.Select(i => i.ProductId).Distinct().ToList();
        var inventories = await _db.StoreProductInventories
            .Where(x => x.StoreId == storeId && itemProductIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, x => x);

        foreach (var it in items)
        {
            if (!inventories.TryGetValue(it.ProductId, out var inv))
            {
                ModelState.AddModelError(string.Empty, "لا يوجد مخزون لهذا المنتج");
                return View(model);
            }

            if (inv.QuantityOnHand < it.Quantity)
            {
                ModelState.AddModelError(string.Empty, "الكمية غير كافية في المخزن");
                return View(model);
            }
        }

        if (!model.CardId.HasValue)
        {
            ModelState.AddModelError(nameof(ReceiptCreateViewModel.CardId), "اختر بطاقة");
            return View(model);
        }

        var card = await _db.RationCards
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.CardId == model.CardId.Value);

        if (card is null)
        {
            ModelState.AddModelError(nameof(ReceiptCreateViewModel.CardId), "البطاقة غير موجودة");
            return View(model);
        }

        var assigned = await _db.CustomerStoreAssignments.AnyAsync(a => a.CustomerId == card.CustomerId && a.StoreId == storeId && a.EndAt == null);
        if (!assigned)
        {
            return Forbid();
        }

        var storeSetting = await _db.StoreSettings.AsNoTracking().FirstOrDefaultAsync(s => s.StoreId == storeId);
        var threshold = storeSetting?.NearEmptyThresholdAmount ?? 0m;
        var allowEarly = storeSetting?.AllowEarlyOpenNextMonth ?? true;

        // Load monthly balances ordered by oldest first
        var balances = await _db.CardMonthlyBalances
            .Where(b => b.CardId == card.CardId)
            .OrderBy(b => b.YearMonth)
            .Select(b => new { b.CardMonthlyBalanceId, b.YearMonth, b.OpeningBalance, b.IsSwipedInMachine })
            .ToListAsync();

        if (balances.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "لا يوجد رصيد شهري لهذه البطاقة");
            return View(model);
        }

        // Validate swipe status for current month
        var currentYm = GetCurrentYearMonth();
        var currentMonthBalance = balances.FirstOrDefault(b => b.YearMonth == currentYm);
        if (currentMonthBalance == null)
        {
            ModelState.AddModelError(string.Empty, "لا يوجد رصيد مفتوح لهذا الشهر لهذه البطاقة");
            return View(model);
        }
        if (!currentMonthBalance.IsSwipedInMachine)
        {
            ModelState.AddModelError(string.Empty, "البطاقة لم يتم سحبها في الماكينة لهذا الشهر.");
            return View(model);
        }

        var balanceIds = balances.Select(b => b.CardMonthlyBalanceId).ToList();
        var spentByBalance = await _db.WithdrawalAllocations
            .Where(a => balanceIds.Contains(a.CardMonthlyBalanceId))
            .GroupBy(a => a.CardMonthlyBalanceId)
            .Select(g => new { CardMonthlyBalanceId = g.Key, Spent = g.Sum(x => x.AllocatedAmount) })
            .ToDictionaryAsync(x => x.CardMonthlyBalanceId, x => x.Spent);

        var available = balances
            .Select(b =>
            {
                spentByBalance.TryGetValue(b.CardMonthlyBalanceId, out var spent);
                var remaining = b.OpeningBalance - spent;
                return new { b.CardMonthlyBalanceId, b.YearMonth, Remaining = remaining };
            })
            .Where(x => x.Remaining > 0)
            .ToList();

        if (available.Count == 0)
        {
            var totalOpening = balances.Sum(b => b.OpeningBalance);
            var totalSpent = spentByBalance.Values.Sum();
            ModelState.AddModelError(string.Empty, $"لا يوجد رصيد متبقي لهذه البطاقة. إجمالي الافتتاحي: {totalOpening}, إجمالي المستهلك: {totalSpent}");
            return View(model);
        }


        if (allowEarly && available[0].Remaining <= threshold)
        {
            ModelState.AddModelError(string.Empty, "Balance is near empty. You may need to open next month from card details before withdrawing.");
            // Still allow user to proceed if they already have next month balance with remaining.
        }

        // Prepare per-month product prices lookup
        var yearMonths = available.Select(a => a.YearMonth).Distinct().ToList();
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();

        var prices = await _db.StoreProductMonthlyPrices
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && yearMonths.Contains(p.YearMonth) && productIds.Contains(p.ProductId))
            .Select(p => new { p.YearMonth, p.ProductId, p.UnitPrice })
            .ToListAsync();

        var priceMap = prices.ToDictionary(p => (p.YearMonth, p.ProductId), p => p.UnitPrice);

        // Ensure prices exist for required months/products (only for months we will use)
        // We'll validate on the fly when allocating.

        await using var tx = await _db.Database.BeginTransactionAsync();

        foreach (var it in items)
        {
            var inv = inventories[it.ProductId];
            inv.QuantityOnHand -= it.Quantity;
            inv.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        var receipt = new WithdrawalReceipt
        {
            CardId = card.CardId,
            StoreId = storeId,
            WithdrawnAt = DateTime.UtcNow,
            TotalAmount = 0m,
            CreatedAt = DateTime.UtcNow
        };
        _db.WithdrawalReceipts.Add(receipt);
        await _db.SaveChangesAsync();

        foreach (var it in items)
        {
            _db.WithdrawalReceiptItems.Add(new WithdrawalReceiptItem
            {
                ReceiptId = receipt.ReceiptId,
                ProductId = it.ProductId,
                Quantity = it.Quantity,
                UnitPrice = null
            });
        }
        await _db.SaveChangesAsync();

        // Allocation algorithm: consume oldest month first (FIFO).
        // Split quantities across months if needed; price is based on (StoreId, YearMonth, ProductId).

        var remainingByMonth = available.ToDictionary(a => a.YearMonth, a => a.Remaining);
        var balanceIdByYearMonth = available.ToDictionary(a => a.YearMonth, a => a.CardMonthlyBalanceId);

        var allocationsByYearMonth = new Dictionary<int, WithdrawalAllocation>();
        var totalAmount = 0m;

        foreach (var it in items)
        {
            var qtyLeft = it.Quantity;

            foreach (var month in available)
            {
                if (qtyLeft <= 0) break;

                var monthRemainingMoney = remainingByMonth[month.YearMonth];
                if (monthRemainingMoney <= 0) continue;

                if (!priceMap.TryGetValue((month.YearMonth, it.ProductId), out var unitPrice))
                {
                    await tx.RollbackAsync();
                    ModelState.AddModelError(string.Empty, $"Missing price for product {it.ProductId} in {month.YearMonth}");
                    return View(model);
                }

                if (unitPrice <= 0)
                {
                    await tx.RollbackAsync();
                    ModelState.AddModelError(string.Empty, $"Invalid price for product {it.ProductId} in {month.YearMonth}");
                    return View(model);
                }

                // How much quantity can this month afford?
                var maxQty = Math.Floor((monthRemainingMoney / unitPrice) * 1000m) / 1000m; // keep 3 decimals
                if (maxQty <= 0) continue;

                var qtyTake = qtyLeft <= maxQty ? qtyLeft : maxQty;
                if (qtyTake <= 0) continue;

                var lineTotal = Math.Round(qtyTake * unitPrice, 2);

                if (!allocationsByYearMonth.TryGetValue(month.YearMonth, out var alloc))
                {
                    alloc = new WithdrawalAllocation
                    {
                        ReceiptId = receipt.ReceiptId,
                        CardMonthlyBalanceId = balanceIdByYearMonth[month.YearMonth],
                        AllocatedAmount = 0m
                    };
                    _db.WithdrawalAllocations.Add(alloc);
                    allocationsByYearMonth.Add(month.YearMonth, alloc);
                    await _db.SaveChangesAsync();
                }

                _db.WithdrawalAllocationItems.Add(new WithdrawalAllocationItem
                {
                    AllocationId = alloc.AllocationId,
                    ProductId = it.ProductId,
                    Quantity = qtyTake,
                    UnitPrice = unitPrice
                });

                alloc.AllocatedAmount += lineTotal;

                remainingByMonth[month.YearMonth] -= lineTotal;
                qtyLeft -= qtyTake;
                totalAmount += lineTotal;
            }

            if (qtyLeft > 0)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Insufficient balance to cover requested quantities");
                return View(model);
            }
        }

        receipt.TotalAmount = totalAmount;
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
    }

    [HttpGet]
    public async Task<IActionResult> PrintMonthly(int? yearMonth)
    {
        var storeId = _currentStoreProvider.GetStoreId();
        var ym = yearMonth ?? GetCurrentYearMonth();

        var storeName = await _db.Stores
            .AsNoTracking()
            .Where(s => s.StoreId == storeId)
            .Select(s => s.StoreName)
            .FirstOrDefaultAsync();

        var from = new DateTime(ym / 100, ym % 100, 1, 0, 0, 0, DateTimeKind.Utc);
        var toExclusive = from.AddMonths(1);

        var cards = await _db.RationCards
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.CardGroup)
            .Join(
                _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == storeId),
                card => card.CustomerId,
                a => a.CustomerId,
                (card, a) => card)
            .Select(c => new
            {
                c.CardId,
                c.CardNumber,
                CustomerName = c.Customer.FullName,
                Phone = c.Customer.Phone,
                GroupName = c.CardGroup != null ? c.CardGroup.GroupName : "بدون مجموعة"
            })
            .ToListAsync();

        var receipts = await _db.WithdrawalReceipts
            .AsNoTracking()
            .Where(r => r.StoreId == storeId && r.WithdrawnAt >= from && r.WithdrawnAt < toExclusive)
            .Select(r => new { r.ReceiptId, r.CardId, r.WithdrawnAt })
            .ToListAsync();

        var receiptIds = receipts.Select(r => r.ReceiptId).ToList();
        var lines = receiptIds.Count == 0
            ? new List<dynamic>()
            : await (
                from ai in _db.WithdrawalAllocationItems.AsNoTracking()
                join a in _db.WithdrawalAllocations.AsNoTracking() on ai.AllocationId equals a.AllocationId
                join p in _db.Products.AsNoTracking() on ai.ProductId equals p.ProductId
                where receiptIds.Contains(a.ReceiptId)
                select new
                {
                    ReceiptId = a.ReceiptId,
                    ProductName = p.ProductName,
                    ai.Quantity,
                    UnitPrice = (decimal?)ai.UnitPrice,
                    ai.LineTotal
                }).ToListAsync<dynamic>();

        return View(new moa.Models.PrintMonthlyViewModel
        {
            StoreName = storeName ?? string.Empty,
            YearMonth = ym,
            Cards = cards.Select(c => new moa.Models.PrintMonthlyCardVm
            {
                CardId = c.CardId,
                CardNumber = c.CardNumber,
                CustomerName = c.CustomerName,
                Phone = c.Phone,
                GroupName = c.GroupName
            }).ToList(),
            Receipts = receipts.Select(r => new moa.Models.PrintMonthlyReceiptVm
            {
                ReceiptId = r.ReceiptId,
                CardId = r.CardId,
                WithdrawnAt = r.WithdrawnAt
            }).ToList(),
            Lines = lines.Select(l => new moa.Models.PrintMonthlyLineVm
            {
                ReceiptId = (int)l.ReceiptId,
                ProductName = (string)l.ProductName,
                Quantity = (decimal)l.Quantity,
                UnitPrice = (decimal?)(l.UnitPrice),
                LineTotal = (decimal)l.LineTotal
            }).ToList()
        });
    }

    private static int GetCurrentYearMonth()
    {
        var now = DateTime.UtcNow;
        return now.Year * 100 + now.Month;
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var receipt = await _db.WithdrawalReceipts
            .AsNoTracking()
            .Include(r => r.Card)
            .ThenInclude(c => c.Customer)
            .FirstOrDefaultAsync(r => r.ReceiptId == id && r.StoreId == storeId);

        if (receipt is null)
        {
            return NotFound();
        }

        var allocations = await _db.WithdrawalAllocations
            .AsNoTracking()
            .Where(a => a.ReceiptId == receipt.ReceiptId)
            .Join(
                _db.CardMonthlyBalances.AsNoTracking(),
                a => a.CardMonthlyBalanceId,
                b => b.CardMonthlyBalanceId,
                (a, b) => new ReceiptAllocationLineVm
                {
                    YearMonth = b.YearMonth,
                    AllocatedAmount = a.AllocatedAmount
                })
            .OrderBy(x => x.YearMonth)
            .ToListAsync();

        return View(new ReceiptDetailsViewModel
        {
            ReceiptId = receipt.ReceiptId,
            CardNumber = receipt.Card.CardNumber,
            CustomerName = receipt.Card.Customer.FullName,
            WithdrawnAt = receipt.WithdrawnAt,
            TotalAmount = receipt.TotalAmount,
            Allocations = allocations
        });
    }
}
