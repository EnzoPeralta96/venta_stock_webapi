using AutoMapper;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.ListaPrecio.DTO;
using proyecto_venta_stock.ListaPrecio.ListaPrecioRepository;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Shared.ResultPattern;

namespace proyecto_venta_stock.ListaPrecio.Services;

public class ListaPrecioServices : IListaPrecioServices
{
    private readonly IListaPrecioRepository _repository;
    private readonly ILogger<ListaPrecioServices> _logger;
    private readonly VentaStockContext _context;
    private readonly IMapper _mapper;

    public ListaPrecioServices(
        IListaPrecioRepository repository,
        ILogger<ListaPrecioServices> logger,
        IMapper mapper,
        VentaStockContext context)
    {
        _repository = repository;
        _logger = logger;
        _mapper = mapper;
        _context = context;
    }

    public async Task<Result<List<ListaPrecioDTO>>> GetByProveedorAsync(int idProveedor)
    {
        try
        {
            var listas = await _repository.GetByProveedorAsync(idProveedor);
            var dtos = _mapper.Map<List<ListaPrecioDTO>>(listas);
            return Result<List<ListaPrecioDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado: " + ex);
            return Result<List<ListaPrecioDTO>>.Failure(ListaPrecioErrorCode.error_inesperado);
        }
    }

    public async Task<Result<ListaPrecioDTO>> GetByIdAsync(int idLista)
    {
        try
        {
            var lista = await _repository.GetByIdAsync(idLista);
            if (lista is null)
                return Result<ListaPrecioDTO>.Failure(ListaPrecioErrorCode.lista_not_found);

            var dto = _mapper.Map<ListaPrecioDTO>(lista);
            return Result<ListaPrecioDTO>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado: " + ex);
            return Result<ListaPrecioDTO>.Failure(ListaPrecioErrorCode.error_inesperado);
        }
    }

    public async Task<Result<bool>> CreateAsync(ListaPrecioCreateDTO dto, int idUsuario)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var nameInUse = await _repository.ExistsNameAsync(dto.IdProveedor, dto.Nombre);

            if (nameInUse)
                return Result<bool>.Failure(ListaPrecioErrorCode.lista_name_in_use);

            var entity = _mapper.Map<Models.ListaPrecio>(dto);

            entity.IdUsuarioRegistra = idUsuario;

            await _repository.CreateAsync(entity);

            await transaction.CommitAsync();

            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Error inesperado: " + ex);
            return Result<bool>.Failure(ListaPrecioErrorCode.error_inesperado);
        }
    }

    public async Task<Result<bool>> UpdateAsync(ListaPrecioUpdateDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _repository.GetByIdAsync(dto.IdLista);

            if (existing is null)
                return Result<bool>.Failure(ListaPrecioErrorCode.lista_not_found);

            var nameInUse = await _repository.ExistsNameAsync(existing.IdProveedor!.Value, dto.Nombre, excludeId: dto.IdLista);

            if (nameInUse)
                return Result<bool>.Failure(ListaPrecioErrorCode.lista_name_in_use);

            existing.Nombre = dto.Nombre;
            existing.Observaciones = dto.Observaciones;
            existing.IvaPorDefecto = dto.IvaPorDefecto;

            await _repository.UpdateAsync(existing);
            await transaction.CommitAsync();
            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Error inesperado: " + ex);
            return Result<bool>.Failure(ListaPrecioErrorCode.error_inesperado);
        }
    }

    public async Task<Result<bool>> DeleteAsync(int idLista)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _repository.GetByIdAsync(idLista);

            if (existing is null)
                return Result<bool>.Failure(ListaPrecioErrorCode.lista_not_found);

            if (await _repository.HasItemsAsync(idLista))
                return Result<bool>.Failure(ListaPrecioErrorCode.lista_tiene_items);

            await _repository.DeleteAsync(existing);

            await transaction.CommitAsync();

            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Error inesperado: " + ex);
            return Result<bool>.Failure(ListaPrecioErrorCode.error_inesperado);
        }
    }

    public async Task<Result<bool>> ToggleActivoAsync(int idLista)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _repository.GetByIdAsync(idLista);
            if (existing is null)
                return Result<bool>.Failure(ListaPrecioErrorCode.lista_not_found);

            existing.Activo = !existing.Activo;

            await _repository.UpdateAsync(existing);
            await transaction.CommitAsync();

            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError("Error inesperado: " + ex);
            return Result<bool>.Failure(ListaPrecioErrorCode.error_inesperado);
        }
    }
}
