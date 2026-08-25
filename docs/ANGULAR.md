# Angular

This file explains the `web/` frontend. Read it before you change, run, or
test the Angular app.

## Why Angular

`web/` is the demo UI. It uploads a document, shows the parsed outline, and
runs the chat that answers questions about it (see `docs/DEMO.md`).

The project chose Angular for three reasons.

1. **Fast start with the CLI.** `ng new` scaffolds a working app, a test
   runner, and a build pipeline in one command. No separate bundler or test
   framework setup is needed.
2. **One batteries-included framework.** Components, routing, forms, and
   HTTP all ship in `@angular/*` packages with one version number. The demo
   does not need to pick and wire separate libraries for each concern.
3. **Structure that matches a small, typed backend client.** The API client
   (`context-smith-api.ts`) is one injectable service with typed methods.
   Angular's dependency injection and strict TypeScript config keep that
   client and its consumers in sync without extra state-management
   libraries.

The app uses modern Angular: standalone components (no `NgModule`), the
`@angular/build` esbuild-based builder, and Vitest as the unit test runner —
not the older Karma/Jasmine setup.

## Project layout

```
web/
├── src/
│   ├── app/
│   │   ├── app.ts               # Root standalone component
│   │   ├── app.html / app.css   # Root component template and styles
│   │   ├── app.config.ts        # Application-wide providers (HTTP client, error listeners)
│   │   ├── context-smith-api.ts # Typed HTTP client for ContextSmith.Api
│   │   ├── models.ts            # Shared request/response types
│   │   └── document-outline.ts  # Outline rendering component
│   ├── main.ts                  # Bootstraps the app
│   └── index.html
├── angular.json                 # CLI project and build configuration
├── package.json
└── tsconfig*.json
```

`context-smith-api.ts` calls the API at `http://localhost:5010`, hardcoded.
Run `ContextSmith.Api` on that port before using the app (see the root
`README.md`).

## How to run

Needs Node.js 22.22.3+, 24.15.0+, or 26+ — see
[docs/PREREQUISITES.md](PREREQUISITES.md).

```bash
cd web
npm install   # first time only, or after a package.json change
npm start     # same as `ng serve`
```

Open http://localhost:4200. The app reloads on every source file change.
Start `ContextSmith.Api` first (`dotnet run --project src/ContextSmith.Api`
from the repository root) — the frontend has no offline mode.

## How to develop

Generate a new component with the CLI, from `web/`:

```bash
ng generate component component-name
```

Format code with Prettier before committing:

```bash
npx prettier --write .
```

`web/.prettierrc` holds the formatting rules. `tsconfig.json` turns on
strict compiler options (`noImplicitOverride`, `noImplicitReturns`,
`strictInjectionParameters`, `strictInputAccessModifiers`, and more) —
fix a type error rather than loosen these options.

## How to test

Unit tests run through Vitest, via the Angular CLI:

```bash
cd web
ng test
```

`app.spec.ts` is the current test — it renders the root component and
checks it creates and shows the `ContextSmith` heading. Add a `.spec.ts`
file next to each component or service you add.

There is no end-to-end test suite yet. Angular CLI does not include one by
default; `ng e2e` prompts you to pick a framework if you set one up.

## How to build

```bash
cd web
ng build
```

Output goes to `web/dist/`. The production configuration is the default —
it enables output hashing and enforces the bundle-size budgets set in
`angular.json` (500 kB warning / 1 MB error for the initial bundle).

## Dev, test, and prod environments

`angular.json` already defines `development` and `production` build
configurations (see the `serve` and `build` targets). `ng serve` uses
`development` by default; `ng build` uses `production` by default. That is
the only environment split in place today.

There is no environment-file setup (`environment.ts` /
`environment.prod.ts`) and no `test` configuration. The clearest sign: the
API base URL is a hardcoded constant in `context-smith-api.ts`
(`http://localhost:5010`), not read from a config that changes per
environment. A test run and a production build hit the same URL a
developer's machine hits.

If the app needs to point at a different API URL per environment (a test
deployment, a staging API, a production API), the standard Angular pattern
is:

1. Add `src/environments/environment.ts` (default/dev) and
   `environment.prod.ts` (or one file per environment), each exporting an
   `apiBaseUrl`.
2. Wire `fileReplacements` in the relevant `angular.json` build
   configuration so the right file is swapped in at build time.
3. Import `environment` in `context-smith-api.ts` instead of the hardcoded
   constant.

This is not implemented. Treat it as the next step before the app is
built or served against anything other than a developer's local API.

## CI/CD

The only pipeline is `.github/workflows/build.yml`, and it currently
builds and tests the .NET solution only — it does not install Node.js, run
`npm ci`, `ng build`, or `ng test` for `web/`. Treat a green CI run as proof
the API builds and its tests pass, not proof the Angular app builds or its
tests pass. Run `ng build` and `ng test` locally before you push a change
to `web/`.

Adding an Angular job to `build.yml` (Node.js setup, `npm ci`, `ng build`,
`ng test`) is open work, not yet done.

## Security

- `pre-commit` (see [docs/PREREQUISITES.md](PREREQUISITES.md)) runs a
  secret-scanning hook against every file, including `web/`. Install it
  once per clone with `pre-commit install`.
- The `dotnet-vulnerable-packages` pre-commit hook checks only NuGet
  packages. There is no equivalent check for `web/`'s npm dependencies —
  no `npm audit`, no Dependabot config, no CI security job for the
  frontend. Run `npm audit` in `web/` yourself before you add or update a
  dependency.
- `context-smith-api.ts` calls a hardcoded `http://localhost:5010`. This is
  correct for local development only. A deployed build needs the API base
  URL made configurable (for example, through Angular environment files)
  before it points anywhere other than localhost.
- The app sends no authentication token today — it matches the API's
  current no-auth, in-memory design (see the "Persistence" section of the
  root `README.md`). Add both together if this moves past a local demo.
