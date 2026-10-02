using Bonzer.Infrastructure.Persistence.Conventions;
using Bonzer.Infrastructure.Persistence.Encryption;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Bonzer.Infrastructure.Persistence;

/// <summary>
/// Single EF Core context for the ERP (PostgreSQL, snake_case naming).
/// Mapping lives in Configurations/{Module}/*Configuration.cs; cross-cutting rules
/// (UUID v7 keys, tenant + soft-delete filters, xmin concurrency, encryption, no cascades)
/// live in <see cref="ModelBuilderExtensions"/>; auditing lives in Interceptors/.
/// </summary>
public sealed class BonzerDbContext(
    DbContextOptions<BonzerDbContext> options,
    ICurrentUser currentUser,
    IFieldEncryptor encryptor)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IApplicationDbContext, IDataProtectionKeyContext
{
    /// <summary>ASP.NET Core data-protection keys (reset tokens, etc.) shared by every API instance.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    // Read by the global query filters on every query (see ModelBuilderExtensions.BuildQueryFilter).
    public Guid? CurrentTenantId => currentUser.TenantId;
    public bool BypassTenantFilter => currentUser.IsSystem || currentUser.IsPlatformAdmin;

    // Organization
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LookupValue> LookupValues => Set<LookupValue>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

    // Academics
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();
    public DbSet<AcademicProgram> Programs => Set<AcademicProgram>();
    public DbSet<ClassLevel> ClassLevels => Set<ClassLevel>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ClassSubject> ClassSubjects => Set<ClassSubject>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
    public DbSet<TimetableSlot> TimetableSlots => Set<TimetableSlot>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    // Students
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<StudentSubject> StudentSubjects => Set<StudentSubject>();
    public DbSet<StudentPreviousEducation> StudentPreviousEducations => Set<StudentPreviousEducation>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();

    // Human resources
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffQualification> StaffQualifications => Set<StaffQualification>();
    public DbSet<StaffExperience> StaffExperiences => Set<StaffExperience>();
    public DbSet<StaffDocument> StaffDocuments => Set<StaffDocument>();
    public DbSet<StaffAttendance> StaffAttendances => Set<StaffAttendance>();

    // Attendance
    public DbSet<StudentAttendance> StudentAttendances => Set<StudentAttendance>();
    public DbSet<AttendanceLock> AttendanceLocks => Set<AttendanceLock>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<AttendanceCorrectionRequest> AttendanceCorrectionRequests => Set<AttendanceCorrectionRequest>();

    // Exams
    public DbSet<GradingScheme> GradingSchemes => Set<GradingScheme>();
    public DbSet<GradeBand> GradeBands => Set<GradeBand>();
    public DbSet<ExamType> ExamTypes => Set<ExamType>();
    public DbSet<ExamComponentType> ExamComponentTypes => Set<ExamComponentType>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamSubject> ExamSubjects => Set<ExamSubject>();
    public DbSet<ExamSubjectComponent> ExamSubjectComponents => Set<ExamSubjectComponent>();
    public DbSet<Mark> Marks => Set<Mark>();
    public DbSet<StudentResult> StudentResults => Set<StudentResult>();
    public DbSet<HallTicket> HallTickets => Set<HallTicket>();

    // Fees
    public DbSet<FeeHead> FeeHeads => Set<FeeHead>();
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeeInstallment> FeeInstallments => Set<FeeInstallment>();
    public DbSet<FeeStructureItem> FeeStructureItems => Set<FeeStructureItem>();
    public DbSet<FeeConcession> FeeConcessions => Set<FeeConcession>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    // Communication
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<Homework> Homeworks => Set<Homework>();
    public DbSet<HomeworkSubmission> HomeworkSubmissions => Set<HomeworkSubmission>();

    // Certificates
    public DbSet<CertificateTemplate> CertificateTemplates => Set<CertificateTemplate>();
    public DbSet<IssuedCertificate> IssuedCertificates => Set<IssuedCertificate>();

    // Audit & security
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserBranchAccess> UserBranchAccess => Set<UserBranchAccess>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Money and marks: numeric(18,2) unless a configuration says otherwise.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);

        // Enums as readable strings ("Present", "Paid") instead of magic numbers.
        foreach (var enumType in typeof(BaseEntity).Assembly.GetTypes().Where(t => t.IsEnum))
        {
            configurationBuilder.Properties(enumType).HaveConversion<string>().HaveMaxLength(50);
            configurationBuilder.Properties(typeof(Nullable<>).MakeGenericType(enumType)).HaveConversion<string>().HaveMaxLength(50);
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // ASP.NET Identity tables

        builder.Owned<Address>();
        builder.Owned<MedicalInfo>();
        builder.Owned<GovernmentSchemeInfo>();

        builder.ApplyConfigurationsFromAssembly(typeof(BonzerDbContext).Assembly);
        builder.ApplyErpConventions(this, encryptor);
    }
}
