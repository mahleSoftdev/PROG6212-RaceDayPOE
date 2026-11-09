# Implementation Status

## Implemented

- EF Core SQL Server Code-First model, initial migration, and model snapshot for the seven Part 1 entities.
- Separate Organiser and Participant tables and role policies. Public Participant registration is available; Organiser creation requires a provisioning secret.
- ASP.NET Core Identity password hashing, cookie authentication, server-side session state, logout, and password changes.
- Own-profile read/update endpoints and role-specific profile aliases.
- Venue, event, category, enrolment, and result endpoints with role and ownership checks.
- Request DTO validation, enum validation, duplicate enrolment checks, category capacity checks, and event ownership validation.
- Swagger UI and XML endpoint summaries.
- API endpoint plan in DOCX and PDF, ERD updated to include EventType and both account entities, and README setup/security documentation.
- GitHub Actions workflow to check docs/schema, build the .NET solution, and run tests on pushes and pull requests.
- API integration tests for registration, password hashing, login/session cookie, unauthenticated access, role rejection, event creation, enrolment, and duplicate enrolment.

## Verification limitation

The current execution environment does not have the .NET SDK (`dotnet`) installed. Therefore, `dotnet build` and `dotnet test` could not be run here. The GitHub Actions workflow is configured to run both. Review its result before treating the implementation as verified. The initial EF Core migration is intended for a new empty database. Existing Part 1 databases need a backup and migration-history baseline before applying migrations.


## Fixes applied in the review pass

- Removed `using RaceDayPOE.Services;` from `Program.cs` (the namespace does not exist and prevented the project compiling).
- Removed an EF Core 9-only type from `ApiFactory` (not available in EF Core 8) and switched to `ConfigureTestServices`.
- Added a classic `RaceDayPOE.sln`; CI now uses it because `.slnx` needs .NET SDK 9+ but CI installs SDK 8.
- Added Swagger 400/401/403 response documentation through an operation filter.
- Expanded the test suite from 6 to 51 tests to cover every Part 2 testing requirement.
- README: CI screenshot and YouTube sections, first-Organiser quick start, corrected step numbering.
- Still unverified: `dotnet build`/`dotnet test` were not run in the authoring environment - check the GitHub Actions result.


## Part 3 (MVC front end)

- `RaceDayPOE.Web`: ASP.NET Core 8 MVC app consuming the API only (typed client, per-user API cookie forwarding, session management, role-based areas).
- API additions: Azure Blob image storage (banner and profile picture endpoints), `GET /api/organisers/me/events`, category enrolled counts, richer enrolment and result read models, migration `AddImageUrls`.
- Dockerfile and docker-compose.yml for the web app; CI builds the MVC project and the Docker image.
- Verification: the MVC app was exercised end to end in a browser against a stand-in API with the same routes and JSON shapes. The API changes and the test project could not be compiled or run in the authoring environment (NuGet was unreachable), so check the first GitHub Actions run.
