using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moa.Data;
using moa.Models;
using moa.Services;

namespace moa.Controllers;

[Authorize]
public class MonthlyPricesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentStoreProvider _currentStoreProvider;

    public MonthlyPricesController(AppDbContext db, ICurrentStoreProvider currentStoreProvider)
    {
        _db = db;
        _currentStoreProvider = currentStoreProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? yearMonth)
    {
        return RedirectToAction("Index", "Prices", new { yearMonth, type = UiTabs.WithdrawalPrice });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(MonthlyPricesViewModel model)
    {
        var vm = new UnifiedMonthlyPricesViewModel
        {
            YearMonth = model.YearMonth,
            PriceType = UiTabs.WithdrawalPrice,
            Rows = (model.Rows ?? new List<MonthlyPriceRowVm>())
                .Select(r => new UnifiedMonthlyPriceRowVm
                {
                    ProductId = r.ProductId,
                    ProductName = r.ProductName,
                    UnitName = r.UnitName,
                    UnitPrice = r.UnitPrice
                }).ToList()
        };

        return await new PricesController(_db, _currentStoreProvider).Save(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyFromPrevious(int yearMonth)
    {
        return await new PricesController(_db, _currentStoreProvider).CopyFromPrevious(yearMonth, UiTabs.WithdrawalPrice);
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
