using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> entity)
    {
        entity.HasKey(e => e.IdCliente).HasName("cliente_pkey");

        entity.ToTable("cliente");

        entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
        entity.Property(e => e.Apellido)
            .HasMaxLength(100)
            .HasColumnName("apellido");
        entity.Property(e => e.Cuit)
            .HasMaxLength(20)
            .HasColumnName("cuit");
        entity.Property(e => e.Dni)
            .HasMaxLength(20)
            .HasColumnName("dni");
        entity.Property(e => e.Mail)
            .HasMaxLength(100)
            .HasColumnName("mail");
        entity.Property(e => e.Nombre)
            .HasMaxLength(100)
            .HasColumnName("nombre");
        entity.Property(e => e.RazonSocial)
            .HasMaxLength(150)
            .HasColumnName("razon_social");
        entity.Property(e => e.Telefono)
            .HasMaxLength(20)
            .HasColumnName("telefono");
        entity.Property(e => e.FechaAlta)
            .HasColumnName("fecha_alta");
        entity.Property(e => e.FechaBaja)
            .HasColumnName("fecha_baja");
    }
}
