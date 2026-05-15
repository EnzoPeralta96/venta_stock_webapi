using AutoMapper;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Features.Ferreteria.Repository;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Proveedor.DTO;
using proyecto_venta_stock.Proveedor.PDF;
using proyecto_venta_stock.Proveedor.ProveedorRepository;
using proyecto_venta_stock.Shared.ResultPattern;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Text.Json;
using venta_stock_webapi.Features.Audit.Repository;
using venta_stock_webapi.Shared.Identity;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.Proveedor.Services
{
    public class ProveedorServices : IProveedorServices
    {
        private readonly ILogger<ProveedorServices> _logger;
        private readonly IProveedorRepository _proveedorRepository;
        private readonly IFerreteriaRepository _ferreteriaRepository;
        private readonly VentaStockContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IUserContext _userContext;
        private readonly IAuditRepository _auditRepository;

        public ProveedorServices(
            IProveedorRepository proveedorRepository,
            IFerreteriaRepository ferreteriaRepository,
            ILogger<ProveedorServices> logger,
            IMapper mapper,
            VentaStockContext dbContext,
            IUserContext userContext,
            IAuditRepository auditRepository)
        {
            _proveedorRepository = proveedorRepository;
            _ferreteriaRepository = ferreteriaRepository;
            _logger = logger;
            _mapper = mapper;
            _dbContext = dbContext;
            _userContext = userContext;
            _auditRepository = auditRepository;
        }

        private async Task LogAsync(string accion, string entidadTipo, string detalle,
            object? anterior = null, object? nuevo = null)
        {
            try
            {
                await _auditRepository.LogAsync(new Auditoria
                {
                    FechaHora         = DateTimeOffset.UtcNow,
                    IdUsuario         = _userContext.UserId,
                    UsuarioNombre     = _userContext.UserName,
                    Accion            = accion,
                    EntidadTipo       = entidadTipo,
                    Detalle           = detalle,
                    ValoresAnteriores = anterior != null ? JsonSerializer.Serialize(anterior) : null,
                    ValoresNuevos     = nuevo    != null ? JsonSerializer.Serialize(nuevo)    : null,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo registrar auditoría.");
            }
        }

        public async Task<Result<bool>> Create(CreateProveedorDTO dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var exists = await _proveedorRepository.Exists(dto.Nombre);

                if (exists)
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_name_in_use);

                var entity = _mapper.Map<Models.Proveedor>(dto);

                _proveedorRepository.Create(entity);

                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> Update(UpdateProveedorDTO dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _proveedorRepository.GetById(dto.IdProveedor);

                if (existing is null)
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_not_found);

                var exists = await _proveedorRepository.Exists(dto.Nombre, excludeId: dto.IdProveedor);

                if (exists)
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_name_in_use);

                // update manual (similar a ProductServices)
                existing.Proveedor1 = dto.Nombre;
                existing.Direccion = dto.Direccion;
                existing.Telefono = dto.Telefono;

                _proveedorRepository.Update(existing);

                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<List<ProveedorDTO>>> GetAll()
        {
            try
            {
                var list = await _proveedorRepository.GetAll();

                var dtos = _mapper.Map<List<ProveedorDTO>>(list);

                return Result<List<ProveedorDTO>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<List<ProveedorDTO>>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<ProveedorDTO>> GetById(int idProveedor)
        {
            try
            {
                var entity = await _proveedorRepository.GetById(idProveedor);

                if (entity is null)
                    return Result<ProveedorDTO>.Failure(ProveedorErrorCode.proveedor_not_found);

                var dto = _mapper.Map<ProveedorDTO>(entity);

                return Result<ProveedorDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<ProveedorDTO>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> Delete(int idProveedor)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try

            {
                var existing = await _proveedorRepository.GetById(idProveedor);

                if (existing is null)
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_not_found);

                _proveedorRepository.Delete(existing);

                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<PagedList<ProveedorDTO>>> ProveedoresPagedAsync(int pageIndex, int pageSize, string searchTerm, string estado = "activos")
        {
            try
            {
                var query = _proveedorRepository.ProveedoresQueryable(searchTerm);

                if (estado.ToLower() == "activos")
                    query = query.Where(p => p.Activo);
                else if (estado.ToLower() == "eliminados")
                    query = query.Where(p => !p.Activo);

                var projected = _mapper.ProjectTo<ProveedorDTO>(query);
                var paged = await PagedList<ProveedorDTO>.CreateAsync(projected, pageIndex, pageSize);

                return Result<PagedList<ProveedorDTO>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<PagedList<ProveedorDTO>>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        private async Task LogToggleEstadoAsync(Models.Proveedor proveedor, bool eraActivo)
        {
            string accion = eraActivo ? "BAJA" : "REACTIVACION";
            await LogAsync(accion, "PROVEEDOR",
                $"Proveedor {(eraActivo ? "dado de baja" : "reactivado")}: '{proveedor.Proveedor1}'",
                new { activo = eraActivo },
                new { activo = !eraActivo });
        }

        public async Task<Result<bool>> ToggleEstado(int idProveedor)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _proveedorRepository.GetById(idProveedor);

                if (existing is null)
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_not_found);

                bool eraActivo = existing.Activo;
                existing.Activo = !existing.Activo;

                if (existing.Activo) existing.FechaBaja = null;

                _proveedorRepository.Update(existing);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await LogToggleEstadoAsync(existing, eraActivo);

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<byte[]>> ExportarExcelAsync()
        {
            try
            {
                var lista = await _proveedorRepository.GetAll();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Proveedores");

                // Header
                var headers = new[] { "#", "Nombre", "Dirección", "Teléfono", "Estado" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = headers[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                    ws.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
                    ws.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                // Data
                for (int i = 0; i < lista.Count; i++)
                {
                    var p = lista[i];
                    ws.Cells[i + 2, 1].Value = i + 1;
                    ws.Cells[i + 2, 2].Value = p.Proveedor1;
                    ws.Cells[i + 2, 3].Value = p.Direccion;
                    ws.Cells[i + 2, 4].Value = p.Telefono;
                    ws.Cells[i + 2, 5].Value = p.Activo ? "Sí" : "No";
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                return Result<byte[]>.Success(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al exportar proveedores: {Ex}", ex);
                return Result<byte[]>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<byte[]>> ExportarPdfAsync()
        {
            try
            {
                var lista = await _proveedorRepository.GetAll();
                var dtos = _mapper.Map<List<ProveedorDTO>>(lista);
                var ferreteria = await _ferreteriaRepository.GetAsync();

                QuestPDF.Settings.License = LicenseType.Community;

                var document = new ProveedorListDocument(dtos, ferreteria);
                var bytes = document.GeneratePdf();

                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al exportar proveedores en PDF: {Ex}", ex);
                return Result<byte[]>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

    }
}