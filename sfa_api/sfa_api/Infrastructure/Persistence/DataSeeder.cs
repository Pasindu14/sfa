using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedAdminUserAsync(db, logger);
    }

    private static async Task SeedAdminUserAsync(AppDbContext db, ILogger logger)
    {
        var adminExists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Role == UserRole.Admin);

        if (adminExists) return;

        var admin = new User
        {
            Name = "System Admin",
            Username = "admin",
            Email = "admin@sfa.com",
            Phone = "+94000000000",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedBy = null,
            UpdatedBy = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        logger.LogInformation("Admin user seeded — username: admin / password: Admin@1234");
    }
}
