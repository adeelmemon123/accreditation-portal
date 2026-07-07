# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

This repo (`accreditation-portal/`, targeting **.NET 10**) is the **Accreditation Portal** — a system for the National Accreditation Council (NAC) to digitize the end-to-end institute/QAB accreditation workflow (registration → self-assessment → desk review → on-site assessment → TA-QEC committee grading → NAC certificate issuance → renewal/expiry tracking).

The full functional/technical requirements (stakeholders, roles, workflow steps, module breakdown, SOPs) are captured as photographed pages in `accreditation-portal/docs/a1.jpg` … `a4.jpg`. Read these before doing any feature design — they are the spec of record and are not duplicated elsewhere in the repo (no README, no separate design docs).

Authentication and role management (ASP.NET Core Identity + EF Core SqlServer) are implemented — see **Architecture** below. Beyond that, the app is still the stock MVC template (`HomeController`, default views) with no accreditation-workflow entities (Institute/QAB applications, checklists, desk review, assessor findings, TA-QEC grading, certificates) yet. There is no test project.

## Commands

Run all commands from the repo root (`D:\accreditation-portal`), which contains the `accreditation-portal.slnx` solution file.

```
dotnet restore                          # restore NuGet packages
dotnet build                            # build the solution
dotnet run --project accreditation-portal   # run the app (see launch profiles below)
dotnet watch --project accreditation-portal run   # run with hot reload
```

Launch profiles (`accreditation-portal/Properties/launchSettings.json`):
- `http` → http://localhost:5184
- `https` → https://localhost:7074 (falls back to http://localhost:5184)

Both profiles set `ASPNETCORE_ENVIRONMENT=Development`, which enables the developer exception page instead of `/Home/Error`.

There are currently no lint, format, or test commands configured (no test project, no `.editorconfig`-driven analyzers beyond the SDK defaults).

EF Core migrations (`dotnet-ef` global tool, run from `accreditation-portal/`):

```
dotnet ef migrations add <Name>
dotnet ef database update
```

The database is SQL Server on the named instance `.\SQLEXPRESS01` (Windows/Trusted auth), database `AccreditationPortalDb` — see `ConnectionStrings:DefaultConnection` in `appsettings.json`. This machine also has a `.\SQLEXPRESS` instance and a `(localdb)\mssqllocaldb` instance, each of which can independently hold a same-named `AccreditationPortalDb` — if migrations seem to "disappear" or a fresh install looks empty, confirm which instance the connection string actually points at (`sqlcmd -S <instance> -Q "SELECT name FROM sys.databases"`) rather than assuming. On every app startup, `Data/SeedData.cs` idempotently seeds the fixed role set and a default Admin user from the `SeedAdmin:Email`/`SeedAdmin:Password` config keys (currently `admin@nac.gov.pk` / `Admin@12345` in `appsettings.json` — change before any real deployment).

## Architecture

Standard ASP.NET Core MVC layout, root namespace `accreditation_portal`:

- `Program.cs` — minimal hosting model entry point. Registers `AddControllersWithViews()`, EF Core (`ApplicationDbContext` via SQL Server), and ASP.NET Core Identity (`AddIdentity<ApplicationUser, IdentityRole>`) with cookie paths pointed at `AccountController`. Middleware order: exception handler + HSTS (non-Development) → HTTPS redirect → routing → **authentication → authorization** → static assets → default route `{controller=Home}/{action=Index}/{id?}`. After `app.Build()`, runs `SeedData.InitializeAsync` in a scoped service provider before `app.Run()`.
- `Authorization/Roles.cs` — single source of truth for role name constants. `Roles.All` (the 7 fixed roles), `Roles.SelfRegisterable` (`Institute`, `QAB` — the only roles a user can pick during public registration), and `Roles.InternallyProvisioned` (`Admin`, `ProvincialTEVTA`, `SectorExpert`, `TAQECChairperson`, `NACChairman` — created only by an Admin via `/Admin/CreateUser`). Add any new role here first; controllers and views read from this class rather than hardcoding strings.
- `Data/ApplicationDbContext.cs` — `IdentityDbContext<ApplicationUser>`; will hold accreditation-domain `DbSet`s as those entities are added.
- `Data/SeedData.cs` — startup role + default Admin seeding, called from `Program.cs`.
- `Models/ApplicationUser.cs` — extends `IdentityUser` with `FullName`, `Province` (used to scope Provincial TEVTA read access and to record Institute/QAB location), `CreatedAtUtc`.
- `Models/AccountViewModels/`, `Models/AdminViewModels/` — view models for the auth and admin-management flows respectively.
- `Controllers/AccountController.cs` — public self-registration (Institute/QAB only, via `Roles.SelfRegisterable`) + login/logout/access-denied. Registration signs the user in immediately after creation.
- `Controllers/AdminController.cs` — `[Authorize(Roles = Roles.Admin)]`. `/Admin/Users` lists all users with their roles; `/Admin/CreateUser` provisions internal accounts (TEVTA/Assessor/TA-QEC/NAC Chairman/extra Admins) restricted to `Roles.InternallyProvisioned`; `/Admin/EditRoles` reassigns any user's roles via checkboxes against `Roles.All`. This is the role management system — there is no separate role-CRUD UI since the role set is fixed by business requirements, not user-defined.
- `Views/Shared/_Layout.cshtml` — injects `SignInManager`/`UserManager` to render Login/Register/Logout and a role-gated "User Management" nav link for Admins.
- `Views/` — Razor views grouped by controller (`Views/Home/`, `Views/Account/`, `Views/Admin/`) plus `Views/Shared/`. `_ViewImports.cshtml` pulls in `accreditation_portal.Models.AccountViewModels`, `accreditation_portal.Models.AdminViewModels`, and `accreditation_portal.Authorization` globally.
- `wwwroot/` — static assets; only jQuery + jQuery Validation (unobtrusive) are vendored under `wwwroot/lib/` (needed for `asp-validation-for`/`data-val` client validation). No jQuery/DOM code beyond that is authored here.

### Styling: Tailwind + Flowbite (CDN, no build step)

The UI uses the **Tailwind v4 browser CDN build** plus **Flowbite** for components — not Bootstrap (removed). Both are loaded as CDN `<script>` tags in `Views/Shared/_Layout.cshtml`, with no npm/PostCSS build step:

- `https://cdn.jsdelivr.net/npm/@tailwindcss/browser@4` — JIT-compiles Tailwind utility classes found in the DOM at runtime.
- A `<style type="text/tailwindcss"> @theme { ... } </style>` block in `_Layout.cshtml` head defines a `primary-*` color scale (standard Tailwind blue), since Flowbite's example markup assumes a `primary` palette exists — the CDN build has no `tailwind.config` to extend, so `@theme` is the only way to add it.
- `https://cdn.jsdelivr.net/npm/flowbite@2/dist/flowbite.min.js` — provides Flowbite's interactive components (e.g. the mobile nav `data-collapse-toggle`) via auto-initialization on data attributes; no manual JS wiring needed for the components currently in use.
- Views use Flowbite's markup patterns (auth cards, tables, form inputs) built from Tailwind utilities — copy Flowbite's published examples rather than inventing new class combinations, and reuse the `primary-*`/`gray-*` shades already established for consistency (light + `dark:` variants throughout).
- `wwwroot/css/site.css` is now just a placeholder for project-specific overrides on top of the CDN build.

As the accreditation workflow (from `docs/`) gets built out, expect the structure to grow along the module lines described there: Institute/QAB application, Desk Review, Assessor, TA-QEC, NAC certificate, Expiry & Renewal, and Reporting — each will need its own controllers/EF entities and `[Authorize(Roles = ...)]` checks against the constants in `Authorization/Roles.cs`.
