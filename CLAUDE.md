# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a sales and inventory management Web API built with ASP.NET Core 8.0. The system manages products, sales, purchases, customers, suppliers, and user permissions for a stock management system.

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

## Current Implementation Status

The User management feature is partially implemented:
- User creation endpoint: `POST /User`
- Validation for duplicate usernames and emails
- AutoMapper integration for UserDTO to Usuario mapping

**Note**: There's a potential bug in `User/Repository/UserRepository/UserRepository.cs` at lines 27 and 32 - the `Exists` and `MailInUse` methods check for `FechaBaja != null` which would only find deleted users. This should likely be `FechaBaja == null` to find active users.
