using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moa.Data;
using moa.Models;
using moa.Services;

namespace moa.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentStoreProvider _currentStoreProvider;

    public ProductsController(AppDbContext db, ICurrentStoreProvider currentStoreProvider)
    {
        _db = db;
        _currentStoreProvider = currentStoreProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var ym = GetCurrentYearMonth();

        var priceMap = await _db.StoreProductMonthlyPrices
            .AsNoTracking()
            .Where(x => x.StoreId == storeId && x.YearMonth == ym)
            .ToDictionaryAsync(x => x.ProductId, x => (decimal?)x.UnitPrice);

        var invMap = await _db.StoreProductInventories
            .AsNoTracking()
            .Where(x => x.StoreId == storeId)
            .ToDictionaryAsync(x => x.ProductId, x => x);

        var items = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreId == storeId)
            .OrderBy(p => p.ProductName)
            .Select(p => new ProductListItemViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                UnitName = p.UnitName,
                IsActive = p.IsActive,
                CurrentUnitPrice = null,
                QuantityOnHand = 0m,
                IsLowStock = false
            })
            .ToListAsync();

        foreach (var i in items)
        {
            if (priceMap.TryGetValue(i.ProductId, out var price))
            {
                i.CurrentUnitPrice = price;
            }

            if (invMap.TryGetValue(i.ProductId, out var inv))
            {
                i.QuantityOnHand = inv.QuantityOnHand;
                i.IsLowStock = inv.LastRestockQuantity > 0m && inv.QuantityOnHand <= Math.Round(inv.LastRestockQuantity * 0.2m, 3);
            }
        }

        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProductFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var exists = await _db.Products.AnyAsync(p => p.StoreId == storeId && p.ProductName == model.ProductName);
        if (exists)
        {
            ModelState.AddModelError(nameof(ProductFormViewModel.ProductName), "اسم المنتج موجود بالفعل");
            return View(model);
        }

        var p = new Product
        {
            StoreId = storeId,
            ProductName = model.ProductName,
            UnitName = model.UnitName,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(p);

        if (model.QuantityOnHand.HasValue || model.LastRestockQuantity.HasValue)
        {
            _db.StoreProductInventories.Add(new StoreProductInventory
            {
                StoreId = storeId,
                Product = p,
                QuantityOnHand = model.QuantityOnHand ?? 0m,
                LastRestockQuantity = model.LastRestockQuantity ?? 0m,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (model.UnitPrice.HasValue)
        {
            _db.StoreProductMonthlyPrices.Add(new StoreProductMonthlyPrice
            {
                StoreId = storeId,
                Product = p,
                YearMonth = GetCurrentYearMonth(),
                UnitPrice = model.UnitPrice.Value,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var p = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == id && x.StoreId == storeId);
        if (p is null)
        {
            return NotFound();
        }

        var ym = GetCurrentYearMonth();
        var price = await _db.StoreProductMonthlyPrices
            .AsNoTracking()
            .Where(x => x.StoreId == storeId && x.ProductId == p.ProductId && x.YearMonth == ym)
            .Select(x => (decimal?)x.UnitPrice)
            .FirstOrDefaultAsync();

        var inv = await _db.StoreProductInventories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.ProductId == p.ProductId);

        return View(new ProductFormViewModel
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            UnitName = p.UnitName,
            IsActive = p.IsActive,
            UnitPrice = price,
            QuantityOnHand = inv?.QuantityOnHand,
            LastRestockQuantity = inv?.LastRestockQuantity
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormViewModel model)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.ProductId is null)
        {
            return BadRequest();
        }

        var p = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == model.ProductId.Value && x.StoreId == storeId);
        if (p is null)
        {
            return NotFound();
        }

        var exists = await _db.Products.AnyAsync(x => x.StoreId == storeId && x.ProductId != p.ProductId && x.ProductName == model.ProductName);
        if (exists)
        {
            ModelState.AddModelError(nameof(ProductFormViewModel.ProductName), "اسم المنتج موجود بالفعل");
            return View(model);
        }

        p.ProductName = model.ProductName;
        p.UnitName = model.UnitName;
        p.IsActive = model.IsActive;

        if (model.QuantityOnHand.HasValue || model.LastRestockQuantity.HasValue)
        {
            var inv = await _db.StoreProductInventories
                .FirstOrDefaultAsync(x => x.StoreId == storeId && x.ProductId == p.ProductId);

            if (inv is null)
            {
                inv = new StoreProductInventory
                {
                    StoreId = storeId,
                    ProductId = p.ProductId,
                    QuantityOnHand = 0m,
                    LastRestockQuantity = 0m,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.StoreProductInventories.Add(inv);
            }

            if (model.QuantityOnHand.HasValue)
            {
                inv.QuantityOnHand = model.QuantityOnHand.Value;
            }

            if (model.LastRestockQuantity.HasValue)
            {
                inv.LastRestockQuantity = model.LastRestockQuantity.Value;
            }

            inv.UpdatedAt = DateTime.UtcNow;
        }

        if (model.UnitPrice.HasValue)
        {
            var ym = GetCurrentYearMonth();
            var existingPrice = await _db.StoreProductMonthlyPrices
                .FirstOrDefaultAsync(x => x.StoreId == storeId && x.ProductId == p.ProductId && x.YearMonth == ym);

            if (existingPrice is null)
            {
                _db.StoreProductMonthlyPrices.Add(new StoreProductMonthlyPrice
                {
                    StoreId = storeId,
                    ProductId = p.ProductId,
                    YearMonth = ym,
                    UnitPrice = model.UnitPrice.Value,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingPrice.UnitPrice = model.UnitPrice.Value;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private static int GetCurrentYearMonth()
    {
        var now = DateTime.UtcNow;
        return now.Year * 100 + now.Month;
    }
}
