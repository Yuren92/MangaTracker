# Manga Tracker · frontend

Aplicación Angular 21 de [Manga Tracker](../../README.md). Habla con la API por HTTPS; el JWT solo se envía a la propia API.

## Comandos

```bash
npm install
npm start          # http://localhost:4200, contra la API en http://localhost:5243
npm test           # tests unitarios (Vitest)
npm run build      # build de producción en dist/manga-tracker-web/browser
npm run e2e        # tests end-to-end (Playwright), ver el README principal
```

## Estructura

```txt
src/app/
  core/       sesión (AuthState, guards), interceptor HTTP, traducción de errores, layout
  features/   una carpeta por área: auth, catalog, collections (páginas, servicios, modelos)
  shared/     componentes y validadores reutilizables
```

## Decisiones

* **Standalone components, signals y control flow** (`@if`, `@for`); sin NgModules.
* **Rutas con carga diferida** (`loadComponent`): cada pantalla se descarga la primera vez que se visita.
* **Textos de la interfaz en el frontend.** La API responde en inglés como contrato; `core/http/api-error.ts` traduce los errores que el usuario puede resolver y sustituye el resto por el mensaje de la pantalla, así nunca aparece texto en inglés.
* **Validación duplicada a propósito.** Las reglas de contraseña se comprueban también en el formulario para avisar antes de enviar; la API sigue siendo quien decide.
* **Interceptor** que añade el token solo a peticiones de `environment.apiUrl` y cierra la sesión ante un 401 (token caducado o revocado).
