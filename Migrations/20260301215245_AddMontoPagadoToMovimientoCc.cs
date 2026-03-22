using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class AddMontoPagadoToMovimientoCc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoPagado",
                table: "movimiento_cc",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoPagado",
                table: "movimiento_cc");
        }
    }
}
