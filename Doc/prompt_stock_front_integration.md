# Proyecto Venta Stock Frontend - Integración de Ajuste de Stock (Ledger)

Hola Claude. Necesito que integres en el frontend de React (`ProyectoFerreteria`) la nueva funcionalidad de "Ajuste de Stock Manual" y bloquees la edición de stock clásico, como parte del refactor arquitectónico hacia el modelo de "Stock Ledger" (Movimientos de Stock) que ya implementamos en el backend.

El frontend de productos está ubicado en:
`src/components/Productos/ProductosPage.jsx` y componentes hijos (`product-form.jsx`, `product-table.jsx`, `product-list.jsx`).

## Lo que debes implementar

### 1. Deshabilitar edición directa de Stock
- En `src/components/Productos/product-form.jsx`: El campo `stock` debe removerse o deshabilitarse (poner `disabled={true}` y un tooltip que diga "El stock ahora se calcula por movimientos. Para cambiarlo usa Ajuste de Stock") si el formulario está en modo "Edición" (`productoEditando != null`). En modo "Creación" sí se debe permitir un stock inicial (que en realidad el backend manejará como un ajuste/compra inicial o deberás ajustar la vista). *Nota: Coordina con lo que hicimos en el backend: si quitamos `Stock` del `ProductUpdateDTO`, aquí no debe enviarse en la petición de PUT.*

### 2. Nuevo Servicio API `StockMovementQueries.js`
Crea `src/services/StockMovementQueries.js` y expón la función para consumir el endpoint que creamos:
```javascript
export async function registrarAjusteStock(ajusteData) {
  // POST /api/StockMovement/ajuste-manual
  // Body: { idProducto, cantidad, idTipoMovimiento, motivo }
}
```

### 3. Componente `AjusteStockModal`
Crea un nuevo componente de diálogo (Shadcn UI `Dialog` o `AlertDialog` customizado) que permita a un empleado hacer un ajuste manual.
El formulario debe tener:
- **Producto:** (Si se abre desde una fila de la tabla, que venga preseleccionado y readonly. Si se abre desde arriba, un Select/ComboBox de productos activos).
- **Tipo de Ajuste:** Un `Select` con las opciones: "Sobrante de stock (+)", "Faltante / Rotura (-)", "Consumo Interno / Retiro (-)". (Mapea esto a los IDs de los Enum que definimos en backend: Ej 5, 6 y 7).
- **Cantidad:** Input numérico (siempre positivo en UI, o dejar que el usuario ponga `5`).
- **Motivo:** Input de texto opcional o requerido para observación ("Conté 5 extra en la tanda de ayer").

### 4. Integración en `ProductosPage.jsx` y vistas hijas
**A. Acceso rápido por Producto (Recomendado):**
En `product-table.jsx` y `product-list.jsx`, donde están las acciones de la fila (los 3 puntitos o los botones de editar/eliminar), agrega una nueva acción: "Ajustar Stock" (con un icono como `Sliders` o `PackagePlus`).
Al hacer click, se debe abrir el `AjusteStockModal` pasándole el `producto` seleccionado.

**B. Refresco de datos:**
Al registrar exitosamente el ajuste en el Modal, dispara un callback que haga refrescar los data grids o actualice el local state del producto (`setProductos(prev => ...)`).

## Instrucciones Finales
1. Usa agentes para inspeccionar `product-form.jsx` primero y entender bien cómo usa `react-hook-form` o el estado local.
2. Mantén los estilos con `Tailwind` y reutiliza componentes de `Shadcn UI` que ya ves en el archivo.
3. El frontend debe mostrar un `toast.success` cuando el ajuste se realiza correctamente.
4. No rompas los filtros de paginación o el layout existente de `ProductosPage.jsx`.
