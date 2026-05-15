using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class PermisoUsuarioConfiguration : IEntityTypeConfiguration<PermisoUsuario>
{
    public void Configure(EntityTypeBuilder<PermisoUsuario> entity)
    {
        entity.HasKey(e => new { e.IdPermiso, e.IdUsuario }).HasName("permisousuario_pkey");

        entity.ToTable("permiso_usuario");

        entity.Property(e => e.IdPermiso).HasColumnName("id_permiso");
        entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
        entity.Property(e => e.FechaAsignacion).HasColumnName("fecha_asignacion");

        entity.HasOne(d => d.IdPermisoNavigation).WithMany(p => p.PermisoUsuarios)
            .HasForeignKey(d => d.IdPermiso)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("permisousuario_id_permiso_fkey");

        entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.PermisoUsuarios)
            .HasForeignKey(d => d.IdUsuario)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("permisousuario_id_usuario_fkey");
    }
}
