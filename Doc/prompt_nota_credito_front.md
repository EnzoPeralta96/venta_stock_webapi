# Prompt — Frontend: Integración Nota de Crédito (Anulación de Venta)

```
Contexto del proyecto:
  - Frontend React, integrado con el backend ASP.NET Core 8.
  - Ya existe la pantalla de listado de ventas (GET /api/Sale) con paginación y filtros.
  - Ya existe la pantalla de detalle de venta (GET /api/Sale/{id}).
  - Ya existe el módulo de Cuenta Corriente con historial de movimientos.
  - Ya existe visualización del PDF de movimientos CC vía GET /api/CurrentAccount/payment-receipt/{id}.

  ANTES DE IMPLEMENTAR:
  Revisa el código existente del frontend, específicamente:
  - Las pantallas/componentes de Ventas (listado y detalle).
  - Los componentes de Cuenta Corriente (historial de movimientos).
  - Cómo se maneja la autenticación JWT y los permisos en el frontend.
  - El patrón de llamadas a la API (axios / fetch / react-query, etc.)

  ══════════════════════════════════════════════════════════
  NUEVOS ENDPOINTS DISPONIBLES EN EL BACKEND
  ══════════════════════════════════════════════════════════

  ── Anulación de venta ──
  POST /api/Sale/{idVenta}/annul
  Policy: PERM:VEN_MANAGE
  Body:
  {
    "idMotivo": 1,                    // int, requerido
    "detalleAdicional": "texto libre", // string, opcional
    "idUsuarioRegistra": 5            // int, id del usuario logueado
  }
  Respuesta exitosa:
  {
    "idVenta": 42,
    "codigoVenta": "VENTA-20250310-0012",
    "estado": "Anulada",
    "idMovimientoNc": 135             // null si la venta fue al contado
  }
  Errores posibles (400 Bad Request con mensaje de texto):
  - "La venta indicada no existe."
  - "La venta indicada ya fue anulada previamente."
  - "El motivo de nota de crédito indicado no existe."
  - "El motivo de nota de crédito indicado no está activo."

  ── Motivos de Nota de Crédito ──
  GET  /api/CreditNoteReason/credit-note-reasons?activo=true   → lista de motivos activos
  GET  /api/CreditNoteReason/credit-note-reasons/{id}          → motivo por ID
  POST /api/CreditNoteReason/credit-note-reasons               → crear motivo (PERM:VEN_MANAGE)
  PUT  /api/CreditNoteReason/credit-note-reasons               → actualizar motivo (PERM:VEN_MANAGE)
  PUT  /api/CreditNoteReason/toggle-state/{id}/{activo}        → activar/desactivar (PERM:VEN_MANAGE)

  Respuesta GET (lista):
  [
    { "idMotivo": 1, "nombre": "Devolución de producto", "activo": true },
    { "idMotivo": 2, "nombre": "Error en la venta",      "activo": true },
    { "idMotivo": 3, "nombre": "Producto defectuoso",     "activo": true }
  ]

  ── PDF de Nota de Crédito (ya existente) ──
  GET /api/CurrentAccount/payment-receipt/{idMovimientoNc}
  Retorna: application/pdf
  Usar idMovimientoNc que viene en la respuesta de la anulación.

  ══════════════════════════════════════════════════════════
  CAMBIOS EN ENDPOINTS EXISTENTES
  ══════════════════════════════════════════════════════════

  GET /api/Sale/{id} (SaleResponseDTO) — el campo "estado" ahora puede ser:
    - "Aprobada" / "Completada" → venta activa
    - "Anulada"                 → venta anulada (mostrar badge rojo, deshabilitar botón anular)

  GET /api/CurrentAccount/movements/{clientId} (AccountMovementDTO) — ahora incluye:
    - "idMotivoNc": int | null
    - "motivoNc":   string | null   → ej. "Devolución de producto"
    Los movimientos de tipo "NOTA_CREDITO" (idTipoMovimiento=4) tendrán estos campos.

  ══════════════════════════════════════════════════════════
  CAMBIOS EN LA UI A IMPLEMENTAR
  ══════════════════════════════════════════════════════════

  ─────────────────────────────────────────────────────────
  1. Pantalla de Detalle de Venta
  ─────────────────────────────────────────────────────────

  Agregar el botón "Anular Venta" en la pantalla de detalle de venta.

  Reglas de visibilidad del botón:
  - Solo visible si el usuario tiene el permiso PERM:VEN_MANAGE.
  - Solo habilitado si el estado de la venta es "Aprobada" o "Completada".
  - Si estado == "Anulada": mostrar badge rojo "ANULADA" en lugar del botón.

  Al hacer clic en "Anular Venta", abrir un MODAL con:

  MODAL — Confirmar anulación de venta:
  ┌─────────────────────────────────────────────────────────────┐
  │  ⚠️ Anular Venta {codigoVenta}                              │
  │                                                             │
  │  Esta acción es irreversible. Se realizará:                 │
  │  • Restitución de stock de todos los productos              │
  │  • [Si CC] Nota de Crédito en la Cuenta Corriente           │
  │                                                             │
  │  Motivo *                                                   │
  │  [Dropdown con motivos activos de GET /credit-note-reasons] │
  │                                                             │
  │  Detalle adicional (opcional)                               │
  │  [Textarea]                                                 │
  │                                                             │
  │  [Cancelar]          [Confirmar anulación]                  │
  └─────────────────────────────────────────────────────────────┘

  Al confirmar:
  1. Llamar POST /api/Sale/{idVenta}/annul con el body.
  2. Si éxito:
     a. Mostrar toast/notificación de éxito: "Venta anulada correctamente."
     b. Si idMovimientoNc no es null:
        - Mostrar botón "Descargar NC" que llame a GET /api/CurrentAccount/payment-receipt/{idMovimientoNc}
          y abra el PDF en una nueva pestaña o lo descargue.
     c. Refrescar el detalle de la venta para mostrar el nuevo estado "Anulada".
  3. Si error: mostrar el mensaje de error retornado por el backend.

  ─────────────────────────────────────────────────────────
  2. Pantalla de Listado de Ventas
  ─────────────────────────────────────────────────────────

  El campo "estado" ya existe en la lista. Solo asegurarse de que:
  - Las ventas con estado "Anulada" muestren el badge con color rojo/gris distinto.
  - Si existe un filtro por estado, agregar la opción "Anuladas".

  ─────────────────────────────────────────────────────────
  3. Historial de CC del Cliente
  ─────────────────────────────────────────────────────────

  En la pantalla del historial de movimientos de cuenta corriente del cliente,
  los movimientos de tipo 4 (NOTA_CREDITO) ahora incluyen el campo motivoNc.

  Mostrar el motivo en la columna "Detalle" o como fila adicional del movimiento:
  - Si motivoNc no es null: mostrar como "NC — {motivoNc}: {detalle}"
  - Badge de color azul para movimientos de tipo NOTA_CREDITO.

  ─────────────────────────────────────────────────────────
  4. Sección de Gestión de Motivos NC (CRUD) — opcional según alcance
  ─────────────────────────────────────────────────────────

  Si el proyecto incluye una sección de configuración para administradores,
  agregar una pantalla de gestión de Motivos de Nota de Crédito.

  Ubicación sugerida: dentro de la sección de Configuración/Administración,
  junto a la gestión de Motivos de Nota de Débito.

  Pantalla con:
  - Tabla de motivos (id, nombre, activo).
  - Botón "Agregar motivo" → formulario con campo Nombre.
  - Botón editar → formulario con Nombre precargado.
  - Toggle activo/inactivo por fila.

  Endpoints a usar:
  - GET /api/CreditNoteReason/credit-note-reasons           → listar
  - POST /api/CreditNoteReason/credit-note-reasons          → crear
  - PUT /api/CreditNoteReason/credit-note-reasons           → actualizar
  - PUT /api/CreditNoteReason/toggle-state/{id}/{activo}    → toggle

  ══════════════════════════════════════════════════════════
  NOTAS PARA LA IMPLEMENTACIÓN
  ══════════════════════════════════════════════════════════

  - El ID del usuario logueado (idUsuarioRegistra) debe obtenerse del token JWT
    o del estado de autenticación global del frontend.

  - El campo idMovimientoNc en la respuesta de annul puede ser null si la venta
    fue pagada al contado. En ese caso no mostrar el botón de descargar NC.

  - Mostrar un spinner/loading durante la llamada a annul ya que es una operación
    que puede tardar (transacción con múltiples pasos en el backend).

  - Los motivos de NC deben cargarse cuando se abre el modal (llamada lazy),
    no en el mount inicial de la pantalla.
```
