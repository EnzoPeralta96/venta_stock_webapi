namespace venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount
{
    public class SaleStrategy : IMovementStrategy
    {

        public CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount)
        {
           decimal newBalance = oldBalance + amount;
           decimal newLimit = oldLimit - amount;

            if (newLimit < 0)
            {
                throw new InvalidOperationException("Insufficient credit limit for this sale.");
            }

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

    public class InterestStrategy : IMovementStrategy
    {
        // Tratado igual que ND: sube deuda y baja disponible.
        // Esto mantiene la invariante que usa el front: creditoDisponible = limiteTotal - saldoActual.
        public CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount)
        {
            decimal newBalance = oldBalance + amount;
            decimal newLimit = oldLimit - amount;

            return new CalculationResult(newBalance, newLimit);
        }
    }

    public class DebitNoteStrategy : IMovementStrategy
    {
        public CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount)
        {
            //Se debe controlar si la nota de debito es por un 
            decimal newBalance = oldBalance + amount;
            decimal newLimit = oldLimit - amount;
            return new CalculationResult(newBalance, newLimit);
        }
    }

    public class CreditNoteStrategy : IMovementStrategy
    {
        public CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount)
        {
            //Se debe controlar si la nota de credito es una bonificacion
            // o si es una devolucion de productos ejecutar el procedimiento que devuelve el stok de los
            //productos involucrados en la venta.
            decimal newBalance = oldBalance - amount;
            decimal newLimit = oldLimit + amount;

            return new CalculationResult(newBalance, newLimit);
        }
    }

    public class LimitModificationStrategy : IMovementStrategy
    {
        // amount actúa como el nuevo límite absoluto.
        // El saldo adeudado no cambia; solo se reemplaza el límite.
        public CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount)
        {
            return new CalculationResult(oldBalance, amount - oldBalance);
        }
    }
}