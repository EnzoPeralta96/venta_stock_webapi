using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Login.DTO;


namespace venta_stock_webapi.Login.Services
{
    public interface ILoginService
    {
        Task<Result<LoginResponseDTO>> AuthenticateAsync(LoginRequestDTO loginRequest);
    }
}