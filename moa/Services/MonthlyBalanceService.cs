using Microsoft.EntityFrameworkCore;
using moa.Data;
using moa.Models;

namespace moa.Services;

public interface IMonthlyBalanceService
{
    Task EnsureBalancesForMonthAsync(int storeId, int yearMonth);
}

public class MonthlyBalanceService : IMonthlyBalanceService
{
    private readonly AppDbContext _db;

    public MonthlyBalanceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnsureBalancesForMonthAsync(int storeId, int yearMonth)
    {
        // Find all active cards assigned to this store
        var activeCards = await _db.RationCards
            .AsNoTracking()
            .Join(
                _db.CustomerStoreAssignments.AsNoTracking().Where(a => a.EndAt == null && a.StoreId == storeId),
                card => card.CustomerId,
                a => a.CustomerId,
                (card, a) => card)
            .Select(c => new { c.CardId, c.MembersCount, c.PricePerPerson })
            .ToListAsync();

        if (activeCards.Count == 0) return;

        var activeCardIds = activeCards.Select(c => c.CardId).ToList();

        // Find which cards already have a balance for the requested month
        var existingBalanceCardIds = await _db.CardMonthlyBalances
            .AsNoTracking()
            .Where(b => b.YearMonth == yearMonth && activeCardIds.Contains(b.CardId))
            .Select(b => b.CardId)
            .ToListAsync();

        var existingSet = new HashSet<int>(existingBalanceCardIds);
        var missingCards = activeCards.Where(c => !existingSet.Contains(c.CardId)).ToList();

        if (missingCards.Count == 0) return;

        var missingCardIds = missingCards.Select(c => c.CardId).ToList();

        // Find the most recent balance for each missing card to carry over the OpeningBalance
        var previousBalances = await _db.CardMonthlyBalances
            .AsNoTracking()
            .Where(b => missingCardIds.Contains(b.CardId) && b.YearMonth < yearMonth)
            .GroupBy(b => b.CardId)
            .Select(g => g.OrderByDescending(b => b.YearMonth).FirstOrDefault())
            .ToListAsync();

        var prevBalanceDict = previousBalances
            .Where(b => b != null)
            .ToDictionary(b => b!.CardId, b => b);

        var newBalances = new List<CardMonthlyBalance>();
        var openAt = new DateTime(yearMonth / 100, yearMonth % 100, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var card in missingCards)
        {
            decimal openingBalance;
            int membersSnapshot;

            if (prevBalanceDict.TryGetValue(card.CardId, out var lastBalance) && lastBalance != null)
            {
                // Carry over the fixed user-defined balance and members snapshot
                openingBalance = lastBalance.OpeningBalance;
                membersSnapshot = lastBalance.MembersCountSnapshot;
            }
            else
            {
                // Fallback for new cards with no history
                membersSnapshot = card.MembersCount;
                openingBalance = ComputeOpeningBalance(card.MembersCount, card.PricePerPerson);
            }

            newBalances.Add(new CardMonthlyBalance
            {
                CardId = card.CardId,
                YearMonth = yearMonth,
                MembersCountSnapshot = membersSnapshot,
                OpeningBalance = openingBalance,
                OpenAt = openAt,
                CloseAt = null,
                IsSwipedInMachine = false,
                Notes = "تم فتحه تلقائياً بواسطة النظام بناءً على الرصيد الثابت", // Auto-created note
                CreatedAt = DateTime.UtcNow
            });
        }

        if (newBalances.Count > 0)
        {
            _db.CardMonthlyBalances.AddRange(newBalances);
            await _db.SaveChangesAsync();
        }
    }

    private static decimal ComputeOpeningBalance(int membersCount, decimal pricePerPerson)
    {
        var fullCount = Math.Min(membersCount, 4);
        var halfCount = Math.Max(membersCount - 4, 0);
        return (fullCount + (halfCount * 0.5m)) * pricePerPerson;
    }
}
