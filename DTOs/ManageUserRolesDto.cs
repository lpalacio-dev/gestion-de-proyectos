using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs
{
    public class ManageUserRolesDto
    {
        [Required(ErrorMessage = "Los roles son requeridos")]
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}

