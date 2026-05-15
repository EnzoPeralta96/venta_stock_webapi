using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class RestoreBarcodUniqueConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar duplicados conservando el registro con id más bajo
            migrationBuilder.Sql(@"
                DELETE FROM codigo_barra
                WHERE id_codigo NOT IN (
                    SELECT MIN(id_codigo)
                    FROM codigo_barra
                    GROUP BY codigo
                );
            ");

            migrationBuilder.CreateIndex(
                name: "codigobarra_codigo_key",
                table: "codigo_barra",
                column: "codigo",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "codigobarra_codigo_key",
                table: "codigo_barra");
        }
    }
}
