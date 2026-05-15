using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class ProductoListaprecioProveedorConfiguration : IEntityTypeConfiguration<ProductoListaprecioProveedor>
{
    public void Configure(EntityTypeBuilder<ProductoListaprecioProveedor> entity)
    {
        entity.HasKey(e => new { e.IdLista, e.IdProducto }).HasName("productolistaprecioproveedor_pkey");

        entity.ToTable("producto_listaprecio_proveedor");

        entity.Property(e => e.IdLista).HasColumnName("id_lista");
        entity.Property(e => e.IdProducto).HasColumnName("id_producto");
        entity.Property(e => e.Margen)
            .HasPrecision(5, 2)
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
    }
}
