using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace venta_stock_webapi.Shared.Identity
{
    public interface IUserContext
    {
        int UserId {get;}
        string? UserName {get;}
        bool IsAuthenticated {get;}
    }
}