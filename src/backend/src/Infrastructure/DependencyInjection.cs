using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RealTimeAuction.Application.Common.Interfaces;
using RealTimeAuction.Domain.Constants;
using RealTimeAuction.Infrastructure.BackgroundJobs;
using RealTimeAuction.Infrastructure.Data;
using RealTimeAuction.Infrastructure.Data.Interceptors;
using RealTimeAuction.Infrastructure.Identity;
using RealTimeAuction.Infrastructure.Messaging;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        Guard.Against.Null(
            connectionString,
            message: "Connection string 'DefaultConnection' not found."
        );

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        builder.Services.AddScoped<AuditInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>(
            (sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseNpgsql(connectionString);
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning)
                );
            }
        );

        builder.Services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>()
        );

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder
            .Services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator))
        );

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetSection("Redis:Configuration").Value;
            options.InstanceName = builder.Configuration.GetSection("Redis:InstanceName").Value;
        });

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<AuctionWonEventConsumer>();

            x.UsingRabbitMq(
                (context, cfg) =>
                {
                    var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");
                    cfg.Host(
                        rabbitConfig["Host"] ?? "localhost",
                        "/",
                        h =>
                        {
                            h.Username(rabbitConfig["Username"] ?? "guest");
                            h.Password(rabbitConfig["Password"] ?? "guest");
                        }
                    );

                    cfg.ReceiveEndpoint(
                        "email-queue",
                        e =>
                        {
                            e.ConfigureConsumer<AuctionWonEventConsumer>(context);
                        }
                    );

                    cfg.ConfigureEndpoints(context);
                }
            );
        });

        builder.Services.AddTransient<IMessagePublisher, RabbitMQMessagePublisher>();
        builder.Services.AddHostedService<AuctionStatusWorker>();
    }
}
