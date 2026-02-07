namespace gestion_de_proyectos.DTOs
{
    public class ProjectDto
    {
        // Id (Guid)
        public Guid Id { get; set; }

        // Name (string)
        public string Name { get; set; } = string.Empty;

        // Description (string?)
        public string? Description { get; set; }

        // Status (string) - Mantiene el valor del enum (ej. "OnHold")
        public string Status { get; set; } = string.Empty;

        // CreationDate (DateTime)
        public DateTime CreationDate { get; set; }

        // OwnerId (string) - ID del ApplicationUser propietario
        public string OwnerId { get; set; } = string.Empty;

        // Opcional: Para incluir detalles del propietario y miembros (según el plan)
        // Se recomienda usar DTOs anidados para esto (ej. UserDto)
        public string? OwnerName { get; set; } // Nombre para una respuesta más legible
        public int MembersCount { get; set; } // Conteo de miembros
    }
}
