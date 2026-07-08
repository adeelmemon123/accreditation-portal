# Accreditation Portal

ASP.NET Core MVC (.NET 10) app for the NAC accreditation workflow. See `CLAUDE.md` for full
architecture notes.

## Running

```
dotnet restore
dotnet ef database update --project accreditation-portal   # applies all migrations, including the Application module
dotnet run --project accreditation-portal
```

## Institute/QAB Application module

Migration `AddApplicationRegistrationSchema` adds the `Applications`, `InstituteProfiles`,
`QABProfiles`, `ApplicationDocuments`, and `ApplicationLogs` tables. Run it the same way as any
other migration:

```
cd accreditation-portal
dotnet ef database update
```

Uploaded application documents (registration certificate, affiliation certificate, fee challan)
are stored **outside `wwwroot`** on local disk, under the path configured by
`AppData:UploadsPath` in `appsettings.json` (defaults to `App_Data/uploads`, relative to the
project's content root). They are never served as static files - only through
`/Applications/Document/{documentId}`, which checks the requester owns the application (or is
Admin) before streaming the file. Swap `IFileStorageService`'s registration in `Program.cs` for
a different implementation (e.g. Azure Blob) to change where files live without touching any
controller.

## Self-Assessment module (Step 2)

Migration `AddSelfAssessmentSchema` adds `ChecklistTemplates`, `ChecklistSections`,
`ChecklistItems`, `SelfAssessmentResponses`, and `SelfAssessmentEvidence`. Two new
`ApplicationStatus` values (`SelfAssessmentInProgress`, `SelfAssessmentSubmitted`) sit between
`Submitted` and `UnderDeskReview`; no existing table/column was changed.

The checklist itself is data-driven (template -> sections -> items), not hardcoded, so it can be
edited without code changes once an editor exists. For now there is **no admin CRUD editor** -
`Data/ChecklistTemplateSeeder.cs` seeds one active template each for Institute and QAB on every
startup (idempotent - skips a type that already has a template). Admin can review what's seeded
read-only at `/Admin/ChecklistTemplates`. **Building the full create/edit/reorder editor UI is a
follow-up task** - the seeder's shape (`ChecklistTemplate` -> `ChecklistSection` -> `ChecklistItem`)
is exactly what that editor should produce, so extend it rather than replacing it.

Evidence files reuse `IFileStorageService` (same PDF/JPG/PNG, 5 MB, magic-byte validation as Step 1
documents) under `applications/{applicationId}/self-assessment/{checklistItemId}/`, served only via
`/SelfAssessment/Evidence/{evidenceId}` with an ownership check.

Run the new migration the same way as any other:

```
cd accreditation-portal
dotnet ef database update
```

The seeder runs automatically on next app startup (via `Program.cs`, right after `SeedData`) - no
separate command needed.
