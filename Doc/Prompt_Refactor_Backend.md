# Prompt para Claude Code: Refactor de Proveedor y CompraProveedor


Hola Claude, necesito que refactoricemos el Backend de nuestro proyecto (ASP.NET Core 8) enfocado en los módulos `Proveedor` y `CompraProveedor`. 
El objetivo es adecuar el backend a un rediseño UX donde las compras serán inmutables (no se editarán, solo se anularán con un motivo obligatorio) y los módulos tendrán mejor seguridad y consistencia.

Ten en cuenta las reglas de arquitectura del proyecto definidas en `AGENTS.md` (Result Pattern, AutoMapper, capas estrictas, MessageProvider, etc.).

A continuación el detalle de los cambios que debes implementar:

## 1. Módulo Proveedor (`Features/Proveedor/`)

### 1.1. Seguridad
- Agrega el atributo `[Authorize]` a nivel de clase en `ProveedorController.cs`.

### 1.2. Consistencia en el Delete
- En `ProveedorController.Delete`, reemplaza la validación actual basada en string (`Convert.ToString(result.ErrorCode) == "proveedor_not_found"`) por el patrón correcto usando el `MessageProvider` y tipado fuerte con el enum correspondiente (igual que en los demás endpoints).

### 1.3. Consistencia de Estados
- En `ProveedorRepository.cs`, método `GetAll()`, cambia el filtro `p.FechaBaja == null` por `p.Activo == true`. Esto lo alínea de forma más consistente con el concepto visual del frontend.

---

## 2. Módulo CompraProveedor (`Features/CompraProveedor/`)

### 2.1. Soporte para fraccionar cantidades
- Cambia la propiedad `Cantidad` de tipo `int` a `decimal` en:
  - `CompraProveedorDetalleCreateDTO` (en `CompraProveedorCreateDTO.cs`)
  - `CompraProveedorDetalleResponseDTO` (en `CompraProveedorResponseDTO.cs`)
- En `CompraProveedorServices.cs`, actualiza la firma del método privado `CalcLinea` para que reciba `decimal cantidad`.

### 2.2. Inmutabilidad de las Compras (Borrar Update)
Las compras ya registradas NO se editan más.
- Elimina el método `Update` completo en `CompraProveedorServices.cs` y su interfaz.
- Elimina el método `UpdateAsync` en `CompraProveedorRepository.cs`.
- Elimina el endpoint `PUT /update` en `CompraProveedorController.cs`.
- Elimina el archivo `CompraProveedorUpdateDTO.cs` por completo ya que no se usará.

### 2.3. Endpoint de Anulación (en reemplazo del ToggleEstado)
La idea de "desactivar" silenciosamente una compra cambia por "Anular" con justificación.
- Elimina el endpoint `PATCH /toggle-estado` en `CompraProveedorController.cs` y su método en el servicio.
- Crea un nuevo DTO: `AnulacionCompraDTO` que tenga una propiedad `Motivo` (string, required).
- En `CompraProveedorController` crea el endpoint `POST /{idCompraProveedor:int}/anular` que reciba el `AnulacionCompraDTO`.
- En `CompraProveedorServices` crea el método `Anular(int idCompraProveedor, AnulacionCompraDTO dto)`:
  - Busca la compra. Si no existe o ya está inactiva, retorna error.
  - El string del `Motivo` se debe concatenar a la `Observacion` existente (ej: `Observacion = Observacion + " - ANULADO: " + dto.Motivo;`).
  - La anulación debe marcar `Activo = false` (o `FechaBaja`, según el estándar del proyecto para soft delete).
  - Al igual que antes, hay que llamar a `_stockMovementService.RegistrarMovimientoAsync` con enum `EgresoAnulacionCompra` (cantidades en negativo) para revertir el stock en el Ledger, indicando como referencia `ANULACION:COMPRA:{id}`.
  - MUY IMPORTANTE: Antes de guardar, asegurate de llamar a `SetAuditContextAsync()` para grabar en las variables de sesión de PostgreSQL quién anula la compra. Para recuperar el "usuario anulador", debes usar `_userContext.UserId`, manejando los posibles casteos o nulos según la inyección de `IUserContext`.

### 2.4. Filtro en Compras a Proveedores
- En `CompraProveedorRepository.cs`, modifica `GetAllWithDetailsAsync()` agregando el filtro `.Where(c => c.Activo)` para que no devuelva las compras anuladas junto con las vigentes.

### 2.5. Seguridad
- Agrega el atributo `[Authorize]` a nivel de clase en `CompraProveedorController.cs`.

### Entregable Dando el Cierre:
Por favor, asegúrate de que luego de aplicar todos estos cambios, la compilación de la solución fluya sin errores antes de devolver el control. Comprueba especialmente los casteos de `Cantidad` y `_userContext.UserId`.
```
