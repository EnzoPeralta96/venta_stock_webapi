using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.User.Services;
using proyecto_venta_stock.User.UserRepository;
using proyecto_venta_stock.Product.ProductRepository;
using proyecto_venta_stock.Product.Services;
using proyecto_venta_stock.Category.CategoryRepository;
using proyecto_venta_stock.Category.Services;
using proyecto_venta_stock.Location.Services;

using proyecto_venta_stock.Location.LocationRepository;

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

/* CORS */
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5175") // origen del front
              .AllowAnyHeader()
              .AllowAnyMethod();
              
    });
});


builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IUserService, UserService>();

/* Product */

builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddScoped<IProductServices, ProductServices>();

/* Category */

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryServices, CategoryService>();

/* Location */

builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<ILocationService, LocationServices>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
