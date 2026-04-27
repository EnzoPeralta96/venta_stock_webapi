# Spec Refactor Arquitectónico: Gestión Dinámica y Masiva de Precios

> **Atención Claude Code:** Este documento contiene las directrices estratégicas de un refactor en la arquitectura de actualización de precios. Lee detenidamente el análisis antes de proceder.

---

## 1. Análisis Profundo y Justificación del Refactor

Hemos realizado un diseño arquitectónico cruzando la usabilidad en la vida real de una ferretería con los requisitos formales del proyecto universitario (`enunciado.txt`).

### A. La Coexistencia Pacífica de las "Listas de Precios"
Actualmente el sistema posee un módulo para "Listas de Precios de Proveedores" (`ListaPrecio`). 
- **Decisión de Diseño:** Conservaremos este módulo intacto, pero su rol pasa a ser el de un **Catálogo Histórico Pasivo**. Ya no es el medio principal para actualizar precios, sino una base de datos de cotizaciones B2B. A futuro, esto permitirá desarrollar inteligencia de negocios (Ej: comparar qué proveedor tiene el costo más bajo para el Producto X). **NO elimines las entidades, ni los controllers ni las pantallas de Lista de Precios.**

### B. El Requerimiento Universitario de Automatización (Excel)
En las líneas 50-51 del `enunciado.txt`, se exige:
> *"Los proveedores suelen enviar listas de precios de los artículos. Necesitamos que el sistema pueda tomar ese listado y se realice en forma automática los precios para ahorrar tiempo."*
- **Decisión:** Para cumplir esto con la mejor UX, construiremos un **Importador Excel Directo** dentro del módulo de Productos. Dado que cada proveedor usa formatos aleatorios, el sistema le proveerá una **Plantilla Excel Estándar** al usuario. El ferretero pegará los datos crudos en la plantilla y la subirá. El Backend recalculará y grabará los nuevos precios de góndola de forma masiva.

### C. La Migración de la Actualización Masiva hacia "Productos"
Remarcar precios por inflación o estrategia comercial es una tarea intrínseca del catálogo del negocio, no de un proveedor específico.
- **Decisión:** La funcionalidad "Actualización Masiva de Precios" se desarrollará como la herramienta principal dentro del módulo **Productos**.

### D. El Requerimiento Universitario de Auditoría
En las líneas 47-49 del enunciado se exige registrar *"quién cambió el precio [...] con nombre, fecha y hora"*.
- **Decisión:** El sistema ya posee un módulo genérico de `Auditoria` funcionando con interceptores EF Core. Al construir los nuevos endpoints de actualización, simplemente debes invocar `SaveChangesAsync()` para que el trigger de auditoría atrape automáticamente al usuario y el cambio del producto. ¡No crees tablas nuevas de historial!

---

## 2. Plan de Implementación (Instrucciones Directas para Claude)

Debes proveer al ferretero de una herramienta hiper-veloz para remarcar precios, ubicada en el módulo de Productos. Mantén la arquitectura modular existente del sistema.

### A. Capa de Backend (`Features/Product/...`)
Crea un nuevo controlador `ProductUpdateController.cs` (o extiende el existente) con dos endpoints clave:
1. `POST /Producto/actualizar-masivo/manual`:
   - Recibe: `List<{ IdProducto, CostoNeto, Iva (opcional), Margen (opcional) }>`
   - Lógica: Itera los DTOs, recalcula `producto.Costo = CostoNeto * (1 + Iva/100)`, recalcula `producto.Precio` usando el Margen. Invoca `SaveChangesAsync()` de forma atómica para registrar en la **Auditoría**.
2. `POST /Producto/actualizar-masivo/excel`:
   - Recibe: `IFormFile excel` (y un parámetro de IVA por defecto, ej: 21%).
   - Lógica: Lee el `.xlsx`. Espera encontrar las columnas `CodigoBarra` y `CostoNeto`. Busca el producto, aplica el recálculo (Neto -> Costo -> Precio final), y realiza el SaveChanges masivo.
3. `GET /Producto/actualizar-masivo/excel-template`:
   - Endpoint utilitario que devuelva un archivo `.xlsx` vacío con las cabeceras obligatorias requeridas por el endpoint de subida.

### B. Capa de Frontend (`src/components/Productos/...`)
Construye una nueva pantalla, por ejemplo `ActualizacionPreciosPage.jsx`, accesible desde el menú principal de Productos.
Esta pantalla debe tener una interfaz con **dos pestañas (Tabs)**:
1. **Modo Manual (Grilla Reactiva):**
   - Buscador de productos para agregarlos a una grilla temporal.
   - En la grilla, las celdas de *Costo* y *Margen* son inputs editables.
   - Mostrar dinámicamente el "Precio Final" resultante al tipear.
   - Botón "Guardar" que dispare el endpoint `/manual`.
2. **Modo Automático (Importador por Plantilla):**
   - Botón visible para **"Descargar Plantilla Excel"**.
   - Componente *Drag & Drop* para soltar la plantilla ya completada por el usuario.
   - Input para seleccionar el "IVA por defecto" aplicable a la plantilla.
   - Botón "Importar". Al finalizar, arrojar `toast.success("Se actualizaron X productos")` y limpiar la pantalla.
