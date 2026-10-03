# Manga Tracker

Manga Tracker es una aplicación web full-stack para gestionar una colección física de manga.

La aplicación permite buscar series y ediciones usando Comic Vine, importar una edición concreta a tu colección personal, marcar qué tomos tienes comprados y consultar qué tomos te faltan. El objetivo del proyecto no es solo construir una aplicación CRUD, sino demostrar una arquitectura limpia, mantenible y cercana a un proyecto real: autenticación, integración con APIs externas, sincronización de datos, gestión de errores, envío real de correos y frontend moderno con Angular.

## Demo

* Frontend: https://manga-tracker-teal.vercel.app
* API: https://mangatracker.runasp.net
* Repositorio: https://github.com/Yuren92/MangaTracker

## Funcionalidades principales

* Registro de usuarios.
* Confirmación de cuenta por email.
* Login con JWT.
* Recuperación y restablecimiento de contraseña por email.
* Envío real de correos mediante SMTP/Brevo.
* Búsqueda de series/ediciones en Comic Vine.
* Vista previa de una edición antes de importarla.
* Importación de ediciones desde Comic Vine.
* Importación de tomos/issues asociados a una edición.
* Gestión de colecciones personales por usuario.
* Vista global de colecciones.
* Vista de detalle de una colección.
* Marcado individual de tomos como comprados o pendientes.
* Marcado masivo de todos los tomos de una colección como comprados.
* Vista global de tomos pendientes.
* Marcado de tomos como comprados directamente desde la pantalla de pendientes.
* Eliminación de colecciones del usuario.
* Sincronización automática de colecciones con Comic Vine.
* Control de errores centralizado con `ProblemDetails`.
* Rate limiting en endpoints sensibles y en endpoints que consumen APIs externas.
* Health check público.
* Frontend responsive.
* Tests unitarios y de integración (API real + SQL Server) para autenticación, autorización, importación y sincronización.

## Stack tecnológico

### Backend

* ASP.NET Core
* C#
* Entity Framework Core
* SQL Server
* JWT Bearer Authentication
* ASP.NET Core Rate Limiting
* ASP.NET Core Health Checks
* SMTP para envío de correos
* xUnit
* FluentAssertions
* NSubstitute

### Frontend

* Angular
* Standalone components
* Signals
* Control flow moderno con `@if` / `@for`
* TypeScript
* SCSS
* Build desplegado en Vercel

### APIs externas

* Comic Vine API
* Brevo SMTP

### Infraestructura

* Frontend desplegado en Vercel.
* Backend desplegado en MonsterASP.NET.
* Base de datos SQL Server remota.
* Frontend y API comunicados directamente por HTTPS, con CORS restringido al origen del frontend.

## Estructura del proyecto

```txt
src/
  MangaTracker.Api
  MangaTracker.Application
  MangaTracker.Domain
  MangaTracker.Infrastructure

frontend/
  manga-tracker-web

tests/
  MangaTracker.Tests
```

## Arquitectura

El backend sigue una arquitectura por capas inspirada en Clean Architecture y DDD.

### `MangaTracker.Domain`

Contiene el modelo de dominio y las entidades principales de negocio.

Entidades principales:

* `User`
* `UserToken`
* `Series`
* `Edition`
* `Tome`
* `UserCollection`
* `UserOwnedTome`

El dominio encapsula reglas básicas como validaciones de entidades, normalización de datos y actualización de detalles de ediciones y tomos.

### `MangaTracker.Application`

Contiene los casos de uso de la aplicación.

Incluye:

* Commands
* Queries
* Handlers
* DTOs
* Excepciones de aplicación
* Abstracciones de repositorios
* Abstracciones para servicios externos

Ejemplos:

* Registro de usuario.
* Login.
* Confirmación de email.
* Recuperación de contraseña.
* Importación de volúmenes desde Comic Vine.
* Marcado de tomos como comprados.
* Eliminación de colecciones.
* Sincronización de colecciones.
* Consulta de tomos pendientes.

Esta capa define las interfaces necesarias, pero no depende de detalles técnicos de infraestructura.

### `MangaTracker.Infrastructure`

Contiene las implementaciones técnicas.

Incluye:

* `DbContext` de Entity Framework Core.
* Repositorios SQL Server.
* Cliente HTTP para Comic Vine.
* Generación de JWT.
* Hashing de contraseñas.
* Hashing de tokens.
* Envío de correos por consola en desarrollo.
* Envío de correos por SMTP en producción.
* Servicio en background para limpieza de usuarios no confirmados y tokens expirados.

### `MangaTracker.Api`

Contiene la capa HTTP.

Incluye:

* Controllers.
* Configuración JWT.
* Configuración CORS.
* Rate limiting.
* Health checks.
* Middleware global de excepciones.
* Servicio de usuario actual basado en claims JWT.

Los controllers se mantienen deliberadamente finos. La lógica de negocio vive en `Application` y `Domain`.

## Modelo de dominio

La aplicación diferencia entre datos de catálogo y datos propios del usuario.

### `Series`

Representa la obra base, por ejemplo `One Piece`, `Berserk` o `Dragon Ball`.

### `Edition`

Representa una edición concreta de una serie. En términos de Comic Vine, está asociada normalmente a un `volume`.

Una misma serie puede tener varias ediciones según idioma, editorial, país o formato.

### `Tome`

Representa un tomo físico dentro de una edición. En Comic Vine, se importa a partir de un `issue`.

### `UserCollection`

Representa que un usuario sigue o colecciona una edición concreta.

### `UserOwnedTome`

Representa que un usuario posee un tomo concreto de una colección.

Esta separación permite que `Series`, `Edition` y `Tome` funcionen como catálogo compartido, mientras que `UserCollection` y `UserOwnedTome` contienen la información específica de cada usuario.

## Flujo de Comic Vine

El flujo principal de catálogo funciona así:

```txt
El usuario busca una serie
  -> El backend consulta Comic Vine
  -> El usuario elige una edición concreta
  -> El backend muestra una vista previa
  -> El usuario importa la edición
  -> El backend guarda Series, Edition y Tomes
  -> Se crea una UserCollection para el usuario autenticado
  -> El usuario marca los tomos que ya tiene comprados
  -> La aplicación calcula automáticamente los tomos pendientes
```

El frontend nunca llama directamente a Comic Vine. Todas las llamadas pasan por el backend.

Esto permite:

* Mantener privada la API key de Comic Vine.
* Normalizar los datos externos.
* Controlar rate limits.
* Centralizar errores.
* Evitar lógica de integración externa en Angular.

## Sincronización automática de colecciones

Manga Tracker incluye sincronización de colecciones con Comic Vine.

Cuando el usuario entra en “Mis colecciones”, la pantalla carga rápido usando los datos locales y, en paralelo, lanza una sincronización automática contra el backend.

El backend:

* Revisa las colecciones del usuario.
* Comprueba qué ediciones necesitan sincronización.
* Evita sincronizar repetidamente ediciones actualizadas recientemente.
* Consulta Comic Vine si es necesario.
* Añade tomos nuevos que no existían en la base de datos.
* Actualiza contadores de tomos.
* Permite que los nuevos tomos aparezcan automáticamente como pendientes.

Esto permite cubrir casos como:

```txt
El usuario tiene One Piece importado con 115 tomos.
Comic Vine añade el tomo 116.
La aplicación sincroniza la edición.
El tomo 116 aparece como pendiente.
```

## Autenticación y correos

La autenticación usa JWT.

El flujo de usuario incluye:

* Registro.
* Confirmación de email.
* Login.
* Cambio de contraseña.
* Recuperación de contraseña.
* Reset de contraseña mediante token seguro.

Revocación de sesiones: cada usuario tiene un `SecurityStamp` aleatorio que viaja como claim en el JWT. En cada petición autenticada se compara con el valor actual en base de datos (`SecurityStampValidator`). Cambiar o restablecer la contraseña rota el stamp, así que todos los tokens emitidos antes dejan de ser válidos al instante, sin esperar a que caduquen. El cambio de contraseña devuelve un token nuevo para mantener la sesión actual.

Enumeración de cuentas: ningún endpoint revela si un email está registrado. El registro responde siempre 202 con el mismo mensaje; si la cuenta ya existía, en lugar de crearla se envía al titular un aviso con enlace para restablecer la contraseña. La recuperación de contraseña y el login dan la misma respuesta para emails desconocidos, y el hash de la contraseña se calcula en todos los casos para que el tiempo de respuesta tampoco los delate.

Los correos se envían usando una abstracción:

```csharp
IEmailSender
```

Implementaciones:

* `ConsoleEmailSender`: usado solo en Development cuando no hay SMTP configurado.
* `SmtpEmailSender`: usado cuando hay configuración SMTP.

Esto permite usar logs en desarrollo y correos reales en producción.

## Seguridad y configuración

El proyecto usa configuración externa para secretos.

No deben subirse al repositorio:

* Connection strings reales.
* JWT secret keys.
* Comic Vine API keys.
* Credenciales SMTP.
* Passwords de base de datos.

Archivos sensibles recomendados fuera de Git:

```txt
appsettings.Development.json
appsettings.Production.json
```

El archivo `appsettings.json` solo contiene estructura base y valores vacíos de ejemplo.

La configuración crítica se valida al arrancar (`ValidateOnStart`): si falta la connection string, la clave JWT es menor de 256 bits, falta la API key de Comic Vine o las URLs no usan HTTPS fuera de Development, la aplicación no arranca. Fuera de Development también es obligatorio configurar SMTP: `ConsoleEmailSender` escribe en los logs los enlaces de confirmación y reseteo (que contienen tokens), así que solo se permite en local.

## Rate limiting

El backend aplica rate limiting en endpoints que pueden consumir recursos externos o sensibles.

Ejemplos:

* Búsqueda en catálogo.
* Vista previa de volúmenes.
* Importación desde Comic Vine.
* Sincronización de colecciones.

Esto ayuda a proteger la API y a no abusar de Comic Vine.

La partición depende del tipo de endpoint:

* Endpoints autenticados (catálogo, importación, sincronización): límite por usuario, a partir del id del JWT. No se puede falsificar y no depende de los proxies que haya delante de la API.
* Endpoints anónimos (login, registro, recuperación de contraseña): límite por IP del cliente.

Para que la IP sea la del cliente real detrás de un proxy, la cabecera `X-Forwarded-For` solo se acepta de los proxies configurados en `ForwardedHeaders:KnownProxies` / `ForwardedHeaders:KnownNetworks`. Si no hay ninguno configurado, la cabecera se ignora por completo: en ASP.NET, dejar esas listas vacías significa confiar en cualquiera, y eso permitiría a un atacante inventarse una IP nueva en cada petición para saltarse el límite.

## Gestión de errores

La API usa un middleware global de excepciones que transforma errores conocidos en respuestas `ProblemDetails`.

Ejemplos:

* `ValidationException` -> `400 Bad Request`
* `NotFoundException` -> `404 Not Found`
* `ConflictException` -> `409 Conflict`
* `DomainException` -> `400 Bad Request`
* Errores inesperados -> `500 Internal Server Error`

En Angular, los errores se procesan con un helper común para mostrar mensajes claros al usuario.

## Frontend

El frontend está construido con Angular moderno.

Características:

* Standalone components.
* Signals.
* Templates con `@if` y `@for`.
* Servicios separados para API.
* Interceptor JWT.
* Guards de autenticación.
* Diseño responsive.
* Cards unificadas para catálogo, colecciones, pendientes y detalle.
* Menú de usuario con cierre al hacer click fuera.
* Interceptor que solo envía el JWT a la propia API.

## Comunicación frontend-backend

El frontend (Vercel) llama directamente a la API por HTTPS (`https://mangatracker.runasp.net`), configurada en `environment.production.ts`. El backend solo acepta peticiones del origen del frontend mediante CORS (`Cors:AllowedOrigins`).

Al principio las llamadas pasaban por un rewrite de Vercel (`/api/*` → backend). Se eliminó por dos motivos:

* El backend ya sirve HTTPS, así que el proxy no aportaba cifrado.
* Detrás del proxy, el backend veía la IP de Vercel en todas las peticiones, de modo que el rate limiting por IP de los endpoints anónimos (login, registro) se compartía entre todos los usuarios. Vercel no publica una lista fija de IPs, así que no se podía configurar como proxy de confianza.

El interceptor de Angular solo añade el JWT a las peticiones dirigidas a la API, nunca a otros orígenes.

`vercel.json` mantiene únicamente el rewrite a `index.html`, necesario para que las rutas internas de Angular funcionen al recargar la página.

## Puesta en marcha local

### Requisitos

* .NET SDK
* Node.js
* npm
* SQL Server o SQL Server Express
* Angular CLI
* API key de Comic Vine

### Backend

Desde la raíz del proyecto:

```powershell
dotnet restore
dotnet build
```

Configura `appsettings.Development.json` en `src/MangaTracker.Api` con tus valores locales:

```json
{
  "ConnectionStrings": {
    "MangaTrackerDb": "Server=YOUR_SERVER\\SQLEXPRESS;Database=MangaTrackerDb;Trusted_Connection=True;TrustServerCertificate=True;"
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
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200"
    ]
  },
  "ComicVine": {
    "BaseUrl": "https://comicvine.gamespot.com/api/",
    "ApiKey": "YOUR_COMIC_VINE_API_KEY"
  },
  "Smtp": {
    "Host": "",
    "Port": 587,
    "Username": "",
    "Password": "",
    "FromEmail": "",
    "FromName": "Manga Tracker",
    "EnableSsl": true
  }
}
```

Aplica migraciones:

```powershell
dotnet ef database update `
  --project src\MangaTracker.Infrastructure\MangaTracker.Infrastructure.csproj `
  --startup-project src\MangaTracker.Api\MangaTracker.Api.csproj
```

Levanta la API:

```powershell
dotnet run --project src\MangaTracker.Api\MangaTracker.Api.csproj
```

Health check:

```txt
http://localhost:5243/health
```

### Frontend

Entra en el proyecto Angular:

```powershell
cd frontend/manga-tracker-web
npm install
npm start
```

La app local estará disponible en:

```txt
http://localhost:4200
```

## Tests

Para ejecutar los tests:

```powershell
dotnet test
```

Hay dos tipos:

* **Unitarios**: handlers de Application con dependencias sustituidas (NSubstitute), validación de configuración, `ComicVineClient` con un `HttpMessageHandler` falso y partición del rate limiting. El tiempo se inyecta con `TimeProvider` para poder probar cooldowns y ordenación.
* **Integración** (`tests/MangaTracker.Tests/Integration`): levantan la API real en memoria con `WebApplicationFactory` contra un SQL Server de verdad. Cada ejecución crea una base de datos nueva aplicando las migraciones y la borra al terminar. Solo se sustituyen los servicios externos (correo y Comic Vine). Cubren el flujo de autenticación (registro, confirmación, login, recuperación y cambio de contraseña, tokens de un solo uso, no enumeración de usuarios) y la autorización entre usuarios (un usuario no puede leer ni modificar colecciones de otro aunque conozca su id).

Los tests de integración usan LocalDB por defecto. Para usar otro servidor, define `MANGATRACKER_TEST_SQLSERVER` con una connection string sin base de datos. No se usa SQLite porque EF Core no traduce a SQLite las comparaciones de `DateTimeOffset` de las consultas de tokens.

## Despliegue

### Frontend

El frontend está desplegado en Vercel.

Configuración usada:

```txt
Root Directory: frontend/manga-tracker-web
Build Command: npm run build
Output Directory: dist/manga-tracker-web/browser
```

### Backend

El backend está desplegado en MonsterASP.NET mediante WebDeploy.

La base de datos usa SQL Server remoto.

Los secretos de producción se configuran en `appsettings.Production.json`, que no debe subirse al repositorio.

### Base de datos

Para generar script SQL de migraciones:

```powershell
dotnet ef migrations script --idempotent `
  --project src\MangaTracker.Infrastructure\MangaTracker.Infrastructure.csproj `
  --startup-project src\MangaTracker.Api\MangaTracker.Api.csproj `
  --output deploy\manga-tracker-migrations.sql
```

## Estado del proyecto

El proyecto incluye:

* Aplicación funcional en producción.
* Frontend desplegado.
* Backend desplegado.
* Base de datos remota.
* Registro y login reales.
* Confirmación de email real.
* Recuperación de contraseña real.
* Integración real con Comic Vine.
* Gestión de colecciones.
* Sincronización automática.
* Vista responsive.

## Próximas mejoras posibles

* Añadir imágenes al README.
* Mejorar filtros de búsqueda.
* Añadir ordenación avanzada de colecciones.
* Añadir dashboard/resumen inicial.
* Añadir favoritos o wishlist.
* Añadir paginación en sincronizaciones grandes.
* Añadir integración con más fuentes de catálogo.
* Añadir tests de integración para endpoints principales.

## Autor

Proyecto desarrollado por Pedro Ballesta Garres.
