using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using venta_stock_webapi.Client.Repository;
using venta_stock_webapi.Client.Services;
using venta_stock_webapi.CurrentAccount.Repository;
using venta_stock_webapi.CurrentAccount.Services.AccountConfigService;
using venta_stock_webapi.CurrentAccount.Services.CurrentAccountService;
using venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount;

using proyecto_venta_stock.Services;
using proyecto_venta_stock.User.Repository.PermitRepository;
using proyecto_venta_stock.User.Services;
using proyecto_venta_stock.User.UserRepository;
using venta_stock_webapi.User.Services;

var builder = WebApplication.CreateBuilder(args);

// =======================
// 📦 Servicios base
// =======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
// 🌐 CORS CONFIG
// =======================
// Nombre de la política CORS
const string FrontendCorsPolicy = "Frontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: FrontendCorsPolicy, policy =>
    {
        policy
            // ⚠️ Es importante especificar el dominio exacto del front-end.
            // No usar AllowAnyOrigin() si AllowCredentials() está presente.
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // 🔑 Habilita envío de cookies o headers de auth
    });
});

// =======================
// 🌐 CORS CONFIG
// =======================
// Nombre de la política CORS
const string FrontendCorsPolicy = "Frontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: FrontendCorsPolicy, policy =>
    {
        policy
            // ⚠️ Es importante especificar el dominio exacto del front-end.
            // No usar AllowAnyOrigin() si AllowCredentials() está presente.
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // 🔑 Habilita envío de cookies o headers de auth
    });
});

// =======================
// 💾 Base de datos
// =======================
var connectionString = builder.Configuration.GetConnectionString("PostgresSQLConnection");
builder.Services.AddDbContext<VentaStockContext>(options =>
    options.UseNpgsql(connectionString)
);

// =======================
// 🧩 AutoMapper
// =======================
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IAccountMovementRepository, AccountMovementRepository>();
builder.Services.AddScoped<IAccountConfigRepository, AccountConfigRepository>();

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IAccountConfigService, AccountConfigService>();
builder.Services.AddScoped<ICurrentAccountService, CurrentAccountService>();

builder.Services.AddSingleton<MovementStrategyFactory>();
// =======================
// 👥 Servicios de usuario y permisos
// =======================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

var app = builder.Build();

// =======================
// 🚀 Middleware
// =======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ⚠️ El orden de los middlewares importa:
app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

// ✅ CORS debe ir antes de Authentication / Authorization
app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
