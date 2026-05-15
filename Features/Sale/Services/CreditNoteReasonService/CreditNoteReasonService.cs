using AutoMapper;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Sale.DTO.CreditNoteReasonDTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Repository;
using venta_stock_webapi.Shared.Extensions;
using venta_stock_webapi.Shared.Identity;

namespace venta_stock_webapi.Sale.Services.CreditNoteReasonService;

public class CreditNoteReasonService : ICreditNoteReasonService
{
    private readonly ICreditNoteReasonRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreditNoteReasonService> _logger;
    private readonly VentaStockContext _context;
    private readonly IUserContext _userContext;

    public CreditNoteReasonService(
        ICreditNoteReasonRepository repository,
        IMapper mapper,
        ILogger<CreditNoteReasonService> logger,
        VentaStockContext context,
        IUserContext userContext)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
        _context = context;
        _userContext = userContext;
    }

    public async Task<Result<CreditNoteReasonDTO>> GetById(int idMotivo)
    {
        try
        {
            var motivo = await _repository.GetByIdAsync(idMotivo);
            if (motivo is null)
                return Result<CreditNoteReasonDTO>.Failure(CreditNoteReasonCode.reason_not_found);
            return Result<CreditNoteReasonDTO>.Success(_mapper.Map<CreditNoteReasonDTO>(motivo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit note reason {IdMotivo}", idMotivo);
            return Result<CreditNoteReasonDTO>.Failure(CreditNoteReasonCode.unexpected_error);
        }
    }

    public async Task<Result<List<CreditNoteReasonDTO>>> GetAll(bool? activo = null)
    {
        try
        {
            var motivos = await _repository.GetAllAsync(activo);
            return Result<List<CreditNoteReasonDTO>>.Success(_mapper.Map<List<CreditNoteReasonDTO>>(motivos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit note reasons");
            return Result<List<CreditNoteReasonDTO>>.Failure(CreditNoteReasonCode.unexpected_error);
        }
    }

    public async Task<Result<string>> Create(CreateCreditNoteReasonDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (await _repository.ExistsByNameAsync(dto.Nombre))
            {
                await transaction.RollbackAsync();
                return Result<string>.Failure(CreditNoteReasonCode.reason_name_exists);
            }

            var motivo = _mapper.Map<MotivoNotaCredito>(dto);
            await _repository.CreateAsync(motivo);

            await transaction.CommitAsync();
            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating credit note reason");
            return Result<string>.Failure(CreditNoteReasonCode.unexpected_error);
        }
    }

    public async Task<Result<string>> Update(UpdateCreditNoteReasonDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (await _repository.GetByIdAsync(dto.IdMotivo) is null)
            {
                await transaction.RollbackAsync();
                return Result<string>.Failure(CreditNoteReasonCode.reason_not_found);
            }

            if (await _repository.ExistsByNameAsync(dto.IdMotivo, dto.Nombre))
            {
                await transaction.RollbackAsync();
                return Result<string>.Failure(CreditNoteReasonCode.reason_name_exists);
            }

            var motivo = _mapper.Map<MotivoNotaCredito>(dto);
            await _context.Database.SetAuditContextAsync(_userContext);
            await _repository.UpdateAsync(motivo);

            await transaction.CommitAsync();
            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating credit note reason {IdMotivo}", dto.IdMotivo);
            return Result<string>.Failure(CreditNoteReasonCode.unexpected_error);
        }
    }

    public async Task<Result<string>> ToggleState(int idMotivo, bool activo)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (await _repository.GetByIdAsync(idMotivo) is null)
            {
                await transaction.RollbackAsync();
                return Result<string>.Failure(CreditNoteReasonCode.reason_not_found);
            }

            await _context.Database.SetAuditContextAsync(_userContext);
            await _repository.ToggleStateAsync(idMotivo, activo);

            await transaction.CommitAsync();
            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error toggling credit note reason state {IdMotivo}", idMotivo);
            return Result<string>.Failure(CreditNoteReasonCode.unexpected_error);
        }
    }
}
