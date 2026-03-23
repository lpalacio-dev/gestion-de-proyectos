namespace gestion_de_proyectos.DTOs.AI
{
    /// <summary>
    /// Representa la sugerencia completa de proyecto generada por la IA.
    /// Este DTO se retorna al frontend para que el usuario revise y edite ANTES de confirmar.
    /// NO representa un proyecto persistido en la base de datos.
    /// </summary>
    public class AIGeneratedProjectDto
    {
        /// <summary>
        /// Nombre sugerido del proyecto (máximo 8 palabras, conciso y descriptivo).
        /// Ejemplo: "Tienda Online con Carrito y Pagos Stripe"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Descripción completa del proyecto generada por la IA.
        /// Resume el alcance, objetivos y tecnologías sugeridas.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Estado inicial sugerido para el proyecto.
        /// Normalmente "OnHold" para proyectos recién creados o "InProgress" si ya hay trabajo.
        /// Valores válidos: "InProgress", "Completed", "OnHold", "Archived"
        /// </summary>
        public string Status { get; set; } = "OnHold";

        /// <summary>
        /// Lista de tareas sugeridas, ordenadas por dependencia lógica (primero lo primero).
        /// </summary>
        public List<AIGeneratedTaskDto> Tasks { get; set; } = new();

        /// <summary>
        /// Nombre del proveedor LLM que generó esta respuesta.
        /// Útil para logging y debugging en el frontend.
        /// Ejemplo: "Groq", "Cerebras", "Gemini", "OpenRouter"
        /// </summary>
        public string GeneratedByProvider { get; set; } = string.Empty;

        /// <summary>
        /// Indica si se activó el mecanismo de fallback para obtener esta respuesta.
        /// True si el proveedor primario falló y se usó uno secundario.
        /// </summary>
        public bool UsedFallback { get; set; }

        /// <summary>
        /// Timestamp de cuándo fue generada esta sugerencia.
        /// El frontend puede usar esto para invalidar sugerencias "viejas".
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
