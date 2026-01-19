using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RealTimeAuction.Web;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace RealTimeAuction.Infrastructure.IntegrationTests;

public abstract class IntegrationTestBase
{
    private PostgreSqlContainer _postgresContainer;
    private RedisContainer _redisContainer;
    private RabbitMqContainer _rabbitMqContainer;

    protected WebApplicationFactory<Program> Factory;
    protected HttpClient Client;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
#pragma warning disable CS0618
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("auction_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _redisContainer = new RedisBuilder().WithImage("redis:7-alpine").Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .Build();
#pragma warning restore CS0618

        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitMqContainer.StartAsync()
        );

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration(
                (context, config) =>
                {
                    var settings = new Dictionary<string, string>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            _postgresContainer.GetConnectionString(),
                        ["Redis:Configuration"] = _redisContainer.GetConnectionString(),
                        ["RabbitMQ:Host"] = _rabbitMqContainer.Hostname,
                        ["RabbitMQ:Port"] = _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
                        ["RabbitMQ:Username"] = "guest",
                        ["RabbitMQ:Password"] = "guest",
                    };

                    config.AddInMemoryCollection(settings!);
                }
            );

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(RealTimeAuction.Application.Common.Interfaces.IUser)
                );
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddScoped<RealTimeAuction.Application.Common.Interfaces.IUser, TestUser>();
            });
        });

        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var userManager =
            scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<RealTimeAuction.Infrastructure.Identity.ApplicationUser>>();
        var user = new RealTimeAuction.Infrastructure.Identity.ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser",
            Email = "test@example.com",
        };

        if (await userManager.FindByIdAsync(user.Id) == null)
        {
            var result = await userManager.CreateAsync(user, "Password123!");
            if (!result.Succeeded)
            {
                throw new Exception(
                    $"Failed to seed test user: {string.Join(", ", result.Errors.Select(e => e.Description))}"
                );
            }

            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await userManager.ConfirmEmailAsync(user, code);
        }
        var loginResponse = await Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=true",
            new { email = "test@example.com", password = "Password123!" }
        );

        if (!loginResponse.IsSuccessStatusCode)
        {
            var content = await loginResponse.Content.ReadAsStringAsync();
            throw new Exception(
                $"Login failed with status {loginResponse.StatusCode} and content: {content}"
            );
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        Client?.Dispose();
        Factory?.Dispose();

        if (_postgresContainer != null)
            await _postgresContainer.DisposeAsync();
        if (_redisContainer != null)
            await _redisContainer.DisposeAsync();
        if (_rabbitMqContainer != null)
            await _rabbitMqContainer.DisposeAsync();
    }
}

public class TestUser : RealTimeAuction.Application.Common.Interfaces.IUser
{
    public string? Id => "test-user-id";
    public List<string>? Roles => new() { "TestRole" };
}
