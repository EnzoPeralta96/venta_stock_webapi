# Prompt — Refactor: Paginación y Resumen de Cuenta Corriente

> Copiar este prompt completo y pegarlo en Claude para que implemente los cambios.

```
Contexto del proyecto:
- ASP.NET Core 8, EF Core, PostgreSQL, patrón Repository + Service + Result<T>
- El módulo de Cuenta Corriente tiene el endpoint GET /api/CurrentAccount/movements/{clientId} que actualmente devuelve TODOS los movimientos del cliente sin paginar.
- El front deriva dos valores críticos de la lista completa:
    opening = movimiento de tipo alta_cliente (IdTipoMovimiento = 2)
    latest  = último movimiento por fecha
  Estos se usan para las cards de resumen (deuda, disponible, saldo a favor).
- Si paginamos el endpoint actual, el front pierde acceso a opening y latest.
- Solución: separar en dos endpoints.

══════════════════════════════════════════════════════════
PARTE A — Nuevo endpoint de resumen
══════════════════════════════════════════════════════════

1. Crear DTO:
   Features/CurrentAccount/DTO/MovementDTO/AccountSummaryDTO.cs

   public class AccountSummaryDTO
   {
       public AccountMovementDTO Opening { get; set; }  // movimiento alta_cliente
       public AccountMovementDTO Latest  { get; set; }  // último movimiento real
   }

2. Agregar método a IAccountMovementRepository:
   Task<(MovimientoCc opening, MovimientoCc latest)> GetAccountSummaryAsync(int clientId);

3. Implementar en AccountMovementRepository:
   - opening: primer movimiento donde IdTipoMovimiento == 2
   - latest: movimiento con Fecha más reciente (ThenByDescending IdMovimiento como desempate). Idealmente excluir IdTipoMovimiento == 2 para que sea el último movimiento operativo real (si no hay otro, que sea null).
   - Ambos con AsNoTracking() e Include de navegaciones necesarias para el mapeo a AccountMovementDTO:
     (IdEstadoNavigation, IdTipoMovimientoNavigation, IdUsuarioRegistraNavigation, IdVentaNavigation con ThenInclude IdEstadoNavigation, IdMotivoNcNavigation si existe)

4. Agregar método a ICurrentAccountService:
   Task<Result<AccountSummaryDTO>> GetAccountSummaryAsync(int clientId);

5. Implementar en CurrentAccountService:
   - Verificar que el cliente existe (usar _clientRepository.ExistsByIdAsync)
   - Llamar al repositorio
   - Si opening es null → Failure(CurrentAccountCode.account_not_found)
   - Mapear con AutoMapper a AccountSummaryDTO (tener en cuenta que latest puede ser null)
   - Retornar Result<AccountSummaryDTO>.Success(...)

6. Agregar endpoint en CurrentAccountController:
   [Authorize(Policy = "PERM:CC_VIEW")]
   [HttpGet("summary/{clientId}")]
   public async Task<IActionResult> GetAccountSummary(int clientId)

══════════════════════════════════════════════════════════
PARTE B — Endpoint de movimientos paginado con búsqueda y filtros
══════════════════════════════════════════════════════════

1. Modificar IAccountMovementRepository:
   Reemplazar la firma actual de GetMovements por:

   Task<PagedList<MovimientoCc>> GetMovementsPagedAsync(
       int clientId,
       int pageIndex,
       int pageSize,
       string searchTerm,
       DateTime? fechaDesde,
       DateTime? fechaHasta,
       int? idTipoMovimiento);

2. Implementar en AccountMovementRepository:
   Query base (IQueryable):
   - Filtrar IdCliente == clientId
   - Excluir tipo alta_cliente: IdTipoMovimiento != 2
   - Include de todas las navegaciones necesarias
   - OrderByDescending Fecha, ThenByDescending IdMovimiento

   Filtros opcionales (aplicar solo si el parámetro tiene valor):
   - searchTerm: Detalle.ToLower().Contains(searchTerm.ToLower())
                 OR (IdVentaNavigation != null && IdVentaNavigation.CodigoVenta.ToLower().Contains(searchTerm.ToLower()))
   - fechaDesde: Fecha >= fechaDesde
   - fechaHasta: Fecha <= fechaHasta.Value.Date.AddDays(1).AddTicks(-1) (inclusivo)
   - idTipoMovimiento: IdTipoMovimiento == idTipoMovimiento.Value

   Retornar: await PagedList<MovimientoCc>.CreateAsync(query, pageIndex, pageSize)
   Usar AsNoTracking() en la query inicial.

3. Modificar ICurrentAccountService:
   Reemplazar firma de GetAccountMovementsByClientId por:

   Task<Result<PagedList<AccountMovementDTO>>> GetAccountMovementsPagedAsync(
       int clientId,
       int pageIndex,
       int pageSize,
       string searchTerm,
       DateTime? fechaDesde,
       DateTime? fechaHasta,
       int? idTipoMovimiento);

4. Implementar en CurrentAccountService:
   - Verificar si el cliente existe
   - Llamar al repositorio con los parámetros
   - La base PagedList<T> ya tiene constructor public PagedList(List<T> items, int count, int pagedIndex, int pageSize)
   - Proyectar los items: var mappedItems = _mapper.Map<List<AccountMovementDTO>>(pagedListFromRepo.Items);
   - Construir el nuevo PagedList de DTOs manualmente:
     var pagedDtos = new PagedList<AccountMovementDTO>(mappedItems, pagedListFromRepo.TotalCount, pageIndex, pageSize);
   - Retornar Result con pagedDtos

5. Modificar endpoint en CurrentAccountController:
   [Authorize(Policy = "PERM:CC_VIEW")]
   [HttpGet("movements/{clientId}")]
   public async Task<IActionResult> GetAccountMovementsPaged(
       int clientId,
       [FromQuery] int pageIndex = 1,
       [FromQuery] string searchTerm = "",
       [FromQuery] DateTime? fechaDesde = null,
       [FromQuery] DateTime? fechaHasta = null,
       [FromQuery] int? idTipoMovimiento = null)
   {
       int pageSize = 10;
       // Llamar a GetAccountMovementsPagedAsync y retornar Ok(result.Value)
   }

══════════════════════════════════════════════════════════
NOTAS IMPORTANTES
══════════════════════════════════════════════════════════
- El endpoint GET /api/CurrentAccount/summary/{clientId} es el único que devuelve el movimiento alta_cliente. El de movements paginado nunca lo incluye.
- El PagedList ya existe en Shared/Paged/PagedList.cs y tiene el constructor necesario.
- Mantener el mismo manejo de errores y MessageProvider que el resto del módulo.
- No eliminar nada del repositorio hasta confirmar que el front no usa métodos viejos si afecta a otro componente.
```
