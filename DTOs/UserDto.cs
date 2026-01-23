namespace gestion_de_proyectos.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime RegistrationDate { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}
