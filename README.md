# Bonzer ERP

A multi-tenant ERP for education, from pre-primary and K-12 schools through colleges, universities and coaching centres.

| Concern        | Choice                                                                 |
|----------------|------------------------------------------------------------------------|
| Runtime        | .NET 8 (LTS; support ends 10 Nov 2026, so plan the move to .NET 10, which is one line in `Directory.Build.props`) |
| Database       | PostgreSQL via EF Core 8 + Npgsql, snake_case naming                   |
| Cache          | Redis (`IDistributedCache`, StackExchange.Redis); in-memory fallback   |
| Logging        | Serilog: console, rolling JSON files, Seq; enriched with TenantId/UserId |
| Auth           | ASP.NET Core Identity + JWT access tokens + rotating refresh tokens    |
| Authorization  | Permission-based (`[HasPermission(...)]`), roles = permission bundles   |
| Validation     | FluentValidation                                                       |
| Errors         | RFC 7807 ProblemDetails via a global exception handler                 |

## Solution layout (Clean Architecture)

```
BonzerERP.sln
Directory.Build.props          <- target framework, nullable, C# version (one place)
Directory.Packages.props       <- every NuGet version (central package management)
docker-compose.yml             <- PostgreSQL 16, Redis 7, Seq
docs/
  HLD.md                       <- high-level design: architecture, tenancy, security, deployment, ADRs
  LLD.md                       <- low-level design: ER diagrams, auth flows, internals, patterns
  DB-REVIEW.md                 <- what was wrong with the old DbContext and how it was fixed
  ROADMAP.md                   <- MVP -> enterprise module plan and competitive differentiators
src/
  Bonzer.Domain/               <- entities, enums, value objects. No dependencies.
    Common/                    <- BaseEntity / TenantEntity / BranchEntity, UUID v7, Address, attributes
    Enums/
    Entities/
      Organization/            <- Tenant, Branch, Department, LookupValue, AppSetting, DocumentSequence
      Academics/               <- AcademicYear, Term, Program, ClassLevel, Section, Subject, Timetable...
      Students/                <- Student, Guardian, StudentGuardian, StudentEnrollment, documents
      HumanResources/          <- Staff, StaffAttendance
      Attendance/              <- StudentAttendance, AttendanceLock, LeaveRequest, corrections
      Exams/                   <- GradingScheme, Exam, ExamSubject, Mark, StudentResult, HallTicket
      Fees/                    <- FeeHead, FeeStructure, Installment, Concession, Invoice, Payment
      Communication/           <- Notification, Homework
      Certificates/            <- CertificateTemplate, IssuedCertificate (TC, bonafide...)
      Audit/                   <- AuditLog
  Bonzer.Application/          <- use cases. Depends on Domain only (+ EF abstractions).
    Common/                    <- IApplicationDbContext, ICurrentUser, ICacheService, exceptions, paging
    Security/                  <- Permissions catalogue, AppRoles + default permission map, claim names
    Features/<Module>/         <- contracts (DTOs + validators) and the module service
  Bonzer.Infrastructure/       <- EF Core, Identity, JWT, Redis, number sequences
    Identity/                  <- ApplicationUser/Role, RefreshToken, TokenService, AuthService, PermissionService
    Persistence/
      BonzerDbContext.cs
      Configurations/<Module>/ <- IEntityTypeConfiguration per entity (tables, lengths, indexes)
      Conventions/             <- rules applied to every entity (keys, filters, concurrency, encryption)
      Interceptors/            <- auditing stamps, soft delete, tenant guard, audit trail
      Encryption/              <- AES-GCM column encryption for Aadhaar / national IDs
      Migrations/
      Seed/                    <- roles & permissions, super admin, demo school
    Caching/  Services/
  Bonzer.Api/                  <- ASP.NET Core host
    Controllers/  Authorization/  Middleware/  Extensions/  Program.cs
tests/
  Bonzer.UnitTests/            <- model, tenant-filter, security and validation tests (no DB needed)
```

Dependencies point inward: `Api -> Infrastructure -> Application -> Domain`.

## Getting started

Everything in Docker (API on http://localhost:8080/swagger, Seq logs on http://localhost:8081):

```bash
docker compose up -d --build
```

Or run only the infrastructure in Docker and the API from your IDE:

```bash
docker compose up -d postgres redis seq   # PostgreSQL on host port 5433 (avoids a local 5432 install)
dotnet run --project src/Bonzer.Api       # Development: migrates DB, seeds roles + demo school
```

The Development settings (`appsettings.Development.json`) apply the migrations and seed:

| Login         | Password      | Role                                 |
|---------------|---------------|--------------------------------------|
| `superadmin`  | `Admin@12345` | SuperAdmin (platform)                |
| `demo.admin`  | `Demo@12345`  | TenantAdmin of the "DEMO" school     |

Open Swagger at `/swagger`, call `POST /api/v1/auth/login`, then click **Authorize** and paste the `accessToken`.

If you use your own PostgreSQL instead of Docker, set the connection string with user-secrets:

```bash
dotnet user-secrets --project src/Bonzer.Api set "ConnectionStrings:Database" "Host=localhost;Database=bonzer_erp;Username=postgres;Password=<yours>"
```

### Production secrets (never commit)

| Setting                         | How to provide                                  |
|---------------------------------|-------------------------------------------------|
| `ConnectionStrings__Database`   | environment / secret store                      |
| `ConnectionStrings__Redis`      | environment / secret store                      |
| `Jwt__SigningKey`               | 32+ random characters                           |
| `Encryption__Key`               | `openssl rand -base64 32` (store it safely: losing it makes encrypted IDs unreadable) |
| `Seed__SuperAdmin__Email/Password` | first deployment only                        |

The app refuses to start if the JWT or encryption key is missing.

## Working with the database

```bash
dotnet ef migrations add <Name> -p src/Bonzer.Infrastructure -s src/Bonzer.Api -o Persistence/Migrations
dotnet ef database update      -p src/Bonzer.Infrastructure -s src/Bonzer.Api
```

Rules the model enforces for you, so new entities don't need to repeat them:

* **Keys**: UUID v7, generated client-side (index friendly, known before save).
* **Tenancy**: every `TenantEntity` gets a `tenant_id` FK and a global filter. Users only ever see their own tenant's rows. Platform admins and system jobs bypass the filter.
* **Soft delete**: `Remove()` sets `is_deleted`; filtered out automatically; unique indexes ignore deleted rows.
* **Audit**: `created_at/by`, `updated_at/by` are stamped automatically, and every change is written to `audit_logs` with old and new values (encrypted fields are masked).
* **Concurrency**: entities implementing `IHasRowVersion` use PostgreSQL `xmin`, so a concurrent edit returns HTTP 409.
* **No cascade deletes** on business data.
* **Enums** are stored as readable strings; **money** is `numeric(18,2)`.

## Adding a module (e.g. Attendance)

1. Entities already exist in `Domain/Entities/Attendance`. Add configurations in `Infrastructure/Persistence/Configurations/Attendance` if needed.
2. Create `Application/Features/Attendance/` with `AttendanceContracts.cs` (DTOs and validators) and `AttendanceService.cs`.
3. Register the service in `Application/DependencyInjection.cs`.
4. Add `Api/Controllers/AttendanceController.cs` and protect actions with `[HasPermission(Permissions.Attendance.Mark)]`.
5. If you need new permissions, add them to `Permissions.cs` and to role defaults in `AppRoles.cs`. They are seeded on the next start.

## Authentication & users

| Endpoint | What it does |
|---|---|
| `POST /auth/login` | Username, email or phone + password → access token (15 min) + refresh token (7 days). 5 failures = 15 min lockout |
| `POST /auth/refresh` | Rotates the refresh token; re-using an old one signs the user out everywhere |
| `POST /auth/logout` / `logout-all` | This device / every device (access tokens die immediately) |
| `GET/PUT /auth/me` | Own profile, roles and effective permissions |
| `POST /auth/change-password` | Returns fresh tokens; other devices are signed out |
| `POST /auth/forgot-password` | Emails a reset link (30 min) or, without email, SMSes a 6-digit code (3 min). Always 202 |
| `POST /auth/reset-password` | `token` (email) **or** `code` (SMS) + new password |
| `GET /auth/sessions`, `DELETE /auth/sessions/{id}` | Signed-in devices |
| `GET /users`, `GET /users/{id}` | Search all logins (staff, parents, students) with roles, branches, linked profile |
| `PUT /users/{id}/roles` · `/status` · `/branches` | Assign roles, activate/deactivate, multi-branch access |
| `POST /users/{id}/unlock` · `/reset-password` · `/logout-all` | Admin support actions |
| `POST /users/guardians/{id}/login`, `POST /users/students/{id}/login` | Parent / student portal logins |
| `POST /staff/{id}/login`, `PUT /staff/{id}/roles` | Employee logins (see Staff module) |

All paths are under `/api/v1`. Every new or admin-reset account gets a temporary password and can only call
`/auth/*` until it is changed. Role changes, deactivation, password resets and "log out everywhere" take
effect on the next request: each token carries a security-stamp hash that is checked on every call (cached
for 2 minutes). All sign-ins, failures, resets and role changes are written to `audit_logs`.

Email is sent over SMTP when `Messaging:Smtp:Host` is set. Until an SMS gateway is added to `MessageSender`,
SMS messages are only logged (with their content only in Development).

## Roles

25 built-in roles, defined in `Application/Security/AppRoles.cs` and synced at every startup:

| Group | Roles |
|---|---|
| Platform | SuperAdmin |
| Management | TenantAdmin, Director |
| Leadership | Principal, VicePrincipal, BranchAdmin |
| Academic | AcademicCoordinator (HOD / dean), Teacher (lecturer / tutor), ExamController, Registrar |
| Office | AdmissionOfficer, Receptionist, Accountant, HRManager, ITAdmin |
| Support | Librarian, TransportManager, Driver, HostelWarden, Nurse, Counselor, StoreKeeper, Security |
| Portal | Parent, Student |

"Class teacher" and "subject teacher" are assignments (`teacher_assignments`), not roles.

A role is just a set of permissions stored in `role_claims`. Built-in roles are read-only; a school creates
its own roles with `POST /api/v1/roles`, optionally copying another role, and can override a built-in role
for itself by creating a role with the same name. Nobody can grant permissions they don't hold, and
SuperAdmin, TenantAdmin and Director are reserved. Permission changes take effect
without users signing in again: permissions are resolved per request and cached in Redis for 30 minutes.
Call `IPermissionService.InvalidateRoleAsync` after editing a role (the seeder already does) so the change
applies immediately.
