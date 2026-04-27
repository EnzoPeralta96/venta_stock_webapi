# Explicacion del Refactor de Configuracion de Entity Framework Core

## 1. Objetivo del refactor

El objetivo de este refactor fue dejar de tener un `DbContext` monolitico, con toda la configuracion del modelo concentrada en un unico metodo `OnModelCreating`, para pasar a una estructura mas modular, mantenible y escalable.

Antes del refactor, el archivo `Data/VentaStockContext.cs` tenia dos responsabilidades al mismo tiempo:

1. Actuar como punto de entrada a la base de datos mediante los `DbSet`.
2. Contener toda la configuracion de mapeo de todas las entidades.

Eso hacia que el archivo creciera demasiado, fuera dificil de leer y complicara el mantenimiento.

Despues del refactor, la configuracion de cada entidad quedo separada en su propia clase dentro de `Data/Configurations`, mientras que el contexto quedo reducido a su funcion principal: representar la sesion de trabajo con la base de datos y centralizar los `DbSet`.

## 2. Como estaba antes

Antes, el patron era este:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Usuario>(entity =>
    {
        entity.HasKey(e => e.IdUsuario);
        entity.ToTable("usuario");
        entity.Property(e => e.Nombre).HasColumnName("nombre");
        // muchas lineas mas...
    });

    modelBuilder.Entity<Producto>(entity =>
    {
        // configuracion de otra entidad
    });

    modelBuilder.Entity<Cliente>(entity =>
    {
        // configuracion de otra entidad
    });

    // decenas de entidades mas...
}
```

Este enfoque funciona, pero tiene varias desventajas:

- El `DbContext` termina concentrando demasiada logica de infraestructura.
- Buscar la configuracion de una entidad especifica se vuelve incomodo.
- Cambiar una sola entidad obliga a abrir un archivo enorme.
- Aumenta la probabilidad de errores al editar bloques largos.
- El merge de ramas en Git se vuelve mas conflictivo, porque muchos cambios caen sobre el mismo archivo.

## 3. Que se hizo en el refactor

Se aplico el patron recomendado por EF Core para configuraciones separadas por entidad.

### Estructura nueva

Se creo la carpeta:

```text
Data/
├── VentaStockContext.cs
└── Configurations/
    ├── UsuarioConfiguration.cs
    ├── ProductoConfiguration.cs
    ├── ClienteConfiguration.cs
    ├── VentaPendienteConfiguration.cs
    ├── TipoMovimientoStockConfiguration.cs
    └── ...
```

Cada archivo de configuracion ahora representa una unica entidad.

Por ejemplo:

- `UsuarioConfiguration.cs` configura `Usuario`
- `ProductoConfiguration.cs` configura `Producto`
- `MovimientoCcConfiguration.cs` configura `MovimientoCc`
- `UnidadMedidaConfiguration.cs` configura `UnidadMedida`

En total, el `DbContext` quedo mucho mas limpio y la configuracion quedo distribuida de manera atomica.

## 4. Como quedo el `DbContext`

Ahora `VentaStockContext` mantiene:

- Los constructores del contexto.
- Los `DbSet`.
- La configuracion global del modelo.
- La carga automatica de configuraciones.

La parte importante quedo asi:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasPostgresExtension("pgcrypto");
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(VentaStockContext).Assembly);

    OnModelCreatingPartial(modelBuilder);
}
```

Esto significa que el contexto ya no define manualmente la configuracion de cada entidad, sino que delega esa responsabilidad a clases especializadas.

## 5. Que significa cada parte del nuevo `OnModelCreating`

### `protected override void OnModelCreating(ModelBuilder modelBuilder)`

Este metodo pertenece a `DbContext` y EF Core lo ejecuta cuando construye el modelo interno de entidades.

Ese modelo interno le dice a EF Core:

- que entidades existen,
- a que tablas mapean,
- que propiedades van a que columnas,
- que claves primarias y foraneas existen,
- que indices, restricciones, seeds y relaciones hay.

Es decir: aca se define el "mapa" entre C# y la base de datos.

### `modelBuilder`

`modelBuilder` es una instancia de `ModelBuilder`.

Su funcion es construir el modelo completo de EF Core. Con este objeto se puede:

- configurar entidades,
- agregar restricciones,
- definir convenciones especificas,
- registrar extensiones del proveedor,
- aplicar configuraciones externas.

### `modelBuilder.HasPostgresExtension("pgcrypto");`

Esta linea indica que el modelo utiliza la extension `pgcrypto` de PostgreSQL.

Esto no corresponde a una entidad puntual, sino al modelo general de la base. Por eso tiene sentido que siga en el `DbContext` y no en una clase de configuracion individual.

En otras palabras:

- Una `EntityTypeConfiguration` configura una entidad.
- `HasPostgresExtension(...)` configura algo a nivel modelo/proveedor.

### `modelBuilder.ApplyConfigurationsFromAssembly(typeof(VentaStockContext).Assembly);`

Esta es la linea central del refactor.

Le dice a EF Core:

"Busca dentro del mismo assembly donde esta `VentaStockContext` todas las clases que implementen `IEntityTypeConfiguration<T>` y aplicalas automaticamente al modelo."

Eso evita tener que escribir manualmente algo asi:

```csharp
modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
modelBuilder.ApplyConfiguration(new ProductoConfiguration());
modelBuilder.ApplyConfiguration(new ClienteConfiguration());
// etc...
```

#### Que significa `typeof(VentaStockContext).Assembly`

- `typeof(VentaStockContext)` obtiene el tipo `VentaStockContext`.
- `.Assembly` obtiene el assembly donde ese tipo esta definido.

En este proyecto, eso hace que EF Core escanee el assembly del backend y encuentre todas las clases de configuracion que estan en `Data/Configurations`.

#### Ventaja practica

Cuando agregues una nueva configuracion:

1. creas la clase,
2. implementas `IEntityTypeConfiguration<T>`,
3. EF Core la levantara automaticamente.

No hace falta tocar de nuevo el `OnModelCreating`.

### `OnModelCreatingPartial(modelBuilder);`

Este metodo parcial quedo como punto de extension.

Viene bien cuando el contexto fue scaffolded o cuando queres dejar una puerta abierta para agregar logica extra sin modificar la estructura principal.

No es obligatorio usarlo, pero mantenerlo no rompe nada y conserva flexibilidad.

## 6. Que es `IEntityTypeConfiguration<T>`

Es una interfaz de EF Core disenada justamente para separar la configuracion de cada entidad.

Su forma general es:

```csharp
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        // configuracion de Usuario
    }
}
```

### Que significa `T`

La `T` representa el tipo de entidad que se esta configurando.

Por ejemplo:

- `IEntityTypeConfiguration<Usuario>` configura `Usuario`
- `IEntityTypeConfiguration<Producto>` configura `Producto`
- `IEntityTypeConfiguration<Cliente>` configura `Cliente`

Esto obliga a que cada clase tenga una unica responsabilidad: configurar una sola entidad.

## 7. Que hace el metodo `Configure`

El metodo:

```csharp
public void Configure(EntityTypeBuilder<Usuario> entity)
```

es el lugar donde se define todo el mapeo Fluent API de esa entidad.

EF Core lo invoca automaticamente cuando `ApplyConfigurationsFromAssembly(...)` encuentra la clase.

Ese metodo recibe un `EntityTypeBuilder<Usuario>`, que es el objeto especifico con el que se configura la entidad `Usuario`.

## 8. Que es `EntityTypeBuilder<T>`

`EntityTypeBuilder<T>` es el "constructor fluido" de una entidad.

Permite declarar:

- tabla,
- clave primaria,
- propiedades,
- longitudes,
- precision numerica,
- valores por defecto,
- relaciones,
- indices,
- seeds,
- comentarios,
- restricciones.

Es el objeto central dentro de cada configuracion.

## 9. Explicacion didactica de una clase de configuracion

Tomemos como ejemplo `UsuarioConfiguration`.

```csharp
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> entity)
    {
        entity.HasKey(e => e.IdUsuario).HasName("usuario_pkey");

        entity.ToTable("usuario");

        entity.HasIndex(e => e.Usuario1, "usuario_usuario_key").IsUnique();

        entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
        entity.Property(e => e.Apellido)
            .HasMaxLength(100)
            .HasColumnName("apellido");
        entity.Property(e => e.Email)
            .HasMaxLength(100)
            .HasColumnName("email");
        entity.Property(e => e.Root)
            .HasColumnName("root")
            .HasDefaultValue(false);
    }
}
```

### `entity.HasKey(e => e.IdUsuario)`

Define la clave primaria de la entidad.

En este caso:

- la PK en C# es `IdUsuario`
- EF Core la mapeara como clave primaria de la tabla

### `.HasName("usuario_pkey")`

Asigna el nombre real de la constraint en la base.

Esto es util cuando:

- la base ya existe,
- queres respetar nombres especificos,
- necesitas mantener consistencia con PostgreSQL o con migraciones previas.

### `entity.ToTable("usuario")`

Le dice a EF Core que la entidad `Usuario` se mapea a la tabla `usuario`.

Sin esta linea, EF Core podria intentar inferir otro nombre segun convenciones.

### `entity.HasIndex(e => e.Usuario1, "usuario_usuario_key").IsUnique()`

Aca se define un indice sobre la columna asociada a `Usuario1`.

Ademas:

- se le asigna el nombre `usuario_usuario_key`
- se marca como unico con `IsUnique()`

Eso significa que no puede haber dos usuarios con el mismo nombre de usuario.

### `entity.Property(e => e.Apellido)`

Selecciona una propiedad puntual de la entidad para configurarla.

Todo lo que sigue en cadena aplica sobre esa propiedad.

### `.HasColumnName("apellido")`

Mapea la propiedad C# al nombre real de la columna SQL.

Ejemplo:

- propiedad en C#: `Apellido`
- columna en PostgreSQL: `apellido`

### `.HasMaxLength(100)`

Define la longitud maxima admitida para ese campo.

Esto ayuda a:

- reflejar correctamente el diseno de la base,
- validar longitud desde el modelo,
- generar migraciones consistentes.

### `.HasDefaultValue(false)`

Indica un valor por defecto en la base de datos o en el modelo para esa columna.

En este caso, `root` tendra `false` por defecto.

## 10. Otras piezas comunes que aparecen en las configuraciones

Durante este refactor no todas las entidades usan lo mismo. Algunas usan configuraciones mas simples y otras mas avanzadas.

### `HasPrecision(10, 2)`

Se usa para tipos numericos decimales.

Ejemplo:

```csharp
entity.Property(e => e.Total)
    .HasPrecision(10, 2);
```

Esto significa:

- 10 digitos en total,
- 2 de ellos despues de la coma.

Es importante para importes, precios y saldos.

### `HasDefaultValueSql("CURRENT_TIMESTAMP")`

Define un valor por defecto generado por SQL.

Ejemplo:

```csharp
entity.Property(e => e.Fecha)
    .HasDefaultValueSql("CURRENT_TIMESTAMP");
```

Eso hace que la base cargue automaticamente la fecha actual al insertar.

### `ValueGeneratedOnAdd()`

Indica que el valor se genera al insertar.

Se usa tipicamente para IDs identity o columnas generadas en alta.

### `ValueGeneratedNever()`

Indica que el valor no debe generarse automaticamente.

Esto sirve cuando el valor lo controla la aplicacion o cuando se seed-ea manualmente.

### `HasOne(...).WithMany(...).HasForeignKey(...)`

Esto configura relaciones entre entidades.

Ejemplo conceptual:

```csharp
entity.HasOne(d => d.IdCategoriaNavigation)
    .WithMany(p => p.Productos)
    .HasForeignKey(d => d.IdCategoria);
```

Significa:

- `Producto` tiene una categoria,
- una `Categorium` puede tener muchos productos,
- la FK es `IdCategoria`.

### `OnDelete(DeleteBehavior.SetNull / Restrict / ClientSetNull)`

Define que pasa cuando se elimina el registro relacionado.

Ejemplos:

- `Restrict`: no permite borrar si existen dependencias.
- `SetNull`: pone la FK en `null`.
- `ClientSetNull`: EF intenta representar ese comportamiento del lado cliente.

### `HasConstraintName("...")`

Permite fijar el nombre exacto de una foreign key o constraint.

Esto es importante cuando:

- la base ya existe,
- queres evitar que EF invente nombres distintos,
- necesitas consistencia con migraciones previas.

### `HasCheckConstraint("...", "...")`

Se usa para restricciones de negocio a nivel base.

Ejemplo:

```csharp
entity.ToTable(t => t.HasCheckConstraint(
    "CK_configuracion_cc_monto_limite",
    "monto_limite > 0"));
```

Esto obliga a que `monto_limite` sea mayor que cero.

### `HasComment("...")`

Agrega comentarios descriptivos al esquema de base.

Es util para documentar el proposito de tablas o columnas.

### `HasData(...)`

Se usa para datos semilla.

En este proyecto aparece, por ejemplo, en configuraciones como:

- `TipoMovimientoStockConfiguration`
- `UnidadMedidaConfiguration`

Con esto EF Core sabe que esos registros forman parte del modelo y deben existir.

### `UseIdentityByDefaultColumn()` y `HasIdentityOptions(...)`

Son configuraciones especificas de PostgreSQL para columnas identity.

Permiten indicar como se genera automaticamente un ID.

## 11. Ventajas de este refactor

### 11.1. Responsabilidad unica

Cada archivo configura una sola entidad.

Esto hace que:

- el codigo sea mas entendible,
- el mantenimiento sea mas ordenado,
- el contexto no acumule demasiadas responsabilidades.

### 11.2. Mejor legibilidad

Si queres revisar como se mapea `Usuario`, abris `UsuarioConfiguration.cs`.

No necesitas recorrer cientos de lineas dentro del `DbContext`.

### 11.3. Mejor mantenibilidad

Cuando una entidad cambia:

- modificas su configuracion puntual,
- sin tocar la configuracion de las demas.

Esto reduce errores colaterales.

### 11.4. Menos conflictos en Git

Antes, multiples cambios sobre distintas entidades terminaban en el mismo archivo: `VentaStockContext.cs`.

Ahora:

- una persona puede tocar `ProductoConfiguration.cs`,
- otra `ClienteConfiguration.cs`,
- otra `UsuarioConfiguration.cs`.

Eso disminuye conflictos de merge.

### 11.5. Escalabilidad

A medida que el proyecto crece, esta organizacion soporta mucho mejor nuevas entidades y nuevas reglas.

Un `DbContext` de cientos o miles de lineas se vuelve inmanejable.

En cambio, configuraciones separadas escalan de forma natural.

### 11.6. Mejor revision tecnica

En una revision de codigo:

- es mas facil detectar cambios de esquema,
- se entiende mas rapido que entidad fue modificada,
- se puede razonar mejor sobre relaciones, constraints e indices.

### 11.7. Mas alineado con EF Core

Este enfoque esta alineado con las practicas mas limpias de EF Core para proyectos medianos y grandes.

No cambia la funcionalidad, pero mejora mucho la estructura.

## 12. Que no cambio con este refactor

Es importante entender que este refactor fue estructural, no funcional.

No cambio:

- la logica de negocio,
- la forma en que se usan los repositorios o servicios,
- la forma en que se inyecta `VentaStockContext`,
- el esquema esperado de la base,
- el comportamiento de las entidades ya configuradas.

Lo que cambio fue solamente la organizacion del codigo de configuracion.

## 13. Flujo de trabajo a partir de ahora

Cuando agregues una nueva entidad o quieras modificar una existente, el flujo recomendado seria:

### Caso 1: nueva entidad

1. Crear o actualizar la clase de modelo.
2. Agregar su `DbSet` al `VentaStockContext`.
3. Crear una clase `NombreEntidadConfiguration`.
4. Implementar `IEntityTypeConfiguration<NombreEntidad>`.
5. Configurar tabla, claves, propiedades y relaciones dentro de `Configure`.
6. Crear la migracion si corresponde.

### Caso 2: cambiar una entidad existente

1. Buscar el archivo `XxxConfiguration.cs`.
2. Modificar solo esa configuracion.
3. Generar migracion si el cambio afecta la base.

### Ventaja

No hace falta tocar `OnModelCreating` cada vez, porque `ApplyConfigurationsFromAssembly(...)` detecta automaticamente la nueva clase.

## 14. Nota especifica de este proyecto

En este proyecto existen algunos nombres de entidades que tambien aparecen como namespaces o carpetas del dominio, por ejemplo:

- `ListaPrecio`
- `Proveedor`
- `CompraProveedor`

Por eso, en algunos lugares del contexto se usaron tipos totalmente calificados, por ejemplo:

```csharp
public virtual DbSet<global::proyecto_venta_stock.Models.ListaPrecio> ListaPrecios { get; set; }
```

Esto no cambia el comportamiento de EF Core.

Solamente evita ambiguedades del compilador cuando un nombre de tipo coincide con un namespace del proyecto.

## 15. Conclusion

Este refactor mejora la arquitectura de persistencia sin cambiar la logica funcional del sistema.

La idea principal fue pasar de un contexto centralizado y pesado a un modelo distribuido por entidad, donde:

- el `DbContext` queda limpio,
- cada entidad tiene su propia configuracion,
- EF Core las descubre automaticamente,
- el codigo queda mas claro, mantenible y preparado para crecer.

En resumen:

- `VentaStockContext` ahora define el punto de acceso a la base y la carga global del modelo.
- Cada clase `XxxConfiguration` define el mapeo detallado de una sola entidad.
- `ApplyConfigurationsFromAssembly(...)` conecta ambas piezas y hace que EF Core arme el modelo completo de manera automatica.

Ese es el nucleo conceptual del refactor.
