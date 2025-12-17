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
using proyecto_venta_stock.Product.ProductRepository;
using proyecto_venta_stock.Product.Services;
using proyecto_venta_stock.Category.CategoryRepository;
using proyecto_venta_stock.Category.Services;
using proyecto_venta_stock.Location.Services;
using proyecto_venta_stock.Location.LocationRepository;
using venta_stock_webapi.User.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using venta_stock_webapi.Shared.JwtBinding;
using venta_stock_webapi.Login.JwtService;
using venta_stock_webapi.Login.Services;
using Microsoft.AspNetCore.Identity;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Shared.Auth.PassService;
using Microsoft.AspNetCore.Authorization;
using venta_stock_webapi.Shared.Auth.Authorization;
using proyecto_venta_stock.Configuration;

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
// 💾 Base de datos
// =======================
var connectionString = builder.Configuration.GetConnectionString("PostgresSQLConnection");
builder.Services.AddDbContext<VentaStockContext>(options =>
    options.UseNpgsql(connectionString)
);

// =======================
// JWT AUTHENTICATION
// =======================
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? throw new InvalidOperationException("La sección 'Jwt' no está configurada correctamente.");;

        var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.Key);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) // Elimina el tiempo de tolerancia por defecto
        };
    });

builder.Services.AddAuthorization(
    
);
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();


// =======================
// 🧩 AutoMapper
// =======================
builder.Services.AddAutoMapper(typeof(Program));

// Registrar opciones de importación (Defaults)
builder.Services.Configure<ImportDefaultsOptions>(
    builder.Configuration.GetSection("Defaults")
);

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
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// =======================
// 📦 Servicios de Product, Category y Location
// =======================
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductServices, ProductServices>();

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryServices, CategoryService>();

builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<ILocationService, LocationServices>();

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

// ✅ CORS debe ir antes de Authentication / Authorization
app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
