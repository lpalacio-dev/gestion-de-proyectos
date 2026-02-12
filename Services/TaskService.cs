// Services/TaskService.cs
using AutoMapper;
using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using gestion_de_proyectos.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using EntityTask = gestion_de_proyectos.Models.Task; // Alias para evitar ambigüedad;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using System.Text.Json;


namespace gestion_de_proyectos.Services
{
    // Excepciones personalizadas para manejar respuestas HTTP
    public class NotFoundException : Exception { public NotFoundException(string message) : base(message) { } }
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IProjectService _projectService; // Para verificar el contexto del proyecto
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly IMapper _mapper;
        private readonly IAmazonLambda _lambdaClient;              // NUEVO
        private readonly UserManager<ApplicationUser> _userManager; // NUEVO
        private readonly ApplicationDbContext _dbContext;

        public TaskService(
            ITaskRepository taskRepository,
            IProjectService projectService,
            IUserContextAccessor userContextAccessor,
            IMapper mapper,
            IAmazonLambda lambdaClient,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _taskRepository = taskRepository;
            _projectService = projectService;
            _userContextAccessor = userContextAccessor;
            _mapper = mapper;
            _lambdaClient = lambdaClient;
            _userManager = userManager;
            _dbContext = dbContext;
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

            // NUEVO: Si la tarea fue asignada a alguien, notificar por email (async, no bloquea)
            if (!string.IsNullOrWhiteSpace(dto.AssignedToId))
            {
                await SendNotificationAsync(task, "TaskAssigned", oldStatus: null);
            }

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

            // NUEVO: Capturar valores anteriores ANTES de aplicar el update
            var previousStatus = task.Status;
            var previousAssigneeId = task.AssignedToId;

            // Mapear los campos actualizables
            _mapper.Map(dto, task);

            _taskRepository.Update(task);
            await _taskRepository.SaveChangesAsync();


            // NUEVO: Notificaciones post-update (fire-and-forget)
            var hasNewAssignee = !string.IsNullOrWhiteSpace(task.AssignedToId);
            var assigneeChanged = task.AssignedToId != previousAssigneeId;
            var statusChanged = task.Status != previousStatus;

            if (hasNewAssignee && assigneeChanged)
            {
                // Tarea reasignada → notificar al nuevo asignado
                await SendNotificationAsync(task, "TaskAssigned", oldStatus: null);
            }
            else if (hasNewAssignee && statusChanged)
            {
                // Estado cambiado en tarea que tiene asignado → notificar
                await SendNotificationAsync(task, "TaskStatusChanged", oldStatus: previousStatus);
            }
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

        // ====================================================================
        // NUEVO: lógica de notificación Lambda (fire-and-forget)
        // ====================================================================

        /// <summary>
        /// Invoca TaskNotifierLambda de forma ASÍNCRONA (InvocationType.Event).
        /// 
        /// Fire-and-forget: si Lambda falla, solo se loggea — no interrumpe la operación principal.
        /// Esto es intencional: un fallo en notificaciones no debe impedir que la tarea se guarde.
        /// 
        /// Require que task.AssignedToId esté poblado antes de llamar.
        /// </summary>
        private async Task SendNotificationAsync(EntityTask task, string eventType, string? oldStatus)
        {
            try
            {
                // Obtener nombre del proyecto
                var project = await _dbContext.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == task.ProjectId);

                // Obtener datos del usuario asignado
                var assignedUser = await _userManager.FindByIdAsync(task.AssignedToId!);

                // Si algún dato falta, loggear y salir silenciosamente
                if (project == null || assignedUser == null || string.IsNullOrWhiteSpace(assignedUser.Email))
                {
                    Console.WriteLine(
                        $"[TaskService] Notificación omitida para tarea {task.Id}: " +
                        "no se encontró proyecto o usuario asignado.");
                    return;
                }

                // Obtener nombre del usuario que realiza la acción (quien asignó/cambió estado)
                var currentUserId = _userContextAccessor.GetCurrentUserId();
                var currentUser = await _userManager.FindByIdAsync(currentUserId);

                // Construir el payload que deserializará TaskNotifierLambda
                var notificationEvent = new
                {
                    EventType = eventType,
                    TaskId = task.Id.ToString(),
                    TaskTitle = task.Title,
                    TaskDescription = task.Description,
                    ProjectName = project.Name,
                    AssignedUserEmail = assignedUser.Email,
                    AssignedUserName = assignedUser.UserName ?? assignedUser.Email,
                    AssignerName = currentUser?.UserName,
                    OldStatus = oldStatus,
                    NewStatus = task.Status,
                    DueDate = task.DueDate
                };

                var invokeRequest = new InvokeRequest
                {
                    FunctionName = "TaskNotifierLambda",
                    // Event = asíncrono (fire-and-forget). Lambda devuelve 202 inmediatamente.
                    // NO usar RequestResponse aquí porque bloquearía el request del usuario.
                    InvocationType = InvocationType.Event,
                    Payload = JsonSerializer.Serialize(notificationEvent)
                };

                await _lambdaClient.InvokeAsync(invokeRequest);

                Console.WriteLine(
                    $"[TaskService] Notificación '{eventType}' enviada a Lambda " +
                    $"para tarea {task.Id} → {assignedUser.Email}");
            }
            catch (Exception ex)
            {
                // INTENCIONAL: loggear pero NO relanzar.
                // Un fallo en notificaciones no debe revertir la transacción principal.
                Console.WriteLine(
                    $"[TaskService] Error al invocar TaskNotifierLambda para tarea {task.Id}: " +
                    $"{ex.GetType().Name}: {ex.Message}");
            }
        }

    }
}