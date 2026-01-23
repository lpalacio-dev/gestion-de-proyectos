namespace gestion_de_proyectos.DTOs
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public DateTime RegistrationDate { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();

        // Estadísticas
        public int OwnedProjectsCount { get; set; }
        public int MemberProjectsCount { get; set; }
        public int AssignedTasksCount { get; set; }
    }
}
