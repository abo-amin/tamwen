namespace moa.Models;

public class Owner
{
    public int OwnerId { get; set; }
    public string FullName { get; set; } = null!;
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Store> Stores { get; set; } = new List<Store>();
}

public class Store
{
    public int StoreId { get; set; }
    public int OwnerId { get; set; }
    public string StoreName { get; set; } = null!;
    public string? CommercialRegNo { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Owner Owner { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<StoreProductInventory> Inventories { get; set; } = new List<StoreProductInventory>();
    public ICollection<CustomerStoreAssignment> CustomerAssignments { get; set; } = new List<CustomerStoreAssignment>();
    public ICollection<StoreProductMonthlyPrice> MonthlyPrices { get; set; } = new List<StoreProductMonthlyPrice>();
    public ICollection<StoreProductMonthlyFreeSalePrice> FreeSaleMonthlyPrices { get; set; } = new List<StoreProductMonthlyFreeSalePrice>();
    public ICollection<WithdrawalReceipt> WithdrawalReceipts { get; set; } = new List<WithdrawalReceipt>();
    public ICollection<FreeSaleReceipt> FreeSaleReceipts { get; set; } = new List<FreeSaleReceipt>();

    public ICollection<CardGroup> CardGroups { get; set; } = new List<CardGroup>();

    public StoreSetting? StoreSetting { get; set; }
}

public class CardGroup
{
    public int CardGroupId { get; set; }
    public int StoreId { get; set; }
    public string GroupName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<RationCard> Cards { get; set; } = new List<RationCard>();
}

public class Customer
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = null!;
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public RationCard? RationCard { get; set; }
    public ICollection<CustomerStoreAssignment> StoreAssignments { get; set; } = new List<CustomerStoreAssignment>();
}

public class CustomerStoreAssignment
{
    public int AssignmentId { get; set; }
    public int CustomerId { get; set; }
    public int StoreId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
}

public class RationCard
{
    public int CardId { get; set; }
    public int CustomerId { get; set; }
    public string CardNumber { get; set; } = null!;
    public byte[]? PinHash { get; set; }
    public byte[]? PinSalt { get; set; }
    public int MembersCount { get; set; }
    public decimal PricePerPerson { get; set; }
    public int? CardGroupId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public CardGroup? CardGroup { get; set; }

    public ICollection<CardMonthlyBalance> MonthlyBalances { get; set; } = new List<CardMonthlyBalance>();
    public ICollection<WithdrawalReceipt> WithdrawalReceipts { get; set; } = new List<WithdrawalReceipt>();
}

public class StoreSetting
{
    public int StoreId { get; set; }
    public decimal NearEmptyThresholdAmount { get; set; }
    public bool AllowEarlyOpenNextMonth { get; set; }
    public DateTime CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
}

public class Product
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public string ProductName { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Store Store { get; set; } = null!;

    public StoreProductInventory? Inventory { get; set; }

    public ICollection<StoreProductMonthlyPrice> MonthlyPrices { get; set; } = new List<StoreProductMonthlyPrice>();
    public ICollection<StoreProductMonthlyFreeSalePrice> FreeSaleMonthlyPrices { get; set; } = new List<StoreProductMonthlyFreeSalePrice>();
    public ICollection<WithdrawalReceiptItem> ReceiptItems { get; set; } = new List<WithdrawalReceiptItem>();
    public ICollection<FreeSaleReceiptItem> FreeSaleReceiptItems { get; set; } = new List<FreeSaleReceiptItem>();
    public ICollection<WithdrawalAllocationItem> AllocationItems { get; set; } = new List<WithdrawalAllocationItem>();
}

public class StoreProductInventory
{
    public int StoreProductInventoryId { get; set; }
    public int StoreId { get; set; }
    public int ProductId { get; set; }

    public decimal QuantityOnHand { get; set; }
    public decimal LastRestockQuantity { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Store Store { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class StoreProductMonthlyFreeSalePrice
{
    public int StoreProductMonthlyFreeSalePriceId { get; set; }
    public int StoreId { get; set; }
    public int ProductId { get; set; }
    public int YearMonth { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class StoreProductMonthlyPrice
{
    public int StoreProductMonthlyPriceId { get; set; }
    public int StoreId { get; set; }
    public int ProductId { get; set; }
    public int YearMonth { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class CardMonthlyBalance
{
    public int CardMonthlyBalanceId { get; set; }
    public int CardId { get; set; }
    public int YearMonth { get; set; }
    public int MembersCountSnapshot { get; set; }
    public decimal OpeningBalance { get; set; }
    public DateTime OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
    public bool IsSwipedInMachine { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public RationCard Card { get; set; } = null!;

    public ICollection<WithdrawalAllocation> WithdrawalAllocations { get; set; } = new List<WithdrawalAllocation>();
}

public class WithdrawalReceipt
{
    public int ReceiptId { get; set; }
    public int CardId { get; set; }
    public int StoreId { get; set; }
    public DateTime WithdrawnAt { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    public RationCard Card { get; set; } = null!;
    public Store Store { get; set; } = null!;

    public ICollection<WithdrawalReceiptItem> Items { get; set; } = new List<WithdrawalReceiptItem>();
    public ICollection<WithdrawalAllocation> Allocations { get; set; } = new List<WithdrawalAllocation>();
}

public class FreeSaleReceipt
{
    public int FreeSaleReceiptId { get; set; }
    public int StoreId { get; set; }
    public DateTime SoldAt { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<FreeSaleReceiptItem> Items { get; set; } = new List<FreeSaleReceiptItem>();
}

public class FreeSaleReceiptItem
{
    public int FreeSaleReceiptItemId { get; set; }
    public int FreeSaleReceiptId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public FreeSaleReceipt Receipt { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class WithdrawalReceiptItem
{
    public int ReceiptItemId { get; set; }
    public int ReceiptId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public WithdrawalReceipt Receipt { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class WithdrawalAllocation
{
    public int AllocationId { get; set; }
    public int ReceiptId { get; set; }
    public int CardMonthlyBalanceId { get; set; }
    public decimal AllocatedAmount { get; set; }

    public WithdrawalReceipt Receipt { get; set; } = null!;
    public CardMonthlyBalance CardMonthlyBalance { get; set; } = null!;

    public ICollection<WithdrawalAllocationItem> Items { get; set; } = new List<WithdrawalAllocationItem>();
}

public class WithdrawalAllocationItem
{
    public int AllocationItemId { get; set; }
    public int AllocationId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public WithdrawalAllocation WithdrawalAllocation { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
