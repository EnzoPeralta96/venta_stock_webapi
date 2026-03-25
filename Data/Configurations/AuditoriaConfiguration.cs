using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class AuditoriaConfiguration : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> entity)
    {
        entity.ToTable("auditoria");

        entity.HasKey(e => e.IdAuditoria)
            .HasName("auditoria_pkey");

        entity.Property(e => e.IdAuditoria)
            .HasColumnName("id_auditoria");

        entity.Property(e => e.FechaHora)
            .HasColumnName("fecha_hora")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        entity.Property(e => e.IdUsuario)
            .HasColumnName("id_usuario");

        entity.Property(e => e.UsuarioNombre)
            .HasColumnName("usuario_nombre")
            .HasMaxLength(100);

        entity.Property(e => e.Accion)
            .HasColumnName("accion")
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(e => e.EntidadTipo)
            .HasColumnName("entidad_tipo")
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(e => e.EntidadId)
            .HasColumnName("entidad_id");

        entity.Property(e => e.Detalle)
            .HasColumnName("detalle");

        entity.Property(e => e.ValoresAnteriores)
            .HasColumnName("valores_anteriores")
            .HasColumnType("jsonb");

        entity.Property(e => e.ValoresNuevos)
            .HasColumnName("valores_nuevos")
            .HasColumnType("jsonb");
    }
}
