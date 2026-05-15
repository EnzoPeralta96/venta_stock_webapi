using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount
{
    public record CalculationResult(decimal NewBalance, decimal NewLimit);
    public interface IMovementStrategy
    {
        CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount);
    }
}