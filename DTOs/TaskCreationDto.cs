using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{
    public class TaskCreationDto
    {
        [Required(ErrorMessage = "El título de la tarea es obligatorio.")]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "El estado de la tarea es obligatorio.")]
        // Se asegura que el valor sea uno de los definidos en TaskStatus, aunque se almacene como string
        public string Status { get; set; } = string.Empty;

        // No es requerido por el modelo, pero se puede enviar
        public string? Priority { get; set; }

        public DateTime? DueDate { get; set; }

        // *CLAVE FORÁNEA OBLIGATORIA*
        [Required(ErrorMessage = "El ID del proyecto es obligatorio para crear una tarea.")]
        public Guid ProjectId { get; set; }

        // *CLAVE FORÁNEA OPCIONAL*
        // Se usa Guid? para permitir null si la tarea no está asignada.
        public Guid? AssignedToId { get; set; }
    }
}
