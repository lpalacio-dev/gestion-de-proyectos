using gestion_de_proyectos.Models;
using Microsoft.AspNetCore.Identity;
using Task = System.Threading.Tasks.Task;

namespace gestion_de_proyectos.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Crear roles
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    Console.WriteLine($"✓ Rol '{role}' creado.");
                }
            }

            // Crear usuario admin
            const string adminEmail = "admin@gestion.com";
            const string adminUsername = "admin";
            const string adminPassword = "Admin123!";

            var adminUser = await userManager.FindByNameAsync(adminUsername);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUsername,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    await userManager.AddToRoleAsync(adminUser, "User");
                    Console.WriteLine("✓ Usuario admin creado.");
                }
                else
                {
                    foreach (var error in result.Errors)
                        Console.WriteLine($"  - {error.Description}");
                }
            }
            else
            {
                var userRoles = await userManager.GetRolesAsync(adminUser);

                if (!userRoles.Contains("Admin"))
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                if (!userRoles.Contains("User"))
                    await userManager.AddToRoleAsync(adminUser, "User");

                Console.WriteLine("→ Usuario admin ya existe.");
            }
        }
    }
}