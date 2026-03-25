using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class UbicacionConfiguration : IEntityTypeConfiguration<Ubicacion>
{
    public void Configure(EntityTypeBuilder<Ubicacion> entity)
    {
        entity.HasKey(e => e.IdUbicacion).HasName("ubicacion_pkey");

        entity.ToTable("ubicacion");

        entity.Property(e => e.IdUbicacion).HasColumnName("id_ubicacion").ValueGeneratedOnAdd();
        entity.Property(e => e.Fila).HasColumnName("fila");
        entity.Property(e => e.Nivel).HasColumnName("nivel");
        entity.Property(e => e.Seccion).HasColumnName("seccion");
        entity.Property(e => e.Activo)
            .HasColumnName("activo")
            .HasDefaultValue(true)
            .ValueGeneratedNever();
    }
}
