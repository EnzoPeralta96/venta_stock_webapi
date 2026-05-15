using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class AddUnidadMedidaActivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "unidad_medida",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "unidad_medida",
                keyColumn: "id_unidad_medida",
                keyValue: 1,
                column: "activo",
                value: true);

            migrationBuilder.UpdateData(
                table: "unidad_medida",
                keyColumn: "id_unidad_medida",
                keyValue: 2,
                column: "activo",
                value: true);

            migrationBuilder.UpdateData(
                table: "unidad_medida",
                keyColumn: "id_unidad_medida",
                keyValue: 3,
                column: "activo",
                value: true);

            migrationBuilder.UpdateData(
                table: "unidad_medida",
                keyColumn: "id_unidad_medida",
                keyValue: 4,
                column: "activo",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "activo",
                table: "unidad_medida");
        }
    }
}
