using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using RealTimeAuction.Application.Common.Interfaces;
using RealTimeAuction.Infrastructure.Data;
using RealTimeAuction.Web.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();
        builder.Services.AddScoped<IAuctionHubService, AuctionHubService>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

        builder.Services.AddExceptionHandler<CustomExceptionHandler>();

        builder.Services.AddRazorPages();

        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true
        );

        builder.Services.AddSignalR();

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApiDocument(
            (configure, sp) =>
            {
                configure.Title = "RealTimeAuction API";
            }
        );
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential()
            );
        }
    }
}
