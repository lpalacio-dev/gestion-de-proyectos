using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        // Propiedades de Navegación (Deben referenciar a ApplicationUser en lugar del antiguo User)
        public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();

        public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();

        // Navigation Property: Colección de membresías de proyectos(relación N:M)
        public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
    }
}
