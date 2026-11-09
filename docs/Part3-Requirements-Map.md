# Part 3: where each requirement is implemented

| # | Requirement | Where it lives |
|---|---|---|
| 1 | MVC application consuming the API, no direct DB access or duplicated business logic | `RaceDayPOE.Web/` (project has no EF Core, SQL or Azure packages; a CI step enforces this). All data calls: `Services/IRaceDayApi.cs`, `Services/RaceDayApi.cs`. Per-user API identity: `Infrastructure/ApiCookieHandler.cs` |
| 2 | Register, log in, session state and role, role-specific navigation, protected pages, log out | `Controllers/AccountController.cs`, `Infrastructure/AccountSession.cs`, `Program.cs` (session + cookie auth + `[Authorize(Roles)]` areas), `Views/Shared/_Layout.cshtml` (Participant top bar), `Areas/Organiser/Views/Shared/_OrganiserLayout.cshtml` (Organiser sidebar) |
| 3 | Organiser interface: dashboard, events (create, edit, delete, banner), categories, enrolments, results | `Areas/Organiser/` (Dashboard, Events, Venues, Categories, Enrolments, Results controllers and views) |
| 4 | Participant interface: home with filters, event detail, enrol, my enrolments, my results, profile and picture | `Controllers/HomeController.cs`, `Controllers/EventsController.cs`, `Controllers/ProfileController.cs`, `Areas/Participant/` |
| 5 | Azure Blob Storage through the API only | API: `Services/AzureBlobImageStorage.cs`, `Services/ImageValidator.cs`, `POST /api/events/{id}/banner`, `POST /api/users/me/picture`, migration `AddImageUrls`. MVC only posts files to those endpoints |
| 6 | Docker | `RaceDayPOE.Web/Dockerfile`, `docker-compose.yml`, run command in `README.md` |
| 7 | UI design: brand theme, Bootstrap, colour-coded statuses, graceful invalid input, consistency | `wwwroot/css/raceday.css` (one stylesheet), Bootstrap 5 in `wwwroot/lib`, `Views/Shared/_StatusBadge.cshtml`, `Services/ApiMessages.cs`, `Views/Home/Error.cshtml`, client and server validation on every form |
| 8 | GitHub and CI/CD | Part 3 work is on branch `part3-mvc` (20+ commits). `.github/workflows/ci.yml` builds the MVC project, enforces the no-database rule, runs all tests and builds the Docker image |
