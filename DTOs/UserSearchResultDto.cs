namespace gestion_de_proyectos.DTOs
{
    public class UserSearchResultDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
