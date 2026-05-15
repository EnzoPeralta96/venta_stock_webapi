using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class UnidadMedidaConfiguration : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> entity)
    {
        entity.HasKey(e => e.IdUnidadMedida);
        entity.ToTable("unidad_medida");
        entity.Property(e => e.IdUnidadMedida)
            .UseIdentityByDefaultColumn()
            .HasIdentityOptions(startValue: 5L)
            .HasColumnName("id_unidad_medida");
        entity.Property(e => e.Nombre)
            .HasMaxLength(50)
            .HasColumnName("nombre");
        entity.Property(e => e.Abreviatura)
            .HasMaxLength(10)
            .HasColumnName("abreviatura");
        entity.Property(e => e.Activo)
            .HasColumnName("activo")
            .HasDefaultValue(true)
            .ValueGeneratedNever();

        entity.HasData(
            new UnidadMedida { IdUnidadMedida = 1, Nombre = "Unidad", Abreviatura = "u", Activo = true },
            new UnidadMedida { IdUnidadMedida = 2, Nombre = "Kilogramo", Abreviatura = "kg", Activo = true },
            new UnidadMedida { IdUnidadMedida = 3, Nombre = "Metro", Abreviatura = "m", Activo = true },
            new UnidadMedida { IdUnidadMedida = 4, Nombre = "Litro", Abreviatura = "l", Activo = true }
        );
    }
}
