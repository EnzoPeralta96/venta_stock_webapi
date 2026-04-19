namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO
{
    public class AccountSummaryDTO
    {
        public AccountMovementDTO Opening { get; set; }
        public AccountMovementDTO? Latest { get; set; }
        public AccountMovementDTO? LatestLimitModification { get; set; }
    }
}
