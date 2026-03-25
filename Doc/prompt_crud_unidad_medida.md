# Proyecto Venta Stock - CRUD de Unidades de Medida

Hola Claude. Como ya implementamos la estructura base para que el proyecto utilice `IdUnidadMedida` a lo largo de todos los módulos, necesitamos ahora crear el ABM (CRUD) administrativo para que el gerente pueda agregar o dar de baja unidades a su gusto (Ej: Cargar una nueva unidad métrica o un empaque especial como "Bolsa x50kg").

## Requisitos de Implementación

Como el proyecto tiene los lineamientos de arquitectura muy claros, este CRUD debe ser casi un calco estético y funcional del que hiciste para `TipoMovimientoStock`.

### 1. Modificaciones en Base de Datos
- Por si no lo agregaste aún, la entidad `UnidadMedida` en la base de datos debe contemplar una propiedad de *Baja Lógica / Estado*. Agrega `public bool Activo { get; set; } = true;`. 
- Corre una migración breve si es necesario.

### 2. Backend (Feature Completo)
Crea una carpeta nueva en `Features` llamada `UnidadMedida` y despliega todas las capas:
- **DTOs:** `UnidadMedidaDTO` (Id, Nombre, Abreviatura, Activo), `CreateUnidadMedidaDTO` (Nombre, Abreviatura), `UpdateUnidadMedidaDTO` (Nombre, Abreviatura).
- **Repository:** `IUnidadMedidaRepository` y `UnidadMedidaRepository`.
- **Service:** `IUnidadMedidaService` y `UnidadMedidaService`. Debe usar el patrón `Result<T>` y `MessageProvider` para los errores genéricos o reglas de negocio (ej: `nombre_duplicado`).
- **Controller:** `UnidadMedidaController` con 5 métodos:
  - `GET /api/unidadmedida/admin` (Lista todos los tipos, activos e inactivos, para la grilla).
  - `GET /api/unidadmedida` (Solo los activos, ideal para poblar los selects).
  - `POST /api/unidadmedida` (Crear).
  - `PUT /api/unidadmedida/{id}` (Editar).
  - `PATCH /api/unidadmedida/{id}/toggle` (Desactivar/Activar lógicamente).


