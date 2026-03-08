using System.ComponentModel.DataAnnotations;

namespace moa.Models;

public class FreeSaleItemInput
{
    public int? ProductId { get; set; }

    [Range(0, 1000000)]
    public decimal? Quantity { get; set; }
}

public class FreeSaleCreateViewModel
{
    public List<FreeSaleItemInput> Items { get; set; } = new();
}

public class FreeSaleDetailsItemVm
{
    public string ProductName { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class FreeSaleDetailsViewModel
{
    public int FreeSaleReceiptId { get; set; }
    public DateTime SoldAt { get; set; }
    public decimal TotalAmount { get; set; }
    public List<FreeSaleDetailsItemVm> Items { get; set; } = new();
}

public class FreeSaleListFilter
{
    [Range(2020, 2100)]
    public int? Year { get; set; }

    [Range(1, 12)]
    public int? Month { get; set; }
}

public class FreeSaleListItemVm
{
    public int FreeSaleReceiptId { get; set; }
    public DateTime SoldAt { get; set; }
    public decimal TotalAmount { get; set; }

    public string ItemsSummary { get; set; } = "";
}

public class FreeSaleListViewModel
{
    public FreeSaleListFilter Filter { get; set; } = new();
    public List<FreeSaleListItemVm> Items { get; set; } = new();
}
