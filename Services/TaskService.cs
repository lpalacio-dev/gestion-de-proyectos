// Services/TaskService.cs
using AutoMapper;
using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using gestion_de_proyectos.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using EntityTask = gestion_de_proyectos.Models.Task; // Alias para evitar ambigüedad;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;


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
        private readonly IAmazonSimpleNotificationService _snsClient;             // NUEVO
        private readonly UserManager<ApplicationUser> _userManager; // NUEVO
        private readonly ApplicationDbContext _dbContext;
        private readonly string _snsTopicArn;

        public TaskService(
            ITaskRepository taskRepository,
            IProjectService projectService,
            IUserContextAccessor userContextAccessor,
            IMapper mapper,
            IAmazonSimpleNotificationService snsClient,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IConfiguration configuration
            )
        {
            _taskRepository = taskRepository;
            _projectService = projectService;
            _userContextAccessor = userContextAccessor;
            _mapper = mapper;
            _snsClient = snsClient;
            _userManager = userManager;
            _dbContext = dbContext;
            _snsTopicArn = configuration["AWS:SnsTopicArn"]
                       ?? throw new Exception("AWS:SnsTopicArn no configurado");
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

        /// <summary>
        /// NUEVA implementación: publica en SNS en lugar de invocar Lambda directamente.
        /// SNS distribuye el mensaje a todas las colas suscritas (SQS).
        /// Fire-and-forget: si SNS falla, solo se loggea.
        /// </summary>
        private async Task SendNotificationAsync(EntityTask task, string eventType, string? oldStatus)
        {
            try
            {
                var project = await _dbContext.Projects.AsNoTracking()
                                        .FirstOrDefaultAsync(p => p.Id == task.ProjectId);
                var assignedUser = await _userManager.FindByIdAsync(task.AssignedToId!);

                if (project == null || assignedUser == null || string.IsNullOrWhiteSpace(assignedUser.Email))
                {
                    Console.WriteLine($"[TaskService] Notificación omitida para tarea {task.Id}: datos incompletos.");
                    return;
                }

                var currentUserId = _userContextAccessor.GetCurrentUserId();
                var currentUser = await _userManager.FindByIdAsync(currentUserId);

                var payload = new
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

                // Publicar en SNS — SNS lo distribuye automáticamente a SQS
                var publishRequest = new PublishRequest
                {
                    TopicArn = _snsTopicArn,
                    Message = JsonSerializer.Serialize(payload),
                    Subject = $"TaskEvent:{eventType}",  // Útil para filtros en el futuro
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        // Atributo para filtros de suscripción (útil a futuro)
                        ["EventType"] = new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = eventType
                        }
                    }
                };

                await _snsClient.PublishAsync(publishRequest);

                Console.WriteLine($"[TaskService] Evento '{eventType}' publicado en SNS para tarea {task.Id}");
            }
            catch (Exception ex)
            {
                // Intencional: un fallo en notificaciones NO revierte la transacción principal
                Console.WriteLine($"[TaskService] Error al publicar en SNS para tarea {task.Id}: {ex.GetType().Name}: {ex.Message}");
            }
        }

    }
}