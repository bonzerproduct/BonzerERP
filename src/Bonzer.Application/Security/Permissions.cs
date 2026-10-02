using System.Reflection;

namespace Bonzer.Application.Security;

/// <summary>
/// Fine-grained permissions ("module.action"). Endpoints check permissions, never role names,
/// so a school can create its own roles (e.g. "Exam Coordinator") by combining permissions.
/// Adding a constant here is enough: the catalogue, seeding and validation pick it up by reflection.
/// </summary>
public static class Permissions
{
    public static class Platform
    {
        public const string ManageTenants = "platform.tenants.manage";
    }

    public static class Branches
    {
        public const string View = "branches.view";
        public const string Manage = "branches.manage";
    }

    public static class Users
    {
        public const string View = "users.view";
        public const string Manage = "users.manage";
        public const string ManageRoles = "users.roles.manage";
    }

    public static class Settings
    {
        public const string Manage = "settings.manage";
    }

    public static class Academics
    {
        public const string View = "academics.view";
        public const string Manage = "academics.manage";
        public const string ManageTimetable = "academics.timetable.manage";
    }

    public static class Admissions
    {
        public const string View = "admissions.view";
        public const string Manage = "admissions.manage";
    }

    public static class Students
    {
        public const string View = "students.view";
        public const string Create = "students.create";
        public const string Edit = "students.edit";
        public const string Delete = "students.delete";
        public const string Promote = "students.promote";
        public const string ViewSensitive = "students.sensitive.view";
    }

    public static class Staff
    {
        public const string View = "staff.view";
        public const string Create = "staff.create";
        public const string Edit = "staff.edit";
        public const string Delete = "staff.delete";

        /// <summary>Unmasked Aadhaar, PAN and bank account.</summary>
        public const string ViewSensitive = "staff.sensitive.view";

        /// <summary>Create logins, assign roles, reset passwords.</summary>
        public const string ManageLogin = "staff.login.manage";
        public const string ManageDepartments = "staff.departments.manage";
    }

    public static class Attendance
    {
        public const string View = "attendance.view";
        public const string Mark = "attendance.mark";
        public const string Lock = "attendance.lock";
        public const string ApproveCorrections = "attendance.corrections.approve";
        public const string ManageStaffAttendance = "attendance.staff.manage";
    }

    public static class Leave
    {
        public const string Apply = "leave.apply";
        public const string Approve = "leave.approve";
    }

    public static class Exams
    {
        public const string View = "exams.view";
        public const string Manage = "exams.manage";
        public const string EnterMarks = "exams.marks.enter";
        public const string PublishResults = "exams.results.publish";
    }

    public static class Fees
    {
        public const string View = "fees.view";
        public const string Manage = "fees.manage";
        public const string Collect = "fees.collect";
        public const string Concession = "fees.concession.approve";
        public const string Refund = "fees.refund";
    }

    public static class Payroll
    {
        public const string View = "payroll.view";
        public const string Manage = "payroll.manage";
        public const string Process = "payroll.process";
    }

    public static class Homework
    {
        public const string View = "homework.view";
        public const string Manage = "homework.manage";
    }

    public static class Communication
    {
        public const string Send = "communication.send";
    }

    public static class Certificates
    {
        public const string Issue = "certificates.issue";
    }

    public static class Library
    {
        public const string View = "library.view";
        public const string Manage = "library.manage";
        public const string Issue = "library.issue";
    }

    public static class Transport
    {
        public const string View = "transport.view";
        public const string Manage = "transport.manage";

        /// <summary>Driver / conductor app: trip start-stop, student boarding.</summary>
        public const string OperateTrips = "transport.trips.operate";
    }

    public static class Hostel
    {
        public const string View = "hostel.view";
        public const string Manage = "hostel.manage";
    }

    public static class Inventory
    {
        public const string View = "inventory.view";
        public const string Manage = "inventory.manage";
    }

    public static class Health
    {
        public const string View = "health.view";
        public const string Manage = "health.manage";
    }

    public static class Counseling
    {
        public const string Manage = "counseling.manage";
    }

    public static class Visitors
    {
        public const string Manage = "visitors.manage";
    }

    public static class Reports
    {
        public const string View = "reports.view";
        public const string Finance = "reports.finance.view";
    }

    public static class Audit
    {
        public const string View = "audit.view";
    }

    /// <summary>Self-service portal (parent sees own children, student sees self).</summary>
    public static class Portal
    {
        public const string Access = "portal.access";
        public const string PayFees = "portal.fees.pay";
    }

    public sealed record PermissionInfo(string Module, string Name, string Code);

    private static readonly Lazy<IReadOnlyList<PermissionInfo>> _catalogue = new(() =>
        typeof(Permissions).GetNestedTypes()
            .Where(t => t.IsClass && t.IsAbstract && t.IsSealed) // static classes only
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => new PermissionInfo(t.Name, f.Name, (string)f.GetRawConstantValue()!)))
            .ToList());

    /// <summary>Every permission with its module, for the role editor UI.</summary>
    public static IReadOnlyList<PermissionInfo> Catalogue => _catalogue.Value;

    public static IReadOnlyList<string> All => Catalogue.Select(p => p.Code).ToList();

    /// <summary>Permissions only the platform owner may hold; never grantable inside a tenant.</summary>
    public static bool IsPlatformOnly(string permission) => permission.StartsWith("platform.", StringComparison.Ordinal);
}
