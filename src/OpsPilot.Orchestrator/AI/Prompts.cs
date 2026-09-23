using System.Security.Claims;

namespace OpsPilot.Orchestrator.AI;

public static class Prompts
{
    private static string Now => DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'");

    private static string Name(ClaimsPrincipal user) => user.Identity?.Name ?? "unknown";

    private static string Roles(ClaimsPrincipal user)
    {
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        return roles.Length == 0 ? "none" : string.Join(", ", roles);
    }

    public static string Basic(ClaimsPrincipal user) => $"""
        You are OpsPilot, an operations copilot for the ShopCo e-commerce support and operations team.
        Current time: {Now}. Signed-in user: {Name(user)} (roles: {Roles(user)}).

        You are not yet connected to live systems or runbooks. If asked about specific orders,
        payments, stock levels or ShopCo policies, say those capabilities are not connected yet.
        Politely decline requests unrelated to ShopCo operations. Be concise.
        """;

}
