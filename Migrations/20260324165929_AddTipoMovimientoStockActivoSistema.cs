using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoMovimientoStockActivoSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "tipo_movimiento_stock",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "es_sistema",
                table: "tipo_movimiento_stock",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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
                columns: new[] { "activo", "es_sistema", "es_positivo" },
                values: new object[] { true, true, true });

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 2,
                columns: new[] { "activo", "es_sistema", "es_positivo" },
                values: new object[] { true, true, false });

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 3,
                columns: new[] { "activo", "es_sistema", "es_positivo" },
                values: new object[] { true, true, true });

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 4,
                columns: new[] { "activo", "es_sistema", "es_positivo" },
                values: new object[] { true, true, false });

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 5,
                columns: new[] { "activo", "es_sistema", "es_positivo" },
                values: new object[] { true, false, true });

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 6,
                columns: new[] { "activo", "es_sistema", "es_positivo" },
                values: new object[] { true, false, false });

            migrationBuilder.UpdateData(
                table: "tipo_movimiento_stock",
                keyColumn: "id_tipo_movimiento_stock",
                keyValue: 7,
                columns: new[] { "activo", "es_sistema", "es_positivo" },
                values: new object[] { true, false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "activo",
                table: "tipo_movimiento_stock");

            migrationBuilder.DropColumn(
                name: "es_sistema",
                table: "tipo_movimiento_stock");

            migrationBuilder.DropColumn(
                name: "es_positivo",
                table: "tipo_movimiento_stock");
        }
    }
}
