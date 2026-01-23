using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{
    public class UpdateProjectMemberRoleDto
    {
        /// <summary>
        /// Nuevo rol para el miembro del proyecto.
        /// Valores permitidos: "Member", "Admin"
        /// </summary>
        [Required(ErrorMessage = "El rol es requerido")]
        public string Role { get; set; } = string.Empty;
    }
}
