using InnoviaHub.DataAccess.Entities;
using InnoviaHub.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace InnoviaHub.Api.Data;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(
    RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Enum.GetValues<UserRoles>())
        {
            var roleName = role.ToString();

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }
    }

    public static async Task SeedAdminAsync(
    UserManager<User> userManager,
    IConfiguration configuration)
    {
        var email = configuration["ADMIN_EMAIL"];
        var password = configuration["ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            // ADMIN_PASSWORD kan ha ändrats efter att kontot skapades, så synka lösenordet
            await SyncAdminAsync(userManager, existingUser, password);
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = "Admin",
            LastName = "User"
        };

        var result = await userManager.CreateAsync(admin, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description)
            );

            throw new InvalidOperationException(errors);
        }

        await userManager.AddToRoleAsync(
            admin,
            UserRoles.Admin.ToString()
        );
    }

    private static async Task SyncAdminAsync(
    UserManager<User> userManager,
    User admin,
    string password)
    {
        if (!await userManager.IsInRoleAsync(admin, UserRoles.Admin.ToString()))
        {
            await userManager.AddToRoleAsync(admin, UserRoles.Admin.ToString());
        }

        if (await userManager.CheckPasswordAsync(admin, password))
            return;

        // Validera innan det gamla lösenordet tas bort, så att kontot aldrig blir utan lösenord
        foreach (var validator in userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(userManager, admin, password);

            if (!validation.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    validation.Errors.Select(x => x.Description)
                );

                throw new InvalidOperationException(errors);
            }
        }

        admin.PasswordHash = userManager.PasswordHasher.HashPassword(admin, password);

        var result = await userManager.UpdateAsync(admin);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description)
            );

            throw new InvalidOperationException(errors);
        }
    }
}