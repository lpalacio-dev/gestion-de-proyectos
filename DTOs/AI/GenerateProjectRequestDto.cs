using System.ComponentModel.DataAnnotations;

namespace gestion_de_proyectos.DTOs.AI
{
    /// <summary>
    /// DTO de entrada para solicitar la generación de un proyecto con IA.
    /// El usuario describe en lenguaje natural lo que quiere construir.
    /// </summary>
    public class GenerateProjectRequestDto
    {
        /// <summary>
        /// Descripción en lenguaje natural del proyecto que se quiere generar.
        /// Mínimo 20 caracteres, máximo 2000.
        /// Ejemplo: "Quiero crear una tienda online para vender ropa con carrito de compras y pagos con Stripe."
        /// </summary>
        [Required(ErrorMessage = "La descripción es requerida.")]
        [MinLength(20, ErrorMessage = "La descripción debe tener al menos 20 caracteres.")]
        [MaxLength(2000, ErrorMessage = "La descripción no debe superar los 2000 caracteres.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Idioma preferido para la respuesta de la IA.
        /// Si no se especifica, la IA detecta el idioma de la descripción automáticamente.
        /// Ejemplos: "es", "en", "fr"
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Número máximo de tareas que la IA puede sugerir.
        /// Por defecto 10. Máximo 20.
        /// </summary>
        [Range(1, 20, ErrorMessage = "El número máximo de tareas debe estar entre 1 y 20.")]
        public int MaxTasks { get; set; } = 10;

        /// <summary>
        /// Nivel de detalle en las descripciones de tareas generadas.
        /// "brief" = descripciones cortas, "detailed" = con criterios de aceptación.
        /// Por defecto "brief".
        /// </summary>
        public string DetailLevel { get; set; } = "brief";
    }
}
