using EstateFlow.Core.Models;
using EstateFlow.Data.Context;
using Microsoft.AspNetCore.Identity;

namespace EstateFlow.Api.Services.Auth;

// Registration is restricted to Firm Administrators, so the very first
// administrator has to come from somewhere. On start-up, if no
// FirmAdministrator exists and Bootstrap:AdminEmail is configured:
//   - an existing account with that email is promoted to FirmAdministrator, or
//   - a new account is created (Bootstrap:AdminPassword + AdminFullName required).
// Once an administrator exists this does nothing; remove the Bootstrap__*
// settings from Azure after the first successful start.
public static class AdminBootstrapper
{
    private const string AdminRole = "FirmAdministrator";

    public static async Task EnsureAdministratorAsync(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<EstateFlowDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(AdminBootstrapper));

        var email = configuration["Bootstrap:AdminEmail"]?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return;

        if ((await userManager.GetUsersInRoleAsync(AdminRole)).Count > 0)
            return;

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            var password = configuration["Bootstrap:AdminPassword"];
            var fullName = configuration["Bootstrap:AdminFullName"]?.Trim();

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
            {
                logger.LogWarning(
                    "Bootstrap:AdminEmail is set but no account exists for it. " +
                    "Set Bootstrap:AdminPassword and Bootstrap:AdminFullName to create one.");
                return;
            }

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                logger.LogError(
                    "Could not create the bootstrap administrator: {Errors}",
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (user.ControllerId is null)
        {
            var controllerRecord = new EstateController
            {
                StaffName = user.FullName,
                Role = AdminRole,
                Email = email
            };
            context.Controllers.Add(controllerRecord);
            await context.SaveChangesAsync();

            user.ControllerId = controllerRecord.ControllerId;
            await userManager.UpdateAsync(user);
        }

        await userManager.AddToRoleAsync(user, AdminRole);

        logger.LogWarning(
            "Bootstrap administrator account {UserId} is now a FirmAdministrator. " +
            "Remove the Bootstrap__* settings.",
            user.Id);
    }
}
