using System.Text;
using System.Threading.RateLimiting;

using EstateFlow.Api.Controllers;
using EstateFlow.Api.Security;
using EstateFlow.Api.Services;
using EstateFlow.Api.Services.Audit;
using EstateFlow.Api.Services.Auth;
using EstateFlow.Api.Services.Correspondence;
using EstateFlow.Api.Services.Letters;
using EstateFlow.Api.Services.Notifications;
using EstateFlow.Api.Services.Storage;
using EstateFlow.Core.Interfaces;
using EstateFlow.Core.Models;
using EstateFlow.Core.Workflow;
using EstateFlow.Data.Context;
using EstateFlow.Data.Repositories;
using EstateFlow.Data.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// DATABASE
// ======================================================

// Secrets come from Azure App Service environment variables (or user-secrets
// locally) — never from appsettings.json. Fail fast with a clear message.
var connectionString =
    builder.Configuration.GetConnectionString("EstateFlowDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is missing. Set 'ConnectionStrings__EstateFlowDb' " +
        "in the App Service environment variables (or user-secrets for local development).");
}

// Encryption at rest for ID numbers and account references (POPIA).
// Set DataProtection__FieldKey to a base64 32-byte key to switch it on.
// KEEP A SECURE COPY OF THIS KEY: without it the encrypted values are lost.
var fieldProtector =
    AesGcmFieldProtector.FromBase64Key(
        builder.Configuration["DataProtection:FieldKey"]);

if (fieldProtector is not null)
{
    builder.Services.AddSingleton<IFieldProtector>(fieldProtector);
}

builder.Services.AddDbContext<EstateFlowDbContext>(options =>
    options.UseSqlServer(connectionString));

// ======================================================
// IDENTITY
// ======================================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;

        // Lock an account for 15 minutes after 5 wrong passwords or 2FA codes.
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<EstateFlowDbContext>()
    .AddDefaultTokenProviders();

// ======================================================
// JWT AUTHENTICATION
// ======================================================

var jwtKey =
    builder.Configuration["Jwt:Key"];

// HMAC-SHA256 needs at least 256 bits. Anyone holding this key can mint a
// valid login token for any user, so it must be random and kept out of source.
if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key is missing or too short. Set 'Jwt__Key' to a random value of " +
        "at least 32 bytes (e.g. 'openssl rand -base64 48') in the App Service environment variables.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer =
                builder.Configuration["Jwt:Issuer"],

            ValidAudience =
                builder.Configuration["Jwt:Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey))
        };

    // Tokens last a working day, so check on each request that the account
    // hasn't been deactivated by an administrator in the meantime.
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userManager = context.HttpContext.RequestServices
                .GetRequiredService<UserManager<ApplicationUser>>();

            var user = userId is null ? null : await userManager.FindByIdAsync(userId);

            if (user is null || AdminController.IsDeactivated(user))
            {
                context.Fail("This account is no longer active.");
            }
        }
    };
});

// ======================================================
// AUTHORIZATION
// ======================================================

builder.Services.AddAuthorization();

// ======================================================
// RATE LIMITING (sign-in endpoints)
// ======================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = RateLimitPolicies.AuthPermitLimit,
                Window = RateLimitPolicies.AuthWindow,
                QueueLimit = 0
            }));
});

// ======================================================
// REPOSITORIES
// ======================================================

builder.Services.AddScoped<
    IEstateRepository,
    EstateRepository>();

// ======================================================
// ESTATE SERVICES
// ======================================================

builder.Services.AddScoped<
    IEstateService,
    EstateService>();

builder.Services.AddScoped<TokenService>();

builder.Services.AddSingleton(TimeProvider.System);

// Stage checklist deadlines (confirm the periods with the firm).
builder.Services.Configure<MasterProcessOptions>(
    builder.Configuration.GetSection(MasterProcessOptions.SectionName));

builder.Services.AddScoped<ICorrespondenceLog, CorrespondenceLog>();

// ======================================================
// AZURE BLOB STORAGE
// ======================================================

builder.Services.AddScoped<
    IBlobStorageService,
    AzureBlobStorageService>();

// ======================================================
// DESIGN PATTERN SERVICES
// ======================================================

builder.Services.AddSingleton<
    LetterGeneratorFactory>();

builder.Services.Configure<FirmOptions>(
    builder.Configuration.GetSection(FirmOptions.SectionName));

builder.Services.AddScoped<ILetterTemplateStore, LetterTemplateStore>();
builder.Services.AddScoped<LetterService>();

builder.Services.AddHttpClient<
    INotificationService,
    SendGridNotificationService>();

// ======================================================
// CONTROLLERS
// ======================================================

builder.Services.AddScoped<EstateOwnershipFilter>();
builder.Services.AddScoped<AuditFilter>();

builder.Services.AddControllers(options =>
{
    // Registered first so it wraps the ownership check and also records
    // refused attempts to open someone else's estate.
    options.Filters.AddService<AuditFilter>();

    // Every action with an {estateId} route value is restricted to the
    // Estate Controller the estate is assigned to.
    options.Filters.AddService<EstateOwnershipFilter>();
});

// ======================================================
// SWAGGER
// ======================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "EstateFlow API",
            Version = "v1",
            Description =
                "EstateFlow Estate Administration API"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Enter your JWT bearer token."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,

                            Id = "Bearer"
                        }
                },

                Array.Empty<string>()
            }
        });
});

// ======================================================
// BUILD
// ======================================================

var app = builder.Build();

// ======================================================
// GLOBAL API ERROR HANDLING
// Must be early in the pipeline.
// Unhandled exceptions are logged in full but the client only gets a generic
// message, so connection strings, SQL or stack traces never leave the server.
// ======================================================

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error =
            context.Features.Get<IExceptionHandlerFeature>()?.Error;

        app.Logger.LogError(
            error,
            "Unhandled exception for {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new
        {
            message = "Something went wrong on the EstateFlow server. Please try again, " +
                      "and contact support if the problem continues.",
            traceId = context.TraceIdentifier
        });
    });
});

// ======================================================
// SEED IDENTITY ROLES
// ======================================================

using (var scope = app.Services.CreateScope())
{
    await RoleSeeder.SeedRolesAsync(
        scope.ServiceProvider);

    await AdminBootstrapper.EnsureAdministratorAsync(
        scope.ServiceProvider);

    if (fieldProtector is not null)
    {
        var encrypted = await FieldEncryptionBackfill.RunAsync(
            scope.ServiceProvider.GetRequiredService<EstateFlowDbContext>());

        if (encrypted > 0)
            app.Logger.LogInformation("Encrypted {Count} previously unencrypted values.", encrypted);
    }
    else
    {
        app.Logger.LogWarning(
            "DataProtection:FieldKey is not set — ID numbers and account references are stored unencrypted.");
    }
}

// ======================================================
// SWAGGER
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "EstateFlow API v1");
    });
}

// ======================================================
// HTTP / HTTPS
// ======================================================


if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Executor / heir progress portal (static page in wwwroot/portal).
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/portal"))
    {
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
    }

    await next();
});

app.MapGet("/portal", () => Results.Redirect("/portal/index.html"));
app.UseStaticFiles();

// ======================================================
// AUTHENTICATION
// ======================================================

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

// ======================================================
// CONTROLLERS
// ======================================================

app.MapControllers();

// ======================================================
// RUN
// ======================================================

app.Run();
// Exposes the entry point to the integration tests (WebApplicationFactory<Program>).
public partial class Program { }
