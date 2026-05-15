using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class MedioPagoConfiguration : IEntityTypeConfiguration<MedioPago>
{
    public void Configure(EntityTypeBuilder<MedioPago> entity)
    {
        entity.HasKey(e => e.IdMedioPago).HasName("medio_pago_pkey");

        entity.ToTable("medio_pago");

        entity.Property(e => e.IdMedioPago).HasColumnName("id_medio_pago");
        entity.Property(e => e.MedioPago1)
            .HasMaxLength(50)
            .HasColumnName("medio_pago");
    }
}
