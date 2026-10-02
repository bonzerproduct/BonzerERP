using Bonzer.Application.Security;
using Bonzer.Domain.Entities.Students;

namespace Bonzer.Application.Features.Students;

public sealed class StudentService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IDocumentNumberGenerator numbers,
    IPermissionService permissions,
    TimeProvider clock,
    IValidator<CreateStudentRequest> validator) : IStudentService
{
    public async Task<PagedResult<StudentListItemDto>> GetPagedAsync(StudentQuery query, CancellationToken cancellationToken = default)
    {
        currentUser.EnsureBranchAccess(query.BranchId);

        var students = db.Students.AsNoTracking().Where(s => s.BranchId == query.BranchId);

        if (query.Status.HasValue)
            students = students.Where(s => s.Status == query.Status);

        if (query.AcademicYearId.HasValue || query.ClassLevelId.HasValue || query.SectionId.HasValue)
            students = students.Where(s => s.Enrollments.Any(e =>
                (query.AcademicYearId == null || e.AcademicYearId == query.AcademicYearId) &&
                (query.ClassLevelId == null || e.ClassLevelId == query.ClassLevelId) &&
                (query.SectionId == null || e.SectionId == query.SectionId)));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            students = students.Where(s =>
                s.AdmissionNo.ToLower().Contains(term) ||
                s.FirstName.ToLower().Contains(term) ||
                (s.LastName != null && s.LastName.ToLower().Contains(term)) ||
                s.Guardians.Any(g => g.Guardian.Phone.Contains(term)));
        }

        return await ProjectToListItem(students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<StudentDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var s = await db.Students.AsNoTracking()
                    .Include(x => x.Guardians).ThenInclude(g => g.Guardian)
                    .Include(x => x.Enrollments).ThenInclude(e => e.AcademicYear)
                    .Include(x => x.Enrollments).ThenInclude(e => e.ClassLevel)
                    .Include(x => x.Enrollments).ThenInclude(e => e.Section)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(Student), id);

        currentUser.EnsureBranchAccess(s.BranchId);

        var canSeeSensitive = (await permissions.GetPermissionsAsync(currentUser.TenantId, currentUser.Roles, cancellationToken))
            .Contains(Permissions.Students.ViewSensitive);

        return new StudentDetailDto(
            s.Id, s.BranchId, s.AdmissionNo, s.AdmissionDate, s.FirstName, s.MiddleName, s.LastName, s.Gender,
            s.DateOfBirth, s.BloodGroup, s.Religion, s.Category, s.Phone, s.Email, s.Address, s.Status,
            Mask(s.NationalIdNumber, canSeeSensitive),
            s.Guardians.Select(g => new GuardianDto(g.GuardianId, g.Guardian.FullName, g.Relation, g.Guardian.Phone,
                g.Guardian.Email, g.IsPrimaryContact)).ToList(),
            s.Enrollments.OrderByDescending(e => e.EnrolledOn).Select(e => new EnrollmentDto(
                e.Id, e.AcademicYearId, e.AcademicYear.Name, e.ClassLevelId, e.ClassLevel.Name, e.SectionId,
                e.Section.Name, e.RollNumber, e.Status)).ToList());
    }

    public async Task<Guid> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        currentUser.EnsureBranchAccess(request.BranchId);

        var section = await db.Sections.AsNoTracking()
                          .Where(s => s.Id == request.SectionId && s.BranchId == request.BranchId &&
                                      s.ClassLevelId == request.ClassLevelId && s.AcademicYearId == request.AcademicYearId)
                          .Select(s => new { s.Capacity })
                          .FirstOrDefaultAsync(cancellationToken)
                      ?? throw new BusinessRuleException("The section does not belong to the selected class and academic year.");

        if (section.Capacity.HasValue)
        {
            var enrolled = await db.StudentEnrollments.CountAsync(
                e => e.SectionId == request.SectionId && e.Status == EnrollmentStatus.Active, cancellationToken);
            if (enrolled >= section.Capacity)
                throw new BusinessRuleException($"Section is full ({section.Capacity} seats).");
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var admissionNo = string.IsNullOrWhiteSpace(request.AdmissionNo)
            ? await numbers.NextAsync(request.BranchId, DocumentType.Admission, request.AcademicYearId, cancellationToken)
            : request.AdmissionNo.Trim();

        if (await db.Students.AnyAsync(s => s.BranchId == request.BranchId && s.AdmissionNo == admissionNo, cancellationToken))
            throw new ConflictException($"Admission number '{admissionNo}' is already used.");

        var student = new Student
        {
            BranchId = request.BranchId,
            AdmissionNo = admissionNo,
            AdmissionDate = request.AdmissionDate ?? today,
            FirstName = request.FirstName.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            LastName = request.LastName?.Trim(),
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            BloodGroup = request.BloodGroup,
            Religion = request.Religion,
            Category = request.Category,
            NationalIdNumber = request.NationalIdNumber,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address ?? new Address(),
            Status = StudentStatus.Active
        };

        student.Enrollments.Add(new StudentEnrollment
        {
            BranchId = request.BranchId,
            AcademicYearId = request.AcademicYearId,
            ClassLevelId = request.ClassLevelId,
            SectionId = request.SectionId,
            RollNumber = request.RollNumber,
            EnrolledOn = student.AdmissionDate,
            Status = EnrollmentStatus.Active
        });

        // Re-use an existing guardian with the same phone: siblings share one parent record and one login.
        var phones = request.Guardians.Select(g => g.Phone).Distinct().ToList();
        var existing = await db.Guardians.Where(g => phones.Contains(g.Phone)).ToListAsync(cancellationToken);
        var hasPrimary = request.Guardians.Any(g => g.IsPrimaryContact);

        for (var i = 0; i < request.Guardians.Count; i++)
        {
            var input = request.Guardians[i];
            var guardian = existing.FirstOrDefault(g => g.Phone == input.Phone && g.FirstName == input.FirstName.Trim());
            if (guardian is null)
            {
                guardian = new Guardian
                {
                    FirstName = input.FirstName.Trim(),
                    LastName = input.LastName?.Trim(),
                    Phone = input.Phone,
                    Email = input.Email,
                    Occupation = input.Occupation
                };
                existing.Add(guardian);
            }

            student.Guardians.Add(new StudentGuardian
            {
                Guardian = guardian,
                Relation = input.Relation,
                IsPrimaryContact = input.IsPrimaryContact || (!hasPrimary && i == 0)
            });
        }

        db.Students.Add(student);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return student.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
                      ?? throw new NotFoundException(nameof(Student), id);
        currentUser.EnsureBranchAccess(student.BranchId);

        db.Students.Remove(student); // converted to a soft delete by the auditing interceptor
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentListItemDto>> GetMyChildrenAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.StudentId is { } selfId)
            return await ProjectToListItem(db.Students.AsNoTracking().Where(s => s.Id == selfId)).ToListAsync(cancellationToken);

        var guardianId = currentUser.GuardianId
                         ?? throw new ForbiddenAccessException("This account is not linked to a parent or student profile.");

        return await ProjectToListItem(db.Students.AsNoTracking()
                .Where(s => s.Guardians.Any(g => g.GuardianId == guardianId))
                .OrderBy(s => s.FirstName))
            .ToListAsync(cancellationToken);
    }

    internal static IQueryable<StudentListItemDto> ProjectToListItem(IQueryable<Student> students) =>
        students.Select(s => new
            {
                Student = s,
                Current = s.Enrollments.Where(e => e.Status == EnrollmentStatus.Active).OrderByDescending(e => e.EnrolledOn).FirstOrDefault(),
                PrimaryPhone = s.Guardians.Where(g => g.IsPrimaryContact).Select(g => g.Guardian.Phone).FirstOrDefault()
            })
            .Select(x => new StudentListItemDto(
                x.Student.Id,
                x.Student.AdmissionNo,
                x.Student.FirstName + (x.Student.LastName == null ? "" : " " + x.Student.LastName),
                x.Student.Gender,
                x.Student.DateOfBirth,
                x.Student.Status,
                x.Current == null ? null : x.Current.ClassLevel.Name,
                x.Current == null ? null : x.Current.Section.Name,
                x.Current == null ? null : x.Current.RollNumber,
                x.PrimaryPhone,
                x.Student.PhotoUrl));

    private static string? Mask(string? value, bool reveal) =>
        string.IsNullOrEmpty(value) || reveal ? value : $"XXXX-XXXX-{value[^Math.Min(4, value.Length)..]}";
}
