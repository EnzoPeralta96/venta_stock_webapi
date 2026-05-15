using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Features.Audit.DTO;
using venta_stock_webapi.Shared.Paged;

namespace venta_stock_webapi.Features.Audit.Services
{
    public interface IAuditService
    {
        Task<Result<PagedList<AuditItemDTO>>> SearchAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm,
        string? accion,
        string? entidadTipo,
        string? entidadId,
        int? idUsuario,
        DateTimeOffset? from,
        DateTimeOffset? to
    );
    }
}