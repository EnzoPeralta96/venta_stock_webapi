using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data;

public partial class VentaStockContext : DbContext
{
    public VentaStockContext()
    {
    }

    public VentaStockContext(DbContextOptions<VentaStockContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categorium> Categoria { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<CodigoBarra> CodigoBarras { get; set; }

    public virtual DbSet<Compra> Compras { get; set; }

    public virtual DbSet<DetalleVentum> DetalleVenta { get; set; }

    public virtual DbSet<Estado> Estados { get; set; }

    public virtual DbSet<global::proyecto_venta_stock.Models.ListaPrecio> ListaPrecios { get; set; }

    public virtual DbSet<MedioPago> MedioPagos { get; set; }

    public virtual DbSet<MovimientoCc> MovimientoCcs { get; set; }

    public virtual DbSet<Permiso> Permisos { get; set; }

    public virtual DbSet<CategoriaPermiso> CategoriaPermisos { get; set; }

    public virtual DbSet<PermisoUsuario> PermisoUsuarios { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<ProductoListaprecioProveedor> ProductoListaprecioProveedors { get; set; }

    public virtual DbSet<global::proyecto_venta_stock.Models.Proveedor> Proveedors { get; set; }

    public virtual DbSet<TipoMovimiento> TipoMovimientos { get; set; }

    public virtual DbSet<Ubicacion> Ubicacions { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Ventum> Venta { get; set; }

    public virtual DbSet<ConfiguracionCc> ConfiguracionCcs { get; set; }

    public virtual DbSet<Auditoria> Auditorias { get; set; }

    public virtual DbSet<Ferreteria> Ferreterias { get; set; }

    public virtual DbSet<MotivoNotaDebito> MotivoNotaDebitos { get; set; }

    public virtual DbSet<MotivoNotaCredito> MotivoNotaCreditos { get; set; }

    public virtual DbSet<ConfiguracionInteres> ConfiguracionIntereses { get; set; }

    public virtual DbSet<VentaPendiente> VentaPendiente { get; set; }

    public virtual DbSet<DetalleVentaPendiente> DetalleVentaPendiente { get; set; }

    public virtual DbSet<global::proyecto_venta_stock.Models.CompraProveedor> ComprasProveedor { get; set; }

    public virtual DbSet<CompraProveedorDetalle> ComprasProveedorDetalle { get; set; }

    public virtual DbSet<TipoMovimientoStock> TipoMovimientoStocks { get; set; }

    public virtual DbSet<MovimientoStock> MovimientoStocks { get; set; }

    public virtual DbSet<UnidadMedida> UnidadMedidas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VentaStockContext).Assembly);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
