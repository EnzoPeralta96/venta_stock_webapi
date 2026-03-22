using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Sale.DTO.CreditNoteReasonDTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Repository;

namespace venta_stock_webapi.Sale.Services.CreditNoteReasonService;

public class CreditNoteReasonService : ICreditNoteReasonService
{
    private readonly ICreditNoteReasonRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreditNoteReasonService> _logger;

    public CreditNoteReasonService(
        ICreditNoteReasonRepository repository,
        IMapper mapper,
        ILogger<CreditNoteReasonService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
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
        try
        {
            if (await _repository.ExistsByNameAsync(dto.Nombre))
                return Result<string>.Failure(CreditNoteReasonCode.reason_name_exists);

            var motivo = _mapper.Map<MotivoNotaCredito>(dto);
            await _repository.CreateAsync(motivo);
            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating credit note reason");
            return Result<string>.Failure(CreditNoteReasonCode.unexpected_error);
        }
    }

    public async Task<Result<string>> Update(UpdateCreditNoteReasonDTO dto)
    {
        try
        {
            if (await _repository.GetByIdAsync(dto.IdMotivo) is null)
                return Result<string>.Failure(CreditNoteReasonCode.reason_not_found);

            if (await _repository.ExistsByNameAsync(dto.IdMotivo, dto.Nombre))
                return Result<string>.Failure(CreditNoteReasonCode.reason_name_exists);

            var motivo = _mapper.Map<MotivoNotaCredito>(dto);
            await _repository.UpdateAsync(motivo);
            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating credit note reason {IdMotivo}", dto.IdMotivo);
            return Result<string>.Failure(CreditNoteReasonCode.unexpected_error);
        }
    }

    public async Task<Result<string>> ToggleState(int idMotivo, bool activo)
    {
        try
        {
            if (await _repository.GetByIdAsync(idMotivo) is null)
                return Result<string>.Failure(CreditNoteReasonCode.reason_not_found);

            await _repository.ToggleStateAsync(idMotivo, activo);
            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling credit note reason state {IdMotivo}", idMotivo);
            return Result<string>.Failure(CreditNoteReasonCode.unexpected_error);
        }
    }
}
