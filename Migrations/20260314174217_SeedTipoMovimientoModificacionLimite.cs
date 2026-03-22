using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class SeedTipoMovimientoModificacionLimite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "tipo_movimiento",
                columns: new[] { "id_movimiento", "nombre", "accion" },
                values: new object[] { 10, "modificacion_limite", "Modificación Límite de Crédito" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tipo_movimiento",
                keyColumn: "id_movimiento",
                keyValue: 10);
        }
    }
}
