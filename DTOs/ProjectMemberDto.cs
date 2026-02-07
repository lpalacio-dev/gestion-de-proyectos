namespace gestion_de_proyectos.DTOs
{
    public class ProjectMemberDto
    {
        // Información del Proyecto
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        // Información del Usuario
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }

        // Rol en el Proyecto
        public string Role { get; set; } = string.Empty;

        // Fecha de ingreso
        public DateTime JoinedDate { get; set; }
    }
}
