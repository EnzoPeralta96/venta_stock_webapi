namespace venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount
{
    public class SaleStrategy : IMovementStrategy
    {
        public CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount)
        {
            decimal newBalance = oldBalance + amount;

            // El saldo a favor absorbe la venta primero; solo se descuenta del limite lo que exceda ese saldo
            decimal saldoAFavor = Math.Max(0, -oldBalance);
            decimal amountFromLimit = Math.Max(0, amount - saldoAFavor);
            decimal newLimit = oldLimit - amountFromLimit;

            if (newLimit < 0)
                throw new InvalidOperationException("Insufficient credit limit for this sale.");

            return new CalculationResult(newBalance, newLimit);
        }
    }

    public class PaymentStrategy : IMovementStrategy
    {
        public CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount)
        {
            decimal newBalance = oldBalance - amount;
            decimal newLimit = oldLimit + amount;

            return new CalculationResult(newBalance, newLimit);
        }
    }

    public class DebitNoteStrategy : IMovementStrategy
    {
        public CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount)
        {
            decimal newBalance = oldBalance + amount;
            decimal saldoAFavor = Math.Max(0, -oldBalance);
            decimal amountFromLimit = Math.Max(0, amount - saldoAFavor);
            decimal newLimit = oldLimit - amountFromLimit;
            return new CalculationResult(newBalance, newLimit);
        }
    }

    public class CreditNoteStrategy : IMovementStrategy
    {
        public CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount)
        {
            decimal newBalance = oldBalance - amount;
            // Solo restaurar el límite por la parte que reduce deuda real; el exceso genera saldo a favor sin tocar el límite
            decimal deudaActual = Math.Max(0, oldBalance);
            decimal amountToRestoreLimit = Math.Min(amount, deudaActual);
            decimal newLimit = oldLimit + amountToRestoreLimit;
            return new CalculationResult(newBalance, newLimit);
        }
    }

    public class LimitModificationStrategy : IMovementStrategy
    {
        // amount = nuevo límite total asignado; saldo no cambia; limite_cuenta = nuevo total.
        public CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount)
        {
            return new CalculationResult(oldBalance, amount);
        }
    }
}