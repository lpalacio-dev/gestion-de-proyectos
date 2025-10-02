# 💾 Guía Rápida: Conexión de PostgreSQL a un Proyecto .NET (EF Core - Code First)

Esta guía documenta los pasos esenciales para configurar Entity Framework Core (EF Core) y conectar tu API .NET con una base de datos PostgreSQL usando el enfoque Code First (Código Primero).

---

## 1. Instalación de Paquetes NuGet 📦

Abre la terminal de tu proyecto (.NET CLI) y ejecuta los siguientes comandos para instalar los proveedores de base de datos y las herramientas de migración.

bash
# Proveedor específico para PostgreSQL (Npgsql)
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# Paquete de diseño requerido para generar las migraciones
dotnet add package Microsoft.EntityFrameworkCore.Design

# Herramientas de línea de comandos (dotnet ef)
dotnet add package Microsoft.EntityFrameworkCore.Tools

NOTA: Si el comando dotnet ef no funciona, instálalo globalmente:
dotnet tool install --global dotnet-ef

---

## 2. Definición de Modelos (Entidades) 📝
Crea una carpeta llamada Models (o Entities) y define tus clases de C#. Cada clase representará una tabla en la base de datos.

Ejemplo (Project.cs):

C#

using System.ComponentModel.DataAnnotations;

namespace MiProyecto.Models
{
    public class Project
    {
        public int Id { get; set; } // Clave primaria por convención

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        // Propiedad de navegación (relación uno a muchos)
        public ICollection<Task> Tasks { get; set; } 
    }
}

---

## 3. Creación del DbContext 🌉
Crea una carpeta llamada Data y define tu clase de contexto (DbContext). Esta clase es el puente de EF Core.

Ejemplo (ProjectManagementContext.cs):

C#

using Microsoft.EntityFrameworkCore;
using MiProyecto.Models; // Asegúrate de usar tu namespace de modelos

namespace MiProyecto.Data 
{
    public class ProjectManagementContext : DbContext
    {
        public ProjectManagementContext(DbContextOptions<ProjectManagementContext> options)
            : base(options)
        {
        }

        // Define tus tablas (DbSets) aquí
        public DbSet<Project> Projects { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
    }
}

---

## 4. Configuración de la Conexión en Program.cs 🔗
A. Configuración en appsettings.json
Agrega tu cadena de conexión a PostgreSQL.

JSON

{
  "ConnectionStrings": {
    "PostgreSQLConnection": "Host=localhost;Database=ProjectManagementDB;Username=postgres;Password=your_password"
  },
  // ... resto del archivo
}
B. Registro en Program.cs
Registra el DbContext para que se pueda inyectar en tu aplicación, especificando el uso del proveedor Npgsql.

C#

using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL; // NECESARIO para UseNpgsql
using MiProyecto.Data; 

var builder = WebApplication.CreateBuilder(args);

// ... otros servicios

// 1. Obtiene la cadena de conexión
var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection");

// 2. Registra el DbContext y especifica el proveedor de PostgreSQL
builder.Services.AddDbContext<ProjectManagementContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// ... resto del archivo

---

## 5. Creación y Aplicación de Migraciones 🛠️
Utiliza la terminal para generar el código SQL y aplicarlo a tu base de datos.

A. Crear la Primera Migración
Esto crea los archivos C# que contienen las instrucciones para construir el esquema.

Bash

dotnet ef migrations add InitialCreate
B. Aplicar la Migración a la Base de Datos
Esto ejecuta el código de migración, se conecta a PostgreSQL y crea las tablas.

Bash

dotnet ef database update