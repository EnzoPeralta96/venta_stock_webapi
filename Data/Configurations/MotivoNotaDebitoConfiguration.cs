using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class MotivoNotaDebitoConfiguration : IEntityTypeConfiguration<MotivoNotaDebito>
{
    public void Configure(EntityTypeBuilder<MotivoNotaDebito> entity)
    {
        entity.HasKey(e => e.IdMotivo);
        entity.ToTable("motivo_nota_debito");
        entity.Property(e => e.IdMotivo)
            .HasColumnName("id_motivo")
            .ValueGeneratedOnAdd();
        entity.Property(e => e.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();
        entity.Property(e => e.Activo)
            .HasColumnName("activo")
            .HasDefaultValue(true);
        entity.Property(e => e.Categoria)
            .HasColumnName("categoria")
            .HasMaxLength(50)
            .HasDefaultValue("general")
            .IsRequired();
    }
}
