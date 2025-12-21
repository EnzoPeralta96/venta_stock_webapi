
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.Client.DTO;
using venta_stock_webapi.Client.Message;
using venta_stock_webapi.Client.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClienteController : ControllerBase
    {
        private readonly IClientService _clienteService;

        public ClienteController(IClientService clienteService)
        {
            _clienteService = clienteService;
        }

        [Authorize(Policy = "PERM:CLI_CREATE")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateCliente([FromBody] ClientCreateDTO clienteDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _clienteService.CreateClienteAsync(clienteDTO);

            if (!result.IsSuccess)
            {
                var code = (ClientErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(ClientErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }


        [Authorize(Policy = "PERM:CLI_READ")]
        [HttpGet("client/{id}")]
        public async Task<IActionResult> GetClient(int id)
        {
            var result = await _clienteService.GetClient(id);

            if (!result.IsSuccess)
            {
                var code = (ClientErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(ClientErrorDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }


        [Authorize(Policy = "PERM:CLI_READ")]
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            int pageIndex = 1,
            int pageSize = 10,
            string searchTerm = "",
            string estado = "activos")
        {
            var result = await _clienteService.Search(pageIndex, pageSize, searchTerm, estado);

            if (!result.IsSuccess)
            {
                var code = (ClientErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(ClientErrorDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CLI_UPDATE")]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateClient([FromBody] ClientUpdateDTO clienteDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _clienteService.UpdateClient(clienteDTO);

            if (!result.IsSuccess)
            {
                var code = (ClientErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(ClientErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }

        //Por defecto el permiso es borrar, pero en realidad solo se desactiva/activa el cliente
        [Authorize(Policy = "PERM:CLI_DELETE")]
        [HttpPut("toggle-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] ClientToggleStatusDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _clienteService.ToggleStatus(dto);

            if (!result.IsSuccess)
            {
                var code = (ClientErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(ClientErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }
    }
}