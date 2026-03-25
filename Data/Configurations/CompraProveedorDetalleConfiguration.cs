using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class CompraProveedorDetalleConfiguration : IEntityTypeConfiguration<CompraProveedorDetalle>
{
    public void Configure(EntityTypeBuilder<CompraProveedorDetalle> entity)
    {
        entity.HasKey(e => e.IdCompraProveedorDetalle).HasName("compra_proveedor_detalle_pkey");

        entity.ToTable("compra_proveedor_detalle");

        entity.Property(e => e.IdCompraProveedorDetalle).HasColumnName("id_compra_proveedor_detalle").ValueGeneratedOnAdd();
        entity.Property(e => e.IdCompraProveedor).HasColumnName("id_compra_proveedor");
        entity.Property(e => e.IdProducto).HasColumnName("id_producto");
        entity.Property(e => e.Cantidad).HasPrecision(18, 3).HasColumnName("cantidad");
        entity.Property(e => e.PrecioUnitario).HasPrecision(10, 2).HasColumnName("precio_unitario");
        entity.Property(e => e.DescuentoPorcentaje).HasPrecision(5, 2).HasColumnName("descuento_porcentaje");
        entity.Property(e => e.IvaPorcentaje).HasPrecision(5, 2).HasColumnName("iva_porcentaje");
        entity.Property(e => e.Subtotal).HasPrecision(10, 2).HasColumnName("subtotal");
        entity.Property(e => e.Total).HasPrecision(10, 2).HasColumnName("total");

        entity.HasOne(d => d.IdCompraProveedorNavigation)
            .WithMany(p => p.CompraProveedorDetalles)
            .HasForeignKey(d => d.IdCompraProveedor)
            .HasConstraintName("compra_proveedor_detalle_id_compra_proveedor_fkey");

        entity.HasOne(d => d.IdProductoNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdProducto)
            .HasConstraintName("compra_proveedor_detalle_id_producto_fkey");
    }
}
