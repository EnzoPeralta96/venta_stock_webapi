using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Location.DTO;
using proyecto_venta_stock.Location.LocationRepository;

namespace proyecto_venta_stock.Location.Services
{
    public class LocationServices : ILocationServices
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
        public async Task<Result<bool>> Create(LocationDTO locationDTO)
        {
            try
            {
                bool locationExists = await _locationRepository.Exists(locationDTO.Fila, locationDTO.Seccion, locationDTO.Nivel);
                if (locationExists)
                {
                    _logger.LogWarning("Location already exists");
                    return Result<bool>.Failure("location_already_exists");
                }

                var location = _mapper.Map<Ubicacion>(locationDTO);
                await _locationRepository.Create(location);
                return Result<bool>.Succes(true);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure("error_inesperado");
            }
        }

        public async Task<Result<List<LocationDTO>>> GetAll()
        {
            try
            {
                var locations = await _locationRepository.GetAll();
                var locationDTOs = _mapper.Map<List<LocationDTO>>(locations);
                return Result<List<LocationDTO>>.Succes(locationDTOs);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<LocationDTO>>.Failure("error_inesperado");
            }
        }

        public async Task<Result<LocationDTO>> GetById(int idUbicacion)
        {
            try
            {
                var location = await _locationRepository.GetById(idUbicacion);
                if (location == null) return Result<LocationDTO>.Failure("location_not_found");
                var locationDTO = _mapper.Map<LocationDTO>(location);
                return Result<LocationDTO>.Succes(locationDTO);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<LocationDTO>.Failure("error_inesperado");
            }
        }

        public async Task<Result<bool>> Update(LocationDTO locationDTO)
        {
            try
            {
                var existingLocation = await _locationRepository.GetById(locationDTO.IdUbicacion);
                if (existingLocation == null)
                    return Result<bool>.Failure("location_not_found");

                // validar unicidad (fila, seccion, nivel) excluyendo este Id
                if (await _locationRepository.ExistsExceptId(locationDTO.IdUbicacion, locationDTO.Fila, locationDTO.Seccion, locationDTO.Nivel))
                    return Result<bool>.Failure("location_already_exists");

                // aplicar cambios sobre la entidad existente (trackeada)
                _mapper.Map(locationDTO, existingLocation);

                await _locationRepository.Update(existingLocation);
                return Result<bool>.Succes(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex);
                return Result<bool>.Failure("error_inesperado");
            }
        }

        public async Task<Result<bool>> Delete(int idUbicacion)
        {
            try
            {
                var existing = await _locationRepository.GetById(idUbicacion);
                if (existing == null)
                    return Result<bool>.Failure("location_not_found");

                await _locationRepository.Delete(existing);
                return Result<bool>.Succes(true);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                // Probable FK en uso (productos referenciando la ubicación)
                _logger.LogWarning(dbEx, "Location in use, cannot delete");
                return Result<bool>.Failure("location_in_use");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex);
                return Result<bool>.Failure("error_inesperado");
            }
        }
    }
}
