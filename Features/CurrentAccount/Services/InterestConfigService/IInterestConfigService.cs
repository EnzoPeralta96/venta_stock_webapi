using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.CurrentAccount.DTO.InterestConfigDTO;

namespace venta_stock_webapi.CurrentAccount.Services.InterestConfigService;

public interface IInterestConfigService
{
    Task<Result<InterestConfigDTO>> GetById(int idConfig);
    Task<Result<List<InterestConfigDTO>>> GetAll();
    Task<Result<InterestConfigDTO>> GetCurrent();
    Task<Result<string>> Create(CreateInterestConfigDTO dto);
    Task<Result<string>> Update(UpdateInterestConfigDTO dto);
    Task<Result<string>> SetAsCurrent(int idConfig);
}
