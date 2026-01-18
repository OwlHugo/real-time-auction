using Prometheus;
using RealTimeAuction.Infrastructure.Data;
using RealTimeAuction.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();
builder
    .Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("RedisConnection")!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseHealthChecks("/health");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpMetrics();

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.MapRazorPages();

app.MapGroup("/api/auth").MapIdentityApi<RealTimeAuction.Infrastructure.Identity.ApplicationUser>();

app.MapPost(
        "/api/auth/logout",
        async (
            Microsoft.AspNetCore.Identity.SignInManager<RealTimeAuction.Infrastructure.Identity.ApplicationUser> signInManager
        ) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        }
    )
    .RequireAuthorization();

app.MapGet(
        "/api/auth/me",
        (System.Security.Claims.ClaimsPrincipal user) =>
        {
            if (user.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            return Results.Ok(
                new
                {
                    id = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    email = user.Identity.Name,
                }
            );
        }
    )
    .RequireAuthorization();

app.MapEndpoints();

app.MapHub<AuctionHub>("/hubs/auction");

app.MapMetrics();

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
