using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class TipoMovimientoConfiguration : IEntityTypeConfiguration<TipoMovimiento>
{
    public void Configure(EntityTypeBuilder<TipoMovimiento> entity)
    {
        entity.HasKey(e => e.IdMovimiento).HasName("tipomovimiento_pkey");

        entity.ToTable("tipo_movimiento");

        entity.Property(e => e.IdMovimiento)
            .HasDefaultValueSql("nextval('tipomovimiento_id_movimiento_seq'::regclass)")
            .HasColumnName("id_movimiento");
        entity.Property(e => e.Accion)
            .HasMaxLength(150)
            .HasColumnName("accion");
        entity.Property(e => e.Nombre)
            .HasMaxLength(50)
            .HasColumnName("nombre");
    }
}
