using AutoMapper;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Product.ProductRepository;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.UserRepository;
using venta_stock_webapi.Features.StockMovement.DTO;
using venta_stock_webapi.Features.StockMovement.Messages;
using venta_stock_webapi.Features.StockMovement.Repository;
using venta_stock_webapi.Shared.Paged;

namespace venta_stock_webapi.Features.StockMovement.Services;

public class StockMovementService : IStockMovementService
{
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly DbContext _dbContext;
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<StockMovementService> _logger;

    public StockMovementService(
        ILogger<StockMovementService> logger,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IMovimientoStockRepository movimientoStockRepository,
        IMapper mapper,
        VentaStockContext dbContext)
    {
        _logger = logger;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _movimientoStockRepository = movimientoStockRepository;
        _mapper = mapper;
        _dbContext = dbContext;

    }

    /// <summary>
    /// Registra un movimiento de stock para el producto indicado.
    /// Actualiza el caché Producto.Stock y persiste el movimiento en la tabla movimiento_stock.
    /// Si se llama dentro de una transacción existente, participa en ella.
    /// </summary>
    /// <param name="cantidad">
    /// Positivo para ingresos, negativo para egresos.
    /// El llamador es responsable de enviar el signo correcto según el tipo de movimiento.
    /// </param>
    public async Task<Result<bool>> RegistrarMovimientoAsync(
        int idProducto,
        TipoMovimientoStockEnum tipoMovimiento,
        decimal cantidad,
        string? referencia,
        int idUsuario)
    {
        // Si ya hay una transacción activa (ej: llamado desde SaleService), participamos en ella.
        // Si no hay ninguna (ej: ajuste-manual directo), abrimos una propia para que
        // el set_config y el SaveChanges queden dentro del mismo transaction scope,
        // garantizando que el trigger de auditoría reciba app.user_id correctamente.
        var ownTransaction = _dbContext.Database.CurrentTransaction == null;

        try
        {
            if (ownTransaction)
                await _dbContext.Database.BeginTransactionAsync();

            var user = await _userRepository.Exists(idUsuario);
            if (!user) return Result<bool>.Failure(UserErrorCode.user_not_found);

            var producto = await _productRepository.GetById(idProducto);
            if (producto is null)
                return Result<bool>.Failure(StockMovementErrorCode.producto_not_found);

            // Requerido por el trigger de auditoría para el UPDATE en producto.
            await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.user_id', {0}::text, true);",
                idUsuario);

            var nuevoStock = (producto.Stock ?? 0) + cantidad;
            producto.Stock = nuevoStock;

            var movimientoStock = new MovimientoStock
            {
                IdProducto = idProducto,
                IdTipoMovimientoStock = (int)tipoMovimiento,
                Cantidad = cantidad,
                StockResultante = nuevoStock,
                Fecha = DateTime.UtcNow,
                IdUsuario = idUsuario,
                Referencia = referencia
            };

            _movimientoStockRepository.Add(movimientoStock);
            await _movimientoStockRepository.SaveChangesAsync();

            if (ownTransaction)
                await _dbContext.Database.CurrentTransaction!.CommitAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            if (ownTransaction && _dbContext.Database.CurrentTransaction != null)
                await _dbContext.Database.CurrentTransaction.RollbackAsync();

            _logger.LogError(ex, "Error al registrar movimiento de stock para producto {IdProducto}", idProducto);
            return Result<bool>.Failure(StockMovementErrorCode.unexpected_error);
        }
    }

    public async Task<Result<List<TipoMovimientoStockDTO>>> GetTiposMovimientoAsync()
    {
        try
        {
            var tipos = await _movimientoStockRepository.GetTiposAsync();
            var result = _mapper.Map<List<TipoMovimientoStockDTO>>(tipos);
            return Result<List<TipoMovimientoStockDTO>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tipos de movimiento de stock");
            return Result<List<TipoMovimientoStockDTO>>.Failure(StockMovementErrorCode.unexpected_error);
        }
    }

    public async Task<Result<PagedList<MovimientoStockDTO>>> MovimientosPagedAsync(
        int idProducto,
        int pageIndex,
        int pageSize,
        int? idTipoMovimiento)
    {
        try
        {
            var producto = await _productRepository.GetById(idProducto);
            if (producto is null)
                return Result<PagedList<MovimientoStockDTO>>.Failure(StockMovementErrorCode.producto_not_found);

            var query = _movimientoStockRepository.MovementsQueryable(idProducto, idTipoMovimiento);
            var projected = _mapper.ProjectTo<MovimientoStockDTO>(query);
            var paged = await PagedList<MovimientoStockDTO>.CreateAsync(projected, pageIndex, pageSize);

            return Result<PagedList<MovimientoStockDTO>>.Success(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al paginar movimientos de stock para producto {IdProducto}", idProducto);
            return Result<PagedList<MovimientoStockDTO>>.Failure(StockMovementErrorCode.unexpected_error);
        }
    }
}
