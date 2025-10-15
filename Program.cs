using gestion_de_proyectos;
using gestion_de_proyectos.Profiles;
using gestion_de_proyectos.Repositories;
using gestion_de_proyectos.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
// 1. NUEVOS USINGS NECESARIOS PARA JWT
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection");
// Registra el DbContext usando el proveedor de PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


// 2. Registrar los servicios de Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>() // Usa IdentityUser por defecto y IdentityRole
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. PASO 3: CONFIGURAR LA AUTENTICACIÓN JWT BEARER
// Obtenemos los valores de configuración JWT. 
// Usamos ?? throw para asegurar que el Key exista, es crítico para la seguridad.
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurada en appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Validaciones críticas
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        // Asignación de valores desde la configuración
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,

        // Se utiliza la clave secreta para verificar la firma del token
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
// FIN DE CONFIGURACIÓN JWT

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(typeof(TaskProfile).Assembly);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 4. PASO 3: AGREGAR MIDDLEWARE DE AUTENTICACIÓN
// **DEBE** ir antes de UseAuthorization() para que el usuario pueda ser identificado antes de ser autorizado.
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
