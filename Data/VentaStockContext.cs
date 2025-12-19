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

    public virtual DbSet<ListaPrecio> ListaPrecios { get; set; }

    public virtual DbSet<MedioPago> MedioPagos { get; set; }

    public virtual DbSet<MovimientoCc> MovimientoCcs { get; set; }

    public virtual DbSet<Permiso> Permisos { get; set; }

    public virtual DbSet<CategoriaPermiso> CategoriaPermisos { get; set; }
    public virtual DbSet<PermisoUsuario> PermisoUsuarios { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<ProductoListaprecioProveedor> ProductoListaprecioProveedors { get; set; }

    public virtual DbSet<Proveedor> Proveedors { get; set; }

    public virtual DbSet<TipoMovimiento> TipoMovimientos { get; set; }

    public virtual DbSet<Ubicacion> Ubicacions { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Ventum> Venta { get; set; }

    public virtual DbSet<ConfiguracionCc> ConfiguracionCcs { get; set; }

    // Nuevas entidades de ventas pendientes
    public virtual DbSet<VentaPendiente> VentaPendiente { get; set; }

    public virtual DbSet<DetalleVentaPendiente> DetalleVentaPendiente { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Categorium>(entity =>
        {
            entity.HasKey(e => e.IdCategoria).HasName("categoria_pkey");

            entity.ToTable("categoria");

            entity.Property(e => e.IdCategoria).HasColumnName("id_categoria").ValueGeneratedOnAdd();
            entity.Property(e => e.Categoria)
                .HasMaxLength(100)
                .HasColumnName("categoria");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.IdCliente).HasName("cliente_pkey");

            entity.ToTable("cliente");

            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .HasColumnName("apellido");
            entity.Property(e => e.Cuit)
                .HasMaxLength(20)
                .HasColumnName("cuit");
            entity.Property(e => e.Dni)
                .HasMaxLength(20)
                .HasColumnName("dni");
            entity.Property(e => e.Mail)
                .HasMaxLength(100)
                .HasColumnName("mail");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.RazonSocial)
                .HasMaxLength(150)
                .HasColumnName("razon_social");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
            entity.Property(e => e.FechaAlta)
                .HasColumnName("fecha_alta");
            entity.Property(e => e.FechaBaja)
                .HasColumnName("fecha_baja");
        });

        modelBuilder.Entity<CodigoBarra>(entity =>
        {
            entity.HasKey(e => e.IdCodigo).HasName("codigobarra_pkey");

            entity.ToTable("codigo_barra");

            entity.HasIndex(e => e.Codigo, "codigobarra_codigo_key").IsUnique();

            entity.Property(e => e.IdCodigo)
                .HasDefaultValueSql("nextval('codigobarra_id_codigo_seq'::regclass)")
                .HasColumnName("id_codigo");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Codigo)
                .HasMaxLength(100)
                .HasColumnName("codigo");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.Prinicial)
                .HasDefaultValue(false)
                .HasColumnName("prinicial");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.CodigoBarras)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("codigobarra_id_producto_fkey");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => new { e.IdProducto, e.IdProveedor, e.Fecha }).HasName("compra_pkey");

            entity.ToTable("compra");

            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.PrecioUnitario)
                .HasPrecision(10, 2)
                .HasColumnName("precio_unitario");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.Compras)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("compra_id_producto_fkey");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Compras)
                .HasForeignKey(d => d.IdProveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("compra_id_proveedor_fkey");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Compras)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("compra_id_usuario_fkey");
        });

        modelBuilder.Entity<DetalleVentum>(entity =>
        {
            entity.HasKey(e => new { e.IdVenta, e.IdProducto }).HasName("detalleventa_pkey");

            entity.ToTable("detalle_venta");

            entity.Property(e => e.IdVenta).HasColumnName("id_venta");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.PrecioVenta)
                .HasPrecision(10, 2)
                .HasColumnName("precio_venta");
            entity.Property(e => e.SubTotal)
                .HasPrecision(10, 2)
                .HasColumnName("sub_total");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetalleVenta)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("detalleventa_id_producto_fkey");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.DetalleVenta)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("detalleventa_id_venta_fkey");
        });

        modelBuilder.Entity<Estado>(entity =>
        {
            entity.HasKey(e => e.IdEstado).HasName("estado_pkey");

            entity.ToTable("estado");

            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.Estado1)
                .HasMaxLength(50)
                .HasColumnName("estado");
        });

        modelBuilder.Entity<ListaPrecio>(entity =>
        {
            entity.HasKey(e => e.IdLista).HasName("listaprecio_pkey");

            entity.ToTable("lista_precio");

            entity.Property(e => e.IdLista)
                .HasDefaultValueSql("nextval('listaprecio_id_lista_seq'::regclass)")
                .HasColumnName("id_lista");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.IdUsuarioRegistra).HasColumnName("id_usuario_registra");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(250)
                .HasColumnName("observaciones");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.ListaPrecios)
                .HasForeignKey(d => d.IdProveedor)
                .HasConstraintName("listaprecio_id_proveedor_fkey");

            entity.HasOne(d => d.IdUsuarioRegistraNavigation).WithMany(p => p.ListaPrecios)
                .HasForeignKey(d => d.IdUsuarioRegistra)
                .HasConstraintName("listaprecio_id_usuario_registra_fkey");
        });

        modelBuilder.Entity<MedioPago>(entity =>
        {
            entity.HasKey(e => e.IdMedioPago).HasName("medio_pago_pkey");

            entity.ToTable("medio_pago");

            entity.Property(e => e.IdMedioPago).HasColumnName("id_medio_pago");
            entity.Property(e => e.MedioPago1)
                .HasMaxLength(50)
                .HasColumnName("medio_pago");
        });

        modelBuilder.Entity<MovimientoCc>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento).HasName("movimientocc_pkey");

            entity.ToTable("movimiento_cc");

            entity.Property(e => e.IdMovimiento)
                .HasDefaultValueSql("nextval('movimientocc_id_movimiento_seq'::regclass)")
                .HasColumnName("id_movimiento");
            entity.Property(e => e.Detalle).HasColumnName("detalle");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha");
            entity.Property(e => e.FechaAutorizacion)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_autorizacion");
            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.IdTipoMovimiento).HasColumnName("id_tipo_movimiento");
            entity.Property(e => e.IdUsuarioAutoriza).HasColumnName("id_usuario_autoriza");
            entity.Property(e => e.IdUsuarioRegistra).HasColumnName("id_usuario_registra");
            entity.Property(e => e.IdVenta).HasColumnName("id_venta");
            entity.Property(e => e.Importe)
                .HasPrecision(10, 2)
                .HasColumnName("importe");
            entity.Property(e => e.LimiteCuenta)
                .HasPrecision(10, 2)
                .HasColumnName("limite_cuenta");
            entity.Property(e => e.SaldoActual)
                .HasPrecision(10, 2)
                .HasColumnName("saldo_actual");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.MovimientoCcs)
                .HasForeignKey(d => d.IdCliente)
                .HasConstraintName("movimientocc_id_cliente_fkey");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.MovimientoCcs)
                .HasForeignKey(d => d.IdEstado)
                .HasConstraintName("movimientocc_id_estado_fkey");

            entity.HasOne(d => d.IdTipoMovimientoNavigation).WithMany(p => p.MovimientoCcs)
                .HasForeignKey(d => d.IdTipoMovimiento)
                .HasConstraintName("movimientocc_id_tipo_movimiento_fkey");

            entity.HasOne(d => d.IdUsuarioAutorizaNavigation).WithMany(p => p.MovimientoCcIdUsuarioAutorizaNavigations)
                .HasForeignKey(d => d.IdUsuarioAutoriza)
                .HasConstraintName("movimientocc_id_usuario_autoriza_fkey");

            entity.HasOne(d => d.IdUsuarioRegistraNavigation).WithMany(p => p.MovimientoCcIdUsuarioRegistraNavigations)
                .HasForeignKey(d => d.IdUsuarioRegistra)
                .HasConstraintName("movimientocc_id_usuario_registra_fkey");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.MovimientoCcs)
                .HasForeignKey(d => d.IdVenta)
                .HasConstraintName("movimientocc_id_venta_fkey");
        });

        modelBuilder.Entity<Permiso>(entity =>
     {
         entity.HasKey(e => e.IdPermiso).HasName("permiso_pkey");

         entity.ToTable("permiso");

         entity.Property(e => e.IdPermiso)
             .HasColumnName("id_permiso");

         entity.Property(e => e.Permiso1)
             .HasColumnName("permiso")
             .HasMaxLength(100);

         entity.Property(e => e.Descripcion)
             .HasColumnName("descripcion")
             .HasMaxLength(150);

         entity.Property(e => e.IdCategoriaPermiso)
             .HasColumnName("id_categoria_permiso");

         entity.HasOne(d => d.CategoriaPermiso)
             .WithMany(p => p.Permisos)
             .HasForeignKey(d => d.IdCategoriaPermiso)
             .OnDelete(DeleteBehavior.Restrict)
             .HasConstraintName("permiso_id_categoria_permiso_fkey");
     });

        modelBuilder.Entity<CategoriaPermiso>(entity =>
        {
            entity.HasKey(e => e.IdCategoriaPermiso).HasName("categoria_permiso_pkey");

            entity.ToTable("categoria_permiso");

            entity.Property(e => e.IdCategoriaPermiso)
                .HasColumnName("id_categoria_permiso");

            entity.Property(e => e.Categoria)
                .HasColumnName("categoria")
                .HasMaxLength(100) // o el tamaño que definiste en la DB
                .IsRequired();
        });




        modelBuilder.Entity<PermisoUsuario>(entity =>
        {
            entity.HasKey(e => new { e.IdPermiso, e.IdUsuario }).HasName("permisousuario_pkey");

            entity.ToTable("permiso_usuario");

            entity.Property(e => e.IdPermiso).HasColumnName("id_permiso");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.FechaAsignacion).HasColumnName("fecha_asignacion");

            entity.HasOne(d => d.IdPermisoNavigation).WithMany(p => p.PermisoUsuarios)
                .HasForeignKey(d => d.IdPermiso)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("permisousuario_id_permiso_fkey");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.PermisoUsuarios)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("permisousuario_id_usuario_fkey");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("producto_pkey");

            entity.ToTable("producto");

            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .HasColumnName("descripcion");
            entity.Property(e => e.IdCategoria).HasColumnName("id_categoria");
            entity.Property(e => e.IdUbicacion).HasColumnName("id_ubicacion");
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .HasColumnName("marca");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.StockMinimo).HasColumnName("stock_minimo");
            entity.Property(e => e.VentaSinStock).HasColumnName("venta_sin_stock");
            entity.Property(e => e.Activo)
                        .HasColumnName("activo")
                        .HasDefaultValue(true);


            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("producto_id_categoria_fkey");

            entity.HasOne(d => d.IdUbicacionNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdUbicacion)
                .HasConstraintName("producto_id_ubicacion_fkey");
        });

        modelBuilder.Entity<ProductoListaprecioProveedor>(entity =>
        {
            entity.HasKey(e => new { e.IdLista, e.IdProducto }).HasName("productolistaprecioproveedor_pkey");

            entity.ToTable("producto_listaprecio_proveedor");

            entity.Property(e => e.IdLista).HasColumnName("id_lista");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.Margen)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("30.00")
                .HasColumnName("margen");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");

            entity.HasOne(d => d.IdListaNavigation).WithMany(p => p.ProductoListaprecioProveedors)
                .HasForeignKey(d => d.IdLista)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("productolistaprecioproveedor_id_lista_fkey");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.ProductoListaprecioProveedors)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("productolistaprecioproveedor_id_producto_fkey");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.IdProveedor).HasName("proveedor_pkey");

            entity.ToTable("proveedor");

            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.Direccion)
                .HasMaxLength(200)
                .HasColumnName("direccion");
            entity.Property(e => e.Proveedor1)
                .HasMaxLength(100)
                .HasColumnName("proveedor");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<TipoMovimiento>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento).HasName("tipomovimiento_pkey");

            entity.ToTable("tipo_movimiento");

            entity.Property(e => e.IdMovimiento)
                .HasDefaultValueSql("nextval('tipomovimiento_id_movimiento_seq'::regclass)")
                .HasColumnName("id_movimiento");
            entity.Property(e => e.Accion)
                .HasMaxLength(150)
                .HasColumnName("accion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Ubicacion>(entity =>
        {
            entity.HasKey(e => e.IdUbicacion).HasName("ubicacion_pkey");

            entity.ToTable("ubicacion");

            entity.Property(e => e.IdUbicacion).HasColumnName("id_ubicacion").ValueGeneratedOnAdd();
            entity.Property(e => e.Fila).HasColumnName("fila");
            entity.Property(e => e.Nivel).HasColumnName("nivel");
            entity.Property(e => e.Seccion).HasColumnName("seccion");
            entity.Property(e=> e.Activo)
                        .HasColumnName("activo")
                        .HasDefaultValue(true);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("usuario_pkey");

            entity.ToTable("usuario");

            entity.HasIndex(e => e.Usuario1, "usuario_usuario_key").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .HasColumnName("apellido");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
            entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .HasColumnName("password");
            entity.Property(e => e.Rol)
                .HasMaxLength(50)
                .HasColumnName("rol");
            entity.Property(e => e.Usuario1)
                .HasMaxLength(50)
                .HasColumnName("usuario");
        });

        modelBuilder.Entity<Ventum>(entity =>
        {
            entity.HasKey(e => e.IdVenta).HasName("venta_pkey");

            entity.ToTable("venta");

            entity.Property(e => e.IdVenta).HasColumnName("id_venta");
            entity.Property(e => e.CodigoVenta).HasColumnName("codigo_venta");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha");
            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.IdMedioPago).HasColumnName("id_medio_pago");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Total)
                .HasPrecision(10, 2)
                .HasColumnName("total");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdCliente)
                .HasConstraintName("venta_id_cliente_fkey");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdEstado)
                .HasConstraintName("venta_id_estado_fkey");

            entity.HasOne(d => d.IdMedioPagoNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdMedioPago)
                .HasConstraintName("venta_id_medio_pago_fkey");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("venta_id_usuario_fkey");
        });

        modelBuilder.Entity<ConfiguracionCc>(e =>
        {
            e.ToTable("configuracion_cc");
            e.HasKey(x => x.IdConfig);
            e.Property(x => x.IdConfig).HasColumnName("id_config");
            e.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
            e.Property(x => x.MontoLimite).HasColumnName("monto_limite").HasPrecision(18, 2);
            e.Property(x => x.Activo).HasColumnName("activo").HasDefaultValue(true);
            e.HasIndex(x => x.Nombre).IsUnique();
            // EF Core permite check constraints:
            e.ToTable(t => t.HasCheckConstraint("CK_configuracion_cc_monto_limite", "monto_limite > 0"));
        });

        // Configuración de entidades de ventas pendientes
        modelBuilder.Entity<VentaPendiente>(entity =>
        {
            entity.HasKey(e => e.IdVentaPendiente).HasName("venta_pendiente_pkey");

            entity.ToTable("venta_pendiente", tb => tb.HasComment("Ventas que exceden el límite de crédito y requieren autorización"));

            entity.HasIndex(e => e.IdCliente, "idx_venta_pendiente_cliente");

            entity.HasIndex(e => e.IdEstado, "idx_venta_pendiente_estado");

            entity.HasIndex(e => e.FechaRegistro, "idx_venta_pendiente_fecha").IsDescending();

            entity.HasIndex(e => e.IdUsuarioVendedor, "idx_venta_pendiente_vendedor");

            entity.HasIndex(e => e.CodigoVenta, "venta_pendiente_codigo_venta_key").IsUnique();

            entity.Property(e => e.IdVentaPendiente).HasColumnName("id_venta_pendiente");
            entity.Property(e => e.CodigoVenta)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("codigo_venta");
            entity.Property(e => e.Excedente)
                .HasPrecision(18, 2)
                .HasComment("Monto que excede el límite de crédito")
                .HasColumnName("excedente");
            entity.Property(e => e.FechaAutorizacion)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_autorizacion");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.IdMedioPago).HasColumnName("id_medio_pago");
            entity.Property(e => e.IdUsuarioAutoriza).HasColumnName("id_usuario_autoriza");
            entity.Property(e => e.IdUsuarioVendedor).HasColumnName("id_usuario_vendedor");
            entity.Property(e => e.IdVentaGenerada)
                .HasComment("Referencia a la venta definitiva si fue aprobada")
                .HasColumnName("id_venta_generada");
            entity.Property(e => e.LimiteCuenta)
                .HasPrecision(18, 2)
                .HasColumnName("limite_cuenta");
            entity.Property(e => e.ObservacionesAutorizacion).HasColumnName("observaciones_autorizacion");
            entity.Property(e => e.SaldoActual)
                .HasPrecision(18, 2)
                .HasColumnName("saldo_actual");
            entity.Property(e => e.SaldoDespuesVenta)
                .HasPrecision(18, 2)
                .HasColumnName("saldo_despues_venta");
            entity.Property(e => e.Total)
                .HasPrecision(18, 2)
                .HasColumnName("total");

            entity.HasOne(d => d.IdClienteNavigation).WithMany()
                .HasForeignKey(d => d.IdCliente)
                .HasConstraintName("venta_pendiente_id_cliente_fkey");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany()
                .HasForeignKey(d => d.IdEstado)
                .HasConstraintName("venta_pendiente_id_estado_fkey");

            entity.HasOne(d => d.IdMedioPagoNavigation).WithMany()
                .HasForeignKey(d => d.IdMedioPago)
                .HasConstraintName("venta_pendiente_id_medio_pago_fkey");

            entity.HasOne(d => d.IdUsuarioAutorizaNavigation).WithMany()
                .HasForeignKey(d => d.IdUsuarioAutoriza)
                .HasConstraintName("venta_pendiente_id_usuario_autoriza_fkey");

            entity.HasOne(d => d.IdUsuarioVendedorNavigation).WithMany()
                .HasForeignKey(d => d.IdUsuarioVendedor)
                .HasConstraintName("venta_pendiente_id_usuario_vendedor_fkey");

            entity.HasOne(d => d.IdVentaGeneradaNavigation).WithMany()
                .HasForeignKey(d => d.IdVentaGenerada)
                .HasConstraintName("venta_pendiente_id_venta_generada_fkey");
        });

        modelBuilder.Entity<DetalleVentaPendiente>(entity =>
        {
            entity.HasKey(e => e.IdDetalle).HasName("detalle_venta_pendiente_pkey");

            entity.ToTable("detalle_venta_pendiente");

            entity.HasIndex(e => e.IdVentaPendiente, "idx_detalle_venta_pendiente");

            entity.Property(e => e.IdDetalle).HasColumnName("id_detalle");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.IdVentaPendiente).HasColumnName("id_venta_pendiente");
            entity.Property(e => e.PrecioVenta)
                .HasPrecision(18, 2)
                .HasColumnName("precio_venta");
            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 2)
                .HasColumnName("subtotal");

            entity.HasOne(d => d.IdVentaPendienteNavigation).WithMany(p => p.DetalleVentaPendientes)
                .HasForeignKey(d => d.IdVentaPendiente)
                .HasConstraintName("detalle_venta_pendiente_id_venta_pendiente_fkey");

            entity.HasOne(d => d.IdProductoNavigation).WithMany()
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("detalle_venta_pendiente_id_producto_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
