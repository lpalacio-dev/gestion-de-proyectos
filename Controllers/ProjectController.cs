// Controllers/ProjectController.cs
using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gestion_de_proyectos.Controllers
{
    // Seguridad de Roles: Autoriza a usuarios con roles de Admin, ProjectManager o Member
    [Authorize(Roles = "Admin, ProjectManager, Member")]
    [Route("api/projects")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        // GET: api/projects
        // La autorización a nivel de recurso (mostrar solo los propios/miembro) se hace en el Servicio.
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProjectDto>))]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            return Ok(projects);
        }

        // GET: api/projects/{id}
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProjectDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProjectDto>> GetProject(Guid id)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(id);
                return Ok(project);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid(); // 403 Forbidden: Autorización a nivel de recurso falló
            }
        }

        // POST: api/projects
        // [Authorize(Roles = "Admin, ProjectManager")] // Podría ser una restricción más fuerte
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ProjectDto))]
        public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectDto dto)
        {
            var projectDto = await _projectService.CreateProjectAsync(dto);
            return CreatedAtAction(nameof(GetProject), new { id = projectDto.Id }, projectDto);
        }

        // PUT: api/projects/{id}
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateProject(Guid id, UpdateProjectDto dto)
        {
            try
            {
                await _projectService.UpdateProjectAsync(id, dto);
                return NoContent(); // 204 No Content para una actualización exitosa
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid(); // 403 Forbidden: Solo Owner/Admin pueden actualizar
            }
        }

        // DELETE: api/projects/{id}
        // [Authorize(Roles = "Admin, ProjectManager")] // Podría ser una restricción más fuerte
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            try
            {
                await _projectService.DeleteProjectAsync(id);
                return NoContent(); // Retorna 204 incluso si el proyecto no existe (idempotencia)
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid(); // 403 Forbidden: Solo Owner/Admin pueden eliminar
            }
        }
    }
}