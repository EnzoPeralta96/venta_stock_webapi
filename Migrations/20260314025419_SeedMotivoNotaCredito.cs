using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class SeedMotivoNotaCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        }
    }
}
