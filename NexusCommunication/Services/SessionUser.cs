using System.Security.Claims;

namespace NexusCommunication.Services;

public record SessionUser(int Id, string Username, string Name, string Role, int? ShopId, string? ScopeName);

public static class ClaimsExtensions
{
    public static SessionUser ToSession(this ClaimsPrincipal p)
    {
        string? Get(string type) => p.FindFirst(type)?.Value;
        int.TryParse(Get(ClaimTypes.NameIdentifier), out var id);
        int? shop = int.TryParse(Get("shopId"), out var s) ? s : null;
        return new SessionUser(id, Get("username") ?? "", Get(ClaimTypes.Name) ?? "", Get(ClaimTypes.Role) ?? "", shop, Get("scopeName"));
    }
}
