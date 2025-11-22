using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using venta_stock_webapi.Cliente.Repository;
using venta_stock_webapi.Cliente.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("PostgresSQLConnection");
builder.Services.AddDbContext<VentaStockContext>(
   options => options.UseNpgsql(connectionString)
);


builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IAccountMovementRepository, AccountMovementRepository>();

builder.Services.AddScoped<IClienteService, ClienteService>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
