using System.ComponentModel.DataAnnotations;

namespace moa.Models;

public class ReceiptListFilter
{
    [Range(2020, 2100)]
    public int? Year { get; set; }

    [Range(1, 12)]
    public int? Month { get; set; }

    public string? CardNumber { get; set; }

    public string? CustomerName { get; set; }
}

public class ReceiptListItemVm
{
    public int ReceiptId { get; set; }
    public int CardId { get; set; }
    public DateTime WithdrawnAt { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public bool IsSwipedInMachine { get; set; }

    public string ItemsSummary { get; set; } = "";
}

public class ReceiptListViewModel
{
    public ReceiptListFilter Filter { get; set; } = new();
    public List<ReceiptListItemVm> Items { get; set; } = new();
}
