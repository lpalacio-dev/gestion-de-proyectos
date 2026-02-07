using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestion_de_proyectos.Models
{
    public enum TaskStatus
    {
        Pending,        
        InProgress,     
        Completed       
    }
    public enum TaskPriority
    {
        Low,            
        Medium,         
        High            
    }
    public class Task
    {
        // Columna: id_tarea (PK)
        [Key]
        public Guid Id { get; set; } // Using Guid for UUID as suggested

        // Columna: titulo (NOT NULL)
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        // Columna: descripcion (Text, nullable by default)
        public string? Description { get; set; }

        // Columna: estado (Varchar, NOT NULL) - Using string to match Varchar, but consider using the TaskStatus enum for type safety.
        [Required]
        public string Status { get; set; } = TaskStatus.Pending.ToString();

        // Columna: prioridad (Varchar, nullable by default) - Using string to match Varchar.
        public string? Priority { get; set; } // Example: TaskPriority.Medium.ToString();

        // Columna: fecha_limite (Date, nullable by default)
        public DateTime? DueDate { get; set; }

        // Columna: id_proyecto (FK, NOT NULL)
        [Required]
        [ForeignKey(nameof(Project))]
        public Guid ProjectId { get; set; }

        // Navigation Property: Relación N:1 con Project (El proyecto al que pertenece)
        public Project Project { get; set; } = null!;

        // Columna: id_asignado_a (FK, nullable)
        [ForeignKey(nameof(AssignedUser))]
        public string? AssignedToId { get; set; } // ¡Cambiado de Guid? a string?!

        // Navigation Property: Relación N:1 con ApplicationUser (El usuario responsable/asignado)
        public ApplicationUser? AssignedUser { get; set; } // ¡Cambiado de User a ApplicationUser!
    }

}
