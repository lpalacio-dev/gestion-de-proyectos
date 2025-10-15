using gestion_de_proyectos.Models;
using Microsoft.AspNetCore.Identity; // Necesario para IdentityUser
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace gestion_de_proyectos
{
    // Céntrate en esta línea: Ahora hereda de IdentityDbContext<IdentityUser>
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
       : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<gestion_de_proyectos.Models.Task> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
