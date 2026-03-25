using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class ConfiguracionInteresConfiguration : IEntityTypeConfiguration<ConfiguracionInteres>
{
    public void Configure(EntityTypeBuilder<ConfiguracionInteres> entity)
    {
        entity.HasKey(e => e.IdConfig);
        entity.ToTable("configuracion_interes");
        entity.Property(e => e.IdConfig)
            .HasColumnName("id_config")
            .ValueGeneratedOnAdd();
        entity.Property(e => e.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();
        entity.Property(e => e.PorcentajeInteres)
            .HasColumnName("porcentaje_interes")
            .HasPrecision(5, 2)
            .IsRequired();
        entity.Property(e => e.DiaVencimiento)
            .HasColumnName("dia_vencimiento")
            .IsRequired();
        entity.Property(e => e.EsActual)
            .HasColumnName("es_actual")
            .HasDefaultValue(false);
    }
}
