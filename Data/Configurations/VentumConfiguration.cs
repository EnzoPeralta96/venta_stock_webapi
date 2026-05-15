using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class VentumConfiguration : IEntityTypeConfiguration<Ventum>
{
    public void Configure(EntityTypeBuilder<Ventum> entity)
    {
        entity.HasKey(e => e.IdVenta).HasName("venta_pkey");

        entity.ToTable("venta");

        entity.Property(e => e.IdVenta).HasColumnName("id_venta");
        entity.Property(e => e.CodigoVenta).HasColumnName("codigo_venta");
        entity.Property(e => e.Fecha)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("timestamp without time zone")
            .HasColumnName("fecha");
        entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
        entity.Property(e => e.IdEstado).HasColumnName("id_estado");
        entity.Property(e => e.IdMedioPago).HasColumnName("id_medio_pago");
        entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
        entity.Property(e => e.Total)
            .HasPrecision(10, 2)
            .HasColumnName("total");
        entity.Property(e => e.IdMotivoNc).HasColumnName("id_motivo_nc");
        entity.Property(e => e.DetalleNc)
            .HasColumnName("detalle_nc")
            .HasMaxLength(500);

        entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Venta)
            .HasForeignKey(d => d.IdCliente)
            .HasConstraintName("venta_id_cliente_fkey");

        entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Venta)
            .HasForeignKey(d => d.IdEstado)
            .HasConstraintName("venta_id_estado_fkey");

        entity.HasOne(d => d.IdMedioPagoNavigation).WithMany(p => p.Venta)
            .HasForeignKey(d => d.IdMedioPago)
            .HasConstraintName("venta_id_medio_pago_fkey");

        entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Venta)
            .HasForeignKey(d => d.IdUsuario)
            .HasConstraintName("venta_id_usuario_fkey");

        entity.HasOne(d => d.IdMotivoNcNavigation).WithMany()
            .HasForeignKey(d => d.IdMotivoNc)
            .HasConstraintName("fk_venta_motivo_nc");
    }
}
