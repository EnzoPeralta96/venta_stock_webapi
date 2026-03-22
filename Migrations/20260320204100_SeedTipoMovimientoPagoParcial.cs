using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class SeedTipoMovimientoPagoParcial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "tipo_movimiento",
                columns: new[] { "id_movimiento", "nombre", "accion" },
                values: new object[] { 11, "pago_parcial", "Pago Parcial" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tipo_movimiento",
                keyColumn: "id_movimiento",
                keyValue: 11);
        }
    }
}
