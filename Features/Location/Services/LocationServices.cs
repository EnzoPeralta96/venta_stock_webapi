using AutoMapper;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Location.DTO;
using proyecto_venta_stock.Location.LocationRepository;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.Location.Services
{
    public class LocationServices : ILocationService
    {
        private readonly ILogger<LocationServices> _logger;
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;
        public LocationServices(ILocationRepository locationRepository, ILogger<LocationServices> logger, IMapper mapper)
        {
            _locationRepository = locationRepository;
            _logger = logger;
            _mapper = mapper;
        }
       public async Task<Result<PagedList<LocationDTO>>> SearchAsync(int pageIndex, int pageSize, string searchTerm, bool activos)
        {
            try
            {
                var query = _locationRepository.LocationsQueryable(searchTerm, activos);
                var projected = _mapper.ProjectTo<LocationDTO>(query);
                var paged = await PagedList<LocationDTO>.CreateAsync(projected, pageIndex, pageSize);
                return Result<PagedList<LocationDTO>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar ubicaciones");
                return Result<PagedList<LocationDTO>>.Failure(LocationErrorCode.unexpected_error);
            }
        }

        // 🔎 Obtener ubicación por ID
        public async Task<Result<LocationDTO>> GetByIdAsync(int id)
        {
            try
            {
                var ubicacion = await _locationRepository.GetByIdAsync(id);
                if (ubicacion == null)
                    return Result<LocationDTO>.Failure(LocationErrorCode.location_not_found);

                var dto = _mapper.Map<LocationDTO>(ubicacion);
                return Result<LocationDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ubicación");
                return Result<LocationDTO>.Failure(LocationErrorCode.unexpected_error);
            }
        }

        // ➕ Crear ubicación
        public async Task<Result<LocationDTO>> CreateAsync(LocationCreateUpdateDTO dto)
        {
            try
            {
                var exists = await _locationRepository.ExistsAsync(dto.Fila, dto.Seccion, dto.Nivel);
                if (exists)
                    return Result<LocationDTO>.Failure(LocationErrorCode.duplicate_location);

                var ubicacion = _mapper.Map<Ubicacion>(dto);
                ubicacion.Activo = true;

                await _locationRepository.CreateAsync(ubicacion);

                var created = _mapper.Map<LocationDTO>(ubicacion);
                return Result<LocationDTO>.Success(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear ubicación");
                return Result<LocationDTO>.Failure(LocationErrorCode.unexpected_error);
            }
        }

        // ✏️ Actualizar ubicación
        public async Task<Result<LocationDTO>> UpdateAsync(int id, LocationCreateUpdateDTO dto)
        {
            try
            {
                var ubicacion = await _locationRepository.GetByIdAsync(id);
                if (ubicacion == null)
                    return Result<LocationDTO>.Failure(LocationErrorCode.location_not_found);

                var inUse = await _locationRepository.IsInUseAsync(id);
                if (inUse)
                    return Result<LocationDTO>.Failure(LocationErrorCode.location_in_use);

                var exists = await _locationRepository.ExistsExceptIdAsync(id, dto.Fila, dto.Seccion, dto.Nivel);
                if (exists)
                    return Result<LocationDTO>.Failure(LocationErrorCode.duplicate_location);

                _mapper.Map(dto, ubicacion);
                await _locationRepository.UpdateAsync(ubicacion);

                var updated = _mapper.Map<LocationDTO>(ubicacion);
                return Result<LocationDTO>.Success(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar ubicación");
                return Result<LocationDTO>.Failure(LocationErrorCode.unexpected_error);
            }
        }

        // 🗑️ Eliminado lógico
        public async Task<Result<bool>> DeleteAsync(int id)
        {
            try
            {
                var ubicacion = await _locationRepository.GetByIdAsync(id);
                if (ubicacion == null)
                    return Result<bool>.Failure(LocationErrorCode.location_not_found);

                var inUse = await _locationRepository.IsInUseAsync(id);
                if (inUse)
                    return Result<bool>.Failure(LocationErrorCode.location_in_use);

                await _locationRepository.DeleteAsync(id);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar ubicación");
                return Result<bool>.Failure(LocationErrorCode.unexpected_error);
            }
        }

        // 🔄 Cambiar estado (activo/inactivo)
        public async Task<Result<bool>> ToggleActivoAsync(int id)
        {
            try
            {
                await _locationRepository.ToggleActivoAsync(id);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de la ubicación");
                return Result<bool>.Failure(LocationErrorCode.unexpected_error);
            }
        }
    }
}
