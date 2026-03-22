using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.ListaPrecio.DTO;
using proyecto_venta_stock.ListaPrecio.Services;
using proyecto_venta_stock.Message;
using venta_stock_webapi.Shared.MessageProvider;

namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("ListaPrecio/{idLista:int}/items")]
public class ListaPrecioItemsController : ControllerBase
{
    private readonly IListaPrecioItemServices _services;

    public ListaPrecioItemsController(IListaPrecioItemServices services)
    {
        _services = services;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems(int idLista)
    {
        var result = await _services.GetItemsAsync(idLista);
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            if (code == ListaPrecioItemErrorCode.lista_not_found)
                return NotFound(MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code));
            return BadRequest(MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code));
        }
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddItem(int idLista, [FromBody] ListaPrecioItemUpsertDTO dto)
    {
        var result = await _services.AddItemAsync(idLista, dto);
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            if (code == ListaPrecioItemErrorCode.lista_not_found || code == ListaPrecioItemErrorCode.producto_not_found)
                return NotFound(MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code));
            return BadRequest(MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code));
        }
        return Ok();
    }

    [HttpPut("{idProducto:int}")]
    public async Task<IActionResult> UpdateItem(int idLista, int idProducto, [FromBody] ListaPrecioItemUpsertDTO dto)
    {
        var result = await _services.UpdateItemAsync(idLista, idProducto, dto);
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            if (code == ListaPrecioItemErrorCode.item_not_found)
                return NotFound(MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code));
            return BadRequest(MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code));
        }
        return Ok();
    }

    [HttpDelete("{idProducto:int}")]
    public async Task<IActionResult> DeleteItem(int idLista, int idProducto)
    {
        var result = await _services.DeleteItemAsync(idLista, idProducto);
        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            if (code == ListaPrecioItemErrorCode.item_not_found)
                return NotFound(MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code));
            return BadRequest(MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code));
        }
        return NoContent();
    }
}
