using AutoMapper;
using System.Text.Json;
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
using venta_stock_webapi.Features.Audit.Repository;

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
        private readonly IAuditRepository _auditRepository;

        public ClientService(
            IClientRepository clienteRepository,
            VentaStockContext context,
            IMapper mapper,
            ILogger<ClientService> logger,
            IAccountMovementRepository accountMovementRepository,
            IUserContext userContext,
            IAuditRepository auditRepository)
        {
            _clienteRepository = clienteRepository;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _accountMovementRepository = accountMovementRepository;
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

                // Auditoría de creación de cliente
                string nombreCliente = clienteDTO.EsEmpresa
                    ? $"'{clienteDTO.RazonSocial}' | CUIT: {clienteDTO.Cuit}"
                    : $"'{clienteDTO.Nombre} {clienteDTO.Apellido}' | DNI: {clienteDTO.Dni}";

                object nuevoClienteAudit = clienteDTO.EsEmpresa
                    ? (object)new { RazonSocial = clienteDTO.RazonSocial, CUIT = clienteDTO.Cuit, Telefono = clienteDTO.Telefono, Mail = clienteDTO.Mail }
                    : new { Nombre = clienteDTO.Nombre, Apellido = clienteDTO.Apellido, DNI = clienteDTO.Dni, Telefono = clienteDTO.Telefono, Mail = clienteDTO.Mail };

                await LogAsync("CREACION", "CLIENTE", $"Cliente creado: {nombreCliente}", null, nuevoClienteAudit);

                if (clienteDTO.TieneCuentaCorriente)
                {
                    string nombreCC = clienteDTO.EsEmpresa
                        ? clienteDTO.RazonSocial ?? "N/A"
                        : $"{clienteDTO.Nombre} {clienteDTO.Apellido}";
                    await LogAsync("CC_CREADA", "CLIENTE",
                        $"Cuenta corriente habilitada: '{nombreCC}' | Límite: ${clienteDTO.LimiteCuenta!.Value:N2} | Saldo inicial: ${clienteDTO.SaldoInicial ?? 0:N2}",
                        null,
                        new { LimiteCuenta = clienteDTO.LimiteCuenta!.Value, SaldoInicial = clienteDTO.SaldoInicial ?? 0 });
                }

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

                // Capturar campos anteriores ANTES de sobrescribir
                var anteriorClienteDict = new Dictionary<string, object?>();
                var nuevoClienteDict    = new Dictionary<string, object?>();

                void CompareField(string campo, object? old, object? newVal)
                {
                    if (!Equals(old, newVal)) { anteriorClienteDict[campo] = old; nuevoClienteDict[campo] = newVal; }
                }

                CompareField("Nombre",      clienteExistente.Nombre,      clienteDTO.Nombre);
                CompareField("Apellido",    clienteExistente.Apellido,    clienteDTO.Apellido);
                CompareField("RazonSocial", clienteExistente.RazonSocial, clienteDTO.RazonSocial);
                CompareField("DNI",         clienteExistente.Dni,         clienteDTO.Dni);
                CompareField("CUIT",        clienteExistente.Cuit,        clienteDTO.Cuit);
                CompareField("Telefono",    clienteExistente.Telefono,    clienteDTO.Telefono);
                CompareField("Mail",        clienteExistente.Mail,        clienteDTO.Mail);

                clienteExistente.Nombre = clienteDTO.Nombre;
                clienteExistente.Apellido = clienteDTO.Apellido;
                clienteExistente.RazonSocial = clienteDTO.RazonSocial;
                clienteExistente.Dni = clienteDTO.Dni;
                clienteExistente.Cuit = clienteDTO.Cuit;
                clienteExistente.Telefono = clienteDTO.Telefono;
                clienteExistente.Mail = clienteDTO.Mail;

                await _clienteRepository.UpdateAsync(clienteExistente);

                await transaction.CommitAsync();

                string nombreClienteUpdate = clienteDTO.EsEmpresa
                    ? clienteDTO.RazonSocial
                    : $"{clienteDTO.Nombre} {clienteDTO.Apellido}".Trim();
                await LogAsync("ACTUALIZACION", "CLIENTE",
                    $"Cliente actualizado: '{nombreClienteUpdate}' | Tel: {clienteDTO.Telefono} | Mail: {clienteDTO.Mail}",
                    anteriorClienteDict.Count > 0 ? anteriorClienteDict : null,
                    nuevoClienteDict.Count    > 0 ? nuevoClienteDict    : null);

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
                var cliente = await _clienteRepository.GetByIdAsync(dto.IdCliente);

                if (cliente == null)
                {
                    return Result<string>.Failure(ClientErrorCode.cliente_not_found);
                }

                string nombreToggle = !string.IsNullOrWhiteSpace(cliente.RazonSocial)
                    ? cliente.RazonSocial
                    : $"{cliente.Nombre} {cliente.Apellido}";

                if (!dto.IsActive)
                {
                    if (cliente.FechaBaja != null)
                    {
                        return Result<string>.Failure(ClientErrorCode.cliente_already_inactive);
                    }

                    await _clienteRepository.UpdateStatusAsync(dto.IdCliente, DateOnly.FromDateTime(DateTime.Now));
                    await transaction.CommitAsync();
                    await LogAsync("BAJA", "CLIENTE", $"Cliente dado de baja: '{nombreToggle}'",
                        new { Activo = true }, new { Activo = false });
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
                    await LogAsync("REACTIVACION", "CLIENTE", $"Cliente reactivado: '{nombreToggle}'",
                        new { Activo = false }, new { Activo = true });
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