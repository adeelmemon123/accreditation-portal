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
