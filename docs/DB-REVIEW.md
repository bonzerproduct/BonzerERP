# Review of the original `BonzerSchoolDbContext`

The original context compiled, but it had correctness bugs, security gaps and design limits that would
block a multi-school, pre-primary-to-university product. This page lists what was found and where it is
fixed in the new model.

## 1. Correctness bugs

| # | Problem in the old context | Effect | Fix |
|---|---|---|---|
| 1 | `TeacherSubjectMap` unique index uses `SectionId`, but the `SectionId` mapping is commented out (the table uses `DivisionId`) | The index targets `SectionId`, which has no real column, so "one teacher per subject per division" is never enforced | `TeacherAssignment` with a unique index on `(staff, section, subject, year)` |
| 2 | `Payment.InvoiceId` is a `varchar(50)` while `Invoice.Id` is a UUID | No foreign key is possible and invoices can be orphaned | `PaymentAllocation` with real FKs to `Invoice` / `InvoiceItem` |
| 3 | Almost no relationships are configured (only `PaymentFeeLine`) | No FK constraints, so the database accepts attendance for students that don't exist | Navigations and FKs on every reference, and `Restrict` on delete |
| 4 | `Attendance` is unique on `(student_id, attendance_date)` | Period-wise or lecture attendance (college, university, tuition) is impossible | `PeriodNo` (0 = daily), unique on `(student, date, period)` |
| 5 | The academic year is stored as a **string** in `class_fee_map`, `class_subject_map`, `teacher_subject_map` and `student_subjects`, but as `AcademicYearId` elsewhere (`ClassFeeMap` has both) | Joins break when a year is renamed and the two values drift apart | `AcademicYearId` FK everywhere |
| 6 | `Student` holds `ClassId`, `DivisionId` and `RollNumber` directly | Promotion overwrites history, so last year's class and report card links are lost | `StudentEnrollment`, one row per year with status (Promoted / Detained...) |
| 7 | Timestamps use `timestamp without time zone` with `CURRENT_TIMESTAMP` (server local time) | Wrong times across time zones and DST, and mixed `DateTime` kinds | UTC everywhere (`timestamptz`), stamped by an interceptor |
| 8 | `Timetable` has no section/division, period times or room | Can't build a real timetable or detect clashes | `TimetableSlot` + `TimetableEntry` with section-slot and teacher-slot unique indexes |
| 9 | `Department.Name` is `varchar(20)` | "Physical Education" or "Computer Science" doesn't fit | 150 characters |
| 10 | Some tables (`Invoice`, `Notification`, `ParentUser`, `Homework`, `GradeMaster`) have no `ToTable`/`HasColumnName`, so they get PascalCase names | Mixed naming: some tables need quoted identifiers in SQL | `UseSnakeCaseNamingConvention()` for everything |
| 11 | Defaults are mixed: `gen_random_uuid()` and `uuid_generate_v4()` (needs the `uuid-ossp` extension) | Random v4 UUIDs fragment large indexes, and ids are unknown until insert | Client-generated UUID v7 for all keys, so no extension is needed |

## 2. Security problems

| Problem | Fix |
|---|---|
| **Two user systems**: Identity (`ApplicationUser`) plus custom `users` (with its own `password_hash`), `roles`, `staff_roles` and `parent_users` tables | One login table (`users`, Identity). Staff, guardians and students link to it with `UserId`. Roles are Identity roles, and permissions are role claims |
| `security_question` / `security_answer` stored on the user (the answer is likely plaintext) | Removed. Use Identity's password reset tokens and OTP instead |
| Aadhaar numbers for the child, father and mother stored in plaintext | `[Encrypted]` → AES-256-GCM at rest, masked in audit logs and API responses unless the caller has `students.sensitive.view` |
| No tenant isolation at all: one school could read another's data if a query forgot a `WHERE` | `TenantId` on every business row, a global query filter, and an interceptor that blocks cross-tenant writes |
| Refresh tokens: unknown storage (the entity isn't shown) | Only SHA-256 hashes are stored, tokens rotate on every refresh, and reusing a token revokes all of that user's sessions |
| No automatic audit trail (`audit_logs` existed, but nothing in the context fills it) | `AuditTrailInterceptor` writes old and new values for every change in the same transaction |

## 3. Design limits for a generic ERP

| Old design | Problem | New design |
|---|---|---|
| `classes` → `divisions` | Only fits K-12 | `Program` (Pre-Primary, B.Sc CS, JEE batch) → `ClassLevel` (Nursery, Std 5, Semester 3) → `Section` (A, Morning batch), with `EducationLevel` and `TermStructure` enums |
| `grade_master` (one global list) | CBSE, state boards, university CGPA and pre-primary descriptive grades can't coexist | `GradingScheme` + `GradeBand` per branch, typed Percentage / GradePoint / Descriptive |
| `fee_structures`, `class_fee_map` and `fee_installments` overlap | Three places to define the same fee, with no concessions | `FeeStructure` → `FeeInstallment` (due date, late-fee rule) → `FeeStructureItem`, plus `FeeConcession`, `Invoice` and `Payment` + `PaymentAllocation` |
| `tc_certificates` and `bonafide_certificates` as separate tables | A new table for every certificate type | `IssuedCertificate` (type enum + JSON snapshot) + `CertificateTemplate` |
| `attendance_date_locks` and `attendance_academic_year_locks` | Two tables for one concept | `AttendanceLock` (date / section+date scope) + `AcademicYear.IsAttendanceLocked` |
| Father, mother and guardian stored as ~15 columns on `students` | Siblings duplicate parent data, and a parent needs one login per child | `Guardian` + `StudentGuardian` (relation, primary contact, pick-up permission). The admission flow reuses a guardian by phone |
| ~90 columns on `students` | Hard to evolve. Indian scheme fields (RTE, BPL, APAAR, UDISE) are mixed with core data | Core columns plus owned value objects: `Address`, `MedicalInfo`, `GovernmentSchemeInfo`. Extra per-school fields go in `custom_fields` (jsonb) |
| Hard-coded master data (religion, category, blood group, medium) | Every change needs a code release | `LookupValue` per tenant |
| No running-number service | Duplicate receipt or admission numbers when two cashiers save at once | `DocumentSequence` + atomic `UPDATE ... RETURNING` in `DocumentNumberGenerator` |
| No soft delete on most tables | Accidental deletes are permanent, and FK errors when deleting masters | `ISoftDeletable` on every business row, with partial unique indexes |
| No optimistic concurrency | Two clerks editing the same student or invoice silently overwrite each other | PostgreSQL `xmin` row version → HTTP 409 |
| `OnModelCreating` was ~1,300 lines of `HasColumnName` | Unreadable, and easy to make the bug in item 1 | Naming convention, per-module `IEntityTypeConfiguration` files and cross-cutting conventions in one place |

## 4. Old → new table map

| Old | New |
|---|---|
| school_groups | tenants |
| branches | branches |
| institution_types, mediums | `Branch.InstitutionType` enum, `Branch.MediumOfInstruction` / lookup_values |
| academic_years | academic_years (+ academic_terms) |
| classes | programs + class_levels |
| divisions | sections |
| subjects | subjects |
| class_subject_map | class_subjects |
| class_teacher_map, teacher_subject_map | teacher_assignments |
| student_subjects | student_subjects (per enrollment) |
| timetable | timetable_slots + timetable_entries |
| school_holidays | holidays (date range, optional class level) |
| students | students + guardians + student_guardians + student_enrollments + student_previous_educations + student_documents |
| parent_users, users, roles, staff_roles | users, roles, user_roles, role_claims (Identity) + user_branch_access |
| staff | staff (+ staff_attendance) |
| department | departments |
| attendance | student_attendance |
| attendance_date_locks, attendance_academic_year_locks | attendance_locks + academic_years.is_attendance_locked |
| leave_requests | leave_requests (student or staff) |
| attendance_correction_requests | attendance_correction_requests |
| exam_types, exam_component_types | exam_types, exam_component_types (per branch) |
| exams, exam_subjects, exam_subject_components | exams, exam_subjects (per class level / section), exam_subject_components |
| marks | marks (status enum instead of three booleans) |
| student_results | student_results |
| grade_master | grading_schemes + grade_bands |
| hall_tickets | hall_tickets |
| fee_heads | fee_heads |
| fee_structures, class_fee_map, fee_installments | fee_structures + fee_installments + fee_structure_items |
| invoices, invoice_items | invoices, invoice_items |
| payments, payment_fee_lines | payments + payment_allocations |
| notifications | notifications + notification_recipients |
| homework | homeworks + homework_submissions |
| tc_certificates, bonafide_certificates | certificate_templates + issued_certificates |
| audit_logs | audit_logs (written automatically) |
| RefreshTokens | refresh_tokens (hashed) |

## 5. Migrating existing data

There is no automatic migration from the old schema. Write a one-off SQL script that runs in this order:

1. Create tenants from `school_groups`, then copy `branches` across.
2. Load programs and class levels from `classes`, and sections from `divisions`.
3. Load students. Split the father, mother and guardian columns into `guardians` and `student_guardians`, de-duplicating by phone number. Create one `student_enrollments` row for the current class.
4. Move users into Identity. If the old password hashes are not ASP.NET Identity hashes, force a password reset (`must_change_password = true`).
5. Re-encrypt Aadhaar values on first save. The encryptor reads legacy plaintext values unchanged.
