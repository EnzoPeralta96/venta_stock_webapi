using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class EstadoConfiguration : IEntityTypeConfiguration<Estado>
{
    public void Configure(EntityTypeBuilder<Estado> entity)
    {
        entity.HasKey(e => e.IdEstado).HasName("estado_pkey");

        entity.ToTable("estado");

        entity.Property(e => e.IdEstado).HasColumnName("id_estado");
        entity.Property(e => e.Estado1)
            .HasMaxLength(50)
            .HasColumnName("estado");
    }
}
