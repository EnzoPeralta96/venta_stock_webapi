using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;

namespace venta_stock_webapi.CurrentAccount.Services.CurrentAccountService
{
    public interface ICurrentAccountService
    {
        Task <Result<List<AccountMovementDTO>>> GetAccountMovementsByClientId(int clientId);
        Task<Result<string>> CreateAccountMovement(CreateCurrentAccountDTO accountMovementDTO);
        Task<Result<bool>> RegisterMovement(AddMovementDTO addMovementDTO);

        Task<Result<List<TypeMovementDTO>>> GetMovementTypes();
    }
}