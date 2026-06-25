using Microsoft.EntityFrameworkCore;
using languagetutor.Models;

namespace languagetutor.Data;

public static class ConfiguredSeedData
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration configuration)
    {
        await SeedConfiguredAdminAsync(db, configuration);
    }

    private static async Task SeedConfiguredAdminAsync(AppDbContext db, IConfiguration configuration)
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (admin == null)
        {
            db.Users.Add(new User
            {
                Email = normalizedEmail,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Name = configuration["Seed:AdminName"] ?? "Selenium Admin",
                Role = "ADMIN",
                PhoneNumber = "0900000000",
                Address = "Test Automation",
                DateOfBirth = new DateOnly(1995, 1, 1),
                Gender = "OTHER",
                LanguagePreference = "EN",
                SkillLevel = "Advanced",
                LearningGoal = "Automation testing"
            });
        }
        else
        {
            admin.Role = "ADMIN";
            admin.Password = BCrypt.Net.BCrypt.HashPassword(password);
            admin.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }
}
