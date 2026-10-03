using System.Security.Claims;
using Microsoft.Extensions.AI;
using OpsPilot.ServiceDefaults;

namespace OpsPilot.Orchestrator.Tools;

public static class ToolCatalog
{
    public static readonly IReadOnlyList<string> ReadTools =
      ["get_order", "list_payment_failures", "get_stock", "search_runbooks"];


    public static readonly IReadOnlyList<string> WriteTools =
           ["reserve_stock", "retry_payment"];


    public static bool CanOperate(ClaimsPrincipal user) =>
           user.IsInRole(OpsRoles.Operator) || user.IsInRole(OpsRoles.Admin);

    public static bool CanView(ClaimsPrincipal user) =>
        user.IsInRole(OpsRoles.Viewer) || CanOperate(user);

    public static IReadOnlyList<string> ToolNamesFor(ClaimsPrincipal user)
    {
        var names = new List<string>();
        if (CanView(user)) names.AddRange(ReadTools);
        if (CanOperate(user)) names.AddRange(WriteTools);
        return names;
    }

    public static IReadOnlyList<AITool> For(ClaimsPrincipal user, OpsTools tools)
    {
        var all = new Dictionary<string, AITool>
        {
            ["get_order"] = AIFunctionFactory.Create(tools.GetOrder, "get_order"),
            ["list_payment_failures"] = AIFunctionFactory.Create(tools.ListPaymentFailures, "list_payment_failures"),
            ["get_stock"] = AIFunctionFactory.Create(tools.GetStock, "get_stock"),
            ["search_runbooks"] = AIFunctionFactory.Create(tools.SearchRunbooks, "search_runbooks"),
            ["reserve_stock"] = AIFunctionFactory.Create(tools.ReserveStock, "reserve_stock"),
            ["retry_payment"] = AIFunctionFactory.Create(tools.RetryPayment, "retry_payment"),
        };

        return ToolNamesFor(user).Select(name => all[name]).ToList();
    }
}