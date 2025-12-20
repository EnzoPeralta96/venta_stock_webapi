using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Features.Audit.Repository
{
    public class AuditRepository : IAuditRepository
    {
        private readonly VentaStockContext _dbContext;
        public AuditRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IQueryable<Auditoria> GetAuditQueryable(string searchTerm, string accion, string entidadTipo, string entidadId, int? idUsuario, DateTimeOffset? from, DateTimeOffset? to)
        {
            var query = _dbContext.Auditorias
                        .AsNoTracking();

            /*if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(a =>
                    (a.Detalle != null && a.Detalle.ToLower().Contains(term)) ||
                    (a.UsuarioNombre != null && a.UsuarioNombre.ToLower().Contains(term)) ||
                    a.Accion.ToLower().Contains(term) ||
                    a.EntidadTipo.ToLower().Contains(term));
            }*/

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var pattern = $"%{searchTerm.Trim()}%";

                query = _dbContext.Auditorias
                    .FromSqlInterpolated($@"
                    SELECT *
                    FROM auditoria a
                    WHERE
                        (a.detalle IS NOT NULL AND a.detalle ILIKE {pattern})
                        OR (a.usuario_nombre IS NOT NULL AND a.usuario_nombre ILIKE {pattern})
                        OR (a.accion ILIKE {pattern})
                        OR (a.entidad_tipo ILIKE {pattern})
                        OR (a.valores_nuevos::text ILIKE {pattern})
                        OR (a.valores_anteriores::text ILIKE {pattern})
                    ")
                    .AsNoTracking();
            }

            if (!string.IsNullOrWhiteSpace(accion))
                query = query.Where(a => a.Accion == accion);

            if (!string.IsNullOrWhiteSpace(entidadTipo))
                query = query.Where(a => a.EntidadTipo == entidadTipo);

            if (!string.IsNullOrWhiteSpace(entidadId))
                query = query.Where(a => a.EntidadId == entidadId);

            if (idUsuario.HasValue)
                query = query.Where(a => a.IdUsuario == idUsuario.Value);

            if (from.HasValue)
                query = query.Where(a => a.FechaHora >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.FechaHora <= to.Value);

            return query.OrderByDescending(a => a.FechaHora);
        }
    }
}