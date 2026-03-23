namespace gestion_de_proyectos.DTOs.AI
{
    /// <summary>
    /// Representa una tarea individual sugerida por la IA.
    /// Es parte de AIGeneratedProjectDto y también se usa en SuggestTasksForProjectAsync.
    /// NO ha sido persistida aún en la base de datos.
    /// </summary>
    public class AIGeneratedTaskDto
    {
        /// <summary>
        /// Título conciso de la tarea.
        /// Ejemplo: "Diseñar esquema de base de datos"
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada de la tarea.
        /// En modo "brief": 1-2 oraciones. En modo "detailed": incluye criterios de aceptación.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Prioridad sugerida por la IA.
        /// Valores válidos: "Low", "Medium", "High"
        /// </summary>
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// Días estimados desde la fecha de inicio del proyecto hasta el vencimiento de esta tarea.
        /// Null si la IA no puede estimar una fecha razonable.
        /// Ejemplo: 7 significa "vence en 7 días desde el inicio".
        /// </summary>
        public int? DueDateOffsetDays { get; set; }

        /// <summary>
        /// Orden lógico de esta tarea dentro del proyecto (1 = primera a hacer).
        /// La IA ordena las tareas por dependencia lógica.
        /// </summary>
        public int OrderIndex { get; set; }
    }
}
