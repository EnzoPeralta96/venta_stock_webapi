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
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);

            if (code == ListaPrecioItemErrorCode.lista_not_found)
                return NotFound(errorMessage);

            return BadRequest(errorMessage);
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
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);

            if (code == ListaPrecioItemErrorCode.lista_not_found || code == ListaPrecioItemErrorCode.producto_not_found)
                return NotFound(errorMessage);

            return BadRequest(errorMessage);
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
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);

            if (code == ListaPrecioItemErrorCode.item_not_found)
                return NotFound(errorMessage);

            return BadRequest(errorMessage);
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
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);

            if (code == ListaPrecioItemErrorCode.item_not_found)
                return NotFound(errorMessage);

            return BadRequest(errorMessage);
        }
        return NoContent();
    }

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

    [HttpPost("import")]
    public async Task<IActionResult> Import(int idLista, IFormFile file, [FromForm] bool actualizarPrecioVenta = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Debe adjuntar un archivo.");

        var result = await _services.ImportarAsync(idLista, file, actualizarPrecioVenta);

        if (!result.IsSuccess)
        {
            var code = (ListaPrecioItemErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(ListaPrecioItemErrorDictionary.Messages, code);
            return code == ListaPrecioItemErrorCode.lista_not_found ? NotFound(errorMessage) : BadRequest(errorMessage);
        }

        return Ok(result.Value);
    }
}
