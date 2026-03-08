using System.ComponentModel.DataAnnotations;

namespace moa.Models;

/// <summary>صفحة تفاصيل مجموعة بطاقات - تُعرض تلقائيًا لكل مجموعة</summary>
public class CardGroupDetailsVm
{
    public int CardGroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<CardGroupCardItemVm> Cards { get; set; } = new();
}

public class CardGroupListItemVm
{
    public int CardGroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public int CardsCount { get; set; }
}

public class CardGroupCardItemVm
{
    public int CardId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string? Phone { get; set; }
    public int? CardGroupId { get; set; }
    public bool HasWithdrawnThisMonth { get; set; }
    public bool IsSwipedInMachine { get; set; }
}

public class CardGroupsIndexViewModel
{
    public List<CardGroupListItemVm> Groups { get; set; } = new();
    public List<CardGroupCardItemVm> Cards { get; set; } = new();

    [Required]
    [StringLength(200)]
    public string NewGroupName { get; set; } = string.Empty;
}

public class CardGroupAssignVm
{
    [Required]
    public int CardId { get; set; }

    [Required]
    public int CardGroupId { get; set; }
}

public class CardGroupDeleteVm
{
    [Required]
    public int CardGroupId { get; set; }
}

