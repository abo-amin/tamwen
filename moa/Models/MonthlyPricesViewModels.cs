using System.ComponentModel.DataAnnotations;

namespace moa.Models;

public class MonthlyPriceRowVm
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string UnitName { get; set; } = null!;

    [Range(0, 1000000)]
    public decimal? UnitPrice { get; set; }
}

public class MonthlyPricesViewModel
{
    [Required]
    [Range(200001, 209912)]
    public int YearMonth { get; set; }

    public List<MonthlyPriceRowVm> Rows { get; set; } = new();
}
