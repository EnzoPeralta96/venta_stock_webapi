using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.Repository;

namespace venta_stock_webapi.CurrentAccount.Services.DebitNoteReasonService;

public class DebitNoteReasonService : IDebitNoteReasonService
{
    private readonly IDebitNoteReasonRepository _debitNoteReasonRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<DebitNoteReasonService> _logger;

    public DebitNoteReasonService(
        IDebitNoteReasonRepository debitNoteReasonRepository,
        IMapper mapper,
        ILogger<DebitNoteReasonService> logger)
    {
        _debitNoteReasonRepository = debitNoteReasonRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<DebitNoteReasonDTO>> GetById(int idMotivo)
    {
        try
        {
            var motivo = await _debitNoteReasonRepository.GetByIdAsync(idMotivo);

            if (motivo is null)
                return Result<DebitNoteReasonDTO>.Failure(DebitNoteReasonCode.reason_not_found);

            var dto = _mapper.Map<DebitNoteReasonDTO>(motivo);
            return Result<DebitNoteReasonDTO>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving debit note reason by ID {IdMotivo}", idMotivo);
            return Result<DebitNoteReasonDTO>.Failure(DebitNoteReasonCode.unexpected_error);
        }
    }

    public async Task<Result<List<DebitNoteReasonDTO>>> GetAll(bool? activo = null, string? categoria = null)
    {
        try
        {
            var motivos = await _debitNoteReasonRepository.GetAllAsync(activo, categoria);
            var dtos = _mapper.Map<List<DebitNoteReasonDTO>>(motivos);
            return Result<List<DebitNoteReasonDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving debit note reasons");
            return Result<List<DebitNoteReasonDTO>>.Failure(DebitNoteReasonCode.unexpected_error);
        }
    }

    public async Task<Result<string>> Create(CreateDebitNoteReasonDTO dto)
    {
        try
        {
            if (await _debitNoteReasonRepository.ExistsByNameAsync(dto.Nombre))
                return Result<string>.Failure(DebitNoteReasonCode.reason_name_exists);

            var motivo = _mapper.Map<MotivoNotaDebito>(dto);
            await _debitNoteReasonRepository.CreateAsync(motivo);

            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating debit note reason");
            return Result<string>.Failure(DebitNoteReasonCode.unexpected_error);
        }
    }

    public async Task<Result<string>> Update(UpdateDebitNoteReasonDTO dto)
    {
        try
        {
            if (await _debitNoteReasonRepository.GetByIdAsync(dto.IdMotivo) is null)
                return Result<string>.Failure(DebitNoteReasonCode.reason_not_found);

            if (await _debitNoteReasonRepository.ExistsByNameAsync(dto.IdMotivo, dto.Nombre))
                return Result<string>.Failure(DebitNoteReasonCode.reason_name_exists);

            var motivo = _mapper.Map<MotivoNotaDebito>(dto);
            await _debitNoteReasonRepository.UpdateAsync(motivo);

            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating debit note reason {IdMotivo}", dto.IdMotivo);
            return Result<string>.Failure(DebitNoteReasonCode.unexpected_error);
        }
    }

    public async Task<Result<string>> ToggleState(int idMotivo, bool activo)
    {
        try
        {
            if (await _debitNoteReasonRepository.GetByIdAsync(idMotivo) is null)
                return Result<string>.Failure(DebitNoteReasonCode.reason_not_found);

            await _debitNoteReasonRepository.ToggleStateAsync(idMotivo, activo);

            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling debit note reason state {IdMotivo}", idMotivo);
            return Result<string>.Failure(DebitNoteReasonCode.unexpected_error);
        }
    }
}
