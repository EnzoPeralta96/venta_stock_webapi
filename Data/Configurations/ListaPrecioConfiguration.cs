using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class ListaPrecioConfiguration : IEntityTypeConfiguration<global::proyecto_venta_stock.Models.ListaPrecio>
{
    public void Configure(EntityTypeBuilder<global::proyecto_venta_stock.Models.ListaPrecio> entity)
    {
        entity.HasKey(e => e.IdLista).HasName("listaprecio_pkey");

        entity.ToTable("lista_precio");

        entity.Property(e => e.IdLista)
            .HasDefaultValueSql("nextval('listaprecio_id_lista_seq'::regclass)")
            .HasColumnName("id_lista");
        entity.Property(e => e.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("timestamp without time zone")
            .HasColumnName("fecha_creacion");
        entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
        entity.Property(e => e.IdUsuarioRegistra).HasColumnName("id_usuario_registra");
        entity.Property(e => e.Nombre)
            .HasMaxLength(100)
            .HasColumnName("nombre");
        entity.Property(e => e.Observaciones)
            .HasMaxLength(250)
            .HasColumnName("observaciones");
        entity.Property(e => e.Activo)
            .HasDefaultValue(true)
            .HasColumnName("activo");

        entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.ListaPrecios)
            .HasForeignKey(d => d.IdProveedor)
            .HasConstraintName("listaprecio_id_proveedor_fkey");

        entity.HasOne(d => d.IdUsuarioRegistraNavigation).WithMany(p => p.ListaPrecios)
            .HasForeignKey(d => d.IdUsuarioRegistra)
            .HasConstraintName("listaprecio_id_usuario_registra_fkey");
    }
}
