using AutoMapper;
using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Repositories;
using TaskModelo = gestion_de_proyectos.Models.Task; // Alias para evitar ambigüedad

namespace gestion_de_proyectos.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IProjectRepository _projectRepository; // Necesario para validar ProjectId
        private readonly IUserRepository _userRepository;       // Necesario para validar AssignedToId
        private readonly IMapper _mapper;

        public TaskService(
            ITaskRepository taskRepository,
            IProjectRepository projectRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        // --- Helper de Mapeo para evitar duplicación de código ---
        private TaskResponseDto MapToResponseDto(TaskModelo task)
        {
            var dto = _mapper.Map<TaskResponseDto>(task);

            // Llenar detalles de las relaciones cargadas por el Repository
            dto.ProjectTitle = task.Project.Name;

            // AssignedUser es nullable, se debe verificar
            dto.AssignedToUsername = task.AssignedUser?.Name;

            return dto;
        }

        // --- Métodos de Lectura ---

        public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync()
        {
            var tasks = await _taskRepository.GetAllAsync();
            return tasks.Select(MapToResponseDto);
        }

        public async Task<TaskResponseDto?> GetTaskByIdAsync(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null) return null;

            return MapToResponseDto(task);
        }

        // --- Método de Creación (Lógica CRÍTICA de Validación) ---

        public async Task<TaskResponseDto> CreateTaskAsync(TaskCreationDto dto)
        {
            // 1. Validar ProjectId (Requerido)
            if (!await _projectRepository.ExistsAsync(dto.ProjectId))
            {
                // Deberías lanzar una excepción personalizada (ej. ResourceNotFoundException)
                // que el controlador atrapará para devolver un 404 o 400.
                throw new InvalidOperationException($"Project with ID {dto.ProjectId} not found.");
            }

            // 2. Validar AssignedToId (Opcional: solo si se proporciona)
            if (dto.AssignedToId.HasValue && !await _userRepository.ExistsAsync(dto.AssignedToId.Value))
            {
                throw new InvalidOperationException($"User with ID {dto.AssignedToId.Value} not found.");
            }

            // 3. Mapeo y Guardado
            var task = _mapper.Map<TaskModelo>(dto);

            await _taskRepository.AddAsync(task);

            // 4. Recargar y mapear: Se recarga para asegurar que las propiedades de navegación 
            //    (Project y AssignedUser) estén pobladas para el DTO de respuesta.
            var createdTask = await _taskRepository.GetByIdAsync(task.Id);

            // Si por alguna razón la recarga falla, devolvemos un error.
            if (createdTask == null) throw new Exception("Error al recuperar la tarea después de la creación.");

            return MapToResponseDto(createdTask);
        }

        // --- Método de Actualización (Lógica de Negocio) ---

        public async Task<TaskResponseDto?> UpdateTaskAsync(Guid id, TaskUpdateDto dto)
        {
            var taskToUpdate = await _taskRepository.GetByIdAsync(id);
            if(taskToUpdate == null) return null;

            // 1. VALIDACIÓN Y ASIGNACIÓN MANUAL DE PROJECTID (FK CRÍTICA)
            if (dto.ProjectId.HasValue)
            {
                if (!await _projectRepository.ExistsAsync(dto.ProjectId.Value))
                {
                    throw new InvalidOperationException($"Project with ID {dto.ProjectId.Value} not found.");
                }
                // ASIGNACIÓN MANUAL GARANTIZADA
                taskToUpdate.ProjectId = dto.ProjectId.Value;
            }
            // NOTA: Si dto.ProjectId NO tiene valor, la propiedad ProjectId de taskToUpdate 
            // MANTIENE su valor original. ¡SOLUCIONADO!

            // 2. VALIDACIÓN Y ASIGNACIÓN MANUAL DE ASSIGNEDTOID
            if (dto.AssignedToId.HasValue)
            {
                if (!await _userRepository.ExistsAsync(dto.AssignedToId.Value))
                {
                    throw new InvalidOperationException($"User with ID {dto.AssignedToId.Value} not found.");
                }
                taskToUpdate.AssignedToId = dto.AssignedToId.Value;
            }
            else if (dto.AssignedToId == null) // Manejo explícito de desasignación (si se envía {"assignedToId": null})
            {
                taskToUpdate.AssignedToId = null;
            }
            // NOTA: Si dto.AssignedToId NO se envía, la propiedad se mantiene.

            // 3. Mapear DTO a Entidad (AutoMapper solo mapea las propiedades no nulas de DTO a la entidad)
            _mapper.Map(dto, taskToUpdate);

            await _taskRepository.UpdateAsync(taskToUpdate);

            // Recargar la tarea para obtener las relaciones actualizadas
            var updatedTask = await _taskRepository.GetByIdAsync(id);

            if (updatedTask == null) throw new Exception("Error al recuperar la tarea después de la actualización.");

            return MapToResponseDto(updatedTask);
        }

        // --- Método de Eliminación ---

        public async Task<bool> DeleteTaskAsync(Guid id)
        {
            var exists = await _taskRepository.ExistsAsync(id);
            if (!exists) return false;

            await _taskRepository.DeleteAsync(id);
            return true;
        }
    }
}
