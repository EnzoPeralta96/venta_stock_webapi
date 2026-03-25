using AutoMapper;
using venta_stock_webapi.Client.DTO;
using venta_stock_webapi.Client.Message;
using venta_stock_webapi.Client.Repository;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Shared.Paged;
using venta_stock_webapi.CurrentAccount.Repository;
using venta_stock_webapi.Data.Audit;
using venta_stock_webapi.Shared.Identity;

namespace venta_stock_webapi.Client.Services
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clienteRepository;
        private readonly IAccountMovementRepository _accountMovementRepository;
        private readonly VentaStockContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ClientService> _logger;
        private readonly IUserContext _userContext;

        public ClientService(
            IClientRepository clienteRepository,
            VentaStockContext context,
            IMapper mapper,
            ILogger<ClientService> logger,
            IAccountMovementRepository accountMovementRepository,
            IUserContext userContext)
        {
            _clienteRepository = clienteRepository;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _accountMovementRepository = accountMovementRepository;
            _userContext = userContext;
        }

        //Agregar validaciones de usuario.
        public async Task<Result<ClientResponseDTO>> CreateClienteAsync(ClientCreateDTO clienteDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validar unicidad de Email (global para todos los clientes)
                if (await _clienteRepository.EmailExistsAsync(clienteDTO.Mail))
                {
                    return Result<ClientResponseDTO>.Failure(ClientErrorCode.email_in_use);
                }

                // Validaciones específicas según tipo de cliente
                if (clienteDTO.EsEmpresa)
                {
                    // EMPRESA: Validar RazonSocial única
                    if (await _clienteRepository.EnterpriseExistsAsync(clienteDTO.RazonSocial))
                    {
                        return Result<ClientResponseDTO>.Failure(ClientErrorCode.empresa_in_use);
                    }
                    // CUIT NO se valida (puede repetirse)
                }
                else
                {
                    // PERSONA FÍSICA: Validar DNI único
                    if (await _clienteRepository.DniExistsAsync(clienteDTO.Dni))
                    {
                        return Result<ClientResponseDTO>.Failure(ClientErrorCode.dni_in_use);
                    }
                }

                // Validar que si tiene CC, debe venir el límite de cuenta
                if (clienteDTO.TieneCuentaCorriente && !clienteDTO.LimiteCuenta.HasValue)
                {
                    return Result<ClientResponseDTO>.Failure(ClientErrorCode.limite_cuenta_required);
                }

                // Crear el cliente
                var cliente = _mapper.Map<Cliente>(clienteDTO);

                cliente.FechaAlta = DateOnly.FromDateTime(DateTime.Now);

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

                    await _accountMovementRepository.CreateMovement(movimientoCC);
                }


                await transaction.CommitAsync();

                // Obtener el cliente recién creado con sus relaciones
                var clienteCompleto = await _clienteRepository.GetByIdAsync(clienteCreado.IdCliente);
                var responseDTO = _mapper.Map<ClientResponseDTO>(clienteCompleto);

                return Result<ClientResponseDTO>.Success(responseDTO);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado al crear cliente: " + ex);
                return Result<ClientResponseDTO>.Failure(ClientErrorCode.unexpected_error);
            }
        }

        public async Task<Result<ClientResponseDTO>> GetClient(int id)
        {
            try
            {
                var cliente = await _clienteRepository.GetByIdAsync(id);

                if (cliente is null) 
                    return Result<ClientResponseDTO>.Failure(ClientErrorCode.cliente_not_found);

                var responseDTO = _mapper.Map<ClientResponseDTO>(cliente);
                return Result<ClientResponseDTO>.Success(responseDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al obtener cliente: " + ex);
                return Result<ClientResponseDTO>.Failure(ClientErrorCode.unexpected_error);
            }
        }

        public async Task<Result<PagedList<ClientResponseDTO>>> Search(int pageIndex, int pageSize, string searchTerm, string estado = "activos")
        {
            try
            {
                var query = _clienteRepository.ClientsQueryable(searchTerm);

                if (estado.ToLower() == "activos")
                    query = query.Where(c => c.FechaBaja == null);
                else if (estado.ToLower() == "eliminados")
                    query = query.Where(c => c.FechaBaja != null);

                var projected = _mapper.ProjectTo<ClientResponseDTO>(query);

                var paged = await PagedList<ClientResponseDTO>.CreateAsync(projected, pageIndex, pageSize);

                return Result<PagedList<ClientResponseDTO>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado en búsqueda de clientes: " + ex);
                return Result<PagedList<ClientResponseDTO>>.Failure(ClientErrorCode.unexpected_error);
            }
        }

        public async Task<Result<ClientResponseDTO>> UpdateClient(ClientUpdateDTO clienteDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var clienteExistente = await _clienteRepository.GetByIdAsync(clienteDTO.IdCliente);

                if (clienteExistente is null)
                    return Result<ClientResponseDTO>.Failure(ClientErrorCode.cliente_not_found);

                // Validar unicidad de Email (global para todos los clientes, excepto el actual)
                if (await _clienteRepository.EmailExistsForOtherClientAsync(clienteDTO.Mail, clienteDTO.IdCliente))
                    return Result<ClientResponseDTO>.Failure(ClientErrorCode.email_in_use);
            

                // Validaciones específicas según tipo de cliente
                if (clienteDTO.EsEmpresa)
                {
                    // EMPRESA: Validar RazonSocial única (excepto el cliente actual)
                    if (await _clienteRepository.EnterpriseExistsForOtherClientAsync(clienteDTO.RazonSocial, clienteDTO.IdCliente))
                        return Result<ClientResponseDTO>.Failure(ClientErrorCode.empresa_in_use);
                    // CUIT NO se valida (puede repetirse)
                }
                else
                {
                    // PERSONA FÍSICA: Validar DNI único (excepto el cliente actual)
                    if (await _clienteRepository.DniExistsForOtherClientAsync(clienteDTO.Dni, clienteDTO.IdCliente))
                        return Result<ClientResponseDTO>.Failure(ClientErrorCode.dni_in_use);
                }

                clienteExistente.Nombre = clienteDTO.Nombre;
                clienteExistente.Apellido = clienteDTO.Apellido;
                clienteExistente.RazonSocial = clienteDTO.RazonSocial;
                clienteExistente.Dni = clienteDTO.Dni;
                clienteExistente.Cuit = clienteDTO.Cuit;
                clienteExistente.Telefono = clienteDTO.Telefono;
                clienteExistente.Mail = clienteDTO.Mail;

                await _clienteRepository.UpdateAsync(clienteExistente);

                await transaction.CommitAsync();

                var clienteActualizado = await _clienteRepository.GetByIdAsync(clienteDTO.IdCliente);

                var responseDTO = _mapper.Map<ClientResponseDTO>(clienteActualizado);

                return Result<ClientResponseDTO>.Success(responseDTO);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado al actualizar cliente: " + ex);
                return Result<ClientResponseDTO>.Failure(ClientErrorCode.unexpected_error);
            }
        }

        public async Task<Result<string>> ToggleStatus(ClientToggleStatusDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await AuditDbSession.SetAsync(_context, _userContext);
                
                var cliente = await _clienteRepository.GetByIdAsync(dto.IdCliente);

                if (cliente == null)
                {
                    return Result<string>.Failure(ClientErrorCode.cliente_not_found);
                }

                if (!dto.IsActive)
                {
                    if (cliente.FechaBaja != null)
                    {
                        return Result<string>.Failure(ClientErrorCode.cliente_already_inactive);
                    }

                    await _clienteRepository.UpdateStatusAsync(dto.IdCliente, DateOnly.FromDateTime(DateTime.Now));
                    await transaction.CommitAsync();
                    return Result<string>.Success("Cliente dado de baja exitosamente.");
                }
                else
                {
                    if (cliente.FechaBaja == null)
                    {
                        return Result<string>.Failure(ClientErrorCode.cliente_already_active);
                    }

                    await _clienteRepository.UpdateStatusAsync(dto.IdCliente, null);
                    await transaction.CommitAsync();
                    return Result<string>.Success("Cliente reactivado exitosamente.");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado al cambiar estado del cliente: " + ex);
                return Result<string>.Failure(ClientErrorCode.unexpected_error);
            }
        }
    }
}