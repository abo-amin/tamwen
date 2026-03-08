using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moa.Data;
using moa.Models;
using System.Security.Claims;

namespace moa.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(RegisterViewModel.Email), "Email already exists");
            return View(model);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var owner = new Owner
        {
            FullName = model.OwnerName,
            CreatedAt = DateTime.UtcNow
        };
        _db.Owners.Add(owner);
        await _db.SaveChangesAsync();

        var store = new Store
        {
            OwnerId = owner.OwnerId,
            StoreName = model.StoreName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Stores.Add(store);
        await _db.SaveChangesAsync();

        var storeSetting = new StoreSetting
        {
            StoreId = store.StoreId,
            NearEmptyThresholdAmount = 20m,
            AllowEarlyOpenNextMonth = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.StoreSettings.Add(storeSetting);
        await _db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            StoreId = store.StoreId,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var err in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }

            await tx.RollbackAsync();
            return View(model);
        }

        await _userManager.AddClaimAsync(user, new Claim("StoreId", store.StoreId.ToString()));

        await tx.CommitAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(model);
        }

        var claims = await _userManager.GetClaimsAsync(user);
        if (!claims.Any(c => c.Type == "StoreId"))
        {
            await _userManager.AddClaimAsync(user, new Claim("StoreId", user.StoreId.ToString()));
            await _signInManager.RefreshSignInAsync(user);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }
}
