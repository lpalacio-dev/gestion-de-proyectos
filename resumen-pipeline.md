# ☁️ AWS ECS Fargate & .NET 8: Manual de Operaciones y Ahorro (2026)

Este documento resume la estrategia de despliegue automatizado diseñada para maximizar el aprendizaje y testeo en AWS utilizando créditos (Free Tier/Credits) sin generar costos imprevistos.

---

## 🛠️ 1. Arquitectura de Despliegue "Cero Gasto"

La estrategia principal consiste en un **Ciclo de Vida Volátil**:
1. **Build:** Crear imagen Docker de .NET 8.
2. **Push:** Subir a ECR (con limpieza automática).
3. **Deploy:** Levantar servicio en Fargate (sin Load Balancer).
4. **Test:** Obtener IP dinámica y validar `/health`.
5. **Destroy:** Eliminar el servicio inmediatamente después de la prueba.

---

## 💡 2. Estrategias de Ahorro de Créditos

### 🛡️ ECR Lifecycle Policy
Para evitar que el almacenamiento de imágenes consuma los $100 USD, se aplica una política de ciclo de vida en el repositorio:
- **Regla:** Mantener solo las últimas **2 imágenes**.
- **Beneficio:** Evita el cobro por GB/mes acumulado de versiones antiguas.

### 🚫 Evitar el Application Load Balancer (ALB)
- **El Problema:** El ALB cuesta ~$20 USD/mes solo por existir.
- **La Solución:** Acceso directo vía **Public IP**. En el pipeline, usamos AWS CLI para extraer la IP dinámica de la interfaz de red (ENI) de la tarea de Fargate.

### ⚡ Fargate Spot
- **Configuración:** Al crear el servicio, usar `FARGATE_SPOT` en lugar de `FARGATE`.
- **Beneficio:** Hasta **70% de descuento** sobre el precio estándar.

---

## 📝 3. Implementación en .NET 8 (HealthCheck)

En `Program.cs`, el servicio debe estar expuesto para que el pipeline valide el éxito del despliegue:

```csharp
// Registro
builder.Services.AddHealthChecks();

// Mapeo (Asegurar que el Security Group permita el puerto de la app)
app.MapHealthChecks("/health");

```

---

## 🤖 4. Comandos Clave del Pipeline (GitHub Actions)

### Obtener IP Pública dinámicamente

Este script es vital cuando no se usa Load Balancer:

```bash
# 1. Obtener ARN de la tarea
TASK_ARN=$(aws ecs list-tasks --cluster $CLUSTER_NAME --service-name $SERVICE_NAME --query 'taskArns[0]' --output text)

# 2. Obtener ID de la interfaz de red (ENI)
ENI_ID=$(aws ecs describe-tasks --cluster $CLUSTER_NAME --tasks $TASK_ARN --query 'tasks[0].attachments[0].details[?name==`networkInterfaceId`].value' --output text)

# 3. Obtener IP Pública
PUBLIC_IP=$(aws ec2 describe-network-interfaces --network-interface-ids $ENI_ID --query 'NetworkInterfaces[0].Association.PublicIp' --output text)

```

### Limpieza Automática (Cleanup)

Configurar siempre como `if: always()` en el workflow para asegurar la destrucción del servicio:

```bash
# Bajar tareas a 0 para permitir borrado
aws ecs update-service --cluster $CLUSTER_NAME --service $SERVICE_NAME --desired-count 0

# Eliminar servicio
aws ecs delete-service --cluster $CLUSTER_NAME --service $SERVICE_NAME

```

---

## 🚨 5. Checklist de Seguridad en la Consola Web

1. **AWS Budgets:** Alerta configurada al 80% de los créditos ($80 USD).
2. **Security Groups:** Puerto de la aplicación abierto a `0.0.0.0/0` (solo para pruebas).
3. **IAM Permissions:** El usuario de GitHub debe tener permisos para `ecs`, `ec2` (interfaces de red) y `ecr`.

---

*Notas creadas en Febrero de 2026 para optimización de infraestructura Cloud.*

```

---


¿Hay algún otro detalle técnico de la configuración de AWS que te gustaría profundizar o con esto ya te sientes listo para lanzar el primer despliegue?
