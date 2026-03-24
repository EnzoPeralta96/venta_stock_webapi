using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class AddStockLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tipo_movimiento_stock",
                columns: table => new
                {
                    id_tipo_movimiento_stock = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_movimiento_stock", x => x.id_tipo_movimiento_stock);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_stock",
                columns: table => new
                {
                    id_movimiento_stock = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_producto = table.Column<int>(type: "integer", nullable: false),
                    id_tipo_movimiento_stock = table.Column<int>(type: "integer", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    stock_resultante = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id_usuario = table.Column<int>(type: "integer", nullable: true),
                    referencia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_stock", x => x.id_movimiento_stock);
                    table.ForeignKey(
                        name: "fk_movimientostock_producto",
                        column: x => x.id_producto,
                        principalTable: "producto",
                        principalColumn: "id_producto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimientostock_tipo",
                        column: x => x.id_tipo_movimiento_stock,
                        principalTable: "tipo_movimiento_stock",
                        principalColumn: "id_tipo_movimiento_stock",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimientostock_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "tipo_movimiento_stock",
                columns: new[] { "id_tipo_movimiento_stock", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "Incremento de stock por recepción de mercadería de proveedor.", "Ingreso por Compra" },
                    { 2, "Reducción de stock por venta a cliente.", "Egreso por Venta" },
                    { 3, "Restitución de stock al anular una venta.", "Reingreso por Anulación de Venta" },
                    { 4, "Reducción de stock al anular o revertir una compra a proveedor.", "Egreso por Anulación de Compra" },
                    { 5, "Incremento manual de stock (ej: sobrante en inventario físico).", "Ajuste Positivo Manual" },
                    { 6, "Reducción manual de stock (ej: faltante en inventario físico).", "Ajuste Negativo Manual" },
                    { 7, "Retiro de mercadería para uso interno o personal del dueño.", "Consumo Interno Dueño" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_stock_id_producto",
                table: "movimiento_stock",
                column: "id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_stock_id_tipo_movimiento_stock",
                table: "movimiento_stock",
                column: "id_tipo_movimiento_stock");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_stock_id_usuario",
                table: "movimiento_stock",
                column: "id_usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimiento_stock");

            migrationBuilder.DropTable(
                name: "tipo_movimiento_stock");
        }
    }
}
