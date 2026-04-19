using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;
using venta_stock_webapi.Shared.Paged;

namespace venta_stock_webapi.CurrentAccount.Services.CurrentAccountService
{
    public interface ICurrentAccountService
    {
        Task<Result<List<AccountMovementDTO>>> GetAccountMovementsByClientId(int clientId);

        Task<Result<AccountSummaryDTO>> GetAccountSummaryAsync(int clientId);

        Task<Result<PagedList<AccountMovementDTO>>> GetAccountMovementsPagedAsync(
            int clientId,
            int pageIndex,
            int pageSize,
            string searchTerm,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            int? idTipoMovimiento);
        Task<Result<string>> CreateAccountMovement(CreateCurrentAccountDTO accountMovementDTO);

        // Retorna el IdMovimiento del nuevo registro para que el front pueda pedir el PDF de inmediato.
        Task<Result<int>> RegisterMovement(AddMovementDTO addMovementDTO);

        Task<Result<List<TypeMovementDTO>>> GetMovementTypes();
        Task<Result<List<PendingSalePaymentDTO>>> GetPendingSalesForPayment(int clientId);
        Task<Result<byte[]>> GeneratePaymentReceiptAsync(int idMovimiento);
        Task<Result<int>> AnnulPayment(AnnulPaymentDTO dto);

        // Registra una Nota de Débito con validaciones específicas (motivo, venta opcional).
        Task<Result<int>> RegisterDebitNote(RegisterDebitNoteDTO dto);

        // Devuelve clientes con deuda vencida que aún no tienen interés aplicado en el mes actual.
        Task<Result<List<OverdueClientDTO>>> GetOverdueClients();

        // Aplica la ND de interés por mora a un cliente específico.
        Task<Result<string>> ApplyInterestToClient(int clientId, int idUsuarioRegistra);

        // Aplica la ND de interés por mora a todos los clientes morosos.
        Task<Result<string>> ApplyInterestToAll(int idUsuarioRegistra);

        // Expone RecomputeMontoPagado para uso externo (ej. SaleService al anular una venta).
        Task RecomputeMontoPagadoPublic(int clientId);

        // Modifica el límite de crédito de un cliente registrando un movimiento de tipo MODIFICACION_LIMITE.
        Task<Result<int>> UpdateAccountLimitAsync(UpdateAccountLimitDTO dto);

        // Devuelve el historial completo de modificaciones de límite (tipo 10) del cliente.
        Task<Result<List<AccountMovementDTO>>> GetLimitHistoryAsync(int clientId);

        // Exportaciones PDF/Excel — Estado de cuenta de cliente
        Task<Result<byte[]>> ExportClientAccountExcelAsync(int idCliente, DateOnly? fechaDesde, DateOnly? fechaHasta, int? idTipoMovimientoCc);
        Task<Result<byte[]>> ExportClientAccountPdfAsync(int idCliente, DateOnly? fechaDesde, DateOnly? fechaHasta, int? idTipoMovimientoCc);
    }
}
