using System.ComponentModel.DataAnnotations;

namespace moa.Models;

public class ReceiptItemInput
{
    public int? ProductId { get; set; }

    [Range(0, 1000000)]
    public decimal? Quantity { get; set; }
}

public class ReceiptCreateViewModel
{
    [Required]
    public int? CardId { get; set; }

    public string? CardNumber { get; set; }

    public string? Pin { get; set; }

    public List<ReceiptItemInput> Items { get; set; } = new();
}

public class ReceiptAllocationLineVm
{
    public int YearMonth { get; set; }
    public decimal AllocatedAmount { get; set; }
}

public class ReceiptDetailsViewModel
{
    public int ReceiptId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public DateTime WithdrawnAt { get; set; }
    public decimal TotalAmount { get; set; }

    public List<ReceiptAllocationLineVm> Allocations { get; set; } = new();
}
