using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class SoporteUnidadesDecimales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "stock_minimo",
                table: "producto",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "stock",
                table: "producto",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_unidad_medida",
                table: "producto",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "stock_resultante",
                table: "movimiento_stock",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "cantidad",
                table: "movimiento_stock",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "cantidad",
                table: "detalle_venta_pendiente",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "cantidad",
                table: "detalle_venta",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "cantidad",
                table: "compra_proveedor_detalle",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "unidad_medida",
                columns: table => new
                {
                    id_unidad_medida = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    abreviatura = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidad_medida", x => x.id_unidad_medida);
                });

            migrationBuilder.InsertData(
                table: "unidad_medida",
                columns: new[] { "id_unidad_medida", "abreviatura", "nombre" },
                values: new object[,]
                {
                    { 1, "u", "Unidad" },
                    { 2, "kg", "Kilogramo" },
                    { 3, "m", "Metro" },
                    { 4, "l", "Litro" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_producto_id_unidad_medida",
                table: "producto",
                column: "id_unidad_medida");

            migrationBuilder.AddForeignKey(
                name: "producto_id_unidad_medida_fkey",
                table: "producto",
                column: "id_unidad_medida",
                principalTable: "unidad_medida",
                principalColumn: "id_unidad_medida",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "producto_id_unidad_medida_fkey",
                table: "producto");

            migrationBuilder.DropTable(
                name: "unidad_medida");

            migrationBuilder.DropIndex(
                name: "IX_producto_id_unidad_medida",
                table: "producto");

            migrationBuilder.DropColumn(
                name: "id_unidad_medida",
                table: "producto");

            migrationBuilder.AlterColumn<int>(
                name: "stock_minimo",
                table: "producto",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "stock",
                table: "producto",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "stock_resultante",
                table: "movimiento_stock",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "cantidad",
                table: "movimiento_stock",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "cantidad",
                table: "detalle_venta_pendiente",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "cantidad",
                table: "detalle_venta",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "cantidad",
                table: "compra_proveedor_detalle",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);
        }
    }
}
