using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> entity)
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
        entity.Property(e => e.Stock).HasPrecision(18, 3).HasColumnName("stock");
        entity.Property(e => e.StockMinimo).HasPrecision(18, 3).HasColumnName("stock_minimo");
        entity.Property(e => e.VentaSinStock).HasColumnName("venta_sin_stock");
        entity.Property(e => e.IdUnidadMedida).HasColumnName("id_unidad_medida");
        entity.Property(e => e.Activo)
            .HasColumnName("activo")
            .HasDefaultValue(true)
            .ValueGeneratedNever();

        entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Productos)
            .HasForeignKey(d => d.IdCategoria)
            .HasConstraintName("producto_id_categoria_fkey");

        entity.HasOne(d => d.IdUbicacionNavigation).WithMany(p => p.Productos)
            .HasForeignKey(d => d.IdUbicacion)
            .HasConstraintName("producto_id_ubicacion_fkey");

        entity.HasOne(d => d.IdUnidadMedidaNavigation).WithMany(p => p.Productos)
            .HasForeignKey(d => d.IdUnidadMedida)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("producto_id_unidad_medida_fkey");
    }
}
