# AcxiomCRM

AcxiomCRM is an ASP.NET Core MVC CRM application built for the supplied assignment specification. It uses ASP.NET Core Identity, EF Core, SQLite, Razor views, Bootstrap 5, role-based authorization, audit logging, dashboard KPIs, Chart.js charts, scoped CRM workflows, and DTO-based APIs.

## Prerequisites

- .NET SDK 10.0.401 or compatible .NET 10 SDK
- SQLite is used through EF Core provider packages

## Run

```powershell
dotnet restore
dotnet build AcxiomCRM.Web\AcxiomCRM.Web.csproj
dotnet run --project AcxiomCRM.Web
```

The existing project launch profile runs on `http://localhost:5218`. The app initializes the local SQLite database automatically when it starts.

## Development Accounts

The application supports the `Admin`, `Manager`, and `SalesExecutive` roles. Development accounts are initialized through local development configuration and database seeding.

Configure development credentials locally before running the application. Do not commit passwords, secrets, or environment-specific credentials to source control.

For evaluation, use the development account credentials provided separately by the project maintainer.

## Evaluation Login Credentials

The following development accounts are configured for local evaluation of AcxiomCRM.

| Role | Email | Password |
|---|---|---|
| Admin | `admin@acxiomcrm.local` | `Admin#12345` |
| Manager | `manager@acxiomcrm.local` | `Manager#12345` |
| Sales Executive | `sales@acxiomcrm.local` | `Sales#12345` |

### Login Instructions

1. Run the application using the instructions in the **Run** section.
2. Open `http://localhost:5218` in your browser.
3. Enter the email address and password for the role you want to evaluate.
4. Verify the role-specific dashboard, permissions, and available CRM modules.

### Important Notes

- These are development-only credentials, not production accounts.
- Login requires the corresponding accounts to exist in the initialized local database.
- The application must be running on the evaluator's machine for the local URL to work.
- For security, do not publish these credentials in a public repository. Share them separately through an approved private channel when required.

## Architecture

- `Controllers`: MVC and API endpoints
- `Models`: Identity user, CRM entities, enums, validation attributes
- `Data`: EF Core `ApplicationDbContext` and seed/bootstrap logic
- `Services`: audit logging and role/ownership scoping
- `ViewModels` and `DTOs`: MVC forms and API contracts
- `Views`: Razor UI using Bootstrap
- `wwwroot/css/site.css`: central Acxiom-inspired enterprise styling variables

## Roles

- `Admin`: full CRM access, user management, audit logs, reports, all records
- `Manager`: team-scoped CRM data, pipeline, follow-ups, reports
- `SalesExecutive`: assigned customers, leads, opportunities, follow-ups, activities, scoped dashboard/reports

Manager scope is implemented with `ApplicationUser.ManagerId`; managers see their own records and records assigned to users who reference them as manager.

## Implemented Modules

- Identity login, register, logout, password policy and lockout configuration
- Role seeding for `Admin`, `Manager`, `SalesExecutive`
- Dashboard KPIs and Chart.js data from authorized database queries
- Customer CRUD with search/filter/sort, duplicate email/phone checks, validation, deactivation
- Lead CRUD, status validation, assignment, qualified lead conversion to customer/opportunity without duplicate creation
- Opportunity CRUD, weighted pipeline, amount/probability/close-date business validation, won/lost audit events
- Follow-up scheduling, completion and rescheduling with date validation
- Activity create/list/complete for calls, meetings, email and tasks
- Admin-only user management and audit log filtering
- Reports page and pipeline CSV export
- Secured APIs for customers, leads, opportunities and pipeline report data
- Audit logging for authentication and important CRM/admin changes

## API Endpoints

All CRM APIs require authentication.

- `GET /api/customers`
- `GET /api/customers/{id}`
- `POST /api/customers`
- `PUT /api/customers/{id}`
- `DELETE /api/customers/{id}`
- `GET /api/leads`
- `POST /api/leads`
- `GET /api/opportunities`
- `POST /api/opportunities`
- `GET /api/reports/pipeline`

Example customer create payload:

```json
{
  "customerName": "Example Customer",
  "email": "customer@example.com",
  "phone": "+91 9876543200",
  "companyName": "Example Ltd",
  "status": "Active",
  "assignedToId": "identity-user-id"
}
```

Validation and duplicate conflicts return `400` or `409` with safe error objects.

## Validation

The app enforces client-side and server-side validation for required fields, email format, phone format, length, numeric ranges, foreign keys, ownership, duplicate customer email/phone, opportunity amount/probability/close-date rules, and follow-up date rules.

Required assignment messages implemented include:

- `Opportunity Amount must be greater than 0.`
- `Probability must be between 0 and 100.`
- `Expected Close Date cannot be in the past.`
- `Follow-up date cannot be earlier than today.`
- `Enter a valid email address.`
- `Enter a valid phone number.`

## Database

The current implementation uses `Database.EnsureCreatedAsync()` for fast local evaluation. The database provider is configured through:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=acxiomcrm.db"
}
```

Migration commands can be introduced with:

```powershell
dotnet ef migrations add InitialCreate --project AcxiomCRM.Web
dotnet ef database update --project AcxiomCRM.Web
```

## Tests And Verification

Current verification performed:

- PDF requirements extracted and used as the implementation baseline.
- `dotnet build AcxiomCRM.Web\AcxiomCRM.Web.csproj -p:UseAppHost=false -o .\artifacts\buildcheck` passes.
- Database schema and seed initialization started successfully in local artifact run.

Known verification still needed:

- End-to-end browser login and CRUD walkthrough.
- Authenticated API calls with real cookies/tokens.
- Automated tests for business rules and ownership authorization.
- Full acceptance checklist execution.

## Known Limitations

- EF migrations are not yet committed; local initialization currently uses `EnsureCreatedAsync`.
- Chart.js is loaded from CDN in the dashboard view.
- API coverage includes required customers/leads/opportunities/pipeline endpoints, but follow-up API endpoints are not yet implemented.
- User management supports create and activate/deactivate; full password reset/unlock screens are not yet implemented.
- Reports are functional but compact; they can be expanded with richer filters and pagination.
