using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class FerreteriaConfiguration : IEntityTypeConfiguration<Ferreteria>
{
    public void Configure(EntityTypeBuilder<Ferreteria> entity)
    {
        entity.HasKey(e => e.IdFerreteria).HasName("empresa_pkey");

        entity.ToTable("empresa");

        entity.Property(e => e.IdFerreteria).HasColumnName("id_empresa");
        entity.Property(e => e.Nombre)
            .HasMaxLength(150)
            .HasColumnName("nombre");
        entity.Property(e => e.Direccion)
            .HasMaxLength(200)
            .HasColumnName("direccion");
        entity.Property(e => e.Telefono)
            .HasMaxLength(30)
            .HasColumnName("telefono");
        entity.Property(e => e.Email)
            .HasMaxLength(150)
            .HasColumnName("email");
        entity.Property(e => e.Cuit)
            .HasMaxLength(20)
            .HasColumnName("cuit");
        entity.Property(e => e.LogoUrl)
            .HasMaxLength(300)
            .HasColumnName("logo_url");
        entity.Property(e => e.FechaActualizacion)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("now()")
            .HasColumnName("fecha_actualizacion");
    }
}
