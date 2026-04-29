# Manga Tracker

API REST hecha con ASP.NET Core para gestionar una colección personal de manga físico.

La aplicación permite:

- Registrarse e iniciar sesión con JWT.
- Confirmar email mediante token.
- Recuperar contraseña.
- Buscar mangas usando MyAnimeList.
- Ver detalles y recomendaciones de manga.
- Añadir mangas a una colección privada.
- Registrar tomos comprados.
- Calcular tomos faltantes.
- Separar la colección por usuario autenticado.

## Tecnologías

- ASP.NET Core
- C#
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- MyAnimeList API
- Clean Architecture / DDD básico
- xUnit
- Rate limiting
- Health checks

## Estructura del proyecto

```txt
src/
  MangaTracker.Api
  MangaTracker.Application
  MangaTracker.Domain
  MangaTracker.Infrastructure

tests/
  MangaTracker.Tests
```

## Capas

### MangaTracker.Domain

Contiene las reglas de negocio principales:

- Entidades:
  - User
  - UserToken
  - MangaCollectionItem
  - OwnedVolume

- Value Objects:
  - VolumeNumber

- Enums:
  - UserTokenType

### MangaTracker.Application

Contiene los casos de uso de la aplicación:

- Registro
- Login
- Confirmación de email
- Reenvío de confirmación de email
- Recuperación de contraseña
- Reset de contraseña
- Cambio de contraseña
- Obtener usuario actual
- Gestión de colección
- Búsqueda y detalle de manga en MyAnimeList

También contiene las interfaces que luego implementa Infrastructure.

### MangaTracker.Infrastructure

Contiene detalles técnicos:

- Entity Framework Core
- SQL Server
- Repositorios
- Cliente HTTP de MyAnimeList
- Generación de JWT
- Hash de contraseñas
- Hash de tokens
- Generación de tokens seguros
- Email sender simulado por logs
- Limpieza automática de tokens y usuarios no confirmados

### MangaTracker.Api

Contiene:

- Controllers
- Middleware global de errores
- Configuración de JWT
- CORS
- Rate limiting
- Health checks
- Servicio de usuario actual basado en JWT

## Funcionalidades

### Autenticación

- Registro de usuarios.
- Confirmación de email mediante token.
- Reenvío de email de confirmación.
- Login con JWT.
- Endpoint `/api/auth/me`.
- Recuperación de contraseña.
- Reset de contraseña.
- Cambio de contraseña estando autenticado.
- Tokens hasheados en base de datos.
- Tokens de un solo uso.
- Invalidación de tokens anteriores.
- Limpieza automática de tokens y usuarios no confirmados.

### Colección de manga

- Añadir manga a la colección usando `malId`.
- Listar colección del usuario autenticado.
- Ver detalle de una serie en colección.
- Añadir tomos comprados.
- Eliminar tomos comprados.
- Actualizar total manual de tomos.
- Calcular tomos faltantes.
- Separación multiusuario mediante `UserId`.

### MyAnimeList

- Buscar manga.
- Ver detalle de manga.
- Ver recomendaciones.
- Saber si un manga ya está en la colección del usuario.

## Endpoints principales

### Auth

```http
POST /api/auth/register
POST /api/auth/login
GET  /api/auth/me
POST /api/auth/confirm-email
POST /api/auth/resend-confirmation-email
POST /api/auth/forgot-password
POST /api/auth/reset-password
POST /api/auth/change-password
```

### MyAnimeList

```http
GET /api/mal/manga/search?query=berserk&limit=10
GET /api/mal/manga/{malId}
```

### Collection

```http
GET    /api/collection
GET    /api/collection/{id}
POST   /api/collection
POST   /api/collection/{id}/volumes
DELETE /api/collection/{id}/volumes/{volumeNumber}
PUT    /api/collection/{id}/total-volumes
```

### Health

```http
GET /health
```

## Configuración local

Crea este archivo:

```txt
src/MangaTracker.Api/appsettings.Development.json
```

Puedes usar como base el archivo:

```txt
src/MangaTracker.Api/appsettings.Development.example.json
```

Ejemplo:

```json
{
  "ConnectionStrings": {
    "MangaTrackerDb": "Server=YOUR_SERVER\\SQLEXPRESS;Database=MangaTrackerDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "MyAnimeList": {
    "BaseUrl": "https://api.myanimelist.net/v2/",
    "ClientId": "YOUR_MAL_CLIENT_ID"
  },
  "Jwt": {
    "Issuer": "MangaTracker",
    "Audience": "MangaTracker",
    "SecretKey": "YOUR_LONG_DEVELOPMENT_SECRET_KEY",
    "ExpirationMinutes": 120
  },
  "AuthLinks": {
    "FrontendBaseUrl": "http://localhost:4200"
  },
  "AuthCleanup": {
    "IntervalHours": 6,
    "DeleteUnconfirmedUsersAfterHours": 48
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200"
    ]
  }
}
```

## Ejecutar localmente

Desde la raíz del proyecto:

```powershell
dotnet restore
dotnet build
dotnet run --project src\MangaTracker.Api\MangaTracker.Api.csproj
```

La API se levanta por defecto en:

```txt
http://localhost:5243
```

Comprobar estado:

```http
GET http://localhost:5243/health
```

## Migraciones

Crear una migración:

```powershell
dotnet ef migrations add NombreMigracion --project src\MangaTracker.Infrastructure\MangaTracker.Infrastructure.csproj --startup-project src\MangaTracker.Api\MangaTracker.Api.csproj --output-dir Persistence\Migrations
```

Aplicar migraciones:

```powershell
dotnet ef database update --project src\MangaTracker.Infrastructure\MangaTracker.Infrastructure.csproj --startup-project src\MangaTracker.Api\MangaTracker.Api.csproj
```

## Tests

Ejecutar tests:

```powershell
dotnet test
```

## Seguridad implementada

- Contraseñas hasheadas.
- JWT Bearer Authentication.
- Confirmación de email.
- Recuperación de contraseña.
- Cambio de contraseña autenticado.
- Tokens hasheados en base de datos.
- Tokens de un solo uso.
- Invalidación de tokens anteriores.
- Rate limiting en endpoints sensibles.
- Validación de email.
- Validación mínima de contraseña.
- Limpieza automática de usuarios no confirmados.
- CORS configurable.
- Errores controlados con `ProblemDetails`.

## Variables de entorno para producción

ASP.NET Core permite sobreescribir configuración usando `__`.

Ejemplos:

```txt
ConnectionStrings__MangaTrackerDb=...
MyAnimeList__ClientId=...
Jwt__SecretKey=...
AuthLinks__FrontendBaseUrl=...
Cors__AllowedOrigins__0=https://tu-frontend.com
```

## Próximos pasos

- Crear frontend en Angular.
- Añadir soporte opcional para PostgreSQL.
- Sustituir el email simulado por un proveedor real.
- Preparar Docker.
- Publicar backend y frontend en hosting gratuito.
