using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_venta_stock.Migrations
{
    /// <inheritdoc />
    public partial class DropTriggersEntidadesPrincipales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar triggers de las tablas de entidades principales.
            // Estas tablas ahora usan auditoría manual en los servicios (LogAsync),
            // por lo que los triggers de PostgreSQL generaban registros duplicados.
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_audit_cliente  ON cliente;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_audit_usuario  ON usuario;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_audit_producto ON producto;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_audit_venta    ON venta;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaurar triggers (referencian fn_auditoria_generica que debe existir en la DB)
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS tr_audit_cliente ON cliente;
                CREATE TRIGGER tr_audit_cliente
                AFTER INSERT OR UPDATE OR DELETE ON cliente
                FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_cliente');
            ");

            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS tr_audit_usuario ON usuario;
                CREATE TRIGGER tr_audit_usuario
                AFTER INSERT OR UPDATE OR DELETE ON usuario
                FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_usuario');
            ");

            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS tr_audit_producto ON producto;
                CREATE TRIGGER tr_audit_producto
                AFTER INSERT OR UPDATE OR DELETE ON producto
                FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_producto');
            ");

            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS tr_audit_venta ON venta;
                CREATE TRIGGER tr_audit_venta
                AFTER INSERT OR UPDATE OR DELETE ON venta
                FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_venta');
            ");
        }
    }
}
