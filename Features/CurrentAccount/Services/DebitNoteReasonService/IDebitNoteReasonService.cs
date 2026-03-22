using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;

namespace venta_stock_webapi.CurrentAccount.Services.DebitNoteReasonService;

public interface IDebitNoteReasonService
{
    Task<Result<DebitNoteReasonDTO>> GetById(int idMotivo);
    Task<Result<List<DebitNoteReasonDTO>>> GetAll(bool? activo = null, string? categoria = null);
    Task<Result<string>> Create(CreateDebitNoteReasonDTO dto);
    Task<Result<string>> Update(UpdateDebitNoteReasonDTO dto);
    Task<Result<string>> ToggleState(int idMotivo, bool activo);
}
