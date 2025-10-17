namespace gestion_de_proyectos.DTOs
{
    public class UpdateTaskDto
    {
        // Title (string)
        public string Title { get; set; } = string.Empty;

        // Description (string?)
        public string? Description { get; set; }

        // Status (string)
        public string Status { get; set; } = string.Empty; // Se puede modificar el estado

        // Priority (string?)
        public string? Priority { get; set; }

        // DueDate (DateTime?)
        public DateTime? DueDate { get; set; }

        // AssignedToId (string?, Opcional) - Se puede reasignar la tarea
        public string? AssignedToId { get; set; }
    }
}
