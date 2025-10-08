namespace gestion_de_proyectos.DTOs
{
    public class ProjectResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }

        // Detalle del propietario
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;

        // Conteo de tareas (útil para la interfaz)
        public int TaskCount { get; set; }


    }
}
