# Prompt — Frontend: Interés por Mora (Cuenta Corriente)

```
Contexto del proyecto:
  - React + Vite, autenticación con JWT almacenado en cookie/localStorage.
  - El módulo de Cuenta Corriente ya existe en el frontend.
  - El backend expone dos grupos de endpoints nuevos (detallados abajo).
  - El usuario logueado tiene permisos "PERM:CC_VIEW" y/o "PERM:CC_MANAGE"
    según su rol. El frontend ya maneja la visibilidad de acciones por permiso.

  ANTES DE IMPLEMENTAR, revisá el código existente del módulo de Cuenta Corriente
  en el frontend para seguir los mismos patrones de componentes, llamadas a la API
  y manejo de estado.

  ══════════════════════════════════════════════════════════
  OBJETIVO
  ══════════════════════════════════════════════════════════

  Implementar dos secciones dentro del módulo de Cuenta Corriente:

  1. Configuración de Interés: ABM de configuraciones de tasa de interés
     (solo para usuarios con PERM:CC_MANAGE).

  2. Panel de Morosos: listado de clientes con deuda vencida, con acciones
     para aplicar interés individual o masivo
     (ver para PERM:CC_VIEW, aplicar para PERM:CC_MANAGE).

  ══════════════════════════════════════════════════════════
  ENDPOINTS DISPONIBLES
  ══════════════════════════════════════════════════════════

  Base URL: /api

  ── Configuración de Interés (/api/InterestConfig) ──────

  GET    /api/InterestConfig/interest-configs
    Respuesta: InterestConfigDTO[]
    {
      idConfig: number,
      nombre: string,
      porcentajeInteres: number,   // ej: 5.00 = 5%
      diaVencimiento: number,      // 1-28
      esActual: boolean
    }

  GET    /api/InterestConfig/interest-configs/current
    Respuesta: InterestConfigDTO  (la configuración activa)
    404 si no hay ninguna activa.

  GET    /api/InterestConfig/interest-configs/{idConfig}
    Respuesta: InterestConfigDTO

  POST   /api/InterestConfig/interest-configs
    Body: { nombre: string, porcentajeInteres: number, diaVencimiento: number }
    Respuesta: 201 Created

  PUT    /api/InterestConfig/interest-configs
    Body: { idConfig: number, nombre: string, porcentajeInteres: number, diaVencimiento: number }
    Respuesta: 200 OK

  PUT    /api/InterestConfig/interest-configs/set-current/{idConfig}
    Sin body.
    Respuesta: 200 OK
    Nota: desactiva todas las demás y marca esta como activa.

  ── Panel de Morosos / Aplicar Interés (/api/CurrentAccount) ──

  GET    /api/CurrentAccount/overdue-clients
    Respuesta: OverdueClientDTO[]
    {
      idCliente: number,
      nombreCliente: string,
      dni: string | null,
      cuit: string | null,
      saldoDeudor: number,
      importeInteres: number,    // calculado: saldoDeudor × (porcentaje/100)
      porcentajeInteres: number,
      diaVencimiento: number
    }
    Nota: retorna lista vacía si hoy <= diaVencimiento (aún no venció).
    400 con mensaje si no hay configuración activa.

  POST   /api/CurrentAccount/apply-interest/{clientId}
    Body: { idUsuarioRegistra: number }
    Respuesta: 200 "Interés aplicado: $XX.XX"
    400 con mensaje en caso de error (ya aplicado, sin deuda, sin config activa).

  POST   /api/CurrentAccount/apply-interest/bulk
    Body: { idUsuarioRegistra: number }
    Respuesta: 200 "Interés aplicado a X clientes. Y clientes con error."

  ══════════════════════════════════════════════════════════
  SECCIÓN 1 — Configuración de Interés
  ══════════════════════════════════════════════════════════

  Ubicación: dentro del módulo de Cuenta Corriente, tab o sección
  "Configuración de Interés" (solo visible para PERM:CC_MANAGE).

  ─────────────────────────────────────────────────────────
  1.1 — Lista de configuraciones
  ─────────────────────────────────────────────────────────

  Tabla con columnas:
  | Nombre | Tasa de interés | Día de vencimiento | Estado | Acciones |

  - "Tasa de interés": mostrar como "X%" (ej: "5.00%")
  - "Día de vencimiento": mostrar como "Día X" (ej: "Día 10")
  - "Estado": badge/chip "Activa" (verde) o "Inactiva" (gris).
    Solo UNA puede estar activa.
  - "Acciones":
      · Botón "Editar" → abre modal de edición.
      · Botón "Marcar como activa" → solo si EsActual == false.
        Muestra confirmación: "¿Marcar '[nombre]' como la configuración activa?
        Esto desactivará la configuración actual."

  Botón "Nueva configuración" en la parte superior (PERM:CC_MANAGE).

  ─────────────────────────────────────────────────────────
  1.2 — Modal: Crear / Editar configuración
  ─────────────────────────────────────────────────────────

  Campos:
  - Nombre (texto, obligatorio, max 100 chars)
  - Tasa de interés % (número decimal, entre 0.01 y 100, obligatorio)
  - Día de vencimiento (número entero, entre 1 y 28, obligatorio)
    Hint: "Los clientes que no paguen antes del día X del mes serán considerados morosos."

  Validaciones client-side antes de enviar:
  - Nombre no vacío
  - Tasa entre 0.01 y 100
  - Día entre 1 y 28

  Al guardar:
  - POST para crear, PUT para editar.
  - En caso de error 400 del backend, mostrar el mensaje recibido.
  - Refrescar la lista al cerrar el modal con éxito.

  ══════════════════════════════════════════════════════════
  SECCIÓN 2 — Panel de Morosos
  ══════════════════════════════════════════════════════════

  Ubicación: dentro del módulo de Cuenta Corriente, tab o sección
  "Panel de Morosos" (visible para PERM:CC_VIEW, acciones de aplicar
  para PERM:CC_MANAGE).

  ─────────────────────────────────────────────────────────
  2.1 — Estado de la configuración activa
  ─────────────────────────────────────────────────────────

  En la parte superior del panel, mostrar un bloque informativo con los datos
  de la ConfiguracionInteres activa (GET /interest-configs/current):

  Ejemplo:
  ┌─────────────────────────────────────────────────────────┐
  │  Configuración activa: "Interés Marzo 2026"             │
  │  Tasa: 5.00%   |   Vence el día 10 de cada mes         │
  └─────────────────────────────────────────────────────────┘

  Si no hay configuración activa (404), mostrar un aviso:
  "No hay configuración de interés activa. Configure una en la sección
  de configuración antes de aplicar interés."
  (con link/botón hacia la sección de Configuración de Interés si el usuario
  tiene PERM:CC_MANAGE).

  ─────────────────────────────────────────────────────────
  2.2 — Lista de morosos
  ─────────────────────────────────────────────────────────

  Tabla con columnas:
  | Cliente | DNI / CUIT | Saldo deudor | Interés a aplicar | Acción |

  - "Cliente": nombreCliente
  - "DNI / CUIT": mostrar DNI si existe, sino CUIT
  - "Saldo deudor": en formato moneda (ej: "$12.500,00")
  - "Interés a aplicar": en formato moneda (ej: "$625,00") + el porcentaje entre paréntesis (ej: "(5%)")
  - "Acción": botón "Aplicar interés" por fila (PERM:CC_MANAGE)

  Si la lista está vacía:
  - Si hoy <= diaVencimiento: "El plazo de pago aún no venció
    (vence el día {diaVencimiento}). No hay morosos por el momento."
  - Si hoy > diaVencimiento: "Todos los clientes tienen el interés aplicado
    este mes."

  ─────────────────────────────────────────────────────────
  2.3 — Botón "Aplicar a todos"
  ─────────────────────────────────────────────────────────

  Visible solo si la lista de morosos tiene al menos 1 elemento y el usuario
  tiene PERM:CC_MANAGE.

  Al hacer clic, mostrar confirmación:
  "¿Aplicar interés del X% a los {N} clientes morosos? Esta acción no se puede deshacer."

  Al confirmar:
  - POST /apply-interest/bulk con { idUsuarioRegistra: <id del usuario logueado> }
  - Mostrar el mensaje de respuesta del backend
    (ej: "Interés aplicado a 8 clientes. 0 clientes con error.")
  - Refrescar la lista de morosos.

  ─────────────────────────────────────────────────────────
  2.4 — Aplicar interés individual
  ─────────────────────────────────────────────────────────

  Al hacer clic en "Aplicar interés" de una fila:
  - Mostrar confirmación:
    "¿Aplicar interés de ${importeInteres} a {nombreCliente}?"
  - POST /apply-interest/{idCliente} con { idUsuarioRegistra: <id del usuario logueado> }
  - Toast de éxito con el mensaje del backend, o toast de error con el mensaje de error.
  - Refrescar la lista de morosos.

  ─────────────────────────────────────────────────────────
  2.5 — Recarga y loading
  ─────────────────────────────────────────────────────────

  - Al entrar al panel, cargar automáticamente la config activa y la lista de morosos.
  - Mostrar spinner mientras cargan.
  - Botón "Actualizar" para refrescar manualmente.

  ══════════════════════════════════════════════════════════
  NOTAS FINALES
  ══════════════════════════════════════════════════════════

  - El backend NO bloquea pagos si hay mora sin interés aplicado. El interés
    se registra independientemente del flujo de pagos.

  - El backend detecta automáticamente si el interés ya fue aplicado en el
    mes actual para cada cliente. Si se intenta aplicar dos veces, devuelve
    error 400 con el mensaje correspondiente.

  - El `idUsuarioRegistra` debe ser el ID del usuario logueado actualmente
    (extraído del JWT / contexto de autenticación).

  - El formato de moneda debe ser consistente con el resto del módulo de CC.

  - Si el backend devuelve 400 en alguna acción individual, mostrar el mensaje
    de error como toast, sin cerrar el panel.
```
