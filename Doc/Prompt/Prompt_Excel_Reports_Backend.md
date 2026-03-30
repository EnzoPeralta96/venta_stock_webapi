# Prompt Claude Code: Importación Excel y Exportación de Reportes (Backend)

```
Hola Claude, necesito implementar dos nuevos requerimientos críticos del cliente en nuestro proyecto ASP.NET Core 8 Web API (gestión de ventas y stock).

**CONTEXTO DEL PROYECTO:**
- Arquitectura estricta en capas (Controllers -> Services -> Repositories -> DTOs).
- Entity Framework Core (PostgreSQL).
- Patrones obligatorios: Result Pattern (`Result<T>`), AutoMapper, MessageProvider para errores, PagedList para listas.
- TODAS las convenciones están en el archivo `AGENTS.md`. Léelo detenidamente antes de empezar.

**ESTRATEGIA DE EJECUCIÓN:**
Quiero que actúes como un orquestador. Primero, haz una planificación leyendo los archivos necesarios. Luego, utilizando tus herramientas de agente y subagentes (si dispones de ellas) o ejecutando bash/herramientas de edición, implementa el código en etapas consecutivas dentro de esta misma sesión. No te detengas hasta completar las dos etapas y entregar un reporte final.

---

### ETAPA 1: Importador de Listas de Precios (Excel/CSV)
**Contexto**: Los proveedores envían listas de precios en Excel. Necesitamos automatizar su carga.
**Objetivo**: Crear un endpoint `POST /ProductoListaPrecioProveedor/{idLista}/import` que reciba un archivo `IFormFile`.
1. Elige una librería (ej. `EPPlus` o `ExcelDataReader` para Excel, o `CsvHelper` para CSV) e instálala vía `dotnet add package` si es necesario.
2. El servicio debe leer el archivo (esperando columnas como Código de Barras e Importe).
3. Por cada fila, buscar si el producto existe. Si existe, hacer un upsert o insert en la tabla intermedia de la lista de precios con el nuevo precio (aplicando el margen si corresponde).
4. El servicio debe retornar un reporte (`ImportResultDTO`) indicando: Total procesados, Actualizados, Errores/No encontrados.

### ETAPA 2: Exportación de Reportes a Excel y PDF
**Contexto**: El cliente y su contador necesitan descargar la info del sistema (ej. listados de Compras y Proveedores).
**Objetivo**: Crear endpoints de exportación para las tablas principales.
1. Para **Excel**, puedes usar `ClosedXML` o `EPPlus`. Para **PDF**, puedes usar `iTextSharp` o integrarlo en el front y aquí solo enviar datos. Te recomiendo implementar exportación directa en backend para Excel (`GET /CompraProveedor/export/excel`, descargando un `FileContentResult` con MIME type de excel) y evaluar si PDF se hace igual o se delega.
2. Implementar los endpoints de exportación (`export/excel`, `export/pdf` si decides hacerlo en back) en los controladores `ProveedorController` y `CompraProveedorController`.

---

**Instrucción Final para ti (Claude):**
Procede a planificar, inspecciona el código existente (como `CompraProveedorServices.cs`, `ProductoListaprecioProveedor` model, etc.), orquesta tus agentes/herramientas paso a paso, y cuando termines ambas etapas ejecuta un `dotnet build` para comprobar que todo compile. Dame un reporte final de los archivos modificados.
```
