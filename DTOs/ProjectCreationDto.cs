using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{
    public class ProjectCreationDto
    {
        [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // OwnerId es requerido para asignar el proyecto al usuario que lo crea.
        [Required(ErrorMessage = "El ID del propietario es obligatorio.")]
        public Guid OwnerId { get; set; }
    }
}
