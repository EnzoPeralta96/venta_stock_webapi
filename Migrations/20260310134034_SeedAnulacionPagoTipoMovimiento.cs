using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class SeedAnulacionPagoTipoMovimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO tipo_movimiento (id_movimiento, nombre, accion) " +
                "VALUES (9, 'anulacion_pago', 'Anulación de pago registrado por error') " +
                "ON CONFLICT (id_movimiento) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM tipo_movimiento WHERE id_movimiento = 9;");
        }
    }
}
