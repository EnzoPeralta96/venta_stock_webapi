using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class CategoriaPermisoConfiguration : IEntityTypeConfiguration<CategoriaPermiso>
{
    public void Configure(EntityTypeBuilder<CategoriaPermiso> entity)
    {
        entity.HasKey(e => e.IdCategoriaPermiso).HasName("categoria_permiso_pkey");

        entity.ToTable("categoria_permiso");

        entity.Property(e => e.IdCategoriaPermiso)
            .HasColumnName("id_categoria_permiso");

        entity.Property(e => e.Categoria)
            .HasColumnName("categoria")
            .HasMaxLength(100)
            .IsRequired();
    }
}
