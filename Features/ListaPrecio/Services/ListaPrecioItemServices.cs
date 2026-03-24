using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
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

    public async Task<Result<ImportListaPrecioResultDTO>> ImportarAsync(int idLista, IFormFile file)
    {
        try
        {
            if (!await _repo.ListaExistsAsync(idLista))
                return Result<ImportListaPrecioResultDTO>.Failure(ListaPrecioItemErrorCode.lista_not_found);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            List<(string codigoBarra, decimal precio)> rows;

            if (ext == ".csv")
                rows = await ParseCsvAsync(file);
            else if (ext is ".xlsx" or ".xls")
                rows = ParseExcel(file);
            else
                return Result<ImportListaPrecioResultDTO>.Failure(ListaPrecioItemErrorCode.formato_no_soportado);

            var resultado = new ImportListaPrecioResultDTO();

            foreach (var (codigoBarra, precio) in rows)
            {
                resultado.TotalProcesados++;

                var idProducto = await _repo.GetProductoIdByCodigoBarraAsync(codigoBarra);
                if (idProducto == null)
                {
                    resultado.Errores.Add($"Código '{codigoBarra}': producto no encontrado.");
                    continue;
                }

                var existing = await _repo.GetItemAsync(idLista, idProducto.Value);
                if (existing != null)
                {
                    existing.Precio = precio;
                    await _repo.UpdateAsync(existing);
                    resultado.Actualizados++;
                }
                else
                {
                    await _repo.CreateAsync(new ProductoListaprecioProveedor
                    {
                        IdLista = idLista,
                        IdProducto = idProducto.Value,
                        Precio = precio
                    });
                    resultado.Insertados++;
                }
            }

            return Result<ImportListaPrecioResultDTO>.Success(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al importar lista de precios: {Ex}", ex);
            return Result<ImportListaPrecioResultDTO>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }

    // ── Parsers ────────────────────────────────────────────────────────────────

    private static async Task<List<(string codigoBarra, decimal precio)>> ParseCsvAsync(IFormFile file)
    {
        var rows = new List<(string, decimal)>();
        using var reader = new StreamReader(file.OpenReadStream());

        // Skip header
        await reader.ReadLineAsync();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');
            if (cols.Length < 2) continue;

            var codigo = cols[0].Trim();
            if (string.IsNullOrWhiteSpace(codigo)) continue;

            if (!decimal.TryParse(cols[1].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var precio)) continue;

            rows.Add((codigo, precio));
        }

        return rows;
    }

    private static List<(string codigoBarra, decimal precio)> ParseExcel(IFormFile file)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var rows = new List<(string, decimal)>();

        using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream);

        var ws = package.Workbook.Worksheets[0];
        if (ws?.Dimension == null) return rows;

        // Row 1 = header, data starts at row 2
        for (int row = 2; row <= ws.Dimension.Rows; row++)
        {
            var codigo = ws.Cells[row, 1].Text.Trim();
            if (string.IsNullOrWhiteSpace(codigo)) continue;

            var precioText = ws.Cells[row, 2].Text.Trim();
            if (!decimal.TryParse(precioText, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var precio)) continue;

            rows.Add((codigo, precio));
        }

        return rows;
    }
}
