using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.ListaPrecio.DTO;
using proyecto_venta_stock.ListaPrecio.Services;
using proyecto_venta_stock.Message;
using venta_stock_webapi.Shared.MessageProvider;

namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("ListaPrecio/{idLista:int}/items")]
[Authorize]
public class ListaPrecioItemsController : ControllerBase
{
    private readonly IListaPrecioItemServices _services;

    public ListaPrecioItemsController(IListaPrecioItemServices services)
    {
        _services = services;
    }

    [Authorize(Policy = "PERM:LP_READ")]
    [HttpGet]
    public async Task<IActionResult> GetItems(int idLista)
    {
        var result = await _services.GetItemsAsync(idLista);

        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);

            if (code == ListaPrecioItemErrorCode.lista_not_found)
                return NotFound(errorMessage);

            return BadRequest(errorMessage);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:LP_ITEM_ADD")]
    [HttpPost]
    public async Task<IActionResult> AddItem(int idLista, [FromBody] ListaPrecioItemUpsertDTO dto)
    {
        var result = await _services.AddItemAsync(idLista, dto);

        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);

            if (code == ListaPrecioItemErrorCode.lista_not_found || code == ListaPrecioItemErrorCode.producto_not_found)
                return NotFound(errorMessage);

            return BadRequest(errorMessage);
        }
        return Ok();
    }

    [Authorize(Policy = "PERM:LP_ITEM_UPDATE")]
    [HttpPut("{idProducto:int}")]
    public async Task<IActionResult> UpdateItem(int idLista, int idProducto, [FromBody] ListaPrecioItemUpsertDTO dto)
    {
        var result = await _services.UpdateItemAsync(idLista, idProducto, dto);
        
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);

            if (code == ListaPrecioItemErrorCode.item_not_found)
                return NotFound(errorMessage);

            return BadRequest(errorMessage);
        }
        return Ok();
    }

    [Authorize(Policy = "PERM:LP_ITEM_DELETE")]
    [HttpDelete("{idProducto:int}")]
    public async Task<IActionResult> DeleteItem(int idLista, int idProducto)
    {
        var result = await _services.DeleteItemAsync(idLista, idProducto);

        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);

            if (code == ListaPrecioItemErrorCode.item_not_found)
                return NotFound(errorMessage);

            return BadRequest(errorMessage);
        }
        return NoContent();
    }

    [Authorize(Policy = "PERM:LP_READ")]
    [HttpGet("plantilla-excel")]
    public async Task<IActionResult> DescargarPlantilla(int idLista)
    {
        var result = await _services.DescargarPlantillaAsync(idLista);

        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);
            return code == ListaPrecioItemErrorCode.lista_not_found ? NotFound(errorMessage) : BadRequest(errorMessage);
        }

        return File(result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"plantilla-lista-{idLista}.xlsx");
    }

    [Authorize(Policy = "PERM:LP_ITEM_ADD")]
    [HttpPost("bulk")]
    public async Task<IActionResult> AddItemsBulk(int idLista, [FromBody] ListaPrecioItemBulkCreateDTO dto)
    {
        var result = await _services.AddItemsBulkAsync(idLista, dto);

        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);
            return code == ListaPrecioItemErrorCode.lista_not_found ? NotFound(errorMessage) : BadRequest(errorMessage);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:LP_ITEM_ADD")]
    [HttpPost("import")]
    public async Task<IActionResult> Import(int idLista, IFormFile file, [FromForm] bool actualizarPrecioVenta = false, [FromForm] decimal ivaAplicacion = 0)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Debe adjuntar un archivo.");

        var result = await _services.ImportarAsync(idLista, file, actualizarPrecioVenta, ivaAplicacion);

        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);
            return code == ListaPrecioItemErrorCode.lista_not_found ? NotFound(errorMessage) : BadRequest(errorMessage);
        }

        return Ok(result.Value);
    }
}
