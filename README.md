# 📊 Sistema de Gestión de Proyectos — Backend

Backend REST API para la gestión colaborativa de proyectos y tareas, con autenticación JWT, autorización basada en roles multinivel y despliegue en AWS ECS Fargate.

---

## 📋 Tabla de Contenidos

- [Descripción General](#-descripción-general)
- [Características](#-características)
- [Arquitectura](#-arquitectura)
- [Tecnologías](#-tecnologías)
- [Integraciones AWS](#-integraciones-aws)
- [Sistema de Autorización](#-sistema-de-autorización)
- [API Endpoints](#-api-endpoints)
- [Configuración Local](#-configuración-local)
- [Despliegue en AWS](#-despliegue-en-aws)
- [CI/CD Pipeline](#-cicd-pipeline)
- [Variables y Secrets](#-variables-y-secrets)

---

## 🎯 Descripción General

Sistema completo de gestión de proyectos que permite a equipos organizar tareas, gestionar miembros con roles diferenciados y controlar el acceso a recursos mediante un sistema de permisos granular de 5 niveles. El backend está construido sobre ASP.NET Core 8, se despliega en **AWS ECS Fargate** con **Application Load Balancer** y **Auto Scaling**, y utiliza una arquitectura event-driven basada en SNS + SQS para el procesamiento asíncrono desacoplado.

| Métrica | Valor |
|---|---|
| Endpoints REST | 28 |
| Controllers | 5 |
| Services | 5 + Autorización |
| DTOs | 19 |
| Modelos de dominio | 4 |
| Líneas de código | ~5,000 |

---

## ✨ Características

### 🔐 Seguridad
- Autenticación JWT con tokens seguros
- Autorización multinivel (Global + Proyecto + Recurso)
- Roles globales: `Admin` y `User`
- Roles de proyecto: `Owner`, `Admin`, `Member`
- Validaciones robustas en todas las capas

### 👤 Gestión de Usuarios
- Registro, login y actualización de perfil
- Foto de perfil con carga a S3 y procesamiento automático vía Lambda
- Búsqueda de usuarios y cambio de contraseña seguro
- Gestión de roles globales (solo Admin)

### 📁 Gestión de Proyectos
- CRUD completo con ownership automático al crear
- Estados: `InProgress`, `Completed`, `OnHold`, `Archived`
- Filtrado por proyectos accesibles al usuario autenticado

### 👥 Gestión de Miembros
- Agregar/eliminar miembros con roles configurables
- Permisos granulares por rol dentro del proyecto
- Función para abandonar un proyecto (excepto el Owner)

### ✅ Gestión de Tareas
- CRUD completo con asignación a usuarios del proyecto
- Prioridades: `Low`, `Medium`, `High`
- Estados: `Pending`, `InProgress`, `Completed`
- Fechas límite opcionales

---

## 🏗️ Arquitectura

```
┌─────────────────────────────────────────┐
│         Capa de Presentación            │
│       (REST API · 28 Endpoints)         │
├─────────────────────────────────────────┤
│         Capa de Servicios               │
│   (Lógica de Negocio + Autorización)    │
├─────────────────────────────────────────┤
│         Capa de Repositorios            │
│      (Acceso a Datos con EF Core)       │
├─────────────────────────────────────────┤
│            Capa de Datos                │
│       (PostgreSQL + ASP.NET Identity)   │
└─────────────────────────────────────────┘
```

**Principios aplicados:** Clean Architecture · SOLID · Repository Pattern · Service Layer · Dependency Injection

---

## 🛠️ Tecnologías

| Tecnología | Uso |
|---|---|
| ASP.NET Core 8 | Framework principal |
| Entity Framework Core | ORM y migraciones |
| ASP.NET Identity | Gestión de usuarios y roles |
| PostgreSQL 14+ | Base de datos relacional |
| AutoMapper | Mapeo de modelos a DTOs |
| JWT Bearer | Autenticación stateless |
| Amazon ECS Fargate | Hosting del contenedor |
| Application Load Balancer | Entrada HTTPS y health checks |
| Auto Scaling | Escalado dinámico por CPU (1–4 tasks) |
| Amazon RDS Aurora | Base de datos administrada |
| Amazon S3 | Almacenamiento de imágenes |
| Amazon SNS | Broker de eventos (pub/sub) |
| Amazon SQS | Colas de mensajes con DLQ |
| AWS Lambda (×3) | Procesamiento asíncrono desacoplado |
| Docker | Containerización |
| GitHub Actions | CI/CD pipeline |

---

## ☁️ Integraciones AWS

El backend se integra con dos funciones Lambda que gestionan operaciones asíncronas:

```
Backend (ECS Fargate)
        │
        ├──► S3 (uploads de imágenes)
        │         │
        │         └──► ImageProcessorLambda  [trigger: S3 ObjectCreated]
        │                   Genera thumbnail 150×150
        │                   y versión optimizada 500×500 con compresión JPEG
        │
        └──► SNS Topic: task-events-topic  [PublishAsync — fire and forget]
                    │
                    │  Fan-out simultáneo
                    └──► SQS: task-email-queue  (retención 1d · DLQ tras 3 fallos)
                              │
                              └──► TaskNotifierLambda
                                        Envía email HTML al usuario asignado vía SES
               
                                        
```

### ImageProcessorLambda

**Trigger:** S3 `ObjectCreated` en la carpeta `profile-images/`
**Resultado:** Al subir una foto de perfil genera automáticamente `profile-images/thumbnails/` (150×150) y `profile-images/optimized/` (500×500).

### TaskNotifierLambda

**Trigger:** SQS `task-email-queue` (suscrita al SNS Topic)
**Resultado:** Deserializa el sobre SNS, construye un email HTML y lo envía al usuario asignado vía Amazon SES. Reporta fallos individuales con `SQSBatchResponse` para no bloquear el batch completo.tado:** Envía un email HTML al usuario correspondiente vía Amazon SES.

### Permisos IAM requeridos por las Lambdas

```json
{
  "Effect": "Allow",
  "Action": [
    "s3:GetObject", "s3:PutObject", "s3:DeleteObject",
    "sqs:ReceiveMessage", "sqs:DeleteMessage", "sqs:GetQueueAttributes",
    "ses:SendEmail", "ses:SendRawEmail"
  ]
}
```

> **Repositorio de las Lambdas:** [`gestion-proyectos-lambdas`](../gestion-proyectos-lambdas)

---

## 🔐 Sistema de Autorización

La autorización opera en **5 niveles** secuenciales. Toda la lógica está centralizada en `ProjectAuthorizationService`:

| Nivel | Validación |
|---|---|
| 1 | Token JWT válido |
| 2 | Rol global (`Admin` / `User`) |
| 3 | Acceso al proyecto (Owner, Miembro o Admin Global) |
| 4 | Permiso específico según rol en el proyecto |
| 5 | Reglas de negocio (ej. Owner no puede abandonar su proyecto) |

### Matriz de Permisos

| Acción | Owner | Admin Proyecto | Member | Admin Global |
|---|:---:|:---:|:---:|:---:|
| Ver proyecto | ✅ | ✅ | ✅ | ✅ |
| Modificar proyecto | ✅ | ❌ | ❌ | ✅ |
| Eliminar proyecto | ✅ | ❌ | ❌ | ✅ |
| Gestionar miembros | ✅ | ✅ | ❌ | ✅ |
| Gestionar tareas | ✅ | ✅ | ✅ | ✅ |

---

## 🚀 API Endpoints

### Autenticación
```
POST   /api/auth/register                        Registrar usuario
POST   /api/auth/login                           Login y obtención de token JWT
```

### Proyectos
```
GET    /api/projects                             Listar proyectos accesibles
GET    /api/projects/{id}                        Obtener proyecto por ID
POST   /api/projects                             Crear proyecto
PUT    /api/projects/{id}                        Actualizar proyecto
DELETE /api/projects/{id}                        Eliminar proyecto
```

### Miembros
```
GET    /api/projects/{id}/members                Listar miembros
POST   /api/projects/{id}/members                Agregar miembro
PUT    /api/projects/{id}/members/{userId}       Actualizar rol de miembro
DELETE /api/projects/{id}/members/{userId}       Eliminar miembro
POST   /api/projects/{id}/members/leave          Abandonar proyecto
```

### Tareas
```
GET    /api/projects/{id}/tasks                  Listar tareas del proyecto
POST   /api/projects/{id}/tasks                  Crear tarea
PUT    /api/projects/{id}/tasks/{taskId}         Actualizar tarea
DELETE /api/projects/{id}/tasks/{taskId}         Eliminar tarea
```

### Usuarios
```
GET    /api/users/search                         Buscar usuarios
GET    /api/users/me                             Ver mi perfil
PUT    /api/users/me                             Actualizar mi perfil
POST   /api/users/me/change-password             Cambiar contraseña
POST   /api/users/me/profile-image               Subir imagen de perfil
DELETE /api/users/me/profile-image               Eliminar imagen de perfil
GET    /api/users                                Listar todos [Admin]
PUT    /api/users/{id}/roles                     Gestionar roles [Admin]
DELETE /api/users/{id}                           Eliminar usuario [Admin]
```

---

## 🔧 Configuración Local

### Pre-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 14+](https://www.postgresql.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) *(opcional)*

### Instalación

```bash
# 1. Clonar el repositorio
git clone <repo-url>
cd gestion-de-proyectos

# 2. Aplicar migraciones
dotnet ef database update

# 3. Ejecutar
dotnet run
```

### Estructura de `appsettings.json`

```json
{
  "ConnectionStrings": {
    "PostgreSQLConnection": "Host=localhost;Port=5432;Database=gestion_proyectos;Username=postgres;Password=tu_password"
  },
  "Jwt": {
    "Key": "tu-clave-secreta-minimo-32-caracteres",
    "Issuer": "https://tu-api.com",
    "Audience": "https://tu-api.com",
    "ExpiresInMinutes": 60
  },
  "AWS": {
    "Region": "us-east-2",
    "S3BucketName": "tu-bucket-name"
  }
}
```

> ⚠️ No subir `appsettings.Development.json` con credenciales reales al repositorio. Usar variables de entorno o AWS Secrets Manager en producción.

### Usuario Admin por Defecto

```
Username : admin
Password : Admin123!
Roles    : Admin, User
```

> ⚠️ Cambiar estas credenciales antes de cualquier despliegue.

---

## ☁️ Despliegue en AWS

### Infraestructura

```
GitHub Actions
     │
     ▼
Amazon ECR ──► ECS Fargate (Task Definition)
                      │
                      ├──► Application Load Balancer (HTTPS · health checks)
                      ├──► Auto Scaling (1–4 tasks · políticas CPU 30/70%)
                      ├──► Amazon RDS Aurora PostgreSQL
                      ├──► Amazon S3
                      └──► SNS Topic
                                 └──► SQS task-email-queue ──► TaskNotifierLambda
                                 
```

### Servicios requeridos

| Servicio | Propósito |
|---|---|
| Amazon ECR | Registro de imágenes Docker |
| Amazon ECS Fargate | Ejecución del contenedor sin servidor |
| Application Load Balancer | Entrada HTTPS, health checks y routing |
| Auto Scaling | Escalado dinámico basado en CPU |
| Amazon RDS Aurora | Base de datos administrada |
| Amazon S3 | Almacenamiento de imágenes de perfil |
| Amazon SNS | Broker de eventos pub/sub |
| Amazon SQS (×2) | Colas desacopladas con DLQ |
| AWS Lambda (×2) | Procesamiento asíncrono desacoplado |
| Amazon SES | Envío de notificaciones por email |


### Pasos de configuración inicial

```bash
# Crear repositorio ECR
aws ecr create-repository \
  --repository-name gestion-proyectos-backend \
  --region us-east-2 \
  --image-scanning-configuration scanOnPush=true

# Crear cluster ECS
aws ecs create-cluster --cluster-name gestion-proyectos-cluster

# Crear instancia RDS
aws rds create-db-instance \
  --db-instance-identifier gestion-proyectos-db \
  --db-instance-class db.t3.micro \
  --engine postgres \
  --engine-version 14 \
  --master-username postgres \
  --master-user-password <password> \
  --allocated-storage 20

# Crear bucket S3 para imágenes
aws s3 mb s3://gestion-proyectos-media --region us-east-2
```

---

## 🔄 CI/CD Pipeline

El pipeline está en `.github/workflows/deploy-backend.yml` y se ejecuta automáticamente con cada push a `develop`.

### Flujo

```
push → develop
     │
     ▼
1. Checkout del código
     │
     ▼
2. Configurar credenciales AWS
     │
     ▼
3. Login a Amazon ECR
     │
     ▼
4. Build y push de imagen Docker
   (tag = commit SHA)
     │
     ▼
5. Descargar Task Definition actual desde ECS
   y limpiar campos de solo lectura con jq
     │
     ▼
6. Inyectar nueva imagen en la Task Definition
     │
     ▼
7. Registrar Task Definition y desplegar en ECS
   (espera confirmación de estabilidad del servicio)
```

### Secrets requeridos en GitHub

Configurar en **Settings → Secrets and variables → Actions**:

| Secret | Descripción |
|---|---|
| `AWS_ACCESS_KEY_ID` | Access key del usuario IAM de deploy |
| `AWS_SECRET_ACCESS_KEY` | Secret key del usuario IAM de deploy |
| `ECR_REPOSITORY` | Nombre del repositorio ECR |
| `ECS_CLUSTER` | Nombre del cluster ECS |
| `ECS_SERVICE` | Nombre del servicio ECS |
| `TASK_FAMILY` | Nombre de la familia de la Task Definition |
| `CONTAINER_NAME` | Nombre del contenedor en la Task Definition |

### Permisos IAM mínimos para el usuario de deploy

```json
{
  "Effect": "Allow",
  "Action": [
    "ecr:GetAuthorizationToken",
    "ecr:BatchCheckLayerAvailability",
    "ecr:PutImage",
    "ecr:InitiateLayerUpload",
    "ecr:UploadLayerPart",
    "ecr:CompleteLayerUpload",
    "ecs:DescribeTaskDefinition",
    "ecs:RegisterTaskDefinition",
    "ecs:UpdateService",
    "ecs:DescribeServices",
    "iam:PassRole"
  ],
  "Resource": "*"
}
```

---

## 🔑 Variables y Secrets

Variables que deben configurarse en la **ECS Task Definition**. Los valores sensibles deben almacenarse en **AWS Secrets Manager** e inyectarse por referencia:

| Variable | Descripción | Sensible |
|---|---|:---:|
| `ConnectionStrings__PostgreSQLConnection` | Cadena de conexión a RDS | ✅ |
| `Jwt__Key` | Clave secreta para firmar tokens JWT | ✅ |
| `Jwt__Issuer` | Issuer del token JWT | ❌ |
| `Jwt__Audience` | Audience del token JWT | ❌ |
| `AWS__Region` | Región AWS (ej. `us-east-2`) | ❌ |
| `AWS__S3BucketName` | Nombre del bucket S3 | ❌ |
| `AWS__SnsTopicArn` | ARN del SNS Topic para eventos de tareas | ❌ |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ❌ |

---

## ✅ Checklist de Producción

- [ ] Cambiar credenciales del usuario admin por defecto
- [ ] Almacenar todos los secretos en AWS Secrets Manager
- [ ] Habilitar HTTPS y configurar certificado SSL/TLS en el ALB
- [ ] Configurar CORS con dominios específicos permitidos
- [ ] Configurar Security Groups con acceso mínimo necesario
- [ ] Activar backups automáticos en RDS
- [ ] Configurar alertas en CloudWatch para errores, latencia y DLQ
- [ ] Habilitar escaneo de vulnerabilidades en ECR
- [ ] Implementar rate limiting en la API
- [ ] Configurar Multi-AZ en RDS para alta disponibilidad
- [ ] Verificar email remitente en SES y solicitar salida del Sandbox
- [ ] Confirmar suscripciones SNS → SQS activas con estado `Enabled`

---

## 📁 Estructura del Proyecto

```
gestion-de-proyectos/
├── .github/
│   └── workflows/
│       └── deploy-backend.yml       # Pipeline CI/CD
├── Controllers/
│   ├── AuthController.cs
│   ├── ProjectController.cs
│   ├── ProjectMemberController.cs
│   ├── TaskController.cs
│   └── UserController.cs
├── Services/
│   ├── ProjectService.cs
│   ├── TaskService.cs
│   ├── UserService.cs
│   ├── ProjectMemberService.cs
│   ├── ProjectAuthorizationService.cs
│   └── S3Service.cs
├── Repositories/
│   ├── ProjectRepository.cs
│   └── TaskRepository.cs
├── Models/
│   ├── Project.cs
│   ├── Task.cs
│   ├── ProjectMember.cs
│   └── ApplicationUser.cs
├── DTOs/                             # 19 Data Transfer Objects
├── Program.cs
├── ApplicationDbContext.cs
├── MappingProfile.cs
└── Dockerfile
```

## 🔗 Repositorios relacionados

- **Frontend:** [`project-management-front`](../project-management-front) — Angular 20, S3
- **Lambdas:** [`gestion-proyectos-lambdas`](../gestion-proyectos-lambdas) — .NET 8, ImageProcessor + TaskNotifier

---

*Desarrollado con ASP.NET Core 8 y PostgreSQL · Desplegado en AWS ECS Fargate · v1.0.0*