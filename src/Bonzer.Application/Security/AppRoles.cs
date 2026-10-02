using P = Bonzer.Application.Security.Permissions;

namespace Bonzer.Application.Security;

/// <summary>
/// Built-in (system) roles seeded for every installation. They cover K-12 schools, colleges,
/// universities and coaching centres. A school can:
///   - create its own roles from any permissions (Roles API), or
///   - override a built-in role for itself by creating a tenant role with the same name.
/// "Class teacher" and "subject teacher" are not roles: they come from teacher_assignments.
/// </summary>
public static class AppRoles
{
    // Platform
    public const string SuperAdmin = "SuperAdmin";               // you, the ERP vendor

    // Management
    public const string TenantAdmin = "TenantAdmin";             // trust / group owner, all branches, full control
    public const string Director = "Director";                   // chairman / trustee / management: view everything, approve

    // Leadership
    public const string Principal = "Principal";                 // also Head Master, College Principal
    public const string VicePrincipal = "VicePrincipal";
    public const string BranchAdmin = "BranchAdmin";             // office superintendent / campus administrator

    // Academic
    public const string AcademicCoordinator = "AcademicCoordinator"; // coordinator, HOD, dean
    public const string Teacher = "Teacher";                     // teacher, lecturer, professor, tutor
    public const string ExamController = "ExamController";       // exam cell / controller of examinations
    public const string Registrar = "Registrar";                 // colleges & universities: admissions, records, certificates

    // Office
    public const string AdmissionOfficer = "AdmissionOfficer";   // admission counsellor / enquiry desk
    public const string Receptionist = "Receptionist";           // front office
    public const string Accountant = "Accountant";               // fee collection and accounts
    public const string HRManager = "HRManager";                 // employees, staff attendance, payroll
    public const string ITAdmin = "ITAdmin";                     // user accounts, settings

    // Support services
    public const string Librarian = "Librarian";
    public const string TransportManager = "TransportManager";
    public const string Driver = "Driver";                       // also conductor / bus attendant
    public const string HostelWarden = "HostelWarden";
    public const string Nurse = "Nurse";                         // school nurse / medical officer
    public const string Counselor = "Counselor";
    public const string StoreKeeper = "StoreKeeper";             // inventory / stores
    public const string Security = "Security";                   // gate, visitors

    // Portal
    public const string Parent = "Parent";
    public const string Student = "Student";

    /// <summary>Roles that can see every branch of their tenant.</summary>
    public static readonly IReadOnlySet<string> TenantWide = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SuperAdmin, TenantAdmin, Director
    };

    /// <summary>Roles allowed to grant any role / permission inside their tenant.</summary>
    public static readonly IReadOnlySet<string> FullTenantControl = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SuperAdmin, TenantAdmin
    };

    public static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>
    {
        [SuperAdmin] = "Platform owner: manages all institutions",
        [TenantAdmin] = "Institution / trust administrator with full control of all branches",
        [Director] = "Management (chairman, trustee, director): read access to everything, approvals",
        [Principal] = "Head of a branch (principal, head master, college principal)",
        [VicePrincipal] = "Deputy head: academics, discipline, attendance oversight",
        [BranchAdmin] = "Campus administrator / office superintendent",
        [AcademicCoordinator] = "Coordinator, head of department or dean",
        [Teacher] = "Teacher, lecturer, professor or tutor",
        [ExamController] = "Exam cell: schedules, hall tickets, marks, results",
        [Registrar] = "Admissions, student records and certificates (colleges / universities)",
        [AdmissionOfficer] = "Admission enquiries and new admissions",
        [Receptionist] = "Front office",
        [Accountant] = "Fee collection, concessions, refunds, finance reports",
        [HRManager] = "Employees, staff attendance, leave and payroll",
        [ITAdmin] = "User accounts, roles and system settings",
        [Librarian] = "Library catalogue and circulation",
        [TransportManager] = "Routes, vehicles, drivers and transport fees",
        [Driver] = "Driver / conductor mobile app",
        [HostelWarden] = "Hostel rooms, allocation and attendance",
        [Nurse] = "Student health records and infirmary",
        [Counselor] = "Student counselling",
        [StoreKeeper] = "Stores, inventory and assets",
        [Security] = "Gate pass and visitor management",
        [Parent] = "Parent / guardian portal and app",
        [Student] = "Student portal and app"
    };

    private static readonly string[] SelfService = [P.Leave.Apply];

    public static readonly IReadOnlyDictionary<string, string[]> DefaultPermissions = new Dictionary<string, string[]>
    {
        [SuperAdmin] = P.All.ToArray(),

        [TenantAdmin] = P.All.Where(p => !P.IsPlatformOnly(p) && !p.StartsWith("portal.")).ToArray(),

        [Director] =
        [
            P.Branches.View, P.Users.View, P.Academics.View, P.Admissions.View, P.Students.View, P.Staff.View,
            P.Attendance.View, P.Exams.View, P.Fees.View, P.Fees.Concession, P.Payroll.View, P.Leave.Approve,
            P.Library.View, P.Transport.View, P.Hostel.View, P.Inventory.View, P.Reports.View, P.Reports.Finance,
            P.Communication.Send, P.Audit.View
        ],

        [Principal] =
        [
            P.Branches.View, P.Users.View, P.Academics.View, P.Academics.Manage, P.Academics.ManageTimetable,
            P.Admissions.View, P.Admissions.Manage, P.Students.View, P.Students.Edit, P.Students.Promote,
            P.Students.ViewSensitive, P.Staff.View, P.Staff.Edit, P.Attendance.View, P.Attendance.Lock,
            P.Attendance.ApproveCorrections, P.Attendance.ManageStaffAttendance, P.Leave.Apply, P.Leave.Approve,
            P.Exams.View, P.Exams.Manage, P.Exams.PublishResults, P.Fees.View, P.Fees.Concession, P.Homework.View,
            P.Communication.Send, P.Certificates.Issue, P.Library.View, P.Transport.View, P.Hostel.View,
            P.Health.View, P.Reports.View, P.Reports.Finance, P.Audit.View
        ],

        [VicePrincipal] =
        [
            P.Academics.View, P.Academics.Manage, P.Academics.ManageTimetable, P.Admissions.View, P.Students.View,
            P.Students.Edit, P.Students.Promote, P.Staff.View, P.Attendance.View, P.Attendance.Lock,
            P.Attendance.ApproveCorrections, P.Attendance.ManageStaffAttendance, P.Leave.Apply, P.Leave.Approve,
            P.Exams.View, P.Exams.Manage, P.Homework.View, P.Communication.Send, P.Certificates.Issue,
            P.Health.View, P.Reports.View
        ],

        [BranchAdmin] =
        [
            P.Branches.View, P.Users.View, P.Users.Manage, P.Settings.Manage, P.Academics.View, P.Academics.Manage,
            P.Academics.ManageTimetable, P.Admissions.View, P.Admissions.Manage, P.Students.View, P.Students.Create,
            P.Students.Edit, P.Students.Promote, P.Students.ViewSensitive, P.Staff.View, P.Staff.Create, P.Staff.Edit,
            P.Staff.ManageLogin, P.Staff.ManageDepartments, P.Attendance.View, P.Attendance.Lock,
            P.Attendance.ManageStaffAttendance, P.Leave.Apply, P.Leave.Approve, P.Exams.View, P.Exams.Manage,
            P.Fees.View, P.Communication.Send, P.Certificates.Issue, P.Transport.View, P.Hostel.View,
            P.Inventory.View, P.Visitors.Manage, P.Reports.View
        ],

        [AcademicCoordinator] =
        [
            P.Academics.View, P.Academics.Manage, P.Academics.ManageTimetable, P.Students.View, P.Staff.View,
            P.Attendance.View, P.Attendance.Mark, P.Attendance.ApproveCorrections, P.Leave.Apply, P.Leave.Approve,
            P.Exams.View, P.Exams.Manage, P.Exams.EnterMarks, P.Homework.View, P.Homework.Manage,
            P.Communication.Send, P.Reports.View
        ],

        [Teacher] =
        [
            P.Academics.View, P.Students.View, P.Attendance.View, P.Attendance.Mark, P.Leave.Apply,
            P.Exams.View, P.Exams.EnterMarks, P.Homework.View, P.Homework.Manage, P.Communication.Send,
            P.Library.View
        ],

        [ExamController] =
        [
            P.Academics.View, P.Students.View, P.Exams.View, P.Exams.Manage, P.Exams.EnterMarks,
            P.Exams.PublishResults, P.Certificates.Issue, P.Communication.Send, P.Reports.View, P.Leave.Apply
        ],

        [Registrar] =
        [
            P.Academics.View, P.Admissions.View, P.Admissions.Manage, P.Students.View, P.Students.Create,
            P.Students.Edit, P.Students.Promote, P.Students.ViewSensitive, P.Exams.View, P.Exams.PublishResults,
            P.Certificates.Issue, P.Communication.Send, P.Reports.View, P.Leave.Apply
        ],

        [AdmissionOfficer] =
        [
            P.Academics.View, P.Admissions.View, P.Admissions.Manage, P.Students.View, P.Students.Create,
            P.Students.Edit, P.Fees.View, P.Fees.Collect, P.Communication.Send, P.Leave.Apply
        ],

        [Receptionist] =
        [
            P.Academics.View, P.Admissions.View, P.Admissions.Manage, P.Students.View, P.Students.Create,
            P.Fees.View, P.Fees.Collect, P.Communication.Send, P.Visitors.Manage, P.Leave.Apply
        ],

        [Accountant] =
        [
            P.Academics.View, P.Students.View, P.Fees.View, P.Fees.Manage, P.Fees.Collect, P.Fees.Refund,
            P.Payroll.View, P.Reports.View, P.Reports.Finance, P.Leave.Apply
        ],

        [HRManager] =
        [
            P.Staff.View, P.Staff.Create, P.Staff.Edit, P.Staff.Delete, P.Staff.ViewSensitive, P.Staff.ManageLogin,
            P.Staff.ManageDepartments, P.Attendance.ManageStaffAttendance, P.Leave.Apply, P.Leave.Approve,
            P.Payroll.View, P.Payroll.Manage, P.Payroll.Process, P.Reports.View
        ],

        [ITAdmin] =
        [
            P.Branches.View, P.Users.View, P.Users.Manage, P.Users.ManageRoles, P.Settings.Manage, P.Staff.View,
            P.Staff.ManageLogin, P.Audit.View, P.Leave.Apply
        ],

        [Librarian] = [P.Students.View, P.Staff.View, P.Library.View, P.Library.Manage, P.Library.Issue, .. SelfService],

        [TransportManager] =
        [
            P.Students.View, P.Staff.View, P.Transport.View, P.Transport.Manage, P.Transport.OperateTrips,
            P.Fees.View, P.Communication.Send, .. SelfService
        ],

        [Driver] = [P.Transport.View, P.Transport.OperateTrips, .. SelfService],

        [HostelWarden] =
        [
            P.Students.View, P.Hostel.View, P.Hostel.Manage, P.Attendance.View, P.Health.View,
            P.Communication.Send, .. SelfService
        ],

        [Nurse] = [P.Students.View, P.Health.View, P.Health.Manage, P.Communication.Send, .. SelfService],

        [Counselor] = [P.Students.View, P.Students.ViewSensitive, P.Attendance.View, P.Exams.View, P.Counseling.Manage, .. SelfService],

        [StoreKeeper] = [P.Inventory.View, P.Inventory.Manage, .. SelfService],

        [Security] = [P.Visitors.Manage, P.Students.View, .. SelfService],

        [Parent] = [P.Portal.Access, P.Portal.PayFees, P.Leave.Apply],

        [Student] = [P.Portal.Access, P.Homework.View, P.Library.View]
    };
}
