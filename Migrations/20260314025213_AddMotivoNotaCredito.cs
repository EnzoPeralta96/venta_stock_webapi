using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class AddMotivoNotaCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_motivo_nc",
                table: "movimiento_cc",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "motivo_nota_credito",
                columns: table => new
                {
                    id_motivo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motivo_nota_credito", x => x.id_motivo);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_cc_id_motivo_nc",
                table: "movimiento_cc",
                column: "id_motivo_nc");

            migrationBuilder.AddForeignKey(
                name: "fk_movimiento_cc_motivo_nc",
                table: "movimiento_cc",
                column: "id_motivo_nc",
                principalTable: "motivo_nota_credito",
                principalColumn: "id_motivo");

            migrationBuilder.Sql(@"
                INSERT INTO motivo_nota_credito (nombre, activo)
                VALUES
                    ('Devolución de producto', true),
                    ('Error en la venta', true),
                    ('Producto defectuoso', true);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_movimiento_cc_motivo_nc",
                table: "movimiento_cc");

            migrationBuilder.DropTable(
                name: "motivo_nota_credito");

            migrationBuilder.DropIndex(
                name: "IX_movimiento_cc_id_motivo_nc",
                table: "movimiento_cc");

            migrationBuilder.DropColumn(
                name: "id_motivo_nc",
                table: "movimiento_cc");
        }
    }
}
