# Product roadmap: MVP to enterprise

## Where we can win against the market

Indian school ERPs (Entab CampusCare, Fedena, MyClassboard, Teachmint, NextERP, Edunext and others) tend to
be strong in one segment: K-12 *or* higher education *or* coaching. They are also often weak on mobile
experience, data isolation and customization without vendor involvement. Bonzer is built to win on:

1. **One product for every level.** Program → ClassLevel → Section covers Nursery, CBSE/State K-12,
   junior college, semester-based UG/PG and tuition batches. Grading schemes cover percentage, CGPA and
   descriptive grades.
2. **Multi-campus groups.** One tenant, many branches, consolidated reporting, a parent with children
   in two campuses gets one login.
3. **Self-service configuration.** Custom roles from fine-grained permissions, lookup lists, per-branch
   settings, custom fields (jsonb) and certificate templates. No code release needed per school.
4. **Trust and compliance.** Tenant isolation at the data layer, an encrypted Aadhaar, a full audit trail and
   DPDP Act 2023 readiness (consent and data-export endpoints are on the roadmap below).
5. **Parent-first mobile.** Fees, attendance, homework and results in the app, with online payment
   and WhatsApp/SMS notifications.

## Phase 1: MVP (sellable to a single school)

| Module | Status in this codebase |
|---|---|
| Multi-tenant core, branches, settings, lookups | Model ✔ |
| Auth: JWT + refresh rotation, roles + permissions, lockout, rate limit | Done ✔ |
| Academic setup: years, terms, programs, classes, sections, subjects | Model ✔, Academic Year API ✔ |
| Admissions and student profile with guardians, documents, auto admission numbers | Model ✔, Students API ✔ |
| Staff / employee master (profile, qualifications, experience, documents, bank & PF/ESI, login, roles, exit) | Done ✔ |
| Departments, custom roles + permission catalogue | Done ✔ |
| Daily attendance + locks + leave + corrections | Model ✔ |
| Fee structure, invoices, receipts (cash/UPI/cheque), concessions | Model ✔ |
| Exams, marks entry, results, report card PDF | Model ✔ |
| Parent portal: children, attendance, fees, results | Children API ✔ |
| Circulars / notifications (in-app, SMS) | Model ✔ |
| Audit trail | Done ✔ |

Next build steps, in order: Academic setup CRUD → Staff → Attendance APIs → Fee collection and receipts
(including PDF) → Exams and report cards → Parent mobile app (Flutter / React Native) → Admin web
(Angular / React).

## Phase 2: Growth (multi-campus groups and colleges)

* Period-wise attendance UI, timetable builder with clash detection (the schema already enforces it)
* Online payments (Razorpay / PayU) with webhook idempotency (the unique index already exists)
* Promotion wizard: year-end bulk promote, detain and graduate using `NextClassLevelId`
* Certificates: TC, bonafide, character, with templates, QR verification and duplicate tracking
* Homework submissions and grading, plus a class diary
* WhatsApp Business notifications, email, push
* Consolidated dashboards across branches (fees collected, attendance %, strength)
* Admission enquiry → application → admission CRM pipeline
* Import / export (Excel) for students, staff and marks

## Phase 3: Enterprise

* **HR & Payroll**: salary structures, payslips, PF/ESI/TDS, staff leave, biometric integration
* **Transport**: routes, stops, vehicles, GPS tracking, transport fee
* **Library**: catalogue, issue and return, fines, barcode
* **Hostel**: rooms, allocation, mess
* **Inventory and assets**: purchase orders, stock, vendor management
* **Accounting**: ledger export (Tally), GST invoices
* **LMS**: online classes, content, quizzes; university **credits / CGPA** transcripts
* **Compliance**: UDISE+ export, APAAR ID, RTE reimbursement reports, DPDP consent and data-export endpoints
* **Analytics**: at-risk student detection (attendance + marks trends), fee-default prediction
* White-label mobile apps per school group; a public API with webhooks for integrations

## Technical roadmap

| Item | Why |
|---|---|
| Upgrade to **.NET 10 LTS** before Nov 2026 | .NET 8 support ends 10 Nov 2026 |
| Background jobs (Hangfire or Quartz) | Notifications, invoice generation, late fees, report PDFs |
| Outbox pattern for notifications | Never lose a message when SMS/WhatsApp providers fail |
| OpenTelemetry tracing + metrics | Combine with Serilog/Seq to see slow queries per tenant |
| Partition `student_attendance` and `audit_logs` by month | They are the highest-volume tables |
| Read replica for reports | Keep heavy reports away from the transactional database |
| Blob storage (S3 / Azure Blob) for photos and documents | Only URLs are stored in the database |
| Integration tests with Testcontainers (PostgreSQL + Redis) | Test interceptors, filters and transactions against a real database |
| Per-tenant rate limits and feature flags by subscription plan | `Tenant.Plan` already exists |
