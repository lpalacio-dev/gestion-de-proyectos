using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{
    public class UpdateUserProfileDto
    {
        [EmailAddress(ErrorMessage = "Email no válido")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Número de teléfono no válido")]
        public string? PhoneNumber { get; set; }
    }
}
