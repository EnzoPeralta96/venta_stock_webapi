using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Features.Audit.Repository
{
    public interface IAuditRepository
    {
        
        IQueryable<Auditoria> GetAuditQueryable(
            string? searchTerm, string? accion, string? entidadTipo,
            string? entidadId, int? idUsuario, DateTimeOffset? from, DateTimeOffset? to);
            
        
    }
}