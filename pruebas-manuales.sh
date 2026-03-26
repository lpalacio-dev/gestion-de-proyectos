# =============================================================================
# Checklist de Pruebas Manuales — Módulo de IA
# Sistema de Gestión de Proyectos v1.0
# Ejecutar ANTES de cualquier merge a producción.
# =============================================================================
# Prerrequisitos:
#   - Backend corriendo localmente (dotnet run)
#   - .env con las cuatro API keys configuradas
#   - Usuario de prueba autenticado (guardar el JWT como $TOKEN)
#   - Al menos un proyecto existente (guardar su ID como $PROJECT_ID)
# =============================================================================

BASE_URL=https://localhost:5001
TOKEN=<jwt_del_usuario_autenticado>
PROJECT_ID=<guid_de_proyecto_existente>


# =============================================================================
# BLOQUE 1 — Validaciones de entrada (no llegan al LLM)
# =============================================================================

## T01 — Descripción vacía → 400
# Esperado: HTTP 400 con mensaje de validación
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"description": ""}' | jq .

## T02 — Descripción menor a 20 caracteres → 400
# Esperado: HTTP 400 con "al menos 20 caracteres"
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"description": "app corta"}' | jq .

## T03 — Descripción mayor a 2000 caracteres → 400
# Esperado: HTTP 400 con "no debe superar los 2000 caracteres"
LONG=$(python3 -c "print('a' * 2001)")
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"description\": \"$LONG\"}" | jq .

## T04 — Sin token JWT → 401
# Esperado: HTTP 401
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Content-Type: application/json" \
  -d '{"description": "una descripcion valida de al menos veinte caracteres"}' | jq .

## T05 — maxTasks fuera de rango (0 o 21) → 400
# Esperado: HTTP 400
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"description": "descripcion valida de al menos veinte caracteres", "maxTasks": 25}' | jq .


# =============================================================================
# BLOQUE 2 — Generación de proyecto (flujo feliz)
# =============================================================================

## T06 — Descripción en español, dominio técnico
# Esperado: HTTP 200, JSON con name/description/status/tasks, tasks ordenadas
# Verificar: nombre conciso (≤8 palabras), tareas con prioridad válida
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Quiero crear una tienda online para vender ropa con carrito de compras, pagos con Stripe y panel de administración.",
    "maxTasks": 8
  }' | jq .

## T07 — Descripción en inglés
# Esperado: HTTP 200, respuesta en inglés (name, description y tasks en inglés)
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Build a task management app with real-time collaboration using WebSockets and a REST API.",
    "language": "en"
  }' | jq .

## T08 — Descripción vaga (dominio no técnico)
# Esperado: HTTP 200, la IA infiere un proyecto razonable con suposiciones explícitas
# Verificar: no debe fallar, debe generar algo coherente
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Organizar la boda de mi hermana para 150 personas en junio del próximo año."
  }' | jq .

## T09 — Descripción muy específica (microservicios)
# Esperado: HTTP 200, tareas técnicas bien ordenadas (DB primero, infra antes de deploy)
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Migrar un monolito Django a microservicios con FastAPI, PostgreSQL por servicio, RabbitMQ para mensajería y Kubernetes para orquestación.",
    "maxTasks": 15,
    "detailLevel": "detailed"
  }' | jq .

## T10 — Verificar límite de tareas respetado
# Esperado: HTTP 200, tasks.length <= 5 aunque la IA quiera sugerir más
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Crear una plataforma de e-learning completa con cursos, videos, exámenes y certificados.",
    "maxTasks": 5
  }' | jq '.tasks | length'


# =============================================================================
# BLOQUE 3 — Confirmación y persistencia
# =============================================================================

## T11 — Confirmar proyecto completo (todas las tareas)
# Prerrequisito: guardar el output de T06 y usarlo aquí
# Esperado: HTTP 201, Location header apuntando a /api/projects/{id}
curl -s -X POST "$BASE_URL/api/ai/confirm-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Tienda Online Ropa con Stripe",
    "description": "E-commerce de ropa con carrito, pagos y admin.",
    "status": "InProgress",
    "selectedTasks": [
      {"title": "Diseñar esquema de base de datos", "priority": "High", "dueDateOffsetDays": 5},
      {"title": "Configurar autenticación de usuarios", "priority": "High", "dueDateOffsetDays": 7},
      {"title": "Implementar catálogo de productos", "priority": "Medium", "dueDateOffsetDays": 14}
    ]
  }' -i | head -30

## T12 — Confirmar con lista de tareas vacía
# Esperado: HTTP 201, proyecto creado sin tareas
curl -s -X POST "$BASE_URL/api/ai/confirm-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Proyecto Sin Tareas Iniciales",
    "description": "Un proyecto que empezará vacío.",
    "status": "OnHold",
    "selectedTasks": []
  }' | jq '{id, name, membersCount}'

## T13 — Verificar que el proyecto aparece en GET /api/projects
# Prerrequisito: usar el ID retornado en T11
NEW_ID=<id_del_proyecto_creado_en_T11>
curl -s "$BASE_URL/api/projects/$NEW_ID" \
  -H "Authorization: Bearer $TOKEN" | jq '{id, name, status, ownerId}'

## T14 — Verificar que el usuario es Owner automáticamente
curl -s "$BASE_URL/api/projects/$NEW_ID/members" \
  -H "Authorization: Bearer $TOKEN" | jq '.[] | {userId, role}'


# =============================================================================
# BLOQUE 4 — Sugerencia de tareas para proyecto existente
# =============================================================================

## T15 — Sugerir tareas para proyecto con tareas existentes
# Esperado: HTTP 200, sugerencias que NO dupliquen las existentes
curl -s "$BASE_URL/api/ai/suggest-tasks/$PROJECT_ID" \
  -H "Authorization: Bearer $TOKEN" | jq .

## T16 — Verificar que el orderIndex continúa desde las tareas existentes
# Esperado: orderIndex empieza en (número de tareas existentes + 1)
EXISTING_COUNT=$(curl -s "$BASE_URL/api/projects/$PROJECT_ID/tasks" \
  -H "Authorization: Bearer $TOKEN" | jq 'length')
echo "Tareas existentes: $EXISTING_COUNT"
curl -s "$BASE_URL/api/ai/suggest-tasks/$PROJECT_ID" \
  -H "Authorization: Bearer $TOKEN" | jq '.[0].orderIndex'
# El valor debe ser $EXISTING_COUNT + 1

## T17 — Sugerir tareas para proyecto al que el usuario NO tiene acceso → 403
OTHER_PROJECT_ID=<id_de_proyecto_de_otro_usuario>
curl -s "$BASE_URL/api/ai/suggest-tasks/$OTHER_PROJECT_ID" \
  -H "Authorization: Bearer $TOKEN" | jq .

## T18 — Sugerir tareas para proyecto que no existe → 404
curl -s "$BASE_URL/api/ai/suggest-tasks/00000000-0000-0000-0000-000000000000" \
  -H "Authorization: Bearer $TOKEN" | jq .


# =============================================================================
# BLOQUE 5 — Fallback en cascada
# =============================================================================

## T19 — Simular rate limit de Groq (desactivar la API key de Groq en .env)
# Proceso:
#   1. En .env, cambiar AI__Providers__Groq__ApiKey a un valor inválido: "INVALID_KEY"
#   2. Reiniciar el servidor
#   3. Hacer una solicitud de generación
# Esperado:
#   - HTTP 200 (respuesta exitosa de Cerebras o Gemini)
#   - En logs: "[AI:RateLimit] Provider=Groq" o "[AI:HttpError] Provider=Groq"
#   - En la respuesta JSON: "usedFallback": true, "generatedByProvider": "Cerebras" (o Gemini)
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"description": "App para gestión de inventario en un almacén pequeño."}' \
  | jq '{generatedByProvider, usedFallback}'

## T20 — Simular todos los providers caídos → 503
# Proceso:
#   1. Cambiar las cuatro API keys a valores inválidos en .env
#   2. Reiniciar el servidor
#   3. Hacer una solicitud
# Esperado: HTTP 503 con message y failedProviders[]
curl -s -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"description": "Descripcion valida de mas de veinte caracteres para la prueba."}' | jq .
# Restaurar las API keys reales después de esta prueba


# =============================================================================
# BLOQUE 6 — Rate limiting
# =============================================================================

## T21 — Superar el límite (10 requests/hora por defecto)
# Proceso: ejecutar 11 veces seguidas la misma solicitud
# Esperado: las primeras 10 retornan 200, la 11 retorna 429
# Verificar headers: Retry-After, X-RateLimit-Limit, X-RateLimit-Remaining
for i in $(seq 1 11); do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$BASE_URL/api/ai/generate-project" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"description": "Descripcion valida de mas de veinte caracteres para prueba de rate limit."}')
  echo "Solicitud $i: HTTP $STATUS"
done

## T22 — Verificar headers de rate limit en respuesta exitosa
curl -si -X POST "$BASE_URL/api/ai/generate-project" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"description": "Descripcion valida de mas de veinte caracteres para ver headers."}' \
  | grep -E "X-RateLimit|Retry-After|HTTP/"


# =============================================================================
# BLOQUE 7 — Verificación de logs en CloudWatch
# =============================================================================

## T23 — Confirmar propiedades estructuradas en los logs
# Después de ejecutar T06, ir a CloudWatch Logs Insights y correr:
#
#   fields @timestamp, AIOperation, AIProvider, AITokens, AIResponseMs, AIFallback
#   | filter @message like /\[AI:Provider\]/
#   | sort @timestamp desc
#   | limit 5
#
# Esperado: una fila con AIOperation="GenerateProject", AIProvider="Groq" (o el que respondió),
#           AITokens > 0, AIResponseMs > 0, AIFallback=false

## T24 — Correr la query de resumen ejecutivo (query 10 del archivo cloudwatch-queries.logs)
# Esperado: LlamadasExitosas >= número de pruebas del bloque 2 ejecutadas,
#           TodosLosFallaron = 1 (solo T20), ErroresDeParseo = 0


# =============================================================================
# RESULTADO ESPERADO AL FINALIZAR TODAS LAS PRUEBAS
# =============================================================================
#
#  T01–T05  ✅  Validaciones de entrada correctas
#  T06–T10  ✅  Generación funciona en español, inglés, dominios técnicos y no técnicos
#  T11–T14  ✅  Confirmación persiste correctamente, usuario es Owner
#  T15–T18  ✅  Sugerencia de tareas funciona, errores de acceso correctos
#  T19      ✅  Fallback activado cuando Groq falla, respuesta exitosa de proveedor secundario
#  T20      ✅  503 claro cuando todos los providers fallan
#  T21–T22  ✅  Rate limiting bloquea en solicitud 11, headers presentes
#  T23–T24  ✅  Logs estructurados visibles en CloudWatch con propiedades nombradas
