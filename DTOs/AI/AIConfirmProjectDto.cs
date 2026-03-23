using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs.AI
{
    /// <summary>
    /// DTO que el usuario envía para confirmar y persistir el proyecto sugerido por la IA.
    /// El usuario puede haber editado el nombre, descripción, estado o la lista de tareas.
    /// Solo las tareas incluidas en SelectedTasks serán creadas en la base de datos.
    /// </summary>
    public class AIConfirmProjectDto
    {
        // ============================================================================
        // DATOS DEL PROYECTO
        // ============================================================================

        /// <summary>
        /// Nombre final del proyecto (puede diferir del sugerido por la IA si el usuario lo editó).
        /// </summary>
        [Required(ErrorMessage = "El nombre del proyecto es requerido.")]
        [MaxLength(255, ErrorMessage = "El nombre no debe superar 255 caracteres.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Descripción final del proyecto.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Estado inicial del proyecto.
        /// Valores válidos: "InProgress", "Completed", "OnHold", "Archived"
        /// </summary>
        [Required(ErrorMessage = "El estado es requerido.")]
        public string Status { get; set; } = "OnHold";

        // ============================================================================
        // TAREAS SELECCIONADAS
        // ============================================================================

        /// <summary>
        /// Lista de tareas que el usuario quiere crear.
        /// El usuario puede haber eliminado, reordenado o editado las tareas sugeridas por la IA.
        /// Si la lista está vacía, solo se crea el proyecto sin tareas.
        /// </summary>
        public List<AIConfirmTaskDto> SelectedTasks { get; set; } = new();
    }

    /// <summary>
    /// Representa una tarea confirmada por el usuario para ser persistida.
    /// Es la versión "editable" de AIGeneratedTaskDto.
    /// </summary>
    public class AIConfirmTaskDto
    {
        /// <summary>
        /// Título de la tarea (posiblemente editado por el usuario).
        /// </summary>
        [Required(ErrorMessage = "El título de la tarea es requerido.")]
        [MaxLength(255, ErrorMessage = "El título no debe superar 255 caracteres.")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Descripción de la tarea (posiblemente editada por el usuario).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Prioridad de la tarea.
        /// Valores válidos: "Low", "Medium", "High"
        /// </summary>
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// Días desde hoy hasta el vencimiento de esta tarea.
        /// Null para sin fecha límite.
        /// </summary>
        public int? DueDateOffsetDays { get; set; }
    }
}
