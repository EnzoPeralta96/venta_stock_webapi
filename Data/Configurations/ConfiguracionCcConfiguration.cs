using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class ConfiguracionCcConfiguration : IEntityTypeConfiguration<ConfiguracionCc>
{
    public void Configure(EntityTypeBuilder<ConfiguracionCc> entity)
    {
        entity.ToTable("configuracion_cc");
        entity.HasKey(x => x.IdConfig);
        entity.Property(x => x.IdConfig).HasColumnName("id_config");
        entity.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
        entity.Property(x => x.MontoLimite).HasColumnName("monto_limite").HasPrecision(18, 2);
        entity.Property(x => x.Activo).HasColumnName("activo").HasDefaultValue(true);
        entity.HasIndex(x => x.Nombre).IsUnique();
        entity.ToTable(t => t.HasCheckConstraint("CK_configuracion_cc_monto_limite", "monto_limite > 0"));
    }
}
