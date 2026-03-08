namespace moa.Models;

public class PrintMonthlyViewModel
{
    public string StoreName { get; set; } = string.Empty;
    public int YearMonth { get; set; }

    public List<PrintMonthlyCardVm> Cards { get; set; } = new();
    public List<PrintMonthlyReceiptVm> Receipts { get; set; } = new();
    public List<PrintMonthlyLineVm> Lines { get; set; } = new();
}

public class PrintMonthlyCardVm
{
    public int CardId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string? Phone { get; set; }
    public string GroupName { get; set; } = null!;
}

public class PrintMonthlyReceiptVm
{
    public int ReceiptId { get; set; }
    public int CardId { get; set; }
    public DateTime WithdrawnAt { get; set; }
}

public class PrintMonthlyLineVm
{
    public int ReceiptId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
