using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
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
// 🧩 AutoMapper
// =======================
builder.Services.AddAutoMapper(typeof(Program));

// Registrar opciones de importación (Defaults)
builder.Services.Configure<ImportDefaultsOptions>(
    builder.Configuration.GetSection("Defaults")
);

// =======================
// 👥 Servicios de usuario y permisos
// =======================
builder.Services.AddScoped<IUserRepository, UserRepository>();
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
