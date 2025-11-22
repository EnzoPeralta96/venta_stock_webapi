using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Cliente.Repository
{
    public interface IAccountMovementRepository
    {
        Task CreateAccount(MovimientoCc movimiento);
    }
}