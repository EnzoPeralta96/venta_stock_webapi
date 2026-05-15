using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class ProveedorConfiguration : IEntityTypeConfiguration<global::proyecto_venta_stock.Models.Proveedor>
{
    public void Configure(EntityTypeBuilder<global::proyecto_venta_stock.Models.Proveedor> entity)
    {
        entity.HasKey(e => e.IdProveedor).HasName("proveedor_pkey");

        entity.ToTable("proveedor");

        entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
        entity.Property(e => e.Direccion)
            .HasMaxLength(200)
            .HasColumnName("direccion");
        entity.Property(e => e.Proveedor1)
            .HasMaxLength(100)
            .HasColumnName("proveedor");
        entity.Property(e => e.Telefono)
            .HasMaxLength(20)
            .HasColumnName("telefono");
        entity.Property(e => e.Activo)
            .HasColumnName("activo")
            .HasDefaultValue(true);
        entity.Property(e => e.FechaBaja)
            .HasColumnName("fecha_baja");
    }
}
