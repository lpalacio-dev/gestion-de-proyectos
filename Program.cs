using gestion_de_proyectos;
using gestion_de_proyectos.Mappers;
using gestion_de_proyectos.Models;
using gestion_de_proyectos.Repositories;
using gestion_de_proyectos.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Amazon.S3;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// 1. CARGAR VARIABLES DE ENTORNO DESDE .ENV
Env.Load();
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAWSService<IAmazonS3>();

// Registrar los servicios de Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// CONFIGURAR LA AUTENTICACIÓN JWT BEARER
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
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddHealthChecks();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

builder.Services.AddScoped<IUserContextAccessor, UserContextAccessor>();
builder.Services.AddScoped<IProjectAuthorizationService, ProjectAuthorizationService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectMemberService, ProjectMemberService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IS3Service, S3Service>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebAppProxy", app =>
    {
        app.WithOrigins("http://localhost:4200") // El puerto de tu Angular
           .AllowAnyHeader()
           .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Esto ayuda a ver más detalles
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseCors("WebAppProxy");
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/healthz");

// ============================================================================
// FASE 1: SEEDING DE ROLES GLOBALES AJUSTADOS
// ============================================================================

//using (var scope = app.Services.CreateScope())
//{
//    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

//    // Definir roles según el plan
//    // - Admin: Acceso total al sistema
//    // - User: Rol base para todos los usuarios autenticados
//    string[] roles = { "Admin", "User" };

//    // Crear roles si no existen
//    foreach (var role in roles)
//    {
//        if (!await roleManager.RoleExistsAsync(role))
//        {
//            await roleManager.CreateAsync(new IdentityRole(role));
//            Console.WriteLine($"✓ Rol '{role}' creado exitosamente.");
//        }
//        else
//        {
//            Console.WriteLine($"→ Rol '{role}' ya existe.");
//        }
//    }

//    // ============================================================================
//    // CREAR USUARIO ADMINISTRADOR POR DEFECTO
//    // ============================================================================
//    const string adminEmail = "admin@gestion.com";
//    const string adminUsername = "admin";
//    const string adminPassword = "Admin123!"; // ¡Cambiar en producción!

//    var adminUser = await userManager.FindByNameAsync(adminUsername);

//    if (adminUser == null)
//    {
//        adminUser = new ApplicationUser
//        {
//            UserName = adminUsername,
//            Email = adminEmail,
//            EmailConfirmed = true,
//            SecurityStamp = Guid.NewGuid().ToString(),
//            RegistrationDate = DateTime.UtcNow
//        };

//        var result = await userManager.CreateAsync(adminUser, adminPassword);

//        if (result.Succeeded)
//        {
//            // Asignar rol Admin
//            await userManager.AddToRoleAsync(adminUser, "Admin");
//            // También asignar rol User (todos los usuarios deben tenerlo)
//            await userManager.AddToRoleAsync(adminUser, "User");
//        }
//        else
//        {
//            foreach (var error in result.Errors)
//            {
//                Console.WriteLine($"  - {error.Description}");
//            }
//        }
//    }
//    else
//    {
//        Console.WriteLine($"→ Usuario administrador '{adminUsername}' ya existe.");

//        // Verificar que tenga los roles correctos
//        var userRoles = await userManager.GetRolesAsync(adminUser);

//        if (!userRoles.Contains("Admin"))
//        {
//            await userManager.AddToRoleAsync(adminUser, "Admin");
//            Console.WriteLine($"  ✓ Rol 'Admin' agregado al usuario.");
//        }

//        if (!userRoles.Contains("User"))
//        {
//            await userManager.AddToRoleAsync(adminUser, "User");
//            Console.WriteLine($"  ✓ Rol 'User' agregado al usuario.");
//        }
//    }
//}

app.Run();