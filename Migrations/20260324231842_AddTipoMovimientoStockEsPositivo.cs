using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoMovimientoStockEsPositivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "es_positivo",
                table: "tipo_movimiento_stock",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 1,
                column: "es_positivo",
                value: true);

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 2,
                column: "es_positivo",
                value: false);

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 3,
                column: "es_positivo",
                value: true);

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 4,
                column: "es_positivo",
                value: false);

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 5,
                column: "es_positivo",
                value: true);

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 6,
                column: "es_positivo",
                value: false);

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 7,
                column: "es_positivo",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "es_positivo",
                table: "tipo_movimiento_stock");
        }
    }
}
