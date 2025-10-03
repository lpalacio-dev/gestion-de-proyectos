using gestion_de_proyectos.Models;
using Microsoft.EntityFrameworkCore;

namespace gestion_de_proyectos
{
    public class ProjectManagementContext : DbContext
    {
        public ProjectManagementContext(DbContextOptions<ProjectManagementContext> options)
       : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<gestion_de_proyectos.Models.Task> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
