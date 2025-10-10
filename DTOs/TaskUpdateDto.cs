using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{
    public class TaskUpdateDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public string? Priority { get; set; }

        public DateTime? DueDate { get; set; }

        // Se permiten actualizar las relaciones, pero son opcionales
        public Guid? ProjectId { get; set; }

        public Guid? AssignedToId { get; set; }
    }
}
