using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace moa.Models;

public class CardCreateViewModel
{
    [Required(ErrorMessage = "اسم صاحب البطاقة مطلوب")]
    [StringLength(200)]
    public string CustomerName { get; set; } = null!;

    public string? Phone { get; set; }

    [StringLength(50)]
    public string? CardNumber { get; set; }

    public int? CardGroupId { get; set; }

    public List<SelectListItem> CardGroups { get; set; } = new();

    [DataType(DataType.Password)]
    public string? Pin { get; set; }

    [Required]
    [Range(1, 50)]
    public int MembersCount { get; set; }

    [Required]
    [Range(0, 1000000000)]
    public decimal PricePerPerson { get; set; }
}

public class CardSearchViewModel
{
    public string? Name { get; set; }
    public List<CardSearchResultItem> Results { get; set; } = new();
}

public class CardSearchResultItem
{
    public int CardId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public int MembersCount { get; set; }
}

public class CardBalanceRow
{
    public int YearMonth { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
}

public class CardDetailsViewModel
{
    public int CardId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public int MembersCount { get; set; }

    public string? Phone { get; set; }
    public int? CardGroupId { get; set; }

    public decimal NearEmptyThresholdAmount { get; set; }
    public bool AllowEarlyOpenNextMonth { get; set; }

    public List<CardBalanceRow> Balances { get; set; } = new();

    public bool CanOpenNextMonth { get; set; }

    [Required]
    [Range(200001, 209912)]
    public int NextYearMonth { get; set; }

    [Required]
    [Range(0, 1000000000)]
    public decimal NextOpeningBalance { get; set; }
}

public class CardSetPhoneViewModel
{
    public int CardId { get; set; }
    public string? CustomerName { get; set; }
    public string? CardNumber { get; set; }

    [Required(ErrorMessage = "رقم التليفون مطلوب")]
    [StringLength(30)]
    public string Phone { get; set; } = null!;
}

public class CardBatchCreateRowVm
{
    [StringLength(200)]
    public string? CustomerName { get; set; }

    [Range(1, 50)]
    public int MembersCount { get; set; } = 1;

    public bool IsConfirmedDuplicate { get; set; }
}

public class CardBatchExistingItemVm
{
    public string CustomerName { get; set; } = null!;
    public int MembersCount { get; set; }
}

public class CardBatchCreateViewModel
{
    [Display(Name = "الفرن")]
    public int StoreId { get; set; }

    public List<SelectListItem> Stores { get; set; } = new();

    [Display(Name = "مجموعة البطاقات")]
    public int? CardGroupId { get; set; }

    public List<SelectListItem> CardGroups { get; set; } = new();

    public string? StoreName { get; set; }

    public List<CardBatchExistingItemVm> ExistingCards { get; set; } = new();

    public List<CardBatchCreateRowVm> Rows { get; set; } = new();
}
