using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using venta_stock_webapi.Client.Repository;
using venta_stock_webapi.Client.Services;
using venta_stock_webapi.CurrentAccount.Repository;
using venta_stock_webapi.CurrentAccount.Services.AccountConfigService;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

var connectionString = builder.Configuration.GetConnectionString("PostgresSQLConnection");
builder.Services.AddDbContext<VentaStockContext>(
   options => options.UseNpgsql(connectionString)
);


builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IAccountMovementRepository, AccountMovementRepository>();
builder.Services.AddScoped<IAccountConfigRepository, AccountConfigRepository>();

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IAccountConfigService, AccountConfigService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
