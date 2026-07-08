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

## Desk Review module (Step 3)

Migration `AddDeskReviewSchema` adds `DeskReviews` (one-to-one with `Applications`) and
`DeskReviewItemComments`. `UnderDeskReview`, `WorthyForVisit`, and `Deficient` were already present
in `ApplicationStatus` as placeholders from Step 1 - this module is what actually transitions an
application into and out of them:
`SelfAssessmentSubmitted -> UnderDeskReview -> (WorthyForVisit | Deficient)`. No existing
table/column was changed.

This is **Admin-only, read-only verification** of the applicant's Step 2 self-assessment - reviewers
never edit `SelfAssessmentResponse`/`SelfAssessmentEvidence`, they only add their own
`DeskReviewItemComment` notes/flags layered on top, plus one overall decision. A decision requires
`OverallComments` and is final once made (no reopen/reverse through the UI - see `CLAUDE.md`/code
comments if that's ever needed as a manual DB fix). Admin reaches it via the sidebar:
**Desk Review Queue** (`/DeskReview/Queue`, oldest-first) and **Reviewed Applications**
(`/DeskReview/Reviewed`, filterable by decision/date).

Since a reviewer needs to read the applicant's Step 2 evidence, `SelfAssessmentController.Evidence`
now also allows `Admin` (in addition to the owning applicant) - mirroring the bypass
`ApplicationsController.Document` already had for Step 1 documents. Every non-owner view of either
is now audit-logged (`EvidenceViewedByReviewer`) since it's sensitive applicant data. No other
action on `SelfAssessmentController` accepts Admin - its per-request ownership check still 404s for
anyone who isn't the applicant.

Institute/QAB see the reviewer's `OverallComments` (and the Worthy for Visit/Deficient badge) on
their own `Applications/Review` page once decided - they do **not** see per-item
`DeskReviewItemComment` notes/flags, which stay internal to Admin.

Run the new migration the same way as any other:

```
cd accreditation-portal
dotnet ef database update
```
