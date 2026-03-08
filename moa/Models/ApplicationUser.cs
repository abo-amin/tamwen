using Microsoft.AspNetCore.Identity;

namespace moa.Models;

public class ApplicationUser : IdentityUser
{
    public int StoreId { get; set; }
}
