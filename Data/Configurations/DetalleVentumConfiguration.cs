using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class DetalleVentumConfiguration : IEntityTypeConfiguration<DetalleVentum>
{
    public void Configure(EntityTypeBuilder<DetalleVentum> entity)
    {
        entity.HasKey(e => new { e.IdVenta, e.IdProducto }).HasName("detalleventa_pkey");

        entity.ToTable("detalle_venta");

        entity.Property(e => e.IdVenta).HasColumnName("id_venta");
        entity.Property(e => e.IdProducto).HasColumnName("id_producto");
        entity.Property(e => e.Cantidad).HasPrecision(18, 3).HasColumnName("cantidad");
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
    }
}
