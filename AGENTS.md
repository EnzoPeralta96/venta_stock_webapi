# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project Overview

This is a sales and inventory management Web API built with ASP.NET Core 8.0. The system manages products, sales, purchases, customers, suppliers, and user permissions for a stock management system.

**Project Context**: Final university project for "Programador Universitario" degree. Backend developed by Enzo (student developer with medium/advanced technical knowledge), frontend in React by teammate.

## Developer Profile (Enzo)

### Academic Background
University programming student in final stage - all courses completed, working on final project.

### Technical Knowledge

**Databases** (Medium/Advanced):
- SQL databases: SQLite, PostgreSQL, MySQL
- Database analysis, design, and implementation
- Simple, medium, and complex SQL queries
- Stored procedures and functions
- Transactions, triggers, indexes for query optimization
- Data type definitions, views

**Programming Languages**:
- **C** (Advanced): Structured programming up to double pointers, basic file handling (open, read, write), data structures (linked lists, stacks, queues, binary trees), console systems using data structures, threads, MPI implementations (e.g., parallelization of numerical methods)
- **C++** (Strong): Console management systems using OOP and SOLID principles
- **C#/.NET** (Strong): Console applications using OOP and SOLID principles, REST API consumption, RESTful Web API development, MVC web applications, client-server applications
- **Python**: Numerical methods (polynomial roots, linear equation systems, interpolation, quadrature, linear regression, trigonometric approximation/Fourier series, ODEs with IVP), data science (data analysis, exploration, clustering, neural networks including RNNs)
- **Web**: HTML, CSS, JavaScript (very basic), PHP (not used recently, but created a small shopping cart sales system)

**Software Engineering**:
- Medium/high proficiency in OOP
- Theoretical knowledge of software architectures
- Design patterns: Result, Observer, Singleton, State, Template, Factory, Strategy, Repository
- Unit testing with XUnit (C#)

**Additional Knowledge**:
- Communications: OSI model, TCP/IP
- Operating Systems (theory): Processes, memory scheduling, process scheduling, concurrency, deadlock, virtualization

### Coding Style & Architectural Preferences

**Technology Stack**:
- Backend: .NET 7/8 (C#)
- Database: PostgreSQL with EF Core
- Architecture: Strict layered architecture (Controllers → Services → Repositories → DTOs)
- Patterns: Result Pattern, Repository Pattern, AutoMapper
- Authentication: JWT with roles and permissions

**Implemented System Modules**:
- **Usuarios y Permisos**: Roles, permission categories, claims, JWT authentication
- **Productos**: Categories, locations, stock history, stock movements
- **Compras y Ventas**: Complete stock update logic, validations, automatic calculations
- **Cuenta Corriente**: Movements, balances, payments, debts
- **Auditoría**: Timestamps, user tracking, traceability
- **Reportes**: Advanced queries using LINQ and expressions

**Coding Principles**:
- Use DTOs for all input/output
- Clean AutoMapper Profiles
- **Keep business logic in Services, NEVER in Controllers**
- Abstract Repositories with interfaces
- Error handling with `Result<T>` or typed responses
- Clean, readable, predictable, and well-structured code

**EF Core Practices**:
- Migrations and well-defined relationships
- Fluent API for complex configurations
- Advanced LINQ queries
- Optimized queries with `.Include()` and `.AsNoTracking()`

### Code Generation Expectations

When generating code for this project:

**Models**: Classes with validations and attributes

**DTOs**: Clear separation between input/output DTOs

**Services**:
- Complete business logic implementation
- All necessary validations
- Calculations and transformations
- No repository implementation details exposed

**Repositories**:
- Optimized LINQ queries
- Use `.Include()` for eager loading
- Use `.AsNoTracking()` for read-only operations
- Return only necessary data

**Controllers**:
- Minimalist endpoints
- Only call service methods
- Return appropriate HTTP status codes
- Zero business logic

**Mappings**: Clear and explicit AutoMapper Profile configurations

**General**:
- Suggest architectural improvements when appropriate
- Complete missing implementations assuming project best practices
- Follow existing patterns consistently
- Maintain strict separation of concerns

## Essential Commands

### Build and Run
```bash
dotnet build
dotnet run
```

### Database Migrations
```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Apply migrations to database
dotnet ef database update

# Revert last migration
dotnet ef migrations remove
```

### Development
```bash
# Run with hot reload
dotnet watch run

# Restore dependencies
dotnet restore
```

## Architecture

### Database-First Approach
The project uses **Entity Framework Core with PostgreSQL** in a database-first approach. The `VentaStockContext` was scaffolded from an existing database schema.

**IMPORTANT**: The `VentaStockContext.cs` file at lines 56-58 contains a hardcoded connection string in `OnConfiguring`. This should not be modified as the actual connection string is properly configured in `appsettings.json` and injected via DI in `Program.cs:16-19`.

### Layered Architecture Pattern

The codebase follows a feature-based organization with clear separation of concerns:

#### **Feature Folders** (e.g., `User/`)
Each feature domain is organized into:
- `Controllers/` - API endpoints
- `Services/` - Business logic layer
- `Repository/` - Data access layer with interface contracts
- `DTO/` - Data transfer objects for API contracts
- `Profile/` - AutoMapper profiles for entity-to-DTO mappings

#### **Shared Infrastructure**
- `Models/` - EF Core entity models (scaffolded from database)
- `Data/VentaStockContext.cs` - DbContext with all entity configurations
- `Shared/ResultPattern/ResultT.cs` - Result pattern implementation for operation outcomes

### Dependency Injection
Services are registered in `Program.cs` following the pattern:
```csharp
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
```

### Result Pattern
The codebase uses a custom `Result<T>` pattern (in `Shared/ResultPattern/`) to handle operation outcomes:
- `Result<T>.Succes(value)` - Success with value
- `Result<T>.Succes()` - Success without value
- `Result<T>.Failure(message)` - Failure with error message

Controllers check `result.IsSucces` and return appropriate HTTP responses based on `result.ErrosMessage`.

### AutoMapper Configuration
AutoMapper is configured globally in `Program.cs:22` with `typeof(Program)` to scan for all Profile classes. Entity-to-DTO mappings are defined in feature-specific Profile classes (e.g., `User/Profile/UserProfile.cs`).

### MessageProvider Pattern
The project uses a centralized message provider system (`Shared/MessageProvider/MessageProvider.cs`) to convert error codes into user-friendly messages for API responses.

**Implementation per feature** (e.g., `Cliente/Message/ClienteErrorCode.cs`):
```csharp
// 1. Define error code enum
public enum ClienteErrorCode
{
    cliente_not_found,
    dni_in_use,
    email_in_use,
    unexpected_error
}

// 2. Create static dictionary with friendly messages
public static class ClienteErrorDictionary
{
    public static readonly Dictionary<ClienteErrorCode, string> Messages = new()
    {
        { ClienteErrorCode.cliente_not_found, "El cliente indicado no existe." },
        { ClienteErrorCode.dni_in_use, "El DNI ya está registrado." },
        { ClienteErrorCode.email_in_use, "El correo electrónico ya está en uso." },
        { ClienteErrorCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." }
    };
}
```

**Usage in Controllers**:
```csharp
if (!result.IsSucces)
{
    var code = (ClienteErrorCode)result.ErrorCode;
    var errorMessage = MessageProvider.Get(ClienteErrorDictionary.Messages, code);
    return NotFound(errorMessage); // Returns friendly message to API client
}
```

**Pattern Benefits**:
- Centralized error message management
- Type-safe error codes
- Easy to maintain and update messages
- Consistent error responses across all endpoints

### PagedList Pattern
The project implements a generic pagination system (`Shared/Paged/PagedList.cs`) for all list endpoints.

**PagedList Properties**:
- `Items` - List of items for current page
- `PagedIndex` - Current page number
- `PageSize` - Items per page
- `TotalPages` - Total number of pages
- `TotalCount` - Total number of items
- `HasPrevioPage` - Boolean indicating if previous page exists
- `HasNextPage` - Boolean indicating if next page exists

**Implementation in Services**:
```csharp
public async Task<Result<PagedList<ClienteDTO>>> ClientesPagedAsync(
    int pageIndex,
    int pageSize,
    string searchTerm,
    string estado = "activos")
{
    try
    {
        // 1. Get base queryable from repository
        var query = _clienteRepository.ClientesQueryable(searchTerm);

        // 2. Apply filters
        if (estado.ToLower() == "activos")
            query = query.Where(c => c.FechaBaja == null);
        else if (estado.ToLower() == "eliminados")
            query = query.Where(c => c.FechaBaja != null);

        // 3. Project to DTO using AutoMapper
        var projected = _mapper.ProjectTo<ClienteDTO>(query);

        // 4. Create paginated result
        var paged = await PagedList<ClienteDTO>.CreateAsync(projected, pageIndex, pageSize);

        return Result<PagedList<ClienteDTO>>.Succes(paged);
    }
    catch (Exception ex)
    {
        _logger.LogError("Error inesperado: " + ex);
        return Result<PagedList<ClienteDTO>>.Failure(ClienteErrorCode.unexpected_error);
    }
}
```

**Controller Endpoint Example**:
```csharp
[HttpGet("search")]
public async Task<IActionResult> SearchClientes(
    int pageIndex = 1,
    string searchTerm = "",
    string estado = "activos")
{
    int pageSize = 10;
    var result = await _clienteService.ClientesPagedAsync(pageIndex, pageSize, searchTerm, estado);

    if (!result.IsSucces)
    {
        var code = (ClienteErrorCode)result.ErrorCode;
        var errorMessage = MessageProvider.Get(ClienteErrorDictionary.Messages, code);
        return NotFound(errorMessage);
    }

    return Ok(result.Value); // Returns PagedList<ClienteDTO> with metadata
}
```

**Pattern Benefits**:
- Consistent pagination across all features
- Includes navigation metadata (HasNextPage, TotalPages, etc.)
- Efficient database queries using Skip/Take
- Generic implementation works with any entity type

## Domain Model Overview

### Core Entities
- **Usuario** - System users with roles and permissions
- **Producto** - Products with categories, locations, pricing, and stock tracking
- **Cliente** - Customers with personal/business information
- **Proveedor** - Suppliers
- **Ventum** - Sales transactions with details
- **Compra** - Purchase orders
- **MovimientoCc** - Customer account movements (credit/debt)

### Supporting Entities
- **Permiso/PermisoUsuario** - Permission system (many-to-many)
- **CodigoBarra** - Product barcodes (one-to-many with products)
- **ListaPrecio/ProductoListaprecioProveedor** - Supplier price lists
- **Categorium** - Product categories
- **Ubicacion** - Storage locations (section/row/level)
- **Estado** - Generic status entity
- **MedioPago** - Payment methods
- **TipoMovimiento** - Account movement types

## Database Connection

PostgreSQL connection is configured in `appsettings.json` under `ConnectionStrings:PostgresSQLConnection`. The database is hosted on Railway (postgres.railway.internal).

## System Requirements & Documentation

### User Roles & Permissions

**Roles**:
- **Administrador Principal**: Full system access, cannot be deleted, manages all users and permissions
- **Encargado de Precios**: Product management, pricing, stock control, supplier price lists
- **Vendedor**: Sales, customer management, invoice generation, stock queries

**Permission System**: Fine-grained permissions organized by categories (Gestión de Usuarios, Ventas y Finanzas, Reportes y Auditoría)

### Core Business Rules

**Products (Productos)**:
- Each product has: name, brand, category, location (warehouse), price, stock, minimum stock level
- Multiple barcodes per product supported
- Products can be configured for "sell without stock"
- Warehouse location format: `Fila-Sección-Nivel` (e.g., "01 A 01")
- Logical deletion (soft delete) - products marked as inactive

**Customers (Clientes)**:
- Supports both individuals (DNI) and businesses (CUIT/Razón Social)
- Payment types: Cash (Contado) or Current Account (Cuenta Corriente)
- Current account includes credit limit management
- Logical deletion

**Sales (Ventas)**:
- Automatic stock update on sale
- Credit limit validation for current account customers
- Administrator can authorize sales exceeding credit limits (requires explicit approval)
- Invoices must show total in both numbers and words
- All sales tracked with timestamp and responsible user

**Current Account (Cuenta Corriente)**:
- Tracks: Invoices, Debit Notes (ND), Credit Notes (NC), Payment Receipts (RP)
- Automatic debit notes for overdue invoices
- Complete movement history per customer

**Stock Management**:
- Low stock notifications (email + UI list)
- Stock updates when receiving merchandise
- Optional sell-without-stock configuration per product
- Stock history tracking

**Audit & Traceability**:
- All important actions logged (user, timestamp, action, affected entity)
- Accessible only to users with audit permissions
- Enables complete operation traceability

### Search Functionality

Domain-specific search (not a global search):
- **Users**: by name, last name, email, role
- **Clients**: by name, DNI, phone, email
- **Products**: by name, category, barcode, location
- **Sales**: by receipt number, customer, date range, amount

All searches are reactive and show partial results as user types.

### Non-Functional Requirements

- **Response Time**: Max 1.5s for simple operations, max 3s for complex operations (reports, exports)
- **Availability**: 100% during business hours (8:00 AM - 6:00 PM, Monday-Friday)
- **Security**: HTTPS only, JWT authentication, encrypted sensitive data, SQL injection protection
- **Data Validation**: All inputs validated before storage
- **Export Formats**: PDF and Excel for reports and listings

## Current Implementation Status

### Implemented Features
- **User Management**: Partially implemented
  - User creation endpoint: `POST /User`
  - Validation for duplicate usernames and emails
  - AutoMapper integration for UserDTO to Usuario mapping

- **Products**: Partially implemented (according to git history)

### Known Issues & Bugs

**Note**: Bug in `main` branch - `User/Repository/UserRepository/UserRepository.cs:27` and `:32`:
- The `Exists` and `MailInUse` methods check for `FechaBaja != null` (deleted users)
- Should be `FechaBaja == null` to find active users
- **✅ This is correctly implemented in `dev_user` branch**

### Next Implementation: Clientes (Customers)

When implementing the Customer CRUD (RF004.1), follow the established architecture:

**Structure to Create**:
```
Cliente/
├── Controllers/
│   └── ClienteController.cs
├── Services/
│   ├── IClienteService.cs
│   └── ClienteService.cs
├── Repository/
│   ├── IClienteRepository.cs
│   └── ClienteRepository.cs
├── DTO/
│   ├── ClienteCreateDTO.cs
│   ├── ClienteUpdateDTO.cs
│   └── ClienteResponseDTO.cs
└── Profile/
    └── ClienteProfile.cs
```

**Key Validations**:
- DNI or CUIT must be unique
- Email must be unique (if provided)
- Either DNI/Nombre/Apellido OR CUIT/Razón Social must be provided
- Logical deletion only (set FechaBaja, never physical delete)
- Validate credit limit if cuenta corriente is enabled

**Search Criteria**: nombre, DNI, teléfono, email

**Remember**:
- Register all operations in audit history
- Use Result<T> pattern for all service methods
- Keep controllers thin - business logic in services only
- Use `.AsNoTracking()` for read-only queries
