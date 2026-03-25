using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class CodigoBarraConfiguration : IEntityTypeConfiguration<CodigoBarra>
{
    public void Configure(EntityTypeBuilder<CodigoBarra> entity)
    {
        entity.HasKey(e => e.IdCodigo).HasName("codigobarra_pkey");

        entity.ToTable("codigo_barra");

        entity.HasIndex(e => e.Codigo, "codigobarra_codigo_key").IsUnique();

        entity.Property(e => e.IdCodigo)
            .HasDefaultValueSql("nextval('codigobarra_id_codigo_seq'::regclass)")
            .HasColumnName("id_codigo");
        entity.Property(e => e.Activo)
            .HasDefaultValue(true)
            .HasColumnName("activo");
        entity.Property(e => e.Codigo)
            .HasMaxLength(100)
            .HasColumnName("codigo");
        entity.Property(e => e.IdProducto).HasColumnName("id_producto");
        entity.Property(e => e.Prinicial)
            .HasDefaultValue(false)
            .HasColumnName("prinicial");

        entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.CodigoBarras)
            .HasForeignKey(d => d.IdProducto)
            .HasConstraintName("codigobarra_id_producto_fkey");
    }
}
