# NovaBooks

Sistema ERP/POS para una librería física: catálogo de libros, clientes, usuarios, roles y permisos.

## Tecnologías

- .NET 10
- ASP.NET Core Web API con Identity y JWT
- Blazor Interactive Server con MudBlazor
- Entity Framework Core sobre SQL Server
- xUnit, bUnit y SQLite en memoria para las pruebas

## Estructura

| Proyecto | Descripción |
|---|---|
| `NovaBooks.Domain` | Entidades del negocio |
| `NovaBooks.Application` | DTOs, interfaces y reglas de validación |
| `NovaBooks.Infrastructure` | EF Core, migraciones, servicios, seguridad y datos iniciales |
| `NovaBooks.Api` | API REST protegida con JWT y permisos |
| `NovaBooks.Web` | Interfaz web (Blazor + MudBlazor) |
| `NovaBooks.UnitTests` | Pruebas unitarias |
| `NovaBooks.IntegrationTests` | Pruebas de la API y de componentes Web |

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Express o Developer)
- Visual Studio 2026 o VS Code (opcional)

## Puesta en marcha

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/HernandezJP/NovaBooks.git
   cd NovaBooks
   ```
2. Ajustar la cadena de conexión en `NovaBooks.Api/appsettings.json` (`ConnectionStrings:DefaultConnection`) con el servidor y la contraseña de tu SQL Server.
3. Ejecutar la API y la Web (en Visual Studio: inicio de varios proyectos):
   ```bash
   dotnet run --project NovaBooks.Api --launch-profile https
   dotnet run --project NovaBooks.Web --launch-profile https
   ```
4. Abrir `https://localhost:7250`.

En el primer arranque, la API aplica las migraciones y carga los datos iniciales.

| Servicio | URL |
|---|---|
| API | https://localhost:7071 |
| Web | https://localhost:7250 |

## Datos iniciales

Se cargan siempre:
- Catálogos base: idiomas, formatos, impuestos, tipos de identificación y listas de precios.
- Roles y permisos.
- El usuario administrador. Sus credenciales están en `IdentitySeed:Administrator`, dentro de `NovaBooks.Api/appsettings.Development.json`.

Solo en Development se carga además un catálogo de demostración: 10 editoriales, 18 autores y 20 libros con precios y portadas. No se crean clientes de ejemplo.

> ⚠️ Las credenciales y la clave JWT del repositorio son solo para desarrollo. Cámbialas en cualquier otro entorno.

## Funcionalidades del Sprint 1

- Inicio de sesión con JWT y bloqueo temporal por intentos fallidos. Los cambios de estado, contraseña o permisos se aplican de inmediato a las sesiones abiertas.
- Usuarios, roles y permisos, con el rol Administrador protegido.
- Menú y páginas según los permisos del usuario, validados en la Web y en la API.
- Clientes (personas naturales y jurídicas) con código automático, contactos y borrado lógico.
- Libros con validación de ISBN, autores, categorías, historial de precios, costo de referencia y portada.
- Autores y editoriales.

## Pruebas

```bash
dotnet test NovaBooks.slnx
```

Las pruebas de integración levantan la API en memoria con SQLite y no tocan la base de datos real.

## Migraciones

```bash
dotnet ef migrations add NombreDescriptivo --project NovaBooks.Infrastructure
dotnet ef migrations has-pending-model-changes --project NovaBooks.Infrastructure
```

Reglas del proyecto:
- No borrar ni modificar migraciones ya aplicadas.
- Usar borrado lógico.
- Mantener `ResetAndSeedOnStartup` en `false`.
