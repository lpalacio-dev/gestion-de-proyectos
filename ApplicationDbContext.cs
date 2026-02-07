using gestion_de_proyectos.Models;
using Microsoft.AspNetCore.Identity; // Necesario para IdentityUser
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace gestion_de_proyectos
{
    // Céntrate en esta línea: Ahora hereda de IdentityDbContext<IdentityUser>
    // Herencia Modificada: Ahora usa ApplicationUser, IdentityRole, y string para la clave primaria.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
       : base(options)
        {
        }

        // ¡NUEVO! DbSet para la entidad intermedia
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<gestion_de_proyectos.Models.Task> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Llamar a la base para configurar las tablas de Identity
            base.OnModelCreating(builder);

            // 1. Configurar la clave primaria compuesta para ProjectMember
            builder.Entity<ProjectMember>()
                .HasKey(pm => new { pm.ProjectId, pm.UserId });

            // 2. Configurar la relación N:M con Project
            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.ProjectMembers)
                .HasForeignKey(pm => pm.ProjectId);

            // 3. Configurar la relación N:M con ApplicationUser
            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId);
        }


    }
}
