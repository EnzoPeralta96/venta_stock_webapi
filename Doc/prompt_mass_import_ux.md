# Proyecto Venta Stock Backend - Refactor UX "Carga Masiva"

Hola Claude. Como el refactor de las "Unidades de Medida" a Decimales ya fue un éxito, nuestro próximo objetivo es elevar exponencialmente la Calidad de Usuario (UX) y el Poder Lógico de la funcionalidad de **Carga Masiva de Productos** (`POST /Product/importar`).

Actualmente, el sistema acepta CSV y Excel, pero las plantillas exportadas están vacías de contexto. Además, necesitamos potenciar el motor lógico del importador para que el Excel sirva no solo para altas masivas o cambio de precios, sino como una herramienta de **Auditoría de Inventario Físico (Sincronización de Stock Masiva)** utilizando el nuevo Ledger de Movimientos.

## Objetivos Arquitectónicos a Lograr

### 1. Extender el DTO y el Parser
- En `ProductImportRowDTO` agrega la propiedad opcional `public int? IdUnidadMedida { get; set; }`.
- En el método `ParseExcel` y `ParseCsv`, asegúrate de leer una posible columna extra para el `IdUnidadMedida`.
- En el INSERT, si el objeto trae `IdUnidadMedida`, úsalo; si es `null`, asume `1` ("Unidad").

### 2. El Refactor de Sincronización de Stock (EL PUNTO CRÍTICO)
Modifica la forma en que se procesa el campo opcional `Stock` (`decimal?`) que viene en cada fila del Excel. El Excel ahora se usará como validador de stock (Ledger). Sigue EXACTAMENTE esta lógica de negocio:

**A. Para Productos NUEVOS (`existing == null`):**
- Si la columna `Stock` viene vacía/nula: Crea el producto con `Stock = 0` o `null`.
- Si la columna `Stock` tiene un valor `X > 0`: Crea el producto y dispara inmediatamente un llamado a `IStockMovementService.RegistrarMovimientoAsync` de tipo `AjustePositivoManual` por la cantidad `X`. (Motivo: "ALTA POR CARGA MASIVA").

**B. Para Productos EXISTENTES (`existing != null`):**
- Si la columna `Stock` viene vacía/nula: Ignorar por completo el stock. Solo hacer el UPDATE de Precio, Marca, Nombre, etc. Esto preserva el ledger cotidiano.
- Si la columna `Stock` tiene un valor numérico (`X`), el usuario está intentando sincronizar/auditar el inventario físico mediante el Excel. Compara contra el stock actual (`Y`) de la BD:
  - Si `X == Y`: No hacer nada con el stock.
  - Si `X > Y`: Disparar un `AjustePositivoManual` por la diferencia (`Diferencia = X - Y`). Motivo: "SINCRONIZACIÓN DE STOCK POR CARGA MASIVA".
  - Si `X < Y`: Disparar un `AjusteNegativoManual` por la diferencia (`Diferencia = Y - X`). Motivo: "SINCRONIZACIÓN DE STOCK POR CARGA MASIVA".

### 3. ¡La Plantilla Premium! (Multipestaña)
En el método `ExportarPlantillaExcel()`, usando **EPPlus**, pasa de exportar 1 hoja vacía a exportar 4 hojas (Worksheets):
- **Hoja 1 ("Plantilla"):** La grilla lista (Código, Nombre, Marca, Precio, IdCategoria, IdUbicacion, Stock, IdUnidadMedida).
- **Hoja 2 ("Diccionario_Categorias"):** Haz una consulta rápida al `ICategoryRepository` y lista TODOS los registros. Columna A: `IdCategoria`, Columna B: `Nombre`.
- **Hoja 3 ("Diccionario_Ubicaciones"):** Haz una consulta rápida al `ILocationRepository` y lista. Columna A: `IdUbicacion`, Columna B: `Detalle (Fila X - Seccion Y)`. 
- **Hoja 4 ("Diccionario_Unidades"):** Enuméralas estáticamente o desde la BD. Columna A: `IdUnidadMedida`, Columna B: `Abreviatura/Nombre`. (1=Unidad, 2=Kilos, etc).

### 4. Mantener la Compatilibidad y Testing
Al modificar el lector `ParseExcel`, revisa que `ws.Dimension.Columns` se valide correctamente para que, si alguien sube la plantilla vieja (de 6 columnas de antes), tu código no explote buscando la columna del Stock o la Unidad. Usa `TryCatch` a prueba de balas.

¡Con esto, nuestra carga masiva pasará a ser una verdadera herramienta de Auditoría General de Inventarios (Full Count Inventory Upload)!
