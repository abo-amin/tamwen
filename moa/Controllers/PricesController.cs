using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moa.Data;
using moa.Models;
using moa.Services;

namespace moa.Controllers;

[Authorize]
public class PricesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentStoreProvider _currentStoreProvider;

    public PricesController(AppDbContext db, ICurrentStoreProvider currentStoreProvider)
    {
        _db = db;
        _currentStoreProvider = currentStoreProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? yearMonth, string? type)
    {
        var storeId = _currentStoreProvider.GetStoreId();
        var ym = yearMonth ?? GetCurrentYearMonth();
        var priceType = string.IsNullOrWhiteSpace(type) ? UiTabs.WithdrawalPrice : type;

        if (priceType != UiTabs.WithdrawalPrice && priceType != UiTabs.FreeSalePrice)
        {
            priceType = UiTabs.WithdrawalPrice;
        }

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && p.IsActive)
            .OrderBy(p => p.ProductName)
            .Select(p => new { p.ProductId, p.ProductName, p.UnitName })
            .ToListAsync();

        var productIds = products.Select(p => p.ProductId).ToList();

        Dictionary<int, decimal> priceMap;
        if (priceType == UiTabs.FreeSalePrice)
        {
            priceMap = await _db.StoreProductMonthlyFreeSalePrices
                .AsNoTracking()
                .Where(x => x.StoreId == storeId && x.YearMonth == ym && productIds.Contains(x.ProductId))
                .ToDictionaryAsync(x => x.ProductId, x => x.UnitPrice);
        }
        else
        {
            priceMap = await _db.StoreProductMonthlyPrices
                .AsNoTracking()
                .Where(x => x.StoreId == storeId && x.YearMonth == ym && productIds.Contains(x.ProductId))
                .ToDictionaryAsync(x => x.ProductId, x => x.UnitPrice);
        }

        var vm = new UnifiedMonthlyPricesViewModel
        {
            YearMonth = ym,
            PriceType = priceType,
            Rows = products.Select(p => new UnifiedMonthlyPriceRowVm
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                UnitName = p.UnitName,
                UnitPrice = priceMap.TryGetValue(p.ProductId, out var price) ? price : null
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UnifiedMonthlyPricesViewModel model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        if (model.PriceType != UiTabs.WithdrawalPrice && model.PriceType != UiTabs.FreeSalePrice)
        {
            model.PriceType = UiTabs.WithdrawalPrice;
        }

        if (!ModelState.IsValid)
        {
            var prod = await _db.Products.AsNoTracking().Where(p => p.StoreId == storeId && p.IsActive)
                .OrderBy(p => p.ProductName)
                .Select(p => new { p.ProductId, p.ProductName, p.UnitName })
                .ToListAsync();

            var prodMap = prod.ToDictionary(p => p.ProductId, p => (p.ProductName, p.UnitName));
            foreach (var r in model.Rows)
            {
                if (prodMap.TryGetValue(r.ProductId, out var meta))
                {
                    r.ProductName = meta.ProductName;
                    r.UnitName = meta.UnitName;
                }
            }

            return View("Index", model);
        }

        var rows = (model.Rows ?? new List<UnifiedMonthlyPriceRowVm>())
            .Where(r => r.UnitPrice.HasValue)
            .ToList();

        var productIds = rows.Select(r => r.ProductId).Distinct().ToList();

        if (model.PriceType == UiTabs.FreeSalePrice)
        {
            var existing = await _db.StoreProductMonthlyFreeSalePrices
                .Where(x => x.StoreId == storeId && x.YearMonth == model.YearMonth && productIds.Contains(x.ProductId))
                .ToListAsync();

            var existingMap = existing.ToDictionary(x => x.ProductId);

            foreach (var r in rows)
            {
                var price = r.UnitPrice ?? 0m;
                if (existingMap.TryGetValue(r.ProductId, out var e))
                {
                    e.UnitPrice = price;
                }
                else
                {
                    _db.StoreProductMonthlyFreeSalePrices.Add(new StoreProductMonthlyFreeSalePrice
                    {
                        StoreId = storeId,
                        ProductId = r.ProductId,
                        YearMonth = model.YearMonth,
                        UnitPrice = price,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        else
        {
            var existing = await _db.StoreProductMonthlyPrices
                .Where(x => x.StoreId == storeId && x.YearMonth == model.YearMonth && productIds.Contains(x.ProductId))
                .ToListAsync();

            var existingMap = existing.ToDictionary(x => x.ProductId);

            foreach (var r in rows)
            {
                var price = r.UnitPrice ?? 0m;
                if (existingMap.TryGetValue(r.ProductId, out var e))
                {
                    e.UnitPrice = price;
                }
                else
                {
                    _db.StoreProductMonthlyPrices.Add(new StoreProductMonthlyPrice
                    {
                        StoreId = storeId,
                        ProductId = r.ProductId,
                        YearMonth = model.YearMonth,
                        UnitPrice = price,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { yearMonth = model.YearMonth, type = model.PriceType });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyFromPrevious(int yearMonth, string priceType)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        if (priceType != UiTabs.WithdrawalPrice && priceType != UiTabs.FreeSalePrice)
        {
            priceType = UiTabs.WithdrawalPrice;
        }

        var prev = GetPreviousYearMonth(yearMonth);

        if (priceType == UiTabs.FreeSalePrice)
        {
            var prevPrices = await _db.StoreProductMonthlyFreeSalePrices
                .AsNoTracking()
                .Where(x => x.StoreId == storeId && x.YearMonth == prev)
                .Select(x => new { x.ProductId, x.UnitPrice })
                .ToListAsync();

            if (prevPrices.Count == 0)
            {
                return RedirectToAction(nameof(Index), new { yearMonth, type = priceType });
            }

            var productIds = prevPrices.Select(x => x.ProductId).Distinct().ToList();

            var existing = await _db.StoreProductMonthlyFreeSalePrices
                .Where(x => x.StoreId == storeId && x.YearMonth == yearMonth && productIds.Contains(x.ProductId))
                .ToListAsync();

            var existingSet = existing.Select(x => x.ProductId).ToHashSet();

            foreach (var p in prevPrices)
            {
                if (existingSet.Contains(p.ProductId)) continue;

                _db.StoreProductMonthlyFreeSalePrices.Add(new StoreProductMonthlyFreeSalePrice
                {
                    StoreId = storeId,
                    ProductId = p.ProductId,
                    YearMonth = yearMonth,
                    UnitPrice = p.UnitPrice,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else
        {
            var prevPrices = await _db.StoreProductMonthlyPrices
                .AsNoTracking()
                .Where(x => x.StoreId == storeId && x.YearMonth == prev)
                .Select(x => new { x.ProductId, x.UnitPrice })
                .ToListAsync();

            if (prevPrices.Count == 0)
            {
                return RedirectToAction(nameof(Index), new { yearMonth, type = priceType });
            }

            var productIds = prevPrices.Select(x => x.ProductId).Distinct().ToList();

            var existing = await _db.StoreProductMonthlyPrices
                .Where(x => x.StoreId == storeId && x.YearMonth == yearMonth && productIds.Contains(x.ProductId))
                .ToListAsync();

            var existingSet = existing.Select(x => x.ProductId).ToHashSet();

            foreach (var p in prevPrices)
            {
                if (existingSet.Contains(p.ProductId)) continue;

                _db.StoreProductMonthlyPrices.Add(new StoreProductMonthlyPrice
                {
                    StoreId = storeId,
                    ProductId = p.ProductId,
                    YearMonth = yearMonth,
                    UnitPrice = p.UnitPrice,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { yearMonth, type = priceType });
    }

    private static int GetCurrentYearMonth()
    {
        var now = DateTime.UtcNow;
        return now.Year * 100 + now.Month;
    }

    private static int GetPreviousYearMonth(int yearMonth)
    {
        var y = yearMonth / 100;
        var m = yearMonth % 100;
        if (m <= 1)
        {
            return (y - 1) * 100 + 12;
        }

        return y * 100 + (m - 1);
    }
}
