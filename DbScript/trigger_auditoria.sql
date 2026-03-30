-- =============================================================================
-- TRIGGERS DE AUDITORÍA INTEGRAL
-- Sistema: Ferretería - venta_stock_webapi
-- Función base: fn_auditoria_generica(pk_column_name)
-- Requiere: app.user_id seteado en la sesión de PostgreSQL desde el backend.
-- =============================================================================

-- =============================================================================
-- NÚCLEO COMERCIAL
-- =============================================================================

-- Proveedores
DROP TRIGGER IF EXISTS tr_audit_proveedor ON proveedor;
CREATE TRIGGER tr_audit_proveedor
AFTER INSERT OR UPDATE OR DELETE ON proveedor
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_proveedor');

-- Compras a Proveedor
DROP TRIGGER IF EXISTS tr_audit_compra_proveedor ON compra_proveedor;
CREATE TRIGGER tr_audit_compra_proveedor
AFTER INSERT OR UPDATE OR DELETE ON compra_proveedor
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_compra_proveedor');

-- Listas de Precio (cabecera)
DROP TRIGGER IF EXISTS tr_audit_lista_precio ON lista_precio;
CREATE TRIGGER tr_audit_lista_precio
AFTER INSERT OR UPDATE OR DELETE ON lista_precio
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_lista');

-- Movimientos de Cuenta Corriente
DROP TRIGGER IF EXISTS tr_audit_movimiento_cc ON movimiento_cc;
CREATE TRIGGER tr_audit_movimiento_cc
AFTER INSERT OR UPDATE OR DELETE ON movimiento_cc
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_movimiento');

-- Ventas Pendientes de Autorización
DROP TRIGGER IF EXISTS tr_audit_venta_pendiente ON venta_pendiente;
CREATE TRIGGER tr_audit_venta_pendiente
AFTER INSERT OR UPDATE OR DELETE ON venta_pendiente
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_venta_pendiente');

-- =============================================================================
-- DATOS MAESTROS — PRODUCTOS Y STOCK
-- =============================================================================

-- Categorías de producto
DROP TRIGGER IF EXISTS tr_audit_categoria ON categoria;
CREATE TRIGGER tr_audit_categoria
AFTER INSERT OR UPDATE OR DELETE ON categoria
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_categoria');

-- Ubicaciones de almacén
DROP TRIGGER IF EXISTS tr_audit_ubicacion ON ubicacion;
CREATE TRIGGER tr_audit_ubicacion
AFTER INSERT OR UPDATE OR DELETE ON ubicacion
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_ubicacion');

-- Tipos de movimiento de stock (ajuste manual, ingreso compra, etc.)
DROP TRIGGER IF EXISTS tr_audit_tipo_movimiento_stock ON tipo_movimiento_stock;
CREATE TRIGGER tr_audit_tipo_movimiento_stock
AFTER INSERT OR UPDATE OR DELETE ON tipo_movimiento_stock
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_tipo_movimiento_stock');

-- Tipos de movimiento de cuenta corriente
DROP TRIGGER IF EXISTS tr_audit_tipo_movimiento ON tipo_movimiento;
CREATE TRIGGER tr_audit_tipo_movimiento
AFTER INSERT OR UPDATE OR DELETE ON tipo_movimiento
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_movimiento');

-- Unidades de medida
DROP TRIGGER IF EXISTS tr_audit_unidad_medida ON unidad_medida;
CREATE TRIGGER tr_audit_unidad_medida
AFTER INSERT OR UPDATE OR DELETE ON unidad_medida
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_unidad_medida');

-- =============================================================================
-- DATOS DE LA EMPRESA
-- =============================================================================

-- Datos de la ferretería (tabla: empresa)
DROP TRIGGER IF EXISTS tr_audit_empresa ON empresa;
CREATE TRIGGER tr_audit_empresa
AFTER INSERT OR UPDATE OR DELETE ON empresa
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_empresa');

-- =============================================================================
-- CONFIGURACIÓN DE CUENTA CORRIENTE
-- =============================================================================

-- Configuración de límites de crédito
DROP TRIGGER IF EXISTS tr_audit_configuracion_cc ON configuracion_cc;
CREATE TRIGGER tr_audit_configuracion_cc
AFTER INSERT OR UPDATE OR DELETE ON configuracion_cc
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_config');

-- Motivos de nota de débito
DROP TRIGGER IF EXISTS tr_audit_motivo_nota_debito ON motivo_nota_debito;
CREATE TRIGGER tr_audit_motivo_nota_debito
AFTER INSERT OR UPDATE OR DELETE ON motivo_nota_debito
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_motivo');

-- Motivos de nota de crédito
DROP TRIGGER IF EXISTS tr_audit_motivo_nota_credito ON motivo_nota_credito;
CREATE TRIGGER tr_audit_motivo_nota_credito
AFTER INSERT OR UPDATE OR DELETE ON motivo_nota_credito
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_motivo');

-- Configuración de intereses
DROP TRIGGER IF EXISTS tr_audit_configuracion_interes ON configuracion_interes;
CREATE TRIGGER tr_audit_configuracion_interes
AFTER INSERT OR UPDATE OR DELETE ON configuracion_interes
FOR EACH ROW EXECUTE FUNCTION fn_auditoria_generica('id_config');