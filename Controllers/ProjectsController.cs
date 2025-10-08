using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Services;

namespace gestion_de_proyectos.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        // ------------------------------------------------------------------
        // POST: api/projects
        // CREAR un nuevo proyecto
        // ------------------------------------------------------------------
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Para OwnerId no encontrado
        public async Task<IActionResult> CreateProject([FromBody] ProjectCreationDto projectDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdProject = await _projectService.CreateProjectAsync(projectDto);

            // Verificamos si la creación falló (ej: OwnerId no existe, error de DB)
            if (createdProject == null)
            {
                // Devolvemos 404 si el OwnerId no fue encontrado (lógica del Service)
                if (projectDto.OwnerId != Guid.Empty)
                {
                    // Asumimos que si no se pudo crear, es por el OwnerId si no se puede determinar la causa exacta
                    return NotFound(new { message = $"Usuario Propietario con ID '{projectDto.OwnerId}' no encontrado." });
                }
                return BadRequest(new { message = "No se pudo crear el proyecto debido a un error de validación o servidor." });
            }

            // Éxito: 201 Created y devolvemos la respuesta del DTO
            return CreatedAtAction(nameof(GetProjectById), new { id = createdProject.Id }, createdProject);
        }


        // ------------------------------------------------------------------
        // GET: api/projects/{id}
        // OBTENER un proyecto por ID
        // ------------------------------------------------------------------
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectById(Guid id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);

            if (project == null)
            {
                return NotFound($"Proyecto con ID '{id}' no encontrado.");
            }

            return Ok(project);
        }

        // ------------------------------------------------------------------
        // GET: api/projects
        // OBTENER todos los proyectos
        // ------------------------------------------------------------------
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetAllProjects()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            return Ok(projects);
        }


        // ------------------------------------------------------------------
        // PUT: api/projects/{id}
        // ACTUALIZAR un proyecto
        // ------------------------------------------------------------------
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProject(Guid id, [FromBody] ProjectUpdateDto projectDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _projectService.UpdateProjectAsync(id, projectDto);

            if (!success)
            {
                // Una forma simple de distinguir la causa del fallo
                var existingProject = await _projectService.GetProjectByIdAsync(id);
                if (existingProject == null)
                {
                    return NotFound($"Proyecto con ID '{id}' no encontrado.");
                }

                // Aquí podemos generalizar el error de lógica de negocio (ej: estado inválido, nuevo OwnerId inválido)
                return BadRequest("No se pudo actualizar el proyecto. Verifique el formato de los datos (ej: Status, OwnerId).");
            }

            return Ok(new { message = "Proyecto actualizado correctamente." });
        }

        // ------------------------------------------------------------------
        // DELETE: api/projects/{id}
        // ELIMINAR un proyecto
        // ------------------------------------------------------------------
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            var success = await _projectService.DeleteProjectAsync(id);

            if (!success)
            {
                return NotFound($"Proyecto con ID '{id}' no encontrado.");
            }

            // Éxito: 204 No Content (la respuesta estándar para DELETE exitoso sin cuerpo)
            return NoContent();
        }
    }
}
