using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class MovimientoCcConfiguration : IEntityTypeConfiguration<MovimientoCc>
{
    public void Configure(EntityTypeBuilder<MovimientoCc> entity)
    {
        entity.HasKey(e => e.IdMovimiento).HasName("movimientocc_pkey");

        entity.ToTable("movimiento_cc");

        entity.Property(e => e.IdMovimiento)
            .HasDefaultValueSql("nextval('movimientocc_id_movimiento_seq'::regclass)")
            .HasColumnName("id_movimiento");
        entity.Property(e => e.Detalle).HasColumnName("detalle");
        entity.Property(e => e.Fecha)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("timestamp without time zone")
            .HasColumnName("fecha");
        entity.Property(e => e.FechaAutorizacion)
            .HasColumnType("timestamp without time zone")
            .HasColumnName("fecha_autorizacion");
        entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
        entity.Property(e => e.IdEstado).HasColumnName("id_estado");
        entity.Property(e => e.IdTipoMovimiento).HasColumnName("id_tipo_movimiento");
        entity.Property(e => e.IdUsuarioAutoriza).HasColumnName("id_usuario_autoriza");
        entity.Property(e => e.IdUsuarioRegistra).HasColumnName("id_usuario_registra");
        entity.Property(e => e.IdVenta).HasColumnName("id_venta");
        entity.Property(e => e.Importe)
            .HasPrecision(10, 2)
            .HasColumnName("importe");
        entity.Property(e => e.LimiteCuenta)
            .HasPrecision(10, 2)
            .HasColumnName("limite_cuenta");
        entity.Property(e => e.SaldoActual)
            .HasPrecision(10, 2)
            .HasColumnName("saldo_actual");

        entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.MovimientoCcs)
            .HasForeignKey(d => d.IdCliente)
            .HasConstraintName("movimientocc_id_cliente_fkey");

        entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.MovimientoCcs)
            .HasForeignKey(d => d.IdEstado)
            .HasConstraintName("movimientocc_id_estado_fkey");

        entity.HasOne(d => d.IdTipoMovimientoNavigation).WithMany(p => p.MovimientoCcs)
            .HasForeignKey(d => d.IdTipoMovimiento)
            .HasConstraintName("movimientocc_id_tipo_movimiento_fkey");

        entity.HasOne(d => d.IdUsuarioAutorizaNavigation).WithMany(p => p.MovimientoCcIdUsuarioAutorizaNavigations)
            .HasForeignKey(d => d.IdUsuarioAutoriza)
            .HasConstraintName("movimientocc_id_usuario_autoriza_fkey");

        entity.HasOne(d => d.IdUsuarioRegistraNavigation).WithMany(p => p.MovimientoCcIdUsuarioRegistraNavigations)
            .HasForeignKey(d => d.IdUsuarioRegistra)
            .HasConstraintName("movimientocc_id_usuario_registra_fkey");

        entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.MovimientoCcs)
            .HasForeignKey(d => d.IdVenta)
            .HasConstraintName("movimientocc_id_venta_fkey");

        entity.Property(e => e.IdMotivoNd).HasColumnName("id_motivo_nd");

        entity.HasOne(d => d.IdMotivoNdNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdMotivoNd)
            .HasConstraintName("fk_movimiento_cc_motivo_nd");

        entity.Property(e => e.IdMotivoNc).HasColumnName("id_motivo_nc");
        entity.HasOne(d => d.IdMotivoNcNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdMotivoNc)
            .HasConstraintName("fk_movimiento_cc_motivo_nc");
    }
}
