using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class CategoriumConfiguration : IEntityTypeConfiguration<Categorium>
{
    public void Configure(EntityTypeBuilder<Categorium> entity)
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
    }
}
