namespace gestion_de_proyectos.DTOs
{
    // Usado para la respuesta (GET, POST, PUT)
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }
}
