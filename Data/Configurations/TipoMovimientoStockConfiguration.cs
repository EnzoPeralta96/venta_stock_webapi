using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Data.Configurations;

public class TipoMovimientoStockConfiguration : IEntityTypeConfiguration<TipoMovimientoStock>
{
    public void Configure(EntityTypeBuilder<TipoMovimientoStock> entity)
    {
        entity.HasKey(e => e.IdTipoMovimientoStock);
        entity.ToTable("tipo_movimiento_stock");
        entity.Property(e => e.IdTipoMovimientoStock)
            .ValueGeneratedNever()
            .HasColumnName("id_tipo_movimiento_stock");
        entity.Property(e => e.Nombre).HasMaxLength(50).HasColumnName("nombre");
        entity.Property(e => e.Descripcion).HasMaxLength(255).HasColumnName("descripcion");
        entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true).ValueGeneratedNever();
        entity.Property(e => e.EsSistema).HasColumnName("es_sistema").HasDefaultValue(false).ValueGeneratedNever();
        entity.Property(e => e.EsPositivo).HasColumnName("es_positivo").HasDefaultValue(false).ValueGeneratedNever();

        entity.HasData(
            new TipoMovimientoStock { IdTipoMovimientoStock = 1, Nombre = "Ingreso por Compra", Descripcion = "Incremento de stock por recepciÃ³n de mercaderÃ­a de proveedor.", Activo = true, EsSistema = true, EsPositivo = true },
            new TipoMovimientoStock { IdTipoMovimientoStock = 2, Nombre = "Egreso por Venta", Descripcion = "ReducciÃ³n de stock por venta a cliente.", Activo = true, EsSistema = true, EsPositivo = false },
            new TipoMovimientoStock { IdTipoMovimientoStock = 3, Nombre = "Reingreso por AnulaciÃ³n de Venta", Descripcion = "RestituciÃ³n de stock al anular una venta.", Activo = true, EsSistema = true, EsPositivo = true },
            new TipoMovimientoStock { IdTipoMovimientoStock = 4, Nombre = "Egreso por AnulaciÃ³n de Compra", Descripcion = "ReducciÃ³n de stock al anular o revertir una compra a proveedor.", Activo = true, EsSistema = true, EsPositivo = false },
            new TipoMovimientoStock { IdTipoMovimientoStock = 5, Nombre = "Ajuste Positivo Manual", Descripcion = "Incremento manual de stock (ej: sobrante en inventario fÃ­sico).", Activo = true, EsSistema = false, EsPositivo = true },
            new TipoMovimientoStock { IdTipoMovimientoStock = 6, Nombre = "Ajuste Negativo Manual", Descripcion = "ReducciÃ³n manual de stock (ej: faltante en inventario fÃ­sico).", Activo = true, EsSistema = false, EsPositivo = false },
            new TipoMovimientoStock { IdTipoMovimientoStock = 7, Nombre = "Consumo Interno DueÃ±o", Descripcion = "Retiro de mercaderÃ­a para uso interno o personal del dueÃ±o.", Activo = true, EsSistema = false, EsPositivo = false }
        );
    }
}
