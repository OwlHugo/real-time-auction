using System.Security.Claims;
using RealTimeAuction.Application.Common.Interfaces;

namespace RealTimeAuction.Web.Services;

public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Id =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? DisplayName => ToDisplayName(_httpContextAccessor.HttpContext?.User?.Identity?.Name);

    public List<string>? Roles =>
        _httpContextAccessor
            .HttpContext?.User?.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .ToList();

    public static string? ToDisplayName(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return null;

        var at = userName.IndexOf('@');
        return at > 0 ? userName[..at] : userName;
    }
}
