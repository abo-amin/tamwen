using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using moa.Data;
using moa.Models;
using moa.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<IPinHasher, PinHasher>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentStoreProvider, CurrentStoreProvider>();
builder.Services.AddScoped<IMonthlyBalanceService, MonthlyBalanceService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
});

var app = builder.Build();

static string? GetArgValue(string[] args, string key)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static bool HasArg(string[] args, string key)
    => args.Any(a => string.Equals(a, key, StringComparison.OrdinalIgnoreCase));

static List<(string Name, string Unit, decimal? WithdrawalPrice, decimal? FreeSalePrice)> ParseProducts(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return new List<(string, string, decimal?, decimal?)>();

    return raw
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(p =>
        {
            var parts = p.Split(':', StringSplitOptions.TrimEntries);
            var name = parts.Length > 0 ? parts[0] : string.Empty;
            var unit = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : "وحدة";
            decimal? withdrawal = null;
            decimal? freeSale = null;

            if (parts.Length > 2 && decimal.TryParse(parts[2], out var w)) withdrawal = w;
            if (parts.Length > 3 && decimal.TryParse(parts[3], out var f)) freeSale = f;
            if (parts.Length == 3 && withdrawal.HasValue) freeSale = withdrawal;

            return (name, unit, withdrawal, freeSale);
        })
        .Where(x => !string.IsNullOrWhiteSpace(x.name))
        .Select(x => (x.name, x.unit, x.withdrawal, x.freeSale))
        .ToList();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

var supportedCultures = new[] { new CultureInfo("ar-EG") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("ar-EG"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    FallBackToParentCultures = false,
    FallBackToParentUICultures = false
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

if (HasArg(args, "--seed-products"))
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    var db = sp.GetRequiredService<AppDbContext>();
    var users = sp.GetRequiredService<UserManager<ApplicationUser>>();

    var email = GetArgValue(args, "--seed-email") ?? "aboamin.mo12@gmail.com";
    var qtyRaw = GetArgValue(args, "--seed-qty");
    var qty = 1m;
    if (!string.IsNullOrWhiteSpace(qtyRaw) && decimal.TryParse(qtyRaw, out var q)) qty = q;
    if (qty <= 0) qty = 1m;

    var ymRaw = GetArgValue(args, "--seed-ym");
    var ym = 0;
    if (!string.IsNullOrWhiteSpace(ymRaw) && int.TryParse(ymRaw, out var parsedYm)) ym = parsedYm;
    if (ym == 0)
    {
        var nowYm = DateTime.UtcNow;
        ym = nowYm.Year * 100 + nowYm.Month;
    }

    var products = ParseProducts(GetArgValue(args, "--seed-products-list"));

    var user = await users.FindByEmailAsync(email);
    if (user is null)
    {
        Console.WriteLine($"User not found: {email}");
        return;
    }

    if (products.Count == 0)
    {
        Console.WriteLine("No products provided. Use --seed-products-list \"Name:Unit,Name2:Unit2\"");
        return;
    }

    var storeId = user.StoreId;
    var now = DateTime.UtcNow;

    foreach (var (name, unit, withdrawalPrice, freeSalePrice) in products)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.StoreId == storeId && p.ProductName == name);
        if (product is null)
        {
            product = new Product
            {
                StoreId = storeId,
                ProductName = name,
                UnitName = unit,
                IsActive = true,
                CreatedAt = now
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        var inv = await db.StoreProductInventories.FirstOrDefaultAsync(i => i.StoreId == storeId && i.ProductId == product.ProductId);
        if (inv is null)
        {
            db.StoreProductInventories.Add(new StoreProductInventory
            {
                StoreId = storeId,
                ProductId = product.ProductId,
                QuantityOnHand = qty,
                LastRestockQuantity = qty,
                UpdatedAt = now
            });
            await db.SaveChangesAsync();
        }

        if (withdrawalPrice.HasValue)
        {
            var mp = await db.StoreProductMonthlyPrices
                .FirstOrDefaultAsync(x => x.StoreId == storeId && x.ProductId == product.ProductId && x.YearMonth == ym);

            if (mp is null)
            {
                db.StoreProductMonthlyPrices.Add(new StoreProductMonthlyPrice
                {
                    StoreId = storeId,
                    ProductId = product.ProductId,
                    YearMonth = ym,
                    UnitPrice = withdrawalPrice.Value,
                    CreatedAt = now
                });
            }
            else
            {
                mp.UnitPrice = withdrawalPrice.Value;
            }

            await db.SaveChangesAsync();
        }

        if (freeSalePrice.HasValue)
        {
            var fp = await db.StoreProductMonthlyFreeSalePrices
                .FirstOrDefaultAsync(x => x.StoreId == storeId && x.ProductId == product.ProductId && x.YearMonth == ym);

            if (fp is null)
            {
                db.StoreProductMonthlyFreeSalePrices.Add(new StoreProductMonthlyFreeSalePrice
                {
                    StoreId = storeId,
                    ProductId = product.ProductId,
                    YearMonth = ym,
                    UnitPrice = freeSalePrice.Value,
                    CreatedAt = now
                });
            }
            else
            {
                fp.UnitPrice = freeSalePrice.Value;
            }

            await db.SaveChangesAsync();
        }
    }

    var monthlyPricesCount = await db.StoreProductMonthlyPrices.CountAsync(x => x.StoreId == storeId && x.YearMonth == ym);
    var freeSalePricesCount = await db.StoreProductMonthlyFreeSalePrices.CountAsync(x => x.StoreId == storeId && x.YearMonth == ym);

    Console.WriteLine($"Seeded products/prices for {email} (StoreId={storeId}, YearMonth={ym}).");
    Console.WriteLine($"MonthlyPrices rows: {monthlyPricesCount}");
    Console.WriteLine($"FreeSaleMonthlyPrices rows: {freeSalePricesCount}");
    return;
}


app.Run();
