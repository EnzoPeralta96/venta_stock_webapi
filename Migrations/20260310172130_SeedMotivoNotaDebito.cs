using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class SeedMotivoNotaDebito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO motivo_nota_debito (nombre, activo) " +
                "VALUES ('Interés por mora', true), ('Ajuste de precio', true);"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
