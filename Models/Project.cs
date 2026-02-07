using Microsoft.AspNetCore.Server.HttpSys;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestion_de_proyectos.Models
{
    public enum ProjectStatus
    {
        InProgress,
        Completed,
        OnHold,
        Archived
    }

    public class Project
    {
        // Columna: id_proyecto (PK)
        [Key]
        public Guid Id { get; set; } // Using Guid for UUID as suggested

        // Columna: nombre (NOT NULL)
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        // Columna: descripcion (Text, nullable by default)
        public string? Description { get; set; }

        [Required]
        public string Status { get; set; } = ProjectStatus.OnHold.ToString();

        // Columna: fecha_creacion (NOT NULL)
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        // Columna: id_propietario (FK, NOT NULL)
        [Required]
        [ForeignKey(nameof(Owner))]
        public string OwnerId { get; set; } // ¡Cambiado de Guid a string!

        // Navigation Property: Relación N:1 con ApplicationUser (El usuario propietario)
        public ApplicationUser Owner { get; set; } = null!; // ¡Cambiado de User a ApplicationUser!

        // Navigation Property: Relación 1:N con Task
        public ICollection<Task> Tasks { get; set; } = new List<Task>();

        // Navigation Property: Colección de miembros del proyecto (relación N:M)
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    }

}
