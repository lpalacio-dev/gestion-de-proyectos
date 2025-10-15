using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        public required string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
