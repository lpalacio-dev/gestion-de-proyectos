namespace gestion_de_proyectos.DTOs
{
    public class UpdateProjectDto
    {
        // Name (string)
        public string Name { get; set; } = string.Empty;

        // Description (string?)
        public string? Description { get; set; }

        // Status (string) - Para permitir cambios de estado
        public string Status { get; set; } = string.Empty;
    }
}
