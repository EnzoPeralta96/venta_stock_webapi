using proyecto_venta_stock.ListaPrecio.DTO;
using proyecto_venta_stock.ListaPrecio.ListaPrecioRepository;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;

namespace proyecto_venta_stock.ListaPrecio.Services;

public class ListaPrecioItemServices : IListaPrecioItemServices
{
    private readonly IListaPrecioItemRepository _repo;
    private readonly ILogger<ListaPrecioItemServices> _logger;

    public ListaPrecioItemServices(IListaPrecioItemRepository repo, ILogger<ListaPrecioItemServices> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Result<List<ListaPrecioItemDTO>>> GetItemsAsync(int idLista)
    {
        try
        {
            if (!await _repo.ListaExistsAsync(idLista))
                return Result<List<ListaPrecioItemDTO>>.Failure(ListaPrecioItemErrorCode.lista_not_found);

            var items = await _repo.GetItemsByListaAsync(idLista);

            var dtos = items.Select(x => new ListaPrecioItemDTO
            {
                IdLista = x.IdLista,
                IdProducto = x.IdProducto,
                Precio = x.Precio,
                Margen = x.Margen,
                NombreProducto = x.IdProductoNavigation?.Nombre,
                Marca = x.IdProductoNavigation?.Marca
            }).ToList();

            return Result<List<ListaPrecioItemDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado: " + ex);
            return Result<List<ListaPrecioItemDTO>>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }

    public async Task<Result<bool>> AddItemAsync(int idLista, ListaPrecioItemUpsertDTO dto)
    {
        try
        {
            if (!await _repo.ListaExistsAsync(idLista))
                return Result<bool>.Failure(ListaPrecioItemErrorCode.lista_not_found);

            if (!await _repo.ProductoExistsAsync(dto.IdProducto))
                return Result<bool>.Failure(ListaPrecioItemErrorCode.producto_not_found);

            if (await _repo.ItemExistsAsync(idLista, dto.IdProducto))
                return Result<bool>.Failure(ListaPrecioItemErrorCode.item_already_exists);

            var entity = new ProductoListaprecioProveedor
            {
                IdLista = idLista,
                IdProducto = dto.IdProducto,
                Precio = dto.Precio,
                Margen = dto.Margen
            };

            await _repo.CreateAsync(entity);
            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado: " + ex);
            return Result<bool>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }

    public async Task<Result<bool>> UpdateItemAsync(int idLista, int idProducto, ListaPrecioItemUpsertDTO dto)
    {
        try
        {
            var item = await _repo.GetItemAsync(idLista, idProducto);
            if (item == null)
                return Result<bool>.Failure(ListaPrecioItemErrorCode.item_not_found);

            item.Precio = dto.Precio;
            item.Margen = dto.Margen;

            await _repo.UpdateAsync(item);
            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado: " + ex);
            return Result<bool>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }

    public async Task<Result<bool>> DeleteItemAsync(int idLista, int idProducto)
    {
        try
        {
            var item = await _repo.GetItemAsync(idLista, idProducto);
            if (item == null)
                return Result<bool>.Failure(ListaPrecioItemErrorCode.item_not_found);

            await _repo.DeleteAsync(item);
            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado: " + ex);
            return Result<bool>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }
}
