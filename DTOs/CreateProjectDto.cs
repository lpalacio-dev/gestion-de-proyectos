namespace gestion_de_proyectos.DTOs
{
    public class CreateProjectDto
    {
        // Name (string, Requerido)
        public string Name { get; set; } = string.Empty;

        // Description (string?)
        public string? Description { get; set; }
    }
}
