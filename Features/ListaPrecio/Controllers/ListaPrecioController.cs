using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.ListaPrecio.DTO;
using proyecto_venta_stock.ListaPrecio.Services;
using proyecto_venta_stock.Message;
using venta_stock_webapi.Shared.MessageProvider;

namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("[controller]")]
public class ListaPrecioController : ControllerBase
{
    private readonly IListaPrecioServices _listaPrecioServices;

    public ListaPrecioController(IListaPrecioServices listaPrecioServices)
    {
        _listaPrecioServices = listaPrecioServices;
    }

    [HttpGet("proveedor/{idProveedor:int}")]
    public async Task<IActionResult> GetByProveedor(int idProveedor)
    {
        var result = await _listaPrecioServices.GetByProveedorAsync(idProveedor);
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioErrorCode)result.ErrorCode;
            return BadRequest(MessageProvider.Get(ListaPrecioErrorDictionary.Messages, code));
        }
        return Ok(result.Value);
    }

    [HttpGet("{idLista:int}")]
    public async Task<IActionResult> GetById(int idLista)
    {
        var result = await _listaPrecioServices.GetByIdAsync(idLista);
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioErrorCode)result.ErrorCode;
            return NotFound(MessageProvider.Get(ListaPrecioErrorDictionary.Messages, code));
        }
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ListaPrecioCreateDTO dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int idUsuario))
            return Unauthorized();

        var result = await _listaPrecioServices.CreateAsync(dto, idUsuario);
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioErrorCode)result.ErrorCode;
            return BadRequest(MessageProvider.Get(ListaPrecioErrorDictionary.Messages, code));
        }
        return Ok();
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ListaPrecioUpdateDTO dto)
    {
        var result = await _listaPrecioServices.UpdateAsync(dto);
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioErrorCode)result.ErrorCode;
            if (code == ListaPrecioErrorCode.lista_not_found)
                return NotFound(MessageProvider.Get(ListaPrecioErrorDictionary.Messages, code));
            return BadRequest(MessageProvider.Get(ListaPrecioErrorDictionary.Messages, code));
        }
        return Ok();
    }

    [HttpDelete("{idLista:int}")]
    public async Task<IActionResult> Delete(int idLista)
    {
        var result = await _listaPrecioServices.DeleteAsync(idLista);
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioErrorCode)result.ErrorCode;
            if (code == ListaPrecioErrorCode.lista_not_found)
                return NotFound(MessageProvider.Get(ListaPrecioErrorDictionary.Messages, code));
            return BadRequest(MessageProvider.Get(ListaPrecioErrorDictionary.Messages, code));
        }
        return NoContent();
    }

    [HttpPatch("{idLista:int}/toggle-activo")]
    public async Task<IActionResult> ToggleActivo(int idLista)
    {
        var result = await _listaPrecioServices.ToggleActivoAsync(idLista);
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioErrorCode)result.ErrorCode;
            return BadRequest(MessageProvider.Get(ListaPrecioErrorDictionary.Messages, code));
        }
        return Ok();
    }
}
