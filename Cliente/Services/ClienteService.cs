using AutoMapper;
using Microsoft.Extensions.Logging;
using venta_stock_webapi.Cliente.DTO;
using venta_stock_webapi.Cliente.Message;
using venta_stock_webapi.Cliente.Repository;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Cliente.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IAccountMovementRepository _accountMovementRepository;
        private readonly VentaStockContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ClienteService> _logger;

        public ClienteService(
            IClienteRepository clienteRepository,
            VentaStockContext context,
            IMapper mapper,
            ILogger<ClienteService> logger,
            IAccountMovementRepository accountMovementRepository)
        {
            _clienteRepository = clienteRepository;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _accountMovementRepository = accountMovementRepository;

        }

        public async Task<Result<ClienteResponseDTO>> CreateClienteAsync(ClienteCreateDTO clienteDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validar tipo de cliente (Persona Física vs Empresa)
                bool esPersonaFisica = !string.IsNullOrWhiteSpace(clienteDTO.Dni);
                bool esEmpresa = !string.IsNullOrWhiteSpace(clienteDTO.Cuit);

                if (esPersonaFisica)
                {
                    if (string.IsNullOrWhiteSpace(clienteDTO.Nombre) || string.IsNullOrWhiteSpace(clienteDTO.Apellido))
                    {
                        return Result<ClienteResponseDTO>.Failure(ClienteErrorCode.invalid_persona_fisica_data);
                    }
                }

                if (esEmpresa)
                {
                    if (string.IsNullOrWhiteSpace(clienteDTO.RazonSocial))
                    {
                        return Result<ClienteResponseDTO>.Failure(ClienteErrorCode.invalid_empresa_data);
                    }
                }

                // Validar unicidad de DNI
                if (!string.IsNullOrWhiteSpace(clienteDTO.Dni))
                {
                    if (await _clienteRepository.DniExistsAsync(clienteDTO.Dni))
                    {
                        return Result<ClienteResponseDTO>.Failure(ClienteErrorCode.dni_in_use);
                    }
                }

                // Validar unicidad de CUIT
                if (!string.IsNullOrWhiteSpace(clienteDTO.Cuit))
                {
                    if (await _clienteRepository.CuitExistsAsync(clienteDTO.Cuit))
                    {
                        return Result<ClienteResponseDTO>.Failure(ClienteErrorCode.cuit_in_use);
                    }
                }

                // Validar unicidad de Email
                if (await _clienteRepository.EmailExistsAsync(clienteDTO.Mail))
                {
                    return Result<ClienteResponseDTO>.Failure(ClienteErrorCode.email_in_use);
                }

                // Validar que si tiene CC, debe venir el límite de cuenta
                if (clienteDTO.TieneCuentaCorriente && !clienteDTO.LimiteCuenta.HasValue)
                {
                    return Result<ClienteResponseDTO>.Failure(ClienteErrorCode.limite_cuenta_required);
                }

                // Crear el cliente
                var cliente = _mapper.Map<proyecto_venta_stock.Models.Cliente>(clienteDTO);
                var clienteCreado = await _clienteRepository.CreateAsync(cliente);

                // Si tiene cuenta corriente, crear el MovimientoCC inicial
                if (clienteDTO.TieneCuentaCorriente)
                {
                    var movimientoCC = new MovimientoCc
                    {
                        IdTipoMovimiento = 2, // Alta cliente
                        IdEstado = 2, // Aprobado
                        Fecha = DateTime.Now,
                        Importe = 0,
                        Detalle = "Saldo inicial del cliente al registrarse",
                        LimiteCuenta = clienteDTO.LimiteCuenta!.Value,
                        SaldoActual = clienteDTO.SaldoInicial ?? 0,
                        IdUsuarioRegistra = clienteDTO.idUsuarioRegistra,
                        IdCliente = clienteCreado.IdCliente,
                        IdVenta = null,
                        IdUsuarioAutoriza = null,
                        FechaAutorizacion = null
                    };

                  await _accountMovementRepository.CreateAccount(movimientoCC);
                }

                // Obtener el cliente recién creado con sus relaciones
                var clienteCompleto = await _clienteRepository.GetByIdAsync(clienteCreado.IdCliente);
                var responseDTO = _mapper.Map<ClienteResponseDTO>(clienteCompleto);

                return Result<ClienteResponseDTO>.Succes(responseDTO);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado al crear cliente: " + ex);
                return Result<ClienteResponseDTO>.Failure(ClienteErrorCode.unexpected_error);
            }
        }
    }
}