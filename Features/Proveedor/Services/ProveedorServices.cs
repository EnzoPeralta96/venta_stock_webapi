using AutoMapper;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Proveedor.DTO;
using proyecto_venta_stock.Proveedor.ProveedorRepository;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.Proveedor.Services
{
    public class ProveedorServices : IProveedorServices
    {
        private readonly ILogger<ProveedorServices> _logger;
        private readonly IProveedorRepository _proveedorRepository;
        private readonly IMapper _mapper;

        public ProveedorServices(
            IProveedorRepository proveedorRepository,
            ILogger<ProveedorServices> logger,
            IMapper mapper)
        {
            _proveedorRepository = proveedorRepository;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<bool>> Create(ProveedorDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);

                var exists = await _proveedorRepository.Exists(dto.Nombre);
                if (exists) return Result<bool>.Failure(ProveedorErrorCode.proveedor_name_in_use);

                var entity = _mapper.Map<Models.Proveedor>(dto);
                await _proveedorRepository.Create(entity);

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> Update(ProveedorDTO dto)
        {
            try
            {
                var existing = await _proveedorRepository.GetById(dto.IdProveedor);
                if (existing == null) return Result<bool>.Failure(ProveedorErrorCode.proveedor_not_found);

                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);

                var exists = await _proveedorRepository.Exists(dto.Nombre, excludeId: dto.IdProveedor);
                if (exists) return Result<bool>.Failure(ProveedorErrorCode.proveedor_name_in_use);

                // update manual (similar a ProductServices)
                existing.Proveedor1 = dto.Nombre;
                existing.Direccion = dto.Direccion;
                existing.Telefono = dto.Telefono;

                await _proveedorRepository.Update(existing);

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<List<ProveedorDTO>>> GetAll()
        {
            try
            {
                var list = await _proveedorRepository.GetAll();
                var dtos = _mapper.Map<List<ProveedorDTO>>(list);
                return Result<List<ProveedorDTO>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<List<ProveedorDTO>>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<ProveedorDTO>> GetById(int idProveedor)
        {
            try
            {
                var entity = await _proveedorRepository.GetById(idProveedor);
                if (entity == null) return Result<ProveedorDTO>.Failure(ProveedorErrorCode.proveedor_not_found);

                var dto = _mapper.Map<ProveedorDTO>(entity);
                return Result<ProveedorDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<ProveedorDTO>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> Delete(int idProveedor)
        {
            try
            {
                var existing = await _proveedorRepository.GetById(idProveedor);
                if (existing == null) return Result<bool>.Failure(ProveedorErrorCode.proveedor_not_found);

                await _proveedorRepository.Delete(existing);
                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<PagedList<ProveedorDTO>>> ProveedoresPagedAsync(int pageIndex, int pageSize, string searchTerm, string estado = "activos")
        {
            try
            {
                var query = _proveedorRepository.ProveedoresQueryable(searchTerm);

                if (estado.ToLower() == "activos")
                    query = query.Where(p => p.Activo);
                else if (estado.ToLower() == "eliminados")
                    query = query.Where(p => !p.Activo);

                var projected = _mapper.ProjectTo<ProveedorDTO>(query);
                var paged = await PagedList<ProveedorDTO>.CreateAsync(projected, pageIndex, pageSize);

                return Result<PagedList<ProveedorDTO>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<PagedList<ProveedorDTO>>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> ToggleEstado(int idProveedor)
        {
            try
            {
                var existing = await _proveedorRepository.GetById(idProveedor);
                if (existing == null)
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_not_found);

                existing.Activo = !existing.Activo;
                if (existing.Activo) existing.FechaBaja = null; // al reactivar, limpiar fecha de baja
                await _proveedorRepository.Update(existing);

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }
    }
}