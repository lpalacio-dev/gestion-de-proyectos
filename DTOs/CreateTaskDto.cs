namespace gestion_de_proyectos.DTOs
{
    public class CreateTaskDto
    {
        // Title (string, Requerido)
        public string Title { get; set; } = string.Empty;

        // Description (string?)
        public string? Description { get; set; }

        // Priority (string?)
        public string? Priority { get; set; }

        // DueDate (DateTime?)
        public DateTime? DueDate { get; set; }

        // AssignedToId (string?, Opcional) - ID del usuario al que se asignará (si se conoce)
        public string? AssignedToId { get; set; }
    }
}
