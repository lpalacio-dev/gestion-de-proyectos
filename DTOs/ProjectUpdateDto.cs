using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{
    public class ProjectUpdateDto
    {
        [MaxLength(255)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        // Permitimos actualizar el estado (usamos el Enum para la validación).
        public string? Status { get; set; } // Ejemplo: "Completed", "InProgress"

        // Opcional: Si permitimos reasignar el proyecto.
        public Guid? OwnerId { get; set; }
    }
}
