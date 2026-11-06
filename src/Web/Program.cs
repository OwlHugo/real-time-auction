using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Prometheus;
using RealTimeAuction.Infrastructure.Data;
using RealTimeAuction.Web.Hubs;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

// Redis é o que torna o sistema distribuído: backplane do SignalR (grupos entre instâncias),
// contagem de espectadores e chaves de idempotência compartilhadas.
var redisConnection =
    builder.Configuration.GetConnectionString("RedisConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:RedisConnection não configurada.");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnection)
);
builder.Services.AddSignalR().AddStackExchangeRedis(redisConnection);

// Atrás de túnel/proxy (ngrok, Cloudflare Tunnel, Azure): respeita o HTTPS original
// para cookies Secure e redirecionamentos corretos.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// Sem este middleware o IExceptionHandler (CustomExceptionHandler) nunca roda:
// regras de negócio (400, 403, 409) viravam 500.
// CustomExceptionHandler (IExceptionHandler) trata as exceções de regra primeiro; este
// fallback só responde 500 em JSON para o que sobrar.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandler = async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro interno do servidor",
        });
    },
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// EnsureCreated é idempotente: roda sempre, para o banco nunca ficar sem tabelas na demo.
await app.InitialiseDatabaseAsync();

app.UseHealthChecks("/health");
app.UseHttpsRedirection();

// Build do Angular (ng build -> ClientApp/dist). Se existir, o próprio servidor entrega o site,
// então celulares acessam UMA única origem (sem CORS, cookie de login funciona).
var spaDist = Path.Combine(app.Environment.ContentRootPath, "ClientApp", "dist");
var hasSpa = File.Exists(Path.Combine(spaDist, "index.html"));
var spaFiles = hasSpa ? new PhysicalFileProvider(spaDist) : null;

app.UseStaticFiles();
if (spaFiles != null)
{
    app.UseStaticFiles(new StaticFileOptions { FileProvider = spaFiles });
}

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

// Mostra qual instância atendeu: útil na demo para provar que há mais de um nó.
app.MapGet(
    "/api/server-info",
    () =>
        Results.Ok(
            new
            {
                Instance = builder.Configuration["INSTANCE_NAME"] ?? Environment.MachineName,
                UtcNow = DateTime.UtcNow,
            }
        )
);

app.MapEndpoints();

app.MapHub<AuctionHub>("/hubs/auction");

app.MapMetrics();

if (spaFiles != null)
{
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = spaFiles });
}
else
{
    app.MapFallbackToFile("index.html");
}

app.Run();

public partial class Program { }
