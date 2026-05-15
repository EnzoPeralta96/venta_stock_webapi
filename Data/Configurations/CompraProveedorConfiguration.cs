using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class CompraProveedorConfiguration : IEntityTypeConfiguration<global::proyecto_venta_stock.Models.CompraProveedor>
{
    public void Configure(EntityTypeBuilder<global::proyecto_venta_stock.Models.CompraProveedor> entity)
    {
        entity.HasKey(e => e.IdCompraProveedor).HasName("compra_proveedor_pkey");

        entity.ToTable("compra_proveedor");

        entity.Property(e => e.IdCompraProveedor).HasColumnName("id_compra_proveedor").ValueGeneratedOnAdd();
        entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
        entity.Property(e => e.Fecha).HasColumnName("fecha");
        entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
        entity.Property(e => e.TipoComprobante).HasMaxLength(50).HasColumnName("tipo_comprobante");
        entity.Property(e => e.NumeroComprobante).HasMaxLength(50).HasColumnName("numero_comprobante");
        entity.Property(e => e.Observacion).HasMaxLength(500).HasColumnName("observacion");
        entity.Property(e => e.Subtotal).HasPrecision(10, 2).HasColumnName("subtotal");
        entity.Property(e => e.DescuentoTotal).HasPrecision(10, 2).HasColumnName("descuento_total");
        entity.Property(e => e.IvaTotal).HasPrecision(10, 2).HasColumnName("iva_total");
        entity.Property(e => e.Total).HasPrecision(10, 2).HasColumnName("total");
        entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
        entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);

        entity.HasOne(d => d.IdProveedorNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdProveedor)
            .HasConstraintName("compra_proveedor_id_proveedor_fkey");

        entity.HasOne(d => d.IdUsuarioNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdUsuario)
            .HasConstraintName("compra_proveedor_id_usuario_fkey");
    }
}
