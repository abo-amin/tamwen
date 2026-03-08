using System.ComponentModel.DataAnnotations;

namespace moa.Models;

public class MonthlyBalanceListItemVm
{
    public int CardId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public int MembersCount { get; set; }
    public decimal? OpeningBalance { get; set; }
}

public class MonthlyBalancesIndexViewModel
{
    [Range(200001, 209912)]
    public int YearMonth { get; set; }

    public string? Search { get; set; }

    public List<MonthlyBalanceListItemVm> Items { get; set; } = new();
}

public class MonthlyBalanceEditViewModel
{
    [Required]
    public int CardId { get; set; }

    [Required]
    [Range(200001, 209912)]
    public int YearMonth { get; set; }

    public string CustomerName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;

    [Required]
    [Range(1, 50)]
    public int MembersCountSnapshot { get; set; }

    [Required]
    [Range(0, 1000000000)]
    public decimal OpeningBalance { get; set; }

    [Required]
    public DateTime OpenAt { get; set; }
}
