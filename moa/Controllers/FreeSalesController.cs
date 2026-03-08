using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moa.Data;
using moa.Models;
using moa.Services;

namespace moa.Controllers;

[Authorize]
public class FreeSalesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentStoreProvider _currentStoreProvider;

    public FreeSalesController(AppDbContext db, ICurrentStoreProvider currentStoreProvider)
    {
        _db = db;
        _currentStoreProvider = currentStoreProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FreeSaleListFilter filter)
    {
        return RedirectToAction("Index", "Logs", new
        {
            Tab = UiTabs.FreeSales,
            FreeSalesFilter = filter
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var storeId = _currentStoreProvider.GetStoreId();
        var ym = GetCurrentYearMonth();

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && p.IsActive)
            .OrderBy(p => p.ProductName)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.UnitName,
                UnitPrice = _db.StoreProductMonthlyFreeSalePrices
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

        ViewBag.Products = products;
        return View(new FreeSaleCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FreeSaleCreateViewModel model)
    {
        var storeId = _currentStoreProvider.GetStoreId();
        var ym = GetCurrentYearMonth();

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && p.IsActive)
            .OrderBy(p => p.ProductName)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.UnitName,
                UnitPrice = _db.StoreProductMonthlyFreeSalePrices
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

        ViewBag.Products = products;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var items = (model.Items ?? new List<FreeSaleItemInput>())
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

        var freeSalePrices = await _db.StoreProductMonthlyFreeSalePrices
            .AsNoTracking()
            .Where(p => p.StoreId == storeId && p.YearMonth == ym && itemProductIds.Contains(p.ProductId))
            .Select(p => new { p.ProductId, p.UnitPrice })
            .ToListAsync();

        var priceMap = freeSalePrices.ToDictionary(p => p.ProductId, p => p.UnitPrice);

        await using var tx = await _db.Database.BeginTransactionAsync();

        foreach (var it in items)
        {
            var inv = inventories[it.ProductId];
            inv.QuantityOnHand -= it.Quantity;
            inv.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        var receipt = new FreeSaleReceipt
        {
            StoreId = storeId,
            SoldAt = DateTime.UtcNow,
            TotalAmount = 0m,
            CreatedAt = DateTime.UtcNow
        };

        _db.FreeSaleReceipts.Add(receipt);
        await _db.SaveChangesAsync();

        foreach (var it in items)
        {
            if (!priceMap.TryGetValue(it.ProductId, out var unitPrice))
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "لا يوجد سعر بيع حر لهذا المنتج في هذا الشهر");
                return View(model);
            }

            if (unitPrice <= 0)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "سعر البيع الحر غير صحيح");
                return View(model);
            }

            _db.FreeSaleReceiptItems.Add(new FreeSaleReceiptItem
            {
                FreeSaleReceiptId = receipt.FreeSaleReceiptId,
                ProductId = it.ProductId,
                Quantity = it.Quantity,
                UnitPrice = unitPrice,
                LineTotal = 0m
            });
        }

        await _db.SaveChangesAsync();

        var total = await _db.FreeSaleReceiptItems
            .AsNoTracking()
            .Where(x => x.FreeSaleReceiptId == receipt.FreeSaleReceiptId)
            .SumAsync(x => x.LineTotal);

        receipt.TotalAmount = total;
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        return RedirectToAction(nameof(Details), new { id = receipt.FreeSaleReceiptId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var receipt = await _db.FreeSaleReceipts
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.FreeSaleReceiptId == id && r.StoreId == storeId);

        if (receipt is null)
        {
            return NotFound();
        }

        var items = await _db.FreeSaleReceiptItems
            .AsNoTracking()
            .Where(i => i.FreeSaleReceiptId == receipt.FreeSaleReceiptId)
            .Join(
                _db.Products.AsNoTracking(),
                i => i.ProductId,
                p => p.ProductId,
                (i, p) => new FreeSaleDetailsItemVm
                {
                    ProductName = p.ProductName,
                    UnitName = p.UnitName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                })
            .OrderBy(x => x.ProductName)
            .ToListAsync();

        return View(new FreeSaleDetailsViewModel
        {
            FreeSaleReceiptId = receipt.FreeSaleReceiptId,
            SoldAt = receipt.SoldAt,
            TotalAmount = receipt.TotalAmount,
            Items = items
        });
    }

    private static int GetCurrentYearMonth()
    {
        var now = DateTime.UtcNow;
        return now.Year * 100 + now.Month;
    }
}
