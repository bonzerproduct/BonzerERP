using Bonzer.Domain.Entities.Academics;
using Bonzer.Domain.Entities.Attendance;
using Bonzer.Domain.Entities.Audit;
using Bonzer.Domain.Entities.Certificates;
using Bonzer.Domain.Entities.Communication;
using Bonzer.Domain.Entities.Exams;
using Bonzer.Domain.Entities.Fees;
using Bonzer.Domain.Entities.HumanResources;
using Bonzer.Domain.Entities.Organization;
using Bonzer.Domain.Entities.Students;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bonzer.Application.Common.Interfaces;

/// <summary>
/// The application's view of the database. Tenant isolation and soft delete are applied
/// automatically by global query filters; use IgnoreQueryFilters() only in platform-admin code.
/// </summary>
public interface IApplicationDbContext
{
    // Organization
    DbSet<Tenant> Tenants { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Department> Departments { get; }
    DbSet<LookupValue> LookupValues { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<DocumentSequence> DocumentSequences { get; }

    // Academics
    DbSet<AcademicYear> AcademicYears { get; }
    DbSet<AcademicTerm> AcademicTerms { get; }
    DbSet<AcademicProgram> Programs { get; }
    DbSet<ClassLevel> ClassLevels { get; }
    DbSet<Section> Sections { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<ClassSubject> ClassSubjects { get; }
    DbSet<TeacherAssignment> TeacherAssignments { get; }
    DbSet<TimetableSlot> TimetableSlots { get; }
    DbSet<TimetableEntry> TimetableEntries { get; }
    DbSet<Holiday> Holidays { get; }

    // Students
    DbSet<Student> Students { get; }
    DbSet<Guardian> Guardians { get; }
    DbSet<StudentGuardian> StudentGuardians { get; }
    DbSet<StudentEnrollment> StudentEnrollments { get; }
    DbSet<StudentSubject> StudentSubjects { get; }
    DbSet<StudentPreviousEducation> StudentPreviousEducations { get; }
    DbSet<StudentDocument> StudentDocuments { get; }

    // Human resources
    DbSet<Staff> Staff { get; }
    DbSet<StaffQualification> StaffQualifications { get; }
    DbSet<StaffExperience> StaffExperiences { get; }
    DbSet<StaffDocument> StaffDocuments { get; }
    DbSet<StaffAttendance> StaffAttendances { get; }

    // Attendance
    DbSet<StudentAttendance> StudentAttendances { get; }
    DbSet<AttendanceLock> AttendanceLocks { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<AttendanceCorrectionRequest> AttendanceCorrectionRequests { get; }

    // Exams
    DbSet<GradingScheme> GradingSchemes { get; }
    DbSet<GradeBand> GradeBands { get; }
    DbSet<ExamType> ExamTypes { get; }
    DbSet<ExamComponentType> ExamComponentTypes { get; }
    DbSet<Exam> Exams { get; }
    DbSet<ExamSubject> ExamSubjects { get; }
    DbSet<ExamSubjectComponent> ExamSubjectComponents { get; }
    DbSet<Mark> Marks { get; }
    DbSet<StudentResult> StudentResults { get; }
    DbSet<HallTicket> HallTickets { get; }

    // Fees
    DbSet<FeeHead> FeeHeads { get; }
    DbSet<FeeStructure> FeeStructures { get; }
    DbSet<FeeInstallment> FeeInstallments { get; }
    DbSet<FeeStructureItem> FeeStructureItems { get; }
    DbSet<FeeConcession> FeeConcessions { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentAllocation> PaymentAllocations { get; }

    // Communication
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationRecipient> NotificationRecipients { get; }
    DbSet<Homework> Homeworks { get; }
    DbSet<HomeworkSubmission> HomeworkSubmissions { get; }

    // Certificates
    DbSet<CertificateTemplate> CertificateTemplates { get; }
    DbSet<IssuedCertificate> IssuedCertificates { get; }

    // Audit
    DbSet<AuditLog> AuditLogs { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
