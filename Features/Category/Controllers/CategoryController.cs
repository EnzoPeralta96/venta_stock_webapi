using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Category.Services;
using proyecto_venta_stock.Message;
using venta_stock_webapi.Shared.MessageProvider;
using venta_stock_webapi.Features.Category.DTO;
using Microsoft.AspNetCore.Authorization;

namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly ICategoryServices _categoryServices;
    public CategoryController(ICategoryServices categoryServices)
    {
        _categoryServices = categoryServices;
    }

    [Authorize(Policy = "PERM:PROD_CREATE")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDTO category)
    {
        var result = await _categoryServices.Create(category);

        if (!result.IsSuccess)
        {
            var errorCode = (CategoryErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(CategoryErrorDictionary.Messages, errorCode);
            return BadRequest(message);
        }
        
        return Ok(category);
    }

    [Authorize(Policy = "PERM:PROD_UPDATE")]
    [HttpPut("update")]
    public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryDTO category)
    {
        var result = await _categoryServices.Update(category);

        if (!result.IsSuccess)
        {
            var errorCode = (CategoryErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(CategoryErrorDictionary.Messages, errorCode);
            return BadRequest(message);
        }

        return Ok(category);
    }

    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categoryServices.GetAll();

        if (!result.IsSuccess)
        {
            var errorCode = (CategoryErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(CategoryErrorDictionary.Messages, errorCode);
            return BadRequest(message);
        } 

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet("{idCategoria:int}")]
    public async Task<IActionResult> GetById(int idCategoria)
    {
        var result = await _categoryServices.GetById(idCategoria);

        if (!result.IsSuccess)
        {
            var errorCode = (CategoryErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(CategoryErrorDictionary.Messages, errorCode);
            return NotFound(message);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:PROD_DELETE")]
    [HttpDelete("{idCategoria:int}")]
    public async Task<IActionResult> Delete(int idCategoria)
    {
        var result = await _categoryServices.Delete(idCategoria);
        
        if (!result.IsSuccess)
        {
            var errorCode = (CategoryErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(CategoryErrorDictionary.Messages, errorCode);

            return errorCode switch
            {
                CategoryErrorCode.category_not_found => NotFound(message),
                CategoryErrorCode.category_in_use => Conflict(message),
                _ => BadRequest(message),
            };   
        }
        
        return NoContent();
    }
}
