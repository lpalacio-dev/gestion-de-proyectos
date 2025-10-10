using AutoMapper;
using gestion_de_proyectos.DTOs;
// Usamos un alias para evitar el conflicto CS0104: 'Task' es ambiguo
using TaskModelo = gestion_de_proyectos.Models.Task;
using gestion_de_proyectos.Models;

namespace gestion_de_proyectos.Profiles
{
    public class TaskProfile : Profile
    {
        public TaskProfile()
        {
            // ----------------------------------------------------------------
            // 1. DTO de ENTRADA (Creación y Actualización) -> ENTIDAD
            // ----------------------------------------------------------------

            // Mapeo TaskCreationDto -> Task (Entidad)
            CreateMap<TaskCreationDto, TaskModelo>();

            // Mapeo TaskUpdateDto -> Task (Entidad)
            // ReverseMap() no es necesario aquí. Usamos ForAllMembers para ignorar campos nulos.
            CreateMap<TaskUpdateDto, TaskModelo>()
                // 📢 ESTO ES CRÍTICO: Ignoramos las FKs
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedToId, opt => opt.Ignore())

                // Mantiene la lógica de ignorar los campos nulos o no enviados (para Título, Descripción, etc.)
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
                // NOTA: Esta condición permite ignorar los valores null/omitted del DTO 
                // al actualizar la entidad, lo que habilita la actualización parcial (PATCH-like).


            // ----------------------------------------------------------------
            // 2. ENTIDAD -> DTO de RESPUESTA (Resolución de Relaciones)
            // ----------------------------------------------------------------

            // Mapeo Task (Entidad) -> TaskResponseDto
            CreateMap<TaskModelo, TaskResponseDto>()
                // Resolución de ProjectTitle: Mapea la propiedad Name del Project relacionado
                .ForMember(
                    dest => dest.ProjectTitle,
                    opt => opt.MapFrom(src => src.Project.Name)
                ) // src.Project.Name viene de la entidad Project

                // Resolución de AssignedToUsername: Mapea la propiedad Name del AssignedUser.
                // Es crucial verificar si AssignedUser es NULL, ya que AssignedToId es nullable.
                .ForMember(
                    dest => dest.AssignedToUsername,
                    opt => opt.MapFrom(src => src.AssignedUser != null ? src.AssignedUser.Name : null)
                ); // src.AssignedUser.Name viene de la entidad User
        }
    }
}
