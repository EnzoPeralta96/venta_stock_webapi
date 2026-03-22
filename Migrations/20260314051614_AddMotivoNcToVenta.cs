using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class AddMotivoNcToVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_motivo_nc",
                table: "venta",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_venta_id_motivo_nc",
                table: "venta",
                column: "id_motivo_nc");

            migrationBuilder.AddForeignKey(
                name: "fk_venta_motivo_nc",
                table: "venta",
                column: "id_motivo_nc",
                principalTable: "motivo_nota_credito",
                principalColumn: "id_motivo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_venta_motivo_nc",
                table: "venta");

            migrationBuilder.DropIndex(
                name: "IX_venta_id_motivo_nc",
                table: "venta");

            migrationBuilder.DropColumn(
                name: "id_motivo_nc",
                table: "venta");
        }
    }
}
