using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class VentaPendienteConfiguration : IEntityTypeConfiguration<VentaPendiente>
{
    public void Configure(EntityTypeBuilder<VentaPendiente> entity)
    {
        entity.HasKey(e => e.IdVentaPendiente).HasName("venta_pendiente_pkey");

        entity.ToTable("venta_pendiente", tb => tb.HasComment("Ventas que exceden el lÃ­mite de crÃ©dito y requieren autorizaciÃ³n"));

        entity.HasIndex(e => e.IdCliente, "idx_venta_pendiente_cliente");
        entity.HasIndex(e => e.IdEstado, "idx_venta_pendiente_estado");
        entity.HasIndex(e => e.FechaRegistro, "idx_venta_pendiente_fecha").IsDescending();
        entity.HasIndex(e => e.IdUsuarioVendedor, "idx_venta_pendiente_vendedor");
        entity.HasIndex(e => e.CodigoVenta, "venta_pendiente_codigo_venta_key").IsUnique();

        entity.Property(e => e.IdVentaPendiente).HasColumnName("id_venta_pendiente");
        entity.Property(e => e.CodigoVenta)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("codigo_venta");
        entity.Property(e => e.Excedente)
            .HasPrecision(18, 2)
            .HasComment("Monto que excede el lÃ­mite de crÃ©dito")
            .HasColumnName("excedente");
        entity.Property(e => e.FechaAutorizacion)
            .HasColumnType("timestamp without time zone")
            .HasColumnName("fecha_autorizacion");
        entity.Property(e => e.FechaRegistro)
            .HasDefaultValueSql("now()")
            .HasColumnType("timestamp without time zone")
            .HasColumnName("fecha_registro");
        entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
        entity.Property(e => e.IdEstado).HasColumnName("id_estado");
        entity.Property(e => e.IdMedioPago).HasColumnName("id_medio_pago");
        entity.Property(e => e.IdUsuarioAutoriza).HasColumnName("id_usuario_autoriza");
        entity.Property(e => e.IdUsuarioVendedor).HasColumnName("id_usuario_vendedor");
        entity.Property(e => e.IdVentaGenerada)
            .HasComment("Referencia a la venta definitiva si fue aprobada")
            .HasColumnName("id_venta_generada");
        entity.Property(e => e.LimiteCuenta)
            .HasPrecision(18, 2)
            .HasColumnName("limite_cuenta");
        entity.Property(e => e.ObservacionesAutorizacion).HasColumnName("observaciones_autorizacion");
        entity.Property(e => e.SaldoActual)
            .HasPrecision(18, 2)
            .HasColumnName("saldo_actual");
        entity.Property(e => e.SaldoDespuesVenta)
            .HasPrecision(18, 2)
            .HasColumnName("saldo_despues_venta");
        entity.Property(e => e.Total)
            .HasPrecision(18, 2)
            .HasColumnName("total");

        entity.HasOne(d => d.IdClienteNavigation).WithMany()
            .HasForeignKey(d => d.IdCliente)
            .HasConstraintName("venta_pendiente_id_cliente_fkey");

        entity.HasOne(d => d.IdEstadoNavigation).WithMany()
            .HasForeignKey(d => d.IdEstado)
            .HasConstraintName("venta_pendiente_id_estado_fkey");

        entity.HasOne(d => d.IdMedioPagoNavigation).WithMany()
            .HasForeignKey(d => d.IdMedioPago)
            .HasConstraintName("venta_pendiente_id_medio_pago_fkey");

        entity.HasOne(d => d.IdUsuarioAutorizaNavigation).WithMany()
            .HasForeignKey(d => d.IdUsuarioAutoriza)
            .HasConstraintName("venta_pendiente_id_usuario_autoriza_fkey");

        entity.HasOne(d => d.IdUsuarioVendedorNavigation).WithMany()
            .HasForeignKey(d => d.IdUsuarioVendedor)
            .HasConstraintName("venta_pendiente_id_usuario_vendedor_fkey");

        entity.HasOne(d => d.IdVentaGeneradaNavigation).WithMany()
            .HasForeignKey(d => d.IdVentaGenerada)
            .HasConstraintName("venta_pendiente_id_venta_generada_fkey");
    }
}
