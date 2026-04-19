# Validaciones de Input — Frontend y Backend

**Fecha**: Abril 2026
**Área**: Frontend (React) + Backend (ASP.NET Core DTOs)

---

## Resumen

Se implementaron validaciones de entrada en los formularios de **Cliente**, **Proveedor** y **Lista de Precios**, tanto en el frontend (restricciones de input en tiempo real) como en el backend (DataAnnotations en los DTOs).

---

## 1. Cliente

### Frontend — `ClientForm.jsx`

| Campo | Restricción aplicada |
|-------|----------------------|
| DNI | Solo dígitos — `e.target.value.replace(/\D/g, "")`, `inputMode="numeric"`, `maxLength={8}` |
| Teléfono | Solo dígitos — `e.target.value.replace(/\D/g, "")`, `inputMode="numeric"`, `maxLength={15}` |

### Backend — `ClientCreateDTO.cs` y `ClientUpdateDTO.cs`

Ambos DTOs implementan `IValidatableObject` para validación condicional según `EsEmpresa`.

| Campo | Anotación |
|-------|-----------|
| `Nombre` | `[StringLength(100)]` |
| `Apellido` | `[StringLength(100)]` |
| `RazonSocial` | `[StringLength(200)]` |
| `Telefono` | `[Required]`, `[RegularExpression(@"^\d{7,15}$")]`, `[StringLength(15, MinimumLength = 7)]` |
| `Mail` | `[Required]`, `[EmailAddress]`, `[StringLength(200)]` |
| `Dni` | Validado en `Validate()`: regex `^\d{7,8}$` — obligatorio si `EsEmpresa == false` |
| `Cuit` | Validado en `Validate()`: regex `^\d{11}$` — obligatorio si `EsEmpresa == true` |
| `LimiteCuenta` | Validado en `Validate()`: requerido y `> 0` si `TieneCuentaCorriente == true` |
| `SaldoInicial` | Validado en `Validate()`: no puede ser negativo |

**Reglas condicionales (IValidatableObject)**:

- Si `EsEmpresa == false`: `Nombre`, `Apellido` y `Dni` son obligatorios
- Si `EsEmpresa == true`: `RazonSocial` y `Cuit` son obligatorios
- Si `TieneCuentaCorriente == true`: `LimiteCuenta` es obligatorio y debe ser `> 0`

---

## 2. Proveedor

### Frontend — `ProveedorForm.jsx`

| Campo | Restricción aplicada |
|-------|----------------------|
| Teléfono | Solo dígitos — `e.target.value.replace(/\D/g, "")`, `inputMode="numeric"`, `maxLength={15}` |

### Backend — `CreateProveedorDTO.cs` y `UpdateProveedorDTO.cs`

`Telefono` y `Direccion` son **opcionales** (el frontend envía `null` cuando están vacíos).

| Campo | Anotación |
|-------|-----------|
| `Nombre` | `[Required]`, `[StringLength(200, MinimumLength = 2)]` |
| `Direccion` | `string?` — `[StringLength(300)]` (opcional) |
| `Telefono` | `string?` — `[RegularExpression(@"^\d{7,15}$")]`, `[StringLength(15, MinimumLength = 7)]` (opcional) |

> **Nota**: `Telefono` y `Direccion` no tienen `[Required]` porque el frontend puede enviar `null`. La validación de formato solo se activa si el valor no es null.

---

## 3. Lista de Precios

### Frontend — `ProveedorDetailsPage.jsx` (dialog de crear/editar lista)

| Campo | Restricción aplicada |
|-------|----------------------|
| Nombre | `maxLength={200}` |
| Observaciones | `resize-none`, `w-full`, `maxLength={500}` |
| IVA por defecto | `inputMode="decimal"`, filtro `.replace(/[^0-9.]/g, "")`, tope máximo de 100 via `Math.min(val, 100)` |

**Validación en `handleListaSubmit`**:
- Nombre: obligatorio y mínimo 2 caracteres
- IVA: debe estar entre 0 y 100

**Fix de layout**: se agregó `overflow-hidden` a `DialogContent` y `min-w-0` al `<form>` para evitar que el textarea y los inputs se desbordaran fuera del dialog al contener texto largo.

### Backend — `ListaPrecioDTO.cs`

Aplica a `ListaPrecioCreateDTO` y `ListaPrecioUpdateDTO`.

| Campo | Anotación |
|-------|-----------|
| `IdProveedor` | `[Required]`, `[Range(1, int.MaxValue)]` |
| `IdLista` | `[Required]`, `[Range(1, int.MaxValue)]` |
| `Nombre` | `[Required]`, `[StringLength(200, MinimumLength = 2)]` |
| `Observaciones` | `string?` — `[StringLength(500)]` (opcional) |
| `IvaPorDefecto` | `[Range(0, 100)]` — default `21` |

---

## Convenciones aplicadas

### Frontend
- Campos numéricos enteros (DNI, teléfono): `replace(/\D/g, "")` en `onChange` + `inputMode="numeric"`
- Campos decimales (IVA): `replace(/[^0-9.]/g, "")` en `onChange` + `inputMode="decimal"`
- `maxLength` siempre consistente con el límite del DTO backend

### Backend
- Campos opcionales que el frontend puede enviar como `null`: declarados como `string?`, sin `[Required]`
- Validación de formato (regex) en campos `string?` solo se dispara cuando el valor no es null/vacío (comportamiento nativo de DataAnnotations)
- Validación condicional compleja: usar `IValidatableObject.Validate()` en lugar de solo DataAnnotations
