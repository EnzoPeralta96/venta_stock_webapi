using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> entity)
    {
        entity.HasKey(e => e.IdUsuario).HasName("usuario_pkey");

        entity.ToTable("usuario");

        entity.HasIndex(e => e.Usuario1, "usuario_usuario_key").IsUnique();

        entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
        entity.Property(e => e.Apellido)
            .HasMaxLength(100)
            .HasColumnName("apellido");
        entity.Property(e => e.Email)
            .HasMaxLength(100)
            .HasColumnName("email");
        entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
        entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
        entity.Property(e => e.Nombre)
            .HasMaxLength(100)
            .HasColumnName("nombre");
        entity.Property(e => e.Password)
            .HasMaxLength(50)
            .HasColumnName("password");
        entity.Property(e => e.Rol)
            .HasMaxLength(50)
            .HasColumnName("rol");
        entity.Property(e => e.Usuario1)
            .HasMaxLength(50)
            .HasColumnName("usuario");
        entity.Property(e => e.Root)
            .HasColumnName("root")
            .HasDefaultValue(false);
    }
}
