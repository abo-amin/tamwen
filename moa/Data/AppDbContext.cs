using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using moa.Models;

namespace moa.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreSetting> StoreSettings => Set<StoreSetting>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerStoreAssignment> CustomerStoreAssignments => Set<CustomerStoreAssignment>();
    public DbSet<RationCard> RationCards => Set<RationCard>();
    public DbSet<CardGroup> CardGroups => Set<CardGroup>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StoreProductInventory> StoreProductInventories => Set<StoreProductInventory>();
    public DbSet<StoreProductMonthlyPrice> StoreProductMonthlyPrices => Set<StoreProductMonthlyPrice>();
    public DbSet<StoreProductMonthlyFreeSalePrice> StoreProductMonthlyFreeSalePrices => Set<StoreProductMonthlyFreeSalePrice>();
    public DbSet<CardMonthlyBalance> CardMonthlyBalances => Set<CardMonthlyBalance>();

    public DbSet<WithdrawalReceipt> WithdrawalReceipts => Set<WithdrawalReceipt>();
    public DbSet<WithdrawalReceiptItem> WithdrawalReceiptItems => Set<WithdrawalReceiptItem>();
    public DbSet<WithdrawalAllocation> WithdrawalAllocations => Set<WithdrawalAllocation>();
    public DbSet<WithdrawalAllocationItem> WithdrawalAllocationItems => Set<WithdrawalAllocationItem>();

    public DbSet<FreeSaleReceipt> FreeSaleReceipts => Set<FreeSaleReceipt>();
    public DbSet<FreeSaleReceiptItem> FreeSaleReceiptItems => Set<FreeSaleReceiptItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.StoreId).IsRequired();
            e.HasIndex(x => x.StoreId);
            e.HasOne<Store>()
                .WithMany()
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Owner>(e =>
        {
            e.HasKey(x => x.OwnerId);
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.NationalId).HasMaxLength(50);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(400);
        });

        modelBuilder.Entity<Store>(e =>
        {
            e.HasKey(x => x.StoreId);
            e.Property(x => x.StoreName).HasMaxLength(200).IsRequired();
            e.Property(x => x.CommercialRegNo).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Address).HasMaxLength(400);
            e.HasOne(x => x.Owner)
                .WithMany(x => x.Stores)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.OwnerId);
        });

        modelBuilder.Entity<CardGroup>(e =>
        {
            e.HasKey(x => x.CardGroupId);
            e.Property(x => x.GroupName).HasMaxLength(200).IsRequired();

            e.HasOne(x => x.Store)
                .WithMany(x => x.CardGroups)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.StoreId);
            e.HasIndex(x => new { x.StoreId, x.GroupName }).IsUnique();
        });

        modelBuilder.Entity<StoreSetting>(e =>
        {
            e.HasKey(x => x.StoreId);
            e.Property(x => x.NearEmptyThresholdAmount).HasPrecision(18, 2);
            e.Property(x => x.AllowEarlyOpenNextMonth).HasDefaultValue(true);

            e.HasOne(x => x.Store)
                .WithOne(x => x.StoreSetting)
                .HasForeignKey<StoreSetting>(x => x.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.CustomerId);
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.NationalId).HasMaxLength(50);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Address).HasMaxLength(400);

            e.HasIndex(x => x.FullName);
        });

        modelBuilder.Entity<CustomerStoreAssignment>(e =>
        {
            e.HasKey(x => x.AssignmentId);

            e.HasOne(x => x.Customer)
                .WithMany(x => x.StoreAssignments)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Store)
                .WithMany(x => x.CustomerAssignments)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.StoreId);
            e.HasIndex(x => new { x.CustomerId, x.StartAt });

            e.HasIndex(x => x.CustomerId)
                .IsUnique()
                .HasFilter("[EndAt] IS NULL");
        });

        modelBuilder.Entity<RationCard>(e =>
        {
            e.HasKey(x => x.CardId);
            e.Property(x => x.CardNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.PinHash).HasMaxLength(128);
            e.Property(x => x.PinSalt).HasMaxLength(128);
            e.Property(x => x.PricePerPerson).HasPrecision(18, 2);
            e.HasIndex(x => x.CardNumber).IsUnique();
            e.HasIndex(x => x.CustomerId).IsUnique();

            e.HasOne(x => x.Customer)
                .WithOne(x => x.RationCard)
                .HasForeignKey<RationCard>(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CardGroup)
                .WithMany(x => x.Cards)
                .HasForeignKey(x => x.CardGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.CardGroupId);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            e.Property(x => x.UnitName).HasMaxLength(50).IsRequired();

            e.HasOne(x => x.Store)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.StoreId);
            e.HasIndex(x => new { x.StoreId, x.ProductName }).IsUnique();
        });

        modelBuilder.Entity<StoreProductInventory>(e =>
        {
            e.HasKey(x => x.StoreProductInventoryId);
            e.Property(x => x.QuantityOnHand).HasPrecision(18, 3);
            e.Property(x => x.LastRestockQuantity).HasPrecision(18, 3);

            e.HasOne(x => x.Store)
                .WithMany(x => x.Inventories)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Product)
                .WithOne(x => x.Inventory)
                .HasForeignKey<StoreProductInventory>(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.StoreId, x.ProductId }).IsUnique();
            e.HasIndex(x => x.StoreId);
        });

        modelBuilder.Entity<StoreProductMonthlyPrice>(e =>
        {
            e.HasKey(x => x.StoreProductMonthlyPriceId);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);

            e.HasOne(x => x.Store)
                .WithMany(x => x.MonthlyPrices)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Product)
                .WithMany(x => x.MonthlyPrices)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.StoreId, x.ProductId, x.YearMonth }).IsUnique();
            e.HasIndex(x => new { x.StoreId, x.YearMonth });
        });

        modelBuilder.Entity<StoreProductMonthlyFreeSalePrice>(e =>
        {
            e.HasKey(x => x.StoreProductMonthlyFreeSalePriceId);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);

            e.HasOne(x => x.Store)
                .WithMany(x => x.FreeSaleMonthlyPrices)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Product)
                .WithMany(x => x.FreeSaleMonthlyPrices)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.StoreId, x.ProductId, x.YearMonth }).IsUnique();
            e.HasIndex(x => new { x.StoreId, x.YearMonth });
        });

        modelBuilder.Entity<CardMonthlyBalance>(e =>
        {
            e.HasKey(x => x.CardMonthlyBalanceId);
            e.Property(x => x.OpeningBalance).HasPrecision(18, 2);

            e.HasOne(x => x.Card)
                .WithMany(x => x.MonthlyBalances)
                .HasForeignKey(x => x.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.CardId, x.YearMonth }).IsUnique();
            e.HasIndex(x => new { x.CardId, x.OpenAt });
        });

        modelBuilder.Entity<WithdrawalReceipt>(e =>
        {
            e.HasKey(x => x.ReceiptId);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);

            e.HasOne(x => x.Card)
                .WithMany(x => x.WithdrawalReceipts)
                .HasForeignKey(x => x.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Store)
                .WithMany(x => x.WithdrawalReceipts)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.CardId, x.WithdrawnAt });
            e.HasIndex(x => new { x.StoreId, x.WithdrawnAt });
        });

        modelBuilder.Entity<WithdrawalReceiptItem>(e =>
        {
            e.HasKey(x => x.ReceiptItemId);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);

            e.HasOne(x => x.Receipt)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany(x => x.ReceiptItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.ReceiptId, x.ProductId }).IsUnique();

            e.Property(x => x.LineTotal)
                .HasPrecision(18, 2)
                .HasComputedColumnSql("ROUND([Quantity] * COALESCE([UnitPrice], 0), 2)", stored: true);
        });

        modelBuilder.Entity<WithdrawalAllocation>(e =>
        {
            e.HasKey(x => x.AllocationId);
            e.Property(x => x.AllocatedAmount).HasPrecision(18, 2);

            e.HasOne(x => x.Receipt)
                .WithMany(x => x.Allocations)
                .HasForeignKey(x => x.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.CardMonthlyBalance)
                .WithMany(x => x.WithdrawalAllocations)
                .HasForeignKey(x => x.CardMonthlyBalanceId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.ReceiptId, x.CardMonthlyBalanceId }).IsUnique();
            e.HasIndex(x => x.ReceiptId);
            e.HasIndex(x => x.CardMonthlyBalanceId);
        });

        modelBuilder.Entity<WithdrawalAllocationItem>(e =>
        {
            e.HasKey(x => x.AllocationItemId);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);

            e.HasOne(x => x.WithdrawalAllocation)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.AllocationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany(x => x.AllocationItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.LineTotal)
                .HasPrecision(18, 2)
                .HasComputedColumnSql("ROUND([Quantity] * [UnitPrice], 2)", stored: true);

            e.HasIndex(x => x.AllocationId);
        });

        modelBuilder.Entity<FreeSaleReceipt>(e =>
        {
            e.HasKey(x => x.FreeSaleReceiptId);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);

            e.HasOne(x => x.Store)
                .WithMany(x => x.FreeSaleReceipts)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.StoreId, x.SoldAt });
        });

        modelBuilder.Entity<FreeSaleReceiptItem>(e =>
        {
            e.HasKey(x => x.FreeSaleReceiptItemId);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);

            e.HasOne(x => x.Receipt)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.FreeSaleReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany(x => x.FreeSaleReceiptItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.LineTotal)
                .HasPrecision(18, 2)
                .HasComputedColumnSql("ROUND([Quantity] * [UnitPrice], 2)", stored: true);

            e.HasIndex(x => x.FreeSaleReceiptId);
            e.HasIndex(x => new { x.FreeSaleReceiptId, x.ProductId }).IsUnique();
        });
    }
}
