namespace gestion_de_proyectos.DTOs
{
    public class TaskResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }

        // --- Relaciones ---

        // 1. Detalles del Proyecto
        public Guid ProjectId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty; // Incluye el título del proyecto

        // 2. Detalles del Usuario Asignado
        public Guid? AssignedToId { get; set; }
        // Se usa string? ya que AssignedToId es opcional (nullable)
        public string? AssignedToUsername { get; set; } // Incluye el nombre del usuario
    }
}

