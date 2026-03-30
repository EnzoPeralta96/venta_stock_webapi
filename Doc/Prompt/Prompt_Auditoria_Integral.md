# Prompt Claude Code: Auditoría Integral por Triggers

```md
Hola Claude. Actúa como un Arquitecto de Software Senior y Experto en DBA (PostgreSQL) y ASP.NET Core 8.

Tenemos un sistema de gestión maduro (Ferretería) con un mecanismo robusto de auditoría a nivel base de datos.
El backend usa Entity Framework, pero se apoya en Triggers de PostgreSQL (`fn_auditoria_generica` en la DB) que interceptan todo evento DML (INSERT, UPDATE, DELETE). Estos Triggers se alimentan de una sesión de DB donde C# le inyecta previamente el `app.user_id` usando un método como `SetAuditContextAsync`.

**NUESTRO OBJETIVO:**
Extender este mecanismo de auditoría automático a las siguientes entidades comerciales críticas y de Datos Maestros:

**Núcleo Comercial:**
1. Proveedores (`proveedor`)
2. Compras a Proveedor (`compra_proveedor`)
3. Listas de Precio (`lista_precio`)
4. Cuenta Corriente (`movimiento_cc`)
5. Ventas Pendientes de Autorización (`venta_pendiente` - para el límite de crédito).

**Datos Maestros y Configuración (Bajo volumen, Alta sensibilidad):**
6. Productos Extras: `categoria`, `ubicacion`, `tipo_movimiento`, `unidad_medida`.
7. Datos de la Empresa: `ferreteria`.
8. Configuración CC: `configuracion_cc`, `motivo_nota_debito`, `motivo_nota_credito`, `configuracion_interes`.

No se requiere auditar lecturas ni exportaciones (solo DML), ya que sobrecargarían la DB innecesariamente.

**ESTRATEGIA DE EJECUCIÓN:**
Orquesta la implementación en un solo bloque continuo, revisando los archivos C# y SQL pertinentes. 

### PASOS OBLIGATORIOS:

1. **Modificación del Script SQL (`../DbScript/trigger_auditoria.sql`)**:
   - Agrega los `CREATE TRIGGER` llamando a `fn_auditoria_generica('columna_PK')` para TODAS las tablas mencionadas arriba (las 5 del núcleo y las 9 de configuración). Luego yo los ejecutare manualmente en la db para que se creen.
   - Confirma los nombres exactos de las tablas y sus Primary Keys leyendo los Modelos `.cs` si dudas del mapeo ef_core.

2. **Inyección C# del Contexto de Usuario (`app.user_id`)**:
   - Revisa rápidamente los Servicios (`Services/`) vinculados a estas entidades.
   - Asegúrate de que **CADA operacion que llame a `_context.SaveChangesAsync()`** para crear, actualizar o eliminar en estas entidades, invoque PREVIAMENTE el seteo del contexto de usuario transaccional. 
   - Debería haber un método en el repositorio similar a `await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('app.user_id', {_userContext.UserId.ToString()}, true);");`.
   - Si un trigger salta pero el servicio no seteó el `UserId`, arrojará la excepción SQL: *"Auditoría: falta app.user_id"*. Asegúrate de que esto no pase en las ABM de configuración de Ferretería, Categorías, CC, etc.

**Instrucción Final**:
Agrega la batería completa de triggers al archivo SQL e inyecta la configuración del UserId en los servicios C# correspondientes que todavía no lo tengan. Confirma que el proyecto compile con `dotnet build` al finalizar tu gestión y documenta los cambios que aplicaste. ¡Buena suerte, maestro de los Triggers!
```
