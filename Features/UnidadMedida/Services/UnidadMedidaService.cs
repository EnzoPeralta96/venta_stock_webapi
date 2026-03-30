using AutoMapper;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Features.UnidadMedida.DTO;
using venta_stock_webapi.Features.UnidadMedida.Messages;
using venta_stock_webapi.Features.UnidadMedida.Repository;

namespace venta_stock_webapi.Features.UnidadMedida.Services;

public class UnidadMedidaService : IUnidadMedidaService
{
    private readonly IUnidadMedidaRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<UnidadMedidaService> _logger;
    private readonly VentaStockContext _context;

    public UnidadMedidaService(
        IUnidadMedidaRepository repository,
        IMapper mapper,
        ILogger<UnidadMedidaService> logger,
        VentaStockContext context)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
        _context = context;
    }

    public async Task<Result<List<UnidadMedidaDTO>>> GetAllAsync()
    {
        try
        {
            var unidades = await _repository.GetAllAsync();
            return Result<List<UnidadMedidaDTO>>.Success(_mapper.Map<List<UnidadMedidaDTO>>(unidades));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las unidades de medida");
            return Result<List<UnidadMedidaDTO>>.Failure(UnidadMedidaErrorCode.unexpected_error);
        }
    }

    public async Task<Result<List<UnidadMedidaDTO>>> GetActivosAsync()
    {
        try
        {
            var unidades = await _repository.GetActivosAsync();
            return Result<List<UnidadMedidaDTO>>.Success(_mapper.Map<List<UnidadMedidaDTO>>(unidades));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener unidades de medida activas");
            return Result<List<UnidadMedidaDTO>>.Failure(UnidadMedidaErrorCode.unexpected_error);
        }
    }

    public async Task<Result<UnidadMedidaDTO>> CreateAsync(CreateUnidadMedidaDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (await _repository.NombreDuplicadoAsync(dto.Nombre))
                return Result<UnidadMedidaDTO>.Failure(UnidadMedidaErrorCode.nombre_duplicado);

            if (await _repository.AbreviaturaDuplicadaAsync(dto.Abreviatura))
                return Result<UnidadMedidaDTO>.Failure(UnidadMedidaErrorCode.abreviatura_duplicada);

            var unidad = new proyecto_venta_stock.Models.UnidadMedida
            {
                Nombre = dto.Nombre,
                Abreviatura = dto.Abreviatura,
                Activo = true
            };

            _repository.Add(unidad);
            
            await _repository.SaveChangesAsync();

            await transaction.CommitAsync();
            return Result<UnidadMedidaDTO>.Success(_mapper.Map<UnidadMedidaDTO>(unidad));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al crear unidad de medida");
            return Result<UnidadMedidaDTO>.Failure(UnidadMedidaErrorCode.unexpected_error);
        }
    }

    public async Task<Result<UnidadMedidaDTO>> UpdateAsync(int id, UpdateUnidadMedidaDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var unidad = await _repository.GetByIdAsync(id);
            if (unidad is null)
                return Result<UnidadMedidaDTO>.Failure(UnidadMedidaErrorCode.unidad_not_found);

            if (await _repository.NombreDuplicadoAsync(dto.Nombre, excludeId: id))
                return Result<UnidadMedidaDTO>.Failure(UnidadMedidaErrorCode.nombre_duplicado);

            if (await _repository.AbreviaturaDuplicadaAsync(dto.Abreviatura, excludeId: id))
                return Result<UnidadMedidaDTO>.Failure(UnidadMedidaErrorCode.abreviatura_duplicada);

            unidad.Nombre = dto.Nombre;
            unidad.Abreviatura = dto.Abreviatura;

            await _repository.SaveChangesAsync();

            await transaction.CommitAsync();
            return Result<UnidadMedidaDTO>.Success(_mapper.Map<UnidadMedidaDTO>(unidad));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al actualizar unidad de medida {Id}", id);
            return Result<UnidadMedidaDTO>.Failure(UnidadMedidaErrorCode.unexpected_error);
        }
    }

    public async Task<Result<bool>> ToggleActivoAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var unidad = await _repository.GetByIdAsync(id);
            if (unidad is null)
                return Result<bool>.Failure(UnidadMedidaErrorCode.unidad_not_found);

            if (unidad.Activo && await _repository.EstaEnUsoAsync(id))
                return Result<bool>.Failure(UnidadMedidaErrorCode.unidad_en_uso);

            unidad.Activo = !unidad.Activo;
            await _repository.SaveChangesAsync();

            await transaction.CommitAsync();
            return Result<bool>.Success(unidad.Activo);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al cambiar estado de unidad de medida {Id}", id);
            return Result<bool>.Failure(UnidadMedidaErrorCode.unexpected_error);
        }
    }
}
