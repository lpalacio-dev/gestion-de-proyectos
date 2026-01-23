// Mappers/MappingProfile.cs
using AutoMapper;
using gestion_de_proyectos.DTOs;
using gestion_de_proyectos.Models;
using System.Linq; // Necesario para la lógica de conteo en ProjectDto
// Alias para Task
using EntityTask = gestion_de_proyectos.Models.Task;

namespace gestion_de_proyectos.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- Mapeo de PROYECTOS ---

            // 1. Project -> ProjectDto (Lectura/Respuesta)
            // Incluye campos calculados (OwnerName, MembersCount)
            CreateMap<Project, ProjectDto>()
                .ForMember(
                    dest => dest.OwnerName,
                    opt => opt.MapFrom(src => src.Owner.UserName) // Asume que el nombre del Owner es el UserName
                )
                .ForMember(
                    dest => dest.MembersCount,
                    opt => opt.MapFrom(src => src.ProjectMembers.Count) // Cuenta los miembros
                );

            // 2. CreateProjectDto -> Project (Creación/Entrada)
            // Ignora Id, OwnerId, CreationDate, Status (asignados en la capa de Servicio)
            CreateMap<CreateProjectDto, Project>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.CreationDate, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());

            // 3. UpdateProjectDto -> Project (Actualización/Entrada)
            // Mapea los campos que el usuario puede actualizar.
            CreateMap<UpdateProjectDto, Project>()
                // Se asume que solo se actualizan Name, Description, Status. 
                // Ignora Id y OwnerId
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.CreationDate, opt => opt.Ignore());


            // --- Mapeo de TAREAS ---

            // 4. Task -> TaskDto (Lectura/Respuesta)
            // Incluye AssignedToName
            CreateMap<EntityTask, TaskDto>()
                .ForMember(
                    dest => dest.AssignedToName,
                    // Asume que el nombre asignado es el UserName del AssignedUser
                    opt => opt.MapFrom(src => src.AssignedUser != null ? src.AssignedUser.UserName : null)
                );

            // 5. CreateTaskDto -> Task (Creación/Entrada)
            // Ignora Id, ProjectId, Status (Asignados por servicio o con valor por defecto)
            CreateMap<CreateTaskDto, EntityTask>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore()); // El servicio o el modelo asigna el estado inicial

            // 6. UpdateTaskDto -> Task (Actualización/Entrada)
            // Mapea todos los campos de actualización.
            CreateMap<UpdateTaskDto, EntityTask>()
                // Ignora Id y ProjectId (no se deben cambiar en una actualización de tarea)
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore());

            // --- Mapeo de PROJECT MEMBERS ---

            // ProjectMember -> ProjectMemberDto
            CreateMap<ProjectMember, ProjectMemberDto>()
                .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project.Name))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email));
        }
    }
}