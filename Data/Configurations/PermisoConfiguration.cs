using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> entity)
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
    }
}
