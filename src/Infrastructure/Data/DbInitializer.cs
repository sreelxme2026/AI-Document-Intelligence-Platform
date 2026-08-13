using Application.Entities;
using Application.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Application.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        var adminSettings = services
    .GetRequiredService<IOptions<AdminSettings>>()
    .Value;

        var documentUserSettings = services
    .GetRequiredService<IOptions<DocumentUserSettings>>()
    .Value;

        await context.Database.MigrateAsync();

        foreach (var role in Enum.GetValues<UserRole>())
        {
            var roleName = role.ToString();

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(
                    new IdentityRole<Guid>
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant()
                    });
            }
        }

        var adminUser = await userManager.FindByEmailAsync(adminSettings.Email);

        if (adminUser is null)
        {
            adminUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = adminSettings.Email,
                Email = adminSettings.Email,
                EmailConfirmed = true,
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(
                adminUser,
                adminSettings.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Failed to create the initial admin user: {errors}");
            }

            await userManager.AddToRoleAsync(
                adminUser,
                UserRole.Admin.ToString());
        }

        var documentUser = await userManager.FindByEmailAsync(
    documentUserSettings.Email);

        if (documentUser is null)
        {
            documentUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = documentUserSettings.Email,
                Email = documentUserSettings.Email,
                EmailConfirmed = true,
                Role = UserRole.DocumentUser,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(
                documentUser,
                documentUserSettings.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Failed to create the initial document user: {errors}");
            }

            await userManager.AddToRoleAsync(
                documentUser,
                UserRole.DocumentUser.ToString());
        }
    }
}