using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class DetalleVentaPendienteConfiguration : IEntityTypeConfiguration<DetalleVentaPendiente>
{
    public void Configure(EntityTypeBuilder<DetalleVentaPendiente> entity)
    {
        entity.HasKey(e => e.IdDetalle).HasName("detalle_venta_pendiente_pkey");

        entity.ToTable("detalle_venta_pendiente");

        entity.HasIndex(e => e.IdVentaPendiente, "idx_detalle_venta_pendiente");

        entity.Property(e => e.IdDetalle).HasColumnName("id_detalle");
        entity.Property(e => e.Cantidad)
            .HasPrecision(18, 3)
            .HasColumnName("cantidad");
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
    }
}
