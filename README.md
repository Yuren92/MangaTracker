# Manga Tracker

Manga Tracker is a full-stack web application for managing a personal physical manga collection.

The project is built with **ASP.NET Core** and **Angular**, using Comic Vine as the external catalog source. Users can search manga editions, preview Comic Vine volumes, import an edition into their personal collection, track owned tomes, and see which tomes are still pending.

The goal of this project is not just to build a CRUD application, but to demonstrate a clean, maintainable architecture with authentication, external API integration, rate limiting, error handling, and a modern Angular frontend.

---

## Main features

- User registration and login with JWT authentication.
- Email confirmation flow using secure tokens.
- Password recovery and reset flow.
- Authenticated user context through JWT claims.
- Comic Vine catalog search.
- Comic Vine volume preview before import.
- Import Comic Vine volumes as collection editions.
- Import tome/issue data from Comic Vine.
- Track owned and pending tomes per user.
- View all user collections.
- View detailed collection progress.
- Mark individual tomes as owned or pending.
- Mark all tomes in a collection as owned.
- Global pending tomes view.
- Centralized API error handling with `ProblemDetails`.
- Rate limiting for sensitive and external API endpoints.
- Health check endpoint.
- Unit tests for the core Comic Vine import use case.

---

## Tech stack

### Backend

- ASP.NET Core
- C#
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- ASP.NET Core Rate Limiting
- ASP.NET Core Health Checks
- xUnit
- FluentAssertions
- NSubstitute

### Frontend

- Angular
- Standalone components
- Signals
- Modern control flow with `@if` / `@for`
- No NgModules
- Zoneless setup
- TypeScript
- SCSS

### External API

- Comic Vine API

---

## Project structure

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

---

## Architecture overview

The backend follows a layered architecture inspired by Clean Architecture and DDD principles.

### `MangaTracker.Domain`

Contains the core domain model and business entities.

Main entities:

- `User`
- `UserToken`
- `Series`
- `Edition`
- `Tome`
- `UserCollection`
- `UserOwnedTome`

### `MangaTracker.Application`

Contains application use cases, commands, queries, handlers, DTOs, and abstractions.

This layer defines interfaces such as repositories and external API clients, but does not depend on infrastructure details.

Examples:

- Authentication use cases.
- Collection use cases.
- Comic Vine import use case.
- `IComicVineClient` abstraction.
- Repository abstractions.

### `MangaTracker.Infrastructure`

Contains technical implementations.

Examples:

- Entity Framework Core `DbContext`.
- SQL Server repositories.
- Comic Vine HTTP client.
- JWT token generation.
- Password hashing.
- Token hashing.
- Email sender implementation.
- Background cleanup service for expired tokens and unconfirmed users.

### `MangaTracker.Api`

Contains the HTTP API layer.

Examples:

- Controllers.
- JWT configuration.
- CORS configuration.
- Rate limiting policies.
- Health checks.
- Global exception handling middleware.
- Current user service based on authenticated JWT claims.

Controllers are intentionally kept thin. Business logic lives in the application and domain layers.

---

## Domain model

The application models manga collections around physical editions and tomes.

### `Series`

Represents the base work, for example `One Piece` or `Berserk`.

### `Edition`

Represents a concrete publication/edition of a series, usually linked to a Comic Vine volume.

Different publishers, languages, or publication formats can be represented as different editions.

### `Tome`

Represents a physical volume/tome inside an edition. In Comic Vine terms, this is imported from an `issue`.

### `UserCollection`

Represents that a user is following or collecting a specific edition.

### `UserOwnedTome`

Represents that a user owns a specific tome from a collection.

This allows the application to distinguish between catalog data and user-specific ownership data.

---

## Comic Vine flow

The current catalog flow is based on Comic Vine.

```txt
User searches catalog
  -> Backend searches Comic Vine volumes
  -> User selects a volume
  -> Backend previews the volume and its issues
  -> User imports the volume
  -> Backend stores Series, Edition and Tomes
  -> UserCollection is created for the authenticated user
  -> User marks owned tomes
  -> Pending tomes are calculated from the stored edition data
```

The Angular frontend never calls Comic Vine directly. All external API calls go through the ASP.NET Core backend.

This keeps the Comic Vine API key private and allows the backend to normalize external data into the internal domain model.

---

## External API protection

Comic Vine requests are protected at the backend level.

The API uses rate limiting policies for endpoints that depend on external APIs:

| Policy | Limit | Used by |
|---|---:|---|
| `external-api` | 30 requests per minute per IP | Catalog search and volume preview |
| `comic-vine-import` | 5 requests per minute per IP | Comic Vine volume import |

The import endpoint has a stricter limit because importing a volume can trigger multiple internal Comic Vine requests to fetch tome/issue details.

Rate limit responses return `ProblemDetails` with HTTP `429 Too Many Requests`.

---

## Backend endpoints

### Authentication

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

### Catalog

```http
GET  /api/catalog/search?query=one%20piece
POST /api/catalog/comic-vine/volumes/preview
```

### Collections

```http
POST   /api/collections/import-comic-vine-volume
GET    /api/collections
GET    /api/collections/{collectionId}
GET    /api/collections/pending-tomes
POST   /api/collections/{collectionId}/tomes/{tomeId}/owned
DELETE /api/collections/{collectionId}/tomes/{tomeId}/owned
POST   /api/collections/{collectionId}/tomes/owned-all
```

### Health

```http
GET /health
```

---

## Frontend routes

```txt
/                         -> redirects to /collections
/catalog/search
/collections
/collections/:collectionId
/collections/pending-tomes
```

Main frontend pages:

- `CatalogSearchPage`
- `UserCollectionsPage`
- `UserCollectionDetailPage`
- `PendingTomesPage`

The Angular services use `environment.apiUrl` to call the backend.

---

## Local configuration

Create a local development settings file:

```txt
src/MangaTracker.Api/appsettings.Development.json
```

Use this file as a reference:

```txt
src/MangaTracker.Api/appsettings.Example.json
```

Example configuration:

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
  "AuthCleanup": {
    "IntervalHours": 6,
    "DeleteUnconfirmedUsersAfterHours": 48
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200"
    ]
  },
  "ComicVine": {
    "BaseUrl": "https://comicvine.gamespot.com/api/",
    "ApiKey": "YOUR_COMIC_VINE_API_KEY"
  }
}
```

Do not commit real secrets. Local development settings should stay ignored by Git.

---

## Running the backend locally

From the repository root:

```powershell
dotnet restore
dotnet build
dotnet run --project src\MangaTracker.Api\MangaTracker.Api.csproj
```

Check the API health endpoint:

```http
GET http://localhost:5243/health
```

Depending on your local launch profile, the API port may be different.

---

## Running the frontend locally

From the Angular project folder:

```powershell
cd frontend\manga-tracker-web
npm install
npm start
```

The frontend is expected to run at:

```txt
http://localhost:4200
```

Make sure the frontend environment points to the backend API URL.

---

## Database migrations

Create a migration:

```powershell
dotnet ef migrations add MigrationName --project src\MangaTracker.Infrastructure\MangaTracker.Infrastructure.csproj --startup-project src\MangaTracker.Api\MangaTracker.Api.csproj --output-dir Persistence\Migrations
```

Apply migrations:

```powershell
dotnet ef database update --project src\MangaTracker.Infrastructure\MangaTracker.Infrastructure.csproj --startup-project src\MangaTracker.Api\MangaTracker.Api.csproj
```

---

## Tests

Run all tests:

```powershell
dotnet test
```

The test project currently focuses on the application layer and includes unit tests for the Comic Vine volume import use case.

Covered scenarios include:

- Invalid user id.
- Invalid Comic Vine API detail URL.
- Missing Comic Vine volume.
- Import limit validation.
- Successful import of a new edition with user collection creation.

---

## Security and reliability features

- Password hashing.
- JWT Bearer Authentication.
- Authenticated current user service.
- Email confirmation tokens.
- Password reset tokens.
- Hashed user tokens in the database.
- One-time token usage.
- Previous token invalidation.
- Automatic cleanup of expired tokens and unconfirmed users.
- Configurable CORS.
- Health checks.
- Rate limiting for auth-sensitive and external API endpoints.
- Stricter rate limiting for Comic Vine imports.
- Centralized exception handling.
- `ProblemDetails` responses for API errors.
- Comic Vine API key kept server-side.
- Comic Vine API detail URL validation.
- Configured timeout and User-Agent for the Comic Vine HTTP client.

---

## Notable technical decisions

### Comic Vine instead of MyAnimeList

The project initially explored MyAnimeList, but the current version uses Comic Vine because the application is focused on physical manga collections.

Comic Vine provides volume and issue data, which maps better to physical editions and tomes.

### Backend as external API boundary

The frontend does not call Comic Vine directly. The backend owns external API integration, secrets, normalization, rate limiting, and error handling.

### Sequential issue import

Issue details are fetched sequentially during volume import to avoid overwhelming the external Comic Vine API and to keep imports predictable.

### Edition-based collection model

Users do not collect an abstract manga entry directly. They collect a specific edition, and ownership is tracked at tome level.

---

## Current limitations

- Email sending is currently implemented with a development-friendly sender.
- Comic Vine import is synchronous.
- Importing very large volumes is limited to protect the application and the external API.
- The current edition grouping strategy is intentionally simple for V1.
- Frontend end-to-end tests are not included yet.

---

## Future improvements

- Add production email provider integration.
- Add more backend unit tests around collection ownership and domain rules.
- Add frontend tests.
- Add Docker support.
- Add CI pipeline for build and tests.
- Add deployment documentation.
- Improve edition matching and duplicate detection.
- Add screenshots or GIFs to the README.
- Add richer collection statistics.

---

## Purpose

This project is intended as a portfolio application to demonstrate practical full-stack development skills with ASP.NET Core, Angular, clean architecture, authentication, external API integration, and maintainable application design.
