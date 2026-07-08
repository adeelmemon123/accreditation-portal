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

## On-Site Assessment module (Step 4)

Migration `AddAssessmentModuleSchema` adds `AssessmentAssignments` (one-to-one with `Applications`),
`AssessmentTeamMembers`, `AssessmentFindings`, and `AssessmentEvidence`, plus a `Sector` column on
`AspNetUsers` (SectorExpert/Assessor only, captured at `RegisterAssessor`) and on `InstituteProfiles`/
`QABProfiles` (captured in the existing Step 1 profile forms) - added so the assignment picker can
match assessors to applications by Province + Sector. Three new `ApplicationStatus` values:
`WorthyForVisit -> AssessmentAssigned -> AssessmentInProgress -> AssessmentSubmitted`. No existing
table/column was otherwise changed.

**Time-box enforcement is live, not status-based.** `AssessmentAssignment.Status` (`NotStarted` ->
`WindowOpen` -> `WindowClosed`/`FindingsSubmitted`) exists for dashboards, but every write action
(`SaveFindings`, `DeleteEvidence`, `Submit`) re-derives "is the window actually still open" itself via
`AssessmentService.EnsureWindowOpenForEditing`, comparing `DateTime.UtcNow` against
`WindowEndAt` directly - it never trusts `Status` alone. `AssessmentWindowMonitorService` (a
`BackgroundService`, polling every minute) is what flips `Status` to `WindowClosed` and logs
`AssessmentWindowClosed` once `WindowEndAt` lapses, but that's purely for the Admin queue's
"awaiting attention" section - a slow poll can make the dashboard lag reality briefly, it can never
let a write through past the deadline, since the live check doesn't depend on the job having run.

Findings/evidence are entered per checklist item (same `ChecklistItem` structure as Step 2), any
assigned team member may edit any item (`AssessmentFinding` is one row per item, upserted, not
siloed per person). Evidence reuses `IFileStorageService` under
`applications/{applicationId}/assessment/{checklistItemId}/`, served via
`/Assessment/Evidence/{evidenceId}` gated to the assignment's team members/Convener.

Admin reaches this via the sidebar - **On-Site Assessment** (`/Assessment/Queue`): assign a
Convener + Assessor team to a `WorthyForVisit` application (`/Assessment/Assign`), then a separate
**Open Window** action starts the 3-day clock. Assessors get their own **My Assignments** nav
(`/Assessment/MyAssignments`) showing a live countdown; the Convener (an Admin user) shares the same
Findings/Submit page read-only-plus-submit, without edit rights on the findings themselves.

Institute/QAB only see the status badge for these four statuses - assessor findings are internal
input for the next module (TA-QEC) and are never shown to the applicant.

Run the new migration the same way as any other:

```
cd accreditation-portal
dotnet ef database update
```

The background job registers itself automatically (`builder.Services.AddHostedService<AssessmentWindowMonitorService>()`
in `Program.cs`) - no separate command needed, it starts with the app.

## TA-QEC Committee Review module (Step 5)

Migration `AddTaQecModuleSchema` adds `TaQecReviews` (one-to-one with `Applications`) and
`TaQecDiscussionNotes` (append-only - no uniqueness constraint, unlike prior modules' single-note-per-item
pattern, since multiple committee members can each leave multiple notes). It also **renames the
`TAQECChairperson` Identity role to `TAQEC`** via raw SQL in the migration (no user held that role at
the time, so this was a clean rename, not a data migration) and adds `IsChairperson` (bool) to
`ApplicationUser`. Two new `ApplicationStatus` values:
`AssessmentSubmitted -> UnderTaQecReview -> TaQecGraded`. No other existing table/column was changed.

**Role + flag, not two roles.** Every `TAQEC` member can view the report and add discussion notes.
Only locking the final grade requires the `RequireTaQecChairperson` policy (`Authorization/TaQecChairpersonRequirement.cs`
+ `TaQecChairpersonHandler.cs`), which checks `TAQEC` role **and** `IsChairperson == true` - read live
from the DB via `UserManager` on every check (same "live over cached" principle as the Assessment
window enforcement), so Admin toggling the flag on Admin's Edit Roles page (`/Admin/EditRoles`, checkbox
shown only when TAQEC is selected) takes effect immediately, not just after the user's next login.

**The report** (`TaQecService.BuildReportAsync`) joins, per checklist item: the applicant's Step 2
self-score/comments/evidence, Step 3 Desk Review flag/comment (if any), and Step 4 assessor
strengths/weaknesses/findings/recommended-score/evidence - plus a header (applicant name, province,
sector) and stats (avg self-score vs avg assessor score, Desk Review flag count). It's read-only here;
TA-QEC never edits Step 2-4 data, only adds its own discussion notes and final grade.

**PDF export** (`TaQecReportPdfGenerator`, via the newly-added **QuestPDF** package, Community license)
renders the same `TaQecReportViewModel` as a visually-segmented PDF (header/stats/section blocks) -
this is the "PowerPoint style" the workflow doc describes, interpreted as styling rather than a literal
`.pptx` file (confirmed with the user before building). Download via
`/TaQec/DownloadPdf/{applicationId}`.

Since the report needs to link to Step 2/Step 4 evidence, `SelfAssessmentController.Evidence` and
`AssessmentController.Evidence` now also allow `TAQEC` (read-only, same narrow-bypass pattern used for
Admin/Desk-Review's access to Step 2 evidence) - no other action on either controller accepts TAQEC.

Admin reaches the sidebar via **TA-QEC Queue** (`/TaQec/Queue`, oldest-first) and **Graded
Applications** (`/TaQec/Graded`, filterable by grade); TA-QEC users get their own sidebar
(`_TaQecLayout.cshtml`) with the same two links. Institute/QAB only see the status badge for
`UnderTaQecReview`/`TaQecGraded` - the grade and rationale are not revealed to the applicant at this
stage (read as an internal step; the applicant-facing reveal happens at NAC approval, the next module).

Run the new migration the same way as any other:

```
cd accreditation-portal
dotnet ef database update
```
