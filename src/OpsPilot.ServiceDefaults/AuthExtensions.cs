using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OpsPilot.ServiceDefaults;

public static class OpsRoles
{
    public const string Viewer = "ops.viewer";
    public const string Operator = "ops.operator";
    public const string Admin = "ops.admin";
}

public static class OpsPolicies
{
    public const string CanView = "CanView";
    public const string CanOperate = "CanOperate";
    public const string CanAdmin = "CanAdmin";
}


public static class AuthExtensions
{
    public static WebApplicationBuilder AddOpsAuth(this WebApplicationBuilder builder)
    {
        // Configuration-driven: Authentication:Schemes:Bearer:{ValidIssuer, ValidAudiences, SigningKeys | Authority}
        builder.Services.AddAuthentication().AddJwtBearer();

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(OpsPolicies.CanView, p => p.RequireRole(OpsRoles.Viewer, OpsRoles.Operator, OpsRoles.Admin))
            .AddPolicy(OpsPolicies.CanOperate, p => p.RequireRole(OpsRoles.Operator, OpsRoles.Admin))
            .AddPolicy(OpsPolicies.CanAdmin, p => p.RequireRole(OpsRoles.Admin));

        return builder;
    }
}