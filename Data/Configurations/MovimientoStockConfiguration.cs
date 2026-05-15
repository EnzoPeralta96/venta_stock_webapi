using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class MovimientoStockConfiguration : IEntityTypeConfiguration<MovimientoStock>
{
    public void Configure(EntityTypeBuilder<MovimientoStock> entity)
    {
        entity.HasKey(e => e.IdMovimientoStock);
        entity.ToTable("movimiento_stock");
        entity.Property(e => e.IdMovimientoStock).HasColumnName("id_movimiento_stock");
        entity.Property(e => e.IdProducto).HasColumnName("id_producto");
        entity.Property(e => e.IdTipoMovimientoStock).HasColumnName("id_tipo_movimiento_stock");
        entity.Property(e => e.Cantidad).HasPrecision(18, 3).HasColumnName("cantidad");
        entity.Property(e => e.StockResultante).HasPrecision(18, 3).HasColumnName("stock_resultante");
        entity.Property(e => e.Fecha).HasColumnType("timestamp with time zone").HasColumnName("fecha");
        entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
        entity.Property(e => e.Referencia).HasMaxLength(150).HasColumnName("referencia");

        entity.HasOne(d => d.IdProductoNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdProducto)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_movimientostock_producto");

        entity.HasOne(d => d.IdTipoMovimientoStockNavigation)
            .WithMany(p => p.MovimientosStock)
            .HasForeignKey(d => d.IdTipoMovimientoStock)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_movimientostock_tipo");

        entity.HasOne(d => d.IdUsuarioNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdUsuario)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_movimientostock_usuario");
    }
}
