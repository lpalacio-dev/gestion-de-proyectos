using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{
    public class AddProjectMemberDto
    {
        /// <summary>
        /// ID del usuario a agregar al proyecto.
        /// Puede ser el UserId (string) o el Username.
        /// </summary>
        [Required(ErrorMessage = "El UserId o Username es requerido")]
        public string UserIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// Rol que tendrá el usuario en el proyecto.
        /// Valores permitidos: "Member", "Admin" (por ahora)
        /// </summary>
        public string Role { get; set; } = "Member";
    }
}
