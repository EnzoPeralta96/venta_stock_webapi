## 3.1 Requisitos funcionales (RF)

### RF001 Sesiones 

#### RF001.1 Inicio de Sesión y Emisión de token (Autorización)

- Descripción: El sistema deberá permitir que un usuario inicie sesión ingresando nombre de usuario y contraseña. El sistema validará que el usuario exista, que la contraseña sea correcta y que el usuario se encuentre activo.

Si la autenticación es exitosa, el sistema deberá emitir un token de acceso (por ejemplo, JWT) asociado al usuario autenticado, el cual será utilizado posteriormente para acceder a los recursos protegidos del sistema.

- Criterios de aceptación: 

- El usuario puede ingresar su nombre de usuario y contraseña en el formulario de inicio de sesión. 

- El sistema valida las credenciales contra los datos persistidos y verifica que el usuario exista y este activo.

- Si las credenciales son correctas, el sistema retorna una respuesta exitosa que incluye un token de acceso.

- Si las credenciales son incorrectas o el usuario no está activo el sistema retorna un error indicando que las credenciales no son válidas.
		  
#### RF001.2 Control de acceso mediante token y permisos (Autenticación)

- Descripción:  El sistema deberá controlar el acceso a funciones y recursos protegidos mediante un token de acceso válido emitido en el inicio de sesión.
    

Toda solicitud a un recurso protegido deberá incluir el token. El sistema deberá validar su integridad, vigencia (expiración) y la identidad del usuario representado.

Además, el sistema deberá autorizar el acceso en base a los permisos/roles asociados al usuario (ya sea contenidos en el token o recuperados desde el servidor), permitiendo o denegando la operación solicitada.

  

- Criterios de aceptación: 
    

Toda solicitud a una función/recurso protegido debe incluir un token de acceso.

Si el token no existe, es inválido, fue alterado o está expirado, el sistema debe rechazar la solicitud.

Si el token es válido, el sistema evalúa los permisos del usuario para la acción solicitada. Si el usuario posee permisos suficientes, el sistema concede acceso; caso contrario, lo deniega.

El token debe expirar luego de un tiempo de validez configurable (por ejemplo, 4 horas).

### RF002 Funciones para el Usuario Administrativo

#### RF002.1 - Gestión de Administradores (USUARIOS) (CRUD)

- Descripción: El sistema debe permitir la gestión completa (crear, leer, actualizar y eliminar) de usuarios. Esta funcionalidad estará restringida según el tipo de administrador.
    

  

- Criterios de aceptación:
    

Administrador principal predefinido: El sistema debe incluir un administrador principal creado por defecto.  
Este usuario no puede ser eliminado por ningún otro usuario.

  

Creación de usuarios: El administrador principal puede crear nuevos usuarios, ingresando los siguientes datos: usuario, contraseña, nombre, apellido, correo electrónico y rol.

  

Edición y eliminación: Solo el administrador principal tiene permisos para editar o eliminar a otros usuarios.  
Los usuarios pueden editar únicamente su propia información (por ejemplo, cambiar su nombre o correo electrónico).

  

Validación de datos: El sistema debe validar todos los datos ingresados antes de guardar un nuevo registro o aplicar cambios.

  

Visualización, búsqueda y filtrado: El administrador puede ver una lista de todos los usuarios registrados.  
La lista debe contar con opciones de búsqueda y filtrado para facilitar la gestión (por ejemplo, por nombre, correo).

#### RF002.2 - Asignación de Roles y Permisos

- Descripción: El sistema debe permitir la gestión de roles y permisos de los usuarios que acceden al sistema, de forma que cada usuario solo tenga acceso a las funcionalidades autorizadas según su rol.
    
- Criterios de aceptación:
    

Definición de roles: El sistema debe contar con una estructura de roles, tales como:

- Administrador principal
    
- Administrador de Gestión de precios
    
- Vendedor
    

- Asignación de roles:
    

- Asignación de roles y permisos granulares: El administrador principal puede asignar un rol predefinido al momento de la creación o edición del usuario. Además, el sistema debe permitir personalizar (marcar o desmarcar) permisos específicos de forma granular en esa misma interfaz.
    
- Categorización visual: Los permisos deben mostrarse agrupados por módulos o categorías (ej. Gestión de Usuarios, Ventas y Finanzas) y contar con opciones para "Seleccionar todo" por bloque para agilizar la carga.
    
- Seguridad y trazabilidad: Cualquier cambio en los roles o permisos debe quedar registrado en el sistema con información de quién realizó el cambio, la fecha y hora.
    

#### RF002.3 - Generar reportes financieros y de gestión.

- Descripción: El sistema debe permitir la generación de reportes financieros y de gestión que ayuden al dueño o personal autorizado a tomar decisiones estratégicas.
    

- Los reportes que se podrán realizar serán:
    

- Ventas por día, semana o mes.
    
- Margen de utilidad bruta.
    
- Productos más vendidos por categoría.
    
- Clientes más frecuentes.
    
- Total vendido en un período.
    
- Clientes con saldo deudor y monto total adeudado.
    
- Tiempo promedio de cobro de cuenta corriente.
    
- Artículo más vendido
    

- Criterios de aceptación:
    

Solo los usuarios con permisos específicos (por ejemplo, el administrador principal o encargado de reportes) podrán generar y acceder a estos reportes.

  

#### RF002.4 - Permitir ventas a clientes con límite superado en cuenta corriente

- Descripción: El sistema debe bloquear las ventas a clientes que hayan superado su límite de crédito en cuenta corriente, salvo autorización explícita.
    
- Criterios de aceptación:
    

Cada cliente con cuenta corriente debe tener un límite de crédito configurable.

Si el cliente supera ese límite, el sistema:

- Bloquea la venta automáticamente.
    
- Muestra un mensaje de advertencia.
    

  

El administrador principal puede autorizar excepcionalmente la venta superando el límite.  
Todas las excepciones deben quedar registradas (cliente, usuario que autorizó, fecha y hora).

  

#### RF002.5 - Exportar información en formatos PDF y/o Excel

- Descripción: El sistema debe permitir exportar información relevante en formato PDF y Excel, para facilitar su análisis externo, envío o respaldo.
    
- Debe poder exportarse:
    

- Reportes financieros y de gestión.
    
- Listado de productos.
    
- Listado de clientes y estado de cuenta.
    
- Historial de ventas.
    
- Stock actual.
    

- Criterios de aceptación: 
    

La opción de exportación debe estar disponible solo para usuarios con los permisos correspondientes.

El archivo debe contener el nombre del reporte, fecha de generación y formato legible.

  

#### RF002.6 - Acceso al historial de acciones realizadas

- Descripción: El sistema debe mantener un registro detallado de todas las acciones importantes realizadas por los usuarios, para garantizar trazabilidad y control.
    
- Se debe registrar:
    

- Ventas realizadas.
    
- Cambios en precios o stock.
    
- Altas, bajas y modificaciones de productos, clientes y usuarios.
    
- Accesos al sistema.
    
- Cambios de rol o permisos.
    

- El historial debe incluir:
    

- Usuario responsable.
    
- Fecha y hora.
    
- Acción realizada.
    
- Entidad afectada (producto, cliente, usuario, etc.)
    

- Criterios de aceptación: 
    

Solo los usuarios con permisos de auditoría (por ejemplo, el administrador principal) podrán acceder a este historial.

#### RF002.7 - Generar notas de débito

- Descripción: El sistema debe permitir generar notas de débito para aquellos clientes que no hayan pagado sus facturas de cuenta corriente una vez vencida.
    
- Criterios de aceptación:
    

El sistema deberá mostrar un listado de aquellos clientes que no hayan pagado sus facturas adeudadas, mostrando en detalle la factura adeudada con su respectivo monto.

Además deberá permitir generar la nota de débito ingresando o seleccionando el interés correspondiente para cada cliente.

Una vez generada la nota de débito, se deberá registrar ese incremento de la deuda del cliente en la base de datos.

  

#### RF002.8 - Ver movimientos de la cuenta corriente de un cliente.

- Descripción: El sistema debe permitir ver los movimientos de la cuenta corriente del cliente.
    

Dichos movimientos son: facturas emitidas, en caso de existir, notas de crédito o débito, recibos de pago y el saldo actual.

- Criterios de aceptación:
    

Para cada cliente, el sistema deberá mostrar por UI, un botón que al presionar muestre por pantalla los movimientos mencionados del cliente, en un formato claro, ordenado y fácil de leer.

#### RF002.9 - Generar notas de crédito 

- Descripción: El sistema debe permitir generar notas de crédito a favor de los clientes que posean cuenta corriente, por ejemplo, en casos de devoluciones de productos, pagos a favor o resoluciones de saldos. 
    
- Criterios de aceptación:
    

El sistema deberá mostrar un listado o perfil del cliente con cuenta corriente. El usuario administrador podrá seleccionar la opción para generar una nota de crédito, seleccionando un motivo preconfigurado (ej. "Devolución de producto") o ingresando uno manual. Una vez confirmada, el sistema debe incrementar el crédito disponible (o reducir la deuda) del cliente y dejar el movimiento registrado en el historial de la cuenta corriente.

  
  
  

### RF003 Funciones para el Usuario Encargado de Precios

#### RF003.1 CRUD de Productos 

- Descripción: 
    

El sistema debe permitir la gestión integral del catálogo de inventario, habilitando a los usuarios autorizados a crear, visualizar, editar y eliminar (de forma lógica) los productos. Toda la información gestionada debe ser persistida en la base de datos.

Datos del Producto y Validaciones: Para dar de alta o modificar un producto, el sistema manejará los siguientes campos:

- Nombre: Obligatorio. Alfanumérico. Regla: Debe ser único; el sistema no permitirá registrar o actualizar un producto si el nombre ya existe.
    
- Descripción: Opcional. Texto libre.
    
- Categoría: Obligatorio. Selección desde un listado predefinido.
    
- Código de Barras: Obligatorio. Alfanumérico/Numérico.
    
- Precio: Obligatorio. Numérico. Regla: No puede ser un valor negativo (≥ 0).
    
- Stock Inicial: Obligatorio. Numérico. Regla: Puede inicializarse en cero (≥ 0).
    
- Ubicación en depósito: Obligatorio. Alfanumérico. Regla: Se permite asignar la misma ubicación a múltiples productos.
    
- Estado (Activo): Campo interno del sistema (Booleano). Al crear un producto, se establece por defecto en verdadero.
    

Regla de Negocio - Codificación de Ubicación: La ubicación física del producto en el depósito debe seguir una nomenclatura estructurada en tres partes ([Fila] [Sección] [Nivel]):

- Fila: Representada por un número (iniciando en 1).
    
- Sección: Representada por una letra (iniciando en A, desde adelante hacia el fondo de la fila).
    
- Nivel (Altura): Representado por un número (iniciando en 1, de abajo hacia arriba). Ejemplo de formato válido: 1 A 1 (Fila 1, Sección A, Nivel 1).
    

- Criterios de aceptación: 
    

Creación y Edición (Interfaz): El sistema debe proveer un formulario por UI para el alta y modificación de productos. Este formulario debe contar con botones para "Guardar" y "Cancelar". Si el usuario cancela, la operación se anula sin realizar cambios.

  

Validación y Guardado: Al enviar el formulario, el sistema debe validar las reglas de los campos (ej. unicidad de nombre, precio no negativo). Si es exitoso, los datos se guardan/actualizan en la base de datos y el producto se refleja inmediatamente en el listado principal.

  

Eliminación (Borrado Lógico): Cada producto en el listado debe tener un botón/acción de eliminación. Al accionarlo, el sistema debe desplegar un cuadro de diálogo solicitando confirmación ("¿Está seguro que desea eliminar el producto?").

  

Efecto de la Eliminación: Si el usuario confirma la eliminación, el sistema no debe realizar un borrado físico en la base de datos. En su lugar, debe cambiar el estado del campo activo a false (borrado lógico). Como resultado, el producto debe desaparecer del listado de productos activos.

#### RF003.2 Cargar códigos de barra a los productos

- Descripción: 
    

El sistema debe permitir más ingresar un código de barras para un producto, aunque el producto ya cuente con uno o más código de barras.

  

- Criterios de aceptación: 
    

El sistema debe permitir cargar un código de barras para un productos de diferentes maneras: 

- Lector de código de barras.
    
- Cargar el código de barras como imagen mediante un formulario
    
- El código de barra ingresado, se almacena en la base de datos junto con los demás códigos en caso de existir.
    

  

#### RF003.3 Importar listas de precios de proveedores

- Descripción : el sistema debe permitir actualizar los precios de los productos mediante un listado de precios enviados por el proveedor.  
    El listado de precios puede estar en formato .pdf o csv.
    

  

- Criterios de aceptación:
    

El sistema debe permitir importar un archivo .pdf o csv, debe procesarlo y actualizar los precios de los productos en la db.

Luego se debe poder ver el listado de productos con el precio actualizado a través de la UI.

  

#### RF003.4 Visualización de la ubicación de un producto

- Descripción: El sistema debe mostrar la ubicación física de cada producto dentro del depósito directamente en el listado general de inventario, permitiendo al usuario identificar rápidamente dónde encontrar la mercadería sin necesidad de realizar acciones o consultas adicionales.
    

  

- Criterios de aceptación:
    

La ubicación del producto debe mostrarse explícitamente en la columna denominada "Categoría / Ubicación" dentro de la tabla principal de Gestión de Productos.

El dato mostrado debe respetar la codificación o el formato exacto con el que se organizan físicamente los estantes, pasillos o sectores en el depósito (por ejemplo: "1 E 1", "1 C 1", "1 A 1").

#### RF003.5  Visualización y Control de stock bajo

El sistema debe monitorear el stock actual de los productos en relación con su stock mínimo configurado y alertar al usuario sobre aquellos que requieren reposición. 

Criterios de aceptación:

El panel de Gestión de Productos debe incluir un indicador o tarjeta superior denominada "Stock Bajo" que muestre en tiempo real la cantidad total de productos que se encuentran en esta condición.

Al seleccionar la tarjeta "Stock Bajo", el listado de productos debe filtrarse automáticamente para mostrar solo los ítems críticos. Mostrando el stock actual como el límite mínimo configurado y resaltando en color aquellos valores que estén por debajo o igual al mínimo.

  

#### RF003.6 - Ajuste de stock manual 

Descripción: El sistema debe proveer una interfaz para realizar ajustes manuales en el inventario de forma rápida y justificada, ideal para corregir discrepancias tras auditorías o conteos físicos. 

- Criterios de aceptación:
    

El sistema debe proveer un formulario de "Ajuste de Stock". El usuario debe poder buscar y seleccionar el Producto. Al seleccionar el producto, el sistema debe mostrar el Stock actual en formato de solo lectura. 

El formulario debe exigir con carácter obligatorio (*):

- Tipo de Ajuste: (mediante un menú desplegable para indicar si es suma o resta).
    
- Cantidad: (valor numérico a ajustar).
    
- Motivo: (área de texto para justificar la acción, ej: "Sobrante detectado en conteo físico").
    

El sistema debe proveer botones para "Cancelar"  o "Confirmar Ajuste".

#### RF003.7 - CRUD de Proveedores 

Descripción: El sistema debe permitir la gestión completa de los proveedores y distribuidores de la ferretería para centralizar la información de contacto y compras. 

Criterios de aceptación:

- El sistema debe permitir dar de alta un proveedor ingresando datos obligatorios como Nombre/Razón Social, Teléfono y Dirección.
    
- El sistema debe mostrar un listado de proveedores clasificados por estado (Activos / Inactivos).
    
- El usuario debe poder editar los datos de contacto de un proveedor existente.
    
- La eliminación de un proveedor debe ser un borrado lógico, cambiando su estado a "Inactivo".
    

#### RF003.8 - CRUD Listas de Precios de Proveedores 

Descripción: El sistema debe permitir asociar múltiples catálogos o listas de precios a un proveedor específico, manteniendo un historial de las listas vigentes y pasadas. 

Criterios de aceptación:

- Dentro del detalle de cada proveedor, el sistema debe permitir crear una "Lista de Precios" indicando Nombre (ej. "Lista Marzo 2026"), Fecha de vigencia y Observaciones.
    
- El sistema debe permitir ver el detalle de los productos que componen esa lista.
    
- El usuario debe poder editar los metadatos de la lista o "Desactivarla" para que sus precios dejen de estar vigentes en el sistema.
    

#### RF003.9 - Importar productos a listas de precios mediante Excel 

Descripción: El sistema debe facilitar la carga masiva de los artículos y costos de un proveedor a través de la importación de un archivo de hojas de cálculo, evitando la carga manual individual. 

Criterios de aceptación:

- Dentro de una lista de precios específica, debe existir la funcionalidad para importar un archivo en formato .xlsx o .csv.
    
- El sistema debe leer el archivo y procesar los campos correspondientes a Producto, Marca y Precio (Costo).
    
- El sistema debe validar que los datos tengan el formato correcto y guardarlos asociándolos a esa lista de precios en la base de datos.
    
- Se debe notificar al usuario mediante un mensaje de éxito o detallando si hubo errores en alguna fila del archivo.
    

#### RF003.10 - Registrar compras a proveedores 

Descripción: El sistema debe permitir asentar las compras de mercadería realizadas a los proveedores para actualizar el stock y mantener un control de los costos de reposición. 

Criterios de aceptación:

- El usuario debe poder iniciar un registro de compra seleccionando al Proveedor correspondiente.
    
- Debe poder cargar los productos adquiridos, indicando las cantidades ingresadas y el costo unitario de compra.
    
- Al finalizar y confirmar el registro de compra, el sistema debe actualizar (sumar) automáticamente el stock de dichos productos en el depósito.
    
- La operación debe quedar registrada en el sistema, vinculando la fecha, el proveedor, los artículos y el monto total de la compra para futuros reportes financieros.
    

  

### RF004 Funciones para el Usuario Vendedor

#### RF004.1 - CRUD de clientes

Descripción: El sistema debe permitir a los usuarios autorizados (ej. Vendedor, Administrativo) gestionar el padrón de clientes. Esta gestión abarca la visualización del listado, la creación de nuevos perfiles, la edición de información de contacto, la baja lógica y la posterior reactivación de los mismos.

Datos y Validaciones según Tipo de Cliente: El sistema debe diferenciar dinámicamente los campos a completar según la naturaleza jurídica del cliente. Todos los campos listados a continuación son de carácter obligatorio:

- Persona Física (Individuo): Nombre, Apellido, DNI, Dirección, Teléfono y Correo Electrónico.
    
- Persona Jurídica (Empresa): Razón Social, CUIT, Dirección, Teléfono y Correo Electrónico.
    

Reglas de Negocio:

- Unicidad de Datos: Los campos de identificación tributaria/civil (DNI, CUIT/CUIL) y el Correo Electrónico deben ser estrictamente únicos en el sistema. No se puede dar de alta o actualizar un cliente si estos datos ya existen en otro registro.
    
-  Apertura de Cuenta Corriente (CC): La habilitación de una Cuenta Corriente es opcional al momento de crear el cliente. Si el usuario marca la opción de aperturar una CC, el sistema exigirá obligatoriamente que se defina un "Límite de Crédito" y un "Saldo Inicial" (el cual puede reflejar deuda o saldo a favor).
    
- Restricción de Edición: La modificación de un cliente existente está restringida únicamente a datos personales y de contacto. Por seguridad, no se podrán modificar los parámetros de su Cuenta Corriente (límite o saldo) desde el formulario de edición básica del cliente.
    
- Estados y Borrado Lógico: Todo cliente posee un campo de estado interno (Activo = true | false). Al darse de alta, se establece en true. La acción de "Eliminar" realiza un borrado lógico (cambia el estado a false), moviendo al cliente a un registro de "Inactivos". El sistema debe permitir reactivarlo (volver a true) en cualquier momento.
    

Criterios de Aceptación:

- Visualización del Padrón: El sistema debe proveer una vista de tabla con todos los clientes, mostrando columnas clave: Nombre/Razón Social, DNI/CUIT, y un indicador de si posee Cuenta Corriente. La vista debe contar con pestañas o filtros rápidos para alternar entre clientes "Activos" e "Inactivos".
    
- Formulario Dinámico de Alta: El formulario de creación debe adaptar sus campos dependiendo de si el usuario selecciona "Persona Física" o "Persona Jurídica".
    
- Validación de Errores: Al intentar guardar (crear o editar), el sistema debe validar la unicidad de los datos y que los campos requeridos estén completos. Si hay errores, debe mostrar un mensaje claro y evitar que se guarde el registro.
    
- Generación de CC: Si el cliente se crea con la opción de Cuenta Corriente activa, el sistema debe impactar en la base de datos la creación de dicha cuenta vinculada al cliente, respetando el límite y saldo inicial asignados.
    
- Ciclo de Vida (Baja y Reactivación): El listado debe proveer un botón para "Dar de baja" (en la pestaña de activos) y otro para "Reactivar" (en la pestaña de inactivos). Ambas acciones deben solicitar confirmación del usuario antes de ejecutarse y modificar únicamente el estado lógico en la base de datos sin borrar el historial del cliente.
    

  

#### RF004.2 - Registrar ventas

Descripción: El sistema debe proveer una interfaz de Punto de Venta (POS) que permita al usuario registrar transacciones comerciales de forma ágil, seleccionando los productos, asociando al cliente correspondiente y definiendo la condición de pago.

Reglas de Negocio:

- Identificación del Cliente: Toda venta debe estar asociada a un cliente. El sistema debe permitir buscar uno existente o proveer un acceso rápido para dar de alta uno nuevo en el momento.
    
- Métodos de Pago: La venta puede registrarse al "Contado" o mediante "Cuenta Corriente" (CC).
    
- Validación de Cuenta Corriente: Si se selecciona CC, el cliente debe tener una cuenta habilitada. El monto total de la venta no debe superar el crédito disponible del cliente, a menos que el usuario en sesión posea el permiso explícito de "Autorizar venta con límite superado".
    
- Validación de Stock: El sistema debe verificar la disponibilidad física del producto. No se puede vender una cantidad mayor al stock actual, a menos que el usuario posea el permiso de "Vender sin stock".
    
- RN05 - Impacto Operativo: Al confirmarse una venta aprobada, el sistema debe descontar automáticamente el stock de los productos involucrados y, si el pago es por CC, impactar la deuda en el estado de cuenta del cliente.
    

Criterios de Aceptación:

- Carga de Productos y Totales: El usuario debe poder agregar productos al carrito mediante búsqueda por texto o escaneo de código de barras. Por cada ítem agregado, el sistema debe permitir modificar la cantidad y calcular dinámicamente el subtotal por línea y el Total general de la venta visible en pantalla.
    
- Selección de Cliente y Pago: El sistema debe permitir vincular al cliente y seleccionar el método de pago antes de habilitar el botón de confirmación.
    
- Bloqueo por Restricciones: Al intentar confirmar la venta, si el stock es insuficiente o no permite venta sin stock el sistema debe mostrar una alerta clara. Si el límite de CC es excedido dejará la venta en estado "Pendiente" de aprobación por un usuario administrador.
    
- Confirmación exitosa: Tras una validación exitosa, al finalizar la venta, el sistema debe mostrar un mensaje de éxito, generar el registro histórico de la transacción, descontar el inventario y actualizar el saldo del cliente en tiempo real.
    

  

#### RF004.3 - Emitir Facturas con Total en Números y Letras

- Descripción: El sistema debe emitir facturas claras y completas, con el monto total expresado tanto en números como en letras.
    
- El comprobante generado debe mostrar:
    

- Detalle de productos vendidos.
    
- Subtotal
    
- Total expresado en letras (por ejemplo: "Dos mil cuatrocientos pesos").
    
- El formato de factura debe ser compatible con exportación a PDF y/o impresión.
    

  

#### RF004.4 - Consultar Stock Disponible

- Descripción: El vendedor debe poder consultar el stock disponible de productos en tiempo real para poder informar al cliente con precisión.
    
- Criterios de aceptación: 
    

- El vendedor debe buscar productos por nombre, categoría, código de barras o ubicación.
    
- Se debe visualizar  la cantidad disponible en stock.
    
- Se debe ver la ubicación del producto dentro del depósito o estante.
    

  

#### RF004.5 - Vender sin Stock, si la configuración lo permite

- Descripción: El sistema debe permitir la venta de productos sin stock, solo si la configuración general lo habilita.
    
- Criterios de aceptación: 
    

- El sistema debe contar con una opción configurable que defina que productos se permite o no vender sin stock.  
    Si la opción está habilitada el sistema permite finalizar la venta incluso si el stock del producto es 0.
    
- Si está deshabilitada el sistema debe bloquear la venta de productos sin stock y notificar al vendedor.
    

  

### RF005 Funciones para realizar búsqueda

- Descripción : El cliente solicitó tener un super buscador en su sistema, pero en vez de realizar un superbuscador, ofreceremos una solución modular y escalable: 
    

  
Cada dominio del sistema (Usuarios, clientes, productos, ventas) tendrá su propio buscador optimizado para sus datos.

  

Para hacer uso del mismo, el usuario deberá dirigirse a la sección correspondiente y hacer la búsqueda.

  

Para cada uno de los buscadores, el sistema debe permitir hacer búsquedas por diferentes datos, pertenecientes al dominio, que se detallarán en cada buscador.

  

- #### RF005.1 Buscador de usuarios
    

Se debe permitir la búsqueda de usuarios por: nombre, apellido, correo electrónico y rol.

  

- #### RF005.2 Buscador de clientes
    

Se debe permitir la búsqueda por Nombre, dni, teléfono, email.

  

- #### RF005.3 Buscador de productos
    

Se debe permitir la búsqueda por nombre, categoría, código de barras, ubicación.

  
  

- #### RF005.4 Buscador de ventas
    

Se debe permitir la búsqueda por número de comprobante de venta, cliente, rango de fechas e importes.

  

- Criterios de aceptación general para cada buscador:  
    El sistema debe presentar por UI una barra de búsqueda en cada sección del sistema. 
    

La búsqueda se debe realizar en la base de datos, en base al término de búsqueda ingresado por el usuario, a su vez debe mostrar por UI resultados parciales a medida que el usuario ingresa el término de búsqueda.

La búsqueda debe ser reactiva para evitar recargas de páginas.

Debe ser eficiente, traer datos precisos y no demorar más 2 o 3 segundos.

**