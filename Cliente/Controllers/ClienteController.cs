using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.Cliente.DTO;
using venta_stock_webapi.Cliente.Message;
using venta_stock_webapi.Cliente.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.Cliente.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public ClienteController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCliente([FromBody] ClienteCreateDTO clienteDTO)
        {
            var result = await _clienteService.CreateClienteAsync(clienteDTO);

            if (!result.IsSucces)
            {
                var code = (ClienteErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(ClienteErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }
    }
}       