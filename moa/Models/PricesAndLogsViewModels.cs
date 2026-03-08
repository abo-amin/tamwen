using System.ComponentModel.DataAnnotations;

namespace moa.Models;

public static class UiTabs
{
    public const string Withdrawals = "withdrawals";
    public const string FreeSales = "freeSales";

    public const string WithdrawalPrice = "withdrawal";
    public const string FreeSalePrice = "freeSale";
}

public class UnifiedMonthlyPriceRowVm
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string UnitName { get; set; } = null!;

    [Range(0, 1000000)]
    public decimal? UnitPrice { get; set; }
}

public class UnifiedMonthlyPricesViewModel
{
    [Required]
    [Range(200001, 209912)]
    public int YearMonth { get; set; }

    [Required]
    public string PriceType { get; set; } = UiTabs.WithdrawalPrice;

    public List<UnifiedMonthlyPriceRowVm> Rows { get; set; } = new();
}

public class LogsIndexViewModel
{
    [Required]
    public string Tab { get; set; } = UiTabs.Withdrawals;

    public ReceiptListFilter WithdrawalsFilter { get; set; } = new();
    public FreeSaleListFilter FreeSalesFilter { get; set; } = new();

    public ReceiptListViewModel? Withdrawals { get; set; }
    public FreeSaleListViewModel? FreeSales { get; set; }
}
