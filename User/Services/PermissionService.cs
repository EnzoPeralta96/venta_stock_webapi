using AutoMapper;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.DTO;
using proyecto_venta_stock.User.Repository.PermitRepository;
using venta_stock_webapi.User.Message;

namespace venta_stock_webapi.User.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(IPermissionRepository permissionRepository, IMapper mapper, ILogger<PermissionService> logger)
        {
            _permissionRepository = permissionRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<PermissionsCategoryDTO>>> GetPermissions(int? id_category_permission)
        {
            try
            {
                var categories = await _permissionRepository.GetPermissions(id_category_permission);
                var permissionsCategoryDTO = _mapper.Map<List<PermissionsCategoryDTO>>(categories);
                return Result<List<PermissionsCategoryDTO>>.Succes(permissionsCategoryDTO);    
            }
            catch (System.Exception ex)
            {
                 _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<PermissionsCategoryDTO>>.Failure(PermissionErrorCode.unexpected_error);
                throw;
            }
        }
    }
}