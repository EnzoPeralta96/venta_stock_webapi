# Implementación de "Actualización de Límite Global de Cuenta Corriente" en el Frontend

Contexto: En el backend ya se implementó el endpoint `PUT /api/CurrentAccount/update-limit` que permite cambiar el Límite Global de un cliente mediante el uso de configuraciones preexistentes (`ConfiguracionCc`). Al hacerlo, se inserta un nuevo movimiento neutro en el historial con el tipo `MODIFICACION_LIMITE (10)`.

Tareas a realizar en React (`ProyectoFerreteria`):

### 1. Actualizar el cálculo del "Límite Global" en `ClientCurrentAccountTab.jsx`
Actualmente, el límite de la cuenta se extrae del movimiento de *Alta* (`opening.limiteCuenta`). Dado que ahora el Límite Global puede cambiar en el tiempo con nuevos movimientos, el límite vigente debe extraerse del **último movimiento** sumando la deuda actual (`saldoActual`) y el crédito remanente (`limiteCuenta`).

Buscar en `ClientCurrentAccountTab.jsx`:
```javascript
const limiteTotal = opening?.limiteCuenta ?? 0;
```
Reemplazar por:
```javascript
const limiteTotal = (latest?.saldoActual ?? 0) + (latest?.limiteCuenta ?? 0);
```

### 2. Integrar el Endpoint en los Servicios API
En el archivo donde manejas las peticiones de Cuenta Corriente (ej. `src/services/CurrentAccountQueries.js` o similar), agregar la nueva petición:

```javascript
export const updateAccountLimit = async (body) => {
  // body debe ser: { idCliente: int, idConfiguracion: int, idUsuarioRegistra: int, motivo: string }
  const response = await fetchWithAuth(`${BASE_URL}/api/CurrentAccount/update-limit`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Error al actualizar el límite');
  }
  return await response.json();
};
```
*(Asegurarse de tener también un endpoint/fetch para traer la lista de `ConfiguracionCc` activas, necesario para el select del formulario).*

### 3. Crear el componente modal `UpdateAccountLimitModal.jsx` (o Form)
Crear un formulario modal para cambiar el límite. Debe recibir `clientId`, `onClose`, y `onSuccess`.
El formulario debe contener:
- **Select (Dropdown)**: Para elegir una `ConfiguracionCc` (traídas del backend, mostrar nombre y monto).
- **Textarea/Input**: "Motivo de la modificación" (mínimo 5 caracteres).
- **Validaciones**: Mostrar un resumen visual (Ej: "Límite actual: $ X.XXX -> Nuevo Límite: $ Y.YYY").

### 4. Botón en la Interfaz (`ClientCurrentAccountTab.jsx`)
En la cabecera del Tab de Cuenta Corriente (posiblemente al lado de "Gestionar Cuenta"), agregar un botón **"Modificar Límite"**.
- Al hacer clic, abre el `UpdateAccountLimitModal`.
- En el `onSuccess` del modal, lanzar:
  ```javascript
  toast.success("Límite actualizado correctamente");
  handleMovementRegistered(); // Para que recargue la lista de movimientos y se actualicen los montos en la Card
  ```

### Notas Adicionales
- Asegurarse de inyectar el `idUsuarioRegistra` desde el contexto de autenticación o token (`currentUser.id` o similar) al hacer el submit.
- Validar colores/iconos para este botón "Modificar Límite" para que no quite protagonismo al botón de "Gestionar Cuenta" (cobros/notas). Puede usarse un botón variante `outline` o un icono de lápiz/engranaje dentro de las cards de resumen.
