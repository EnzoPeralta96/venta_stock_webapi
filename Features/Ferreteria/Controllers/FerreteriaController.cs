using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.Features.Ferreteria.DTO;
using venta_stock_webapi.Features.Ferreteria.Message;
using venta_stock_webapi.Features.Ferreteria.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.Features.Ferreteria.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FerreteriaController : ControllerBase
    {
        private readonly IFerreteriaService _ferreteriaService;

        public FerreteriaController(IFerreteriaService ferreteriaService)
        {
            _ferreteriaService = ferreteriaService;
        }

        /// <summary>
        /// Obtiene la información de la ferretería
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _ferreteriaService.GetAsync();

            if (!result.IsSuccess)
            {
                var code = (FerreteriaErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(FerreteriaErrorDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Actualiza la información de la ferretería
        /// </summary>
        [Authorize(Policy = "PERM:USR_UPDATE")]
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] FerreteriaUpdateDTO ferreteriaDTO)
        {
            var result = await _ferreteriaService.UpdateAsync(ferreteriaDTO);

            if (!result.IsSuccess)
            {
                var code = (FerreteriaErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(FerreteriaErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(new { message = "Información de la ferretería actualizada correctamente" });
        }
    }
}
