using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> entity)
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
    }
}
