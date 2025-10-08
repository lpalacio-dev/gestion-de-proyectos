using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{

    // Usado para la creación de un nuevo usuario (POST)
    public class UserCreationDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)] // Ejemplo de validación de contraseña
        public string Password { get; set; } = string.Empty;
    }

    // Usado para la actualización de un usuario (PUT/PATCH)
    public class UserUpdateDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        // El email puede no ser requerido si no se permite cambiar
        [EmailAddress]
        public string? Email { get; set; }

        // No se pide el PasswordHash, se actualizaría aparte o en el servicio
    }
}
