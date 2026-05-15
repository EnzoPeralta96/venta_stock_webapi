using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Sale.DTO.CreditNoteReasonDTO;

namespace venta_stock_webapi.Sale.Services.CreditNoteReasonService;

public interface ICreditNoteReasonService
{
    Task<Result<CreditNoteReasonDTO>> GetById(int idMotivo);
    Task<Result<List<CreditNoteReasonDTO>>> GetAll(bool? activo = null);
    Task<Result<string>> Create(CreateCreditNoteReasonDTO dto);
    Task<Result<string>> Update(UpdateCreditNoteReasonDTO dto);
    Task<Result<string>> ToggleState(int idMotivo, bool activo);
}
