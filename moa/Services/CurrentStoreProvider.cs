using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace moa.Services;

public interface ICurrentStoreProvider
{
    int GetStoreId();
}

public class CurrentStoreProvider : ICurrentStoreProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentStoreProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int GetStoreId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var storeIdValue = httpContext?.User?.FindFirstValue("StoreId");

        if (int.TryParse(storeIdValue, out var storeId) && storeId > 0)
        {
            return storeId;
        }

        throw new InvalidOperationException("StoreId claim is missing. User must be logged in.");
    }
}
