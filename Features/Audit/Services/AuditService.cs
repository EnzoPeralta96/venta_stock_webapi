using AutoMapper;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Features.Audit.DTO;
using venta_stock_webapi.Features.Audit.Repository;
using venta_stock_webapi.Shared.Paged;

namespace venta_stock_webapi.Features.Audit.Services
{
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> _logger;
        private readonly IAuditRepository _auditRepository;
        private readonly IMapper _mapper;
        public AuditService(IAuditRepository auditRepository, IMapper mapper, ILogger<AuditService> logger)
        {
            _auditRepository = auditRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PagedList<AuditItemDTO>>> SearchAsync(int pageIndex, int pageSize, string searchTerm, string accion, string entidadTipo, string entidadId, int? idUsuario, DateTimeOffset? from, DateTimeOffset? to)
        {
            try
            {
                var query = _auditRepository.GetAuditQueryable(searchTerm, accion, entidadTipo, entidadId, idUsuario, from, to);

                // 1. Contar total de registros (antes de paginar)
                var totalCount = await query.CountAsync();

                // 2. Aplicar paginación y materializar desde la DB
                var auditorias = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 3. Mapear en memoria (después de materializar)
                var items = _mapper.Map<List<AuditItemDTO>>(auditorias);

                // 4. Crear PagedList manualmente
                var paged = new PagedList<AuditItemDTO>(items, totalCount, pageIndex, pageSize);

                return Result<PagedList<AuditItemDTO>>.Success(paged);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<PagedList<AuditItemDTO>>.Failure(AuditErrorCode.unexpected_error);
            }

        }
    }
}