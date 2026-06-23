using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Product_Management.Models;

namespace Product_Management.Models
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(
            IServiceProvider service)
        {
            var roleManager =
                service.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                service.GetRequiredService<UserManager<ApplicationUser>>();

            var config =
                service.GetRequiredService<IConfiguration>();

            // =====================================
            // CREATE ROLES
            // =====================================

            foreach (var role in new[] { "Admin", "User" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // =====================================
            // CREATE DEFAULT ADMIN
            // =====================================

            string adminEmail =
                config["AdminSettings:Email"] ?? "admin@store.com";

            string adminPassword =
                config["AdminSettings:Password"] ?? "Admin123!";

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Admin",
                    EmailConfirmed = true  // ← important for production
                };

                var result = await userManager.CreateAsync(newAdmin, adminPassword);

                if (result.Succeeded)
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
            }
        }
    }
}