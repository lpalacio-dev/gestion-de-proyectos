using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{
    public class RegisterRequestDto
    {
        [Required]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
