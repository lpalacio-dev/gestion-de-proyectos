// Services/TaskService.cs
using gestion_de_proyectos.DTOs;
using EntityTask = gestion_de_proyectos.Models.Task; // Alias para evitar ambigüedad
using gestion_de_proyectos.Repositories;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gestion_de_proyectos.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IProjectService _projectService; // Para verificar el contexto del proyecto
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly IMapper _mapper;

        public TaskService(
            ITaskRepository taskRepository,
            IProjectService projectService,
            IUserContextAccessor userContextAccessor,
            IMapper mapper)
        {
            _taskRepository = taskRepository;
            _projectService = projectService;
            _userContextAccessor = userContextAccessor;
            _mapper = mapper;
        }

        // Método auxiliar para verificación de autorización del contexto
        private async Task CheckProjectAccess(Guid projectId)
        {
            // Reutiliza la lógica de ProjectService para asegurar que el usuario es miembro/dueño/admin
            try
            {
                await _projectService.GetProjectByIdAsync(projectId);
            }
            catch (NotFoundException)
            {
                throw new NotFoundException($"Project with Id {projectId} not found.");
            }
            // Si GetProjectByIdAsync falla la autorización, lanzará UnauthorizedAccessException.
        }

        // --- Implementación de ITaskService ---

        public async Task<IEnumerable<TaskDto>> GetAllTasksAsync(Guid projectId)
        {
            // Autorización de Contexto: Asegurar que el usuario pueda ver el proyecto padre
            await CheckProjectAccess(projectId);

            var queryable = await _taskRepository.GetAllAsync();

            // Filtra por el proyecto solicitado
            var tasks = await queryable
                .Where(t => t.ProjectId == projectId)
                .Include(t => t.AssignedUser)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TaskDto>>(tasks);
        }

        public async Task<TaskDto> GetTaskByIdAsync(Guid taskId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                throw new NotFoundException($"Task with Id {taskId} not found.");
            }

            // Autorización de Contexto: Asegurar que el usuario pueda ver el proyecto padre
            await CheckProjectAccess(task.ProjectId);

            return _mapper.Map<TaskDto>(task);
        }

        public async Task<TaskDto> CreateTaskAsync(Guid projectId, CreateTaskDto dto)
        {
            // Autorización de Contexto: Solo miembros/dueños pueden crear tareas
            await CheckProjectAccess(projectId);

            var task = _mapper.Map<EntityTask>(dto);
            task.ProjectId = projectId;

            // Lógica de Asignación: Asignar al AssignedToId proporcionado o dejar null
            // (La verificación de si AssignedToId existe se hace en la capa superior o se omite)

            await _taskRepository.AddAsync(task);
            await _taskRepository.SaveChangesAsync();

            return _mapper.Map<TaskDto>(task);
        }

        public async Task UpdateTaskAsync(Guid taskId, UpdateTaskDto dto)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                throw new NotFoundException($"Task with Id {taskId} not found.");
            }

            // Autorización de Contexto: Solo miembros/dueños pueden actualizar tareas
            await CheckProjectAccess(task.ProjectId);

            // Mapear los campos actualizables
            _mapper.Map(dto, task);

            _taskRepository.Update(task);
            await _taskRepository.SaveChangesAsync();
        }

        public async Task DeleteTaskAsync(Guid taskId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null)
            {
                return; // Idempotencia: no hacer nada si ya no existe
            }

            // Autorización de Contexto: Solo miembros/dueños/admin pueden eliminar tareas
            await CheckProjectAccess(task.ProjectId);

            _taskRepository.Delete(task);
            await _taskRepository.SaveChangesAsync();
        }
    }
}