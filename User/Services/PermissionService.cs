using AutoMapper;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.Repository.PermitRepository;
using venta_stock_webapi.User.DTO;

namespace venta_stock_webapi.User.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permitRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(IPermissionRepository permitRepository, IMapper mapper)
        {
            _permitRepository = permitRepository;
            _mapper = mapper;
        }

        public async Task<Result<List<PermissionDTO>>> GetPermissions(int id_category_permission)
        {
            try
            {
                var permissions = await _permitRepository.GetPermissions(id_category_permission);
                var permissionsDTO = _mapper.Map<List<PermissionDTO>>(permissions);
                return Result<List<PermissionDTO>>.Succes(permissionsDTO);    
            }
            catch (System.Exception ex)
            {
                 _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<PermissionDTO>>.Failure("error_inerperado");
                throw;
            }
        }
    }
}