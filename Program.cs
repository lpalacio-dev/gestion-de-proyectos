using Amazon.S3;
using Amazon.SimpleNotificationService;
using DotNetEnv;
using gestion_de_proyectos;
using gestion_de_proyectos.Configuration;          // [IA] nuevo using
using gestion_de_proyectos.Middleware;             // [IA] nuevo using
using gestion_de_proyectos.Data;
using gestion_de_proyectos.Mappers;
using gestion_de_proyectos.Models;
using gestion_de_proyectos.Repositories;
using gestion_de_proyectos.Services;
using gestion_de_proyectos.Services.AI;            // [IA] nuevo using
using gestion_de_proyectos.Services.AI.Providers;  // [IA] nuevo using
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. VARIABLES DE ENTORNO
Env.Load();
builder.Configuration.AddEnvironmentVariables();

// 2. BASE DE DATOS
var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. AWS
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

// 4. IDENTITY
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 5. JWT
var jwtKey      = builder.Configuration["Jwt:Key"]      ?? throw new InvalidOperationException("Jwt:Key no configurada");
var jwtIssuer   = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtIssuer,
        ValidAudience            = jwtAudience,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// 6. HEALTH CHECKS
builder.Services.AddHealthChecks();

// 7. CONTROLLERS + SWAGGER
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 8. AUTOMAPPER
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// 9. REPOSITORIOS
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

// 10. SERVICIOS EXISTENTES
builder.Services.AddScoped<IUserContextAccessor, UserContextAccessor>();
builder.Services.AddScoped<IProjectAuthorizationService, ProjectAuthorizationService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectMemberService, ProjectMemberService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IS3Service, S3Service>();

// ============================================================================
// [IA] 11. MÓDULO DE IA — Configuración
// ============================================================================
builder.Services.Configure<AIOptions>(
    builder.Configuration.GetSection(AIOptions.SectionName));

// [IA] Rate limit — opciones de configuración (sección "AI:RateLimit" en appsettings.json)
builder.Services.Configure<AIRateLimitOptions>(
    builder.Configuration.GetSection(AIRateLimitOptions.SectionName));

// ============================================================================
// [IA] 12. MÓDULO DE IA — HttpClients nombrados (uno por proveedor)
//          Cada cliente tiene su BaseAddress y Timeout propios.
//          IHttpClientFactory crea instancias frescas sin compartir estado.
// ============================================================================
builder.Services.AddHttpClient("Groq", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AI:Providers:Groq:BaseUrl"]
        ?? "https://api.groq.com/openai/v1/");

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("AI:Providers:Groq:TimeoutSeconds", 30));
});

builder.Services.AddHttpClient("Cerebras", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AI:Providers:Cerebras:BaseUrl"]
        ?? "https://api.cerebras.ai/v1/");

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("AI:Providers:Cerebras:TimeoutSeconds", 30));
});

builder.Services.AddHttpClient("Gemini", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AI:Providers:Gemini:BaseUrl"]
        ?? "https://generativelanguage.googleapis.com/v1beta/openai/");

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("AI:Providers:Gemini:TimeoutSeconds", 45));
});

builder.Services.AddHttpClient("OpenRouter", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AI:Providers:OpenRouter:BaseUrl"]
        ?? "https://openrouter.ai/api/v1/");

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("AI:Providers:OpenRouter:TimeoutSeconds", 60));
});

// ============================================================================
// [IA] 13. MÓDULO DE IA — Providers y servicios
//          Los cuatro providers se registran como ILLMProvider.
//          FallbackLLMService recibe IEnumerable<ILLMProvider> y los ordena por Priority.
// ============================================================================
builder.Services.AddScoped<ILLMProvider, GroqProvider>();
builder.Services.AddScoped<ILLMProvider, CerebrasProvider>();
builder.Services.AddScoped<ILLMProvider, GeminiProvider>();
builder.Services.AddScoped<ILLMProvider, OpenRouterProvider>();

builder.Services.AddScoped<IFallbackLLMService, FallbackLLMService>();
builder.Services.AddScoped<IAIService, AIService>();

// 14. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebAppProxy", app =>
    {
        app.WithOrigins("*")
           .AllowAnyHeader()
           .AllowAnyMethod();
    });
});

// ============================================================================
// PIPELINE
// ============================================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseCors("WebAppProxy");
app.UseAIRateLimit(); // [IA] Rate limiting para /api/ai/* (requiere autenticación previa)
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
