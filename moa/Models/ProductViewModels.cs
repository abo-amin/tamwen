using System.ComponentModel.DataAnnotations;

namespace moa.Models;

public class ProductFormViewModel
{
    public int? ProductId { get; set; }

    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string UnitName { get; set; } = null!;

    [Range(0, 999999999)]
    public decimal? UnitPrice { get; set; }

    [Range(0, 999999999)]
    public decimal? QuantityOnHand { get; set; }

    [Range(0, 999999999)]
    public decimal? LastRestockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ProductListItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public decimal? CurrentUnitPrice { get; set; }
    public decimal QuantityOnHand { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsActive { get; set; }
}
