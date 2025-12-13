using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.CurrentAccount.DTO;

namespace venta_stock_webapi.CurrentAccount.Services.AccountConfigService
{
    public interface IAccountConfigService
    {
        Task<Result<AccountConfigDTO>> GetAccountConfigById(int configId);
        Task<Result<List<AccountConfigDTO>>> GetAccountConfigs(bool? activo = null);
        Task<Result<string>> CreateAccountConfig(CreateAccountConfigDTO accountConfigDTO);
        Task<Result<string>> UpdateAccountConfig(UpdateAccountConfigDTO accountConfigDTO);
        Task<Result<string>> ToggleStateAccountConfig(int configId, bool active);
    }
}