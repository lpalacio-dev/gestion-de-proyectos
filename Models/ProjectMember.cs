namespace gestion_de_proyectos.Models
{
    public class ProjectMember
    {
        // NOTA: Esta entidad no necesita su propia clave primaria 'Id' simple. 
        // Su clave primaria será compuesta por ProjectId y UserId.

        // Columna: ProjectId (FK, parte de la PK compuesta)
        public Guid ProjectId { get; set; }

        // Navigation Property: Relación N:1 con Project
        public Project Project { get; set; } = null!;

        // Columna: UserId (FK, parte de la PK compuesta)
        // Debe ser de tipo string para coincidir con ApplicationUser.Id
        public string UserId { get; set; } = string.Empty;

        // Navigation Property: Relación N:1 con ApplicationUser
        public ApplicationUser User { get; set; } = null!;

        // (Opcional) Propiedad para el rol del miembro en el proyecto
        public string Role { get; set; } = "Member"; // Ejemplo: "ProjectManager", "Member"

        // FASE 2: Fecha en que el usuario se unió al proyecto
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    }
}
