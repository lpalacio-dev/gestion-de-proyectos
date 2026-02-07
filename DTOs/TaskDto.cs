namespace gestion_de_proyectos.DTOs
{
    public class TaskDto
    {
        // Id (Guid)
        public Guid Id { get; set; }

        // Title (string)
        public string Title { get; set; } = string.Empty;

        // Description (string?)
        public string? Description { get; set; }

        // Status (string)
        public string Status { get; set; } = string.Empty;

        // Priority (string?)
        public string? Priority { get; set; }

        // DueDate (DateTime?)
        public DateTime? DueDate { get; set; }

        // ProjectId (Guid) - ID del proyecto al que pertenece
        public Guid ProjectId { get; set; }

        // AssignedToId (string?) - ID del ApplicationUser asignado
        public string? AssignedToId { get; set; }

        // Opcional: Nombre del usuario asignado
        public string? AssignedToName { get; set; }
    }
}
