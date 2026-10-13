using System.Net.Http.Headers;
using System.Net.Http.Json;
using EstateFlow.Api.Services.Notifications;
using EstateFlow.Api.Services.Storage;
using EstateFlow.Core.Dtos;
using EstateFlow.Data.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace EstateFlow.Api.Tests;

// Runs the real API pipeline (auth, filters, controllers, Identity) against an
// in-memory database instead of Azure SQL.
public class EstateFlowApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "AdminPassw0rd!";

    private readonly string _databaseName = $"EstateFlowTests-{Guid.NewGuid()}";

    public FakeNotificationService Emails { get; } = new();
    public InMemoryBlobStorage Storage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting is applied before Program.cs reads configuration.
        builder.UseSetting("ConnectionStrings:EstateFlowDb", "Server=unused-in-tests");
        builder.UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long!!");
        builder.UseSetting("Bootstrap:AdminEmail", AdminEmail);
        builder.UseSetting("Bootstrap:AdminPassword", AdminPassword);
        builder.UseSetting("Bootstrap:AdminFullName", "Test Administrator");

        builder.ConfigureServices(services =>
        {
            var sqlOptions = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<EstateFlowDbContext>))
                .ToList();

            foreach (var descriptor in sqlOptions)
                services.Remove(descriptor);

            services.AddDbContext<EstateFlowDbContext>(options =>
                options
                    .UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            services.AddSingleton<INotificationService>(Emails);
            services.AddScoped<IBlobStorageService>(_ => Storage);
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.Token);

        return client;
    }

    public async Task<HttpClient> CreateControllerClientAsync(string email, string password = "ControllerPassw0rd")
    {
        var admin = await CreateAuthenticatedClientAsync(AdminEmail, AdminPassword);

        var register = await admin.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            FullName = email,
            Role = "EstateController"
        });
        register.EnsureSuccessStatusCode();

        return await CreateAuthenticatedClientAsync(email, password);
    }

    public static async Task<Guid> CreateEstateAsync(HttpClient client, string fileNumber)
    {
        var response = await client.PostAsJsonAsync("api/estates", new CreateEstateRequest
        {
            FileNumber = fileNumber,
            DateOfDeath = new DateTime(2026, 1, 15),
            DeceasedFullName = "Test Deceased",
            DeceasedIdNumber = "8001015009087"
        });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return body!["estateId"];
    }
}
