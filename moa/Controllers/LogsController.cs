using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moa.Data;
using moa.Models;
using moa.Services;

namespace moa.Controllers;

[Authorize]
public class LogsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentStoreProvider _currentStoreProvider;

    public LogsController(AppDbContext db, ICurrentStoreProvider currentStoreProvider)
    {
        _db = db;
        _currentStoreProvider = currentStoreProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] LogsIndexViewModel query)
    {
        var storeId = _currentStoreProvider.GetStoreId();

        var tab = string.IsNullOrWhiteSpace(query.Tab) ? UiTabs.Withdrawals : query.Tab;
        if (tab != UiTabs.Withdrawals && tab != UiTabs.FreeSales)
        {
            tab = UiTabs.Withdrawals;
        }

        var vm = new LogsIndexViewModel
        {
            Tab = tab,
            WithdrawalsFilter = query.WithdrawalsFilter ?? new ReceiptListFilter(),
            FreeSalesFilter = query.FreeSalesFilter ?? new FreeSaleListFilter()
        };

        if (tab == UiTabs.FreeSales)
        {
            vm.FreeSales = await BuildFreeSales(storeId, vm.FreeSalesFilter);
        }
        else
        {
            vm.Withdrawals = await BuildWithdrawals(storeId, vm.WithdrawalsFilter);
        }

        return View(vm);
    }

    private async Task<ReceiptListViewModel> BuildWithdrawals(int storeId, ReceiptListFilter filter)
    {
        var q = _db.WithdrawalReceipts
            .AsNoTracking()
            .Where(r => r.StoreId == storeId)
            .Include(r => r.Card)
            .ThenInclude(c => c.Customer)
            .AsQueryable();

        if (filter.Month.HasValue)
        {
            var now = DateTime.UtcNow;
            var year = filter.Year ?? now.Year;
            var from = new DateTime(year, filter.Month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var toExclusive = from.AddMonths(1);
            q = q.Where(r => r.WithdrawnAt >= from && r.WithdrawnAt < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(filter.CardNumber))
        {
            q = q.Where(r => r.Card.CardNumber.Contains(filter.CardNumber));
        }

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
        {
            q = q.Where(r => r.Card.Customer.FullName.Contains(filter.CustomerName));
        }

        var items = await q
            .OrderByDescending(r => r.WithdrawnAt)
            .Select(r => new ReceiptListItemVm
            {
                ReceiptId = r.ReceiptId,
                CardId = r.CardId,
                WithdrawnAt = r.WithdrawnAt,
                CustomerName = r.Card.Customer.FullName,
                CardNumber = r.Card.CardNumber,
                TotalAmount = r.TotalAmount
            })
            .Take(200)
            .ToListAsync();

        var cardMonths = items.Select(i => new { i.CardId, YearMonth = i.WithdrawnAt.Year * 100 + i.WithdrawnAt.Month }).Distinct().ToList();
        
        // Batch fetch balances
        var balances = new Dictionary<(int, int), bool>();
        foreach (var cm in cardMonths)
        {
            var swiped = await _db.CardMonthlyBalances
                .AsNoTracking()
                .Where(b => b.CardId == cm.CardId && b.YearMonth == cm.YearMonth)
                .Select(b => b.IsSwipedInMachine)
                .FirstOrDefaultAsync();
            balances[(cm.CardId, cm.YearMonth)] = swiped;
        }

        foreach (var it in items)
        {
            balances.TryGetValue((it.CardId, it.WithdrawnAt.Year * 100 + it.WithdrawnAt.Month), out var swiped);
            it.IsSwipedInMachine = swiped;
        }

        var receiptIds = items.Select(i => i.ReceiptId).ToList();
        if (receiptIds.Count > 0)
        {
            var lines = await (
                    from ai in _db.WithdrawalAllocationItems.AsNoTracking()
                    join a in _db.WithdrawalAllocations.AsNoTracking() on ai.AllocationId equals a.AllocationId
                    join p in _db.Products.AsNoTracking() on ai.ProductId equals p.ProductId
                    where receiptIds.Contains(a.ReceiptId)
                    select new
                    {
                        a.ReceiptId,
                        p.ProductName,
                        ai.Quantity,
                        ai.UnitPrice
                    })
                .ToListAsync();

            var summaryByReceipt = lines
                .GroupBy(x => new { x.ReceiptId, x.ProductName, x.UnitPrice })
                .Select(g => new
                {
                    g.Key.ReceiptId,
                    g.Key.ProductName,
                    g.Key.UnitPrice,
                    Qty = g.Sum(x => x.Quantity)
                })
                .GroupBy(x => x.ReceiptId)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join("، ", g
                        .OrderBy(x => x.ProductName)
                        .Select(x => $"{x.ProductName}: {x.Qty} × {x.UnitPrice}")));

            foreach (var it in items)
            {
                if (summaryByReceipt.TryGetValue(it.ReceiptId, out var s))
                {
                    it.ItemsSummary = s;
                }
            }
        }

        return new ReceiptListViewModel
        {
            Filter = filter,
            Items = items
        };
    }

    private async Task<FreeSaleListViewModel> BuildFreeSales(int storeId, FreeSaleListFilter filter)
    {
        var q = _db.FreeSaleReceipts
            .AsNoTracking()
            .Where(r => r.StoreId == storeId)
            .AsQueryable();

        if (filter.Month.HasValue)
        {
            var now = DateTime.UtcNow;
            var year = filter.Year ?? now.Year;
            var from = new DateTime(year, filter.Month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var toExclusive = from.AddMonths(1);
            q = q.Where(r => r.SoldAt >= from && r.SoldAt < toExclusive);
        }

        var items = await q
            .OrderByDescending(r => r.SoldAt)
            .Select(r => new FreeSaleListItemVm
            {
                FreeSaleReceiptId = r.FreeSaleReceiptId,
                SoldAt = r.SoldAt,
                TotalAmount = r.TotalAmount
            })
            .Take(200)
            .ToListAsync();

        var receiptIds = items.Select(i => i.FreeSaleReceiptId).ToList();
        if (receiptIds.Count > 0)
        {
            var lines = await (
                    from ri in _db.FreeSaleReceiptItems.AsNoTracking()
                    join p in _db.Products.AsNoTracking() on ri.ProductId equals p.ProductId
                    where receiptIds.Contains(ri.FreeSaleReceiptId)
                    select new
                    {
                        ri.FreeSaleReceiptId,
                        p.ProductName,
                        ri.Quantity,
                        ri.UnitPrice
                    })
                .ToListAsync();

            var summaryByReceipt = lines
                .GroupBy(x => new { x.FreeSaleReceiptId, x.ProductName, x.UnitPrice })
                .Select(g => new
                {
                    g.Key.FreeSaleReceiptId,
                    g.Key.ProductName,
                    g.Key.UnitPrice,
                    Qty = g.Sum(x => x.Quantity)
                })
                .GroupBy(x => x.FreeSaleReceiptId)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join("، ", g
                        .OrderBy(x => x.ProductName)
                        .Select(x => $"{x.ProductName}: {x.Qty} × {x.UnitPrice}")));

            foreach (var it in items)
            {
                if (summaryByReceipt.TryGetValue(it.FreeSaleReceiptId, out var s))
                {
                    it.ItemsSummary = s;
                }
            }
        }

        return new FreeSaleListViewModel
        {
            Filter = filter,
            Items = items
        };
    }
}
