using Bonzer.Application.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bonzer.Infrastructure.Persistence.Seed;

/// <summary>
/// Development-only sample school ("DEMO") with pre-primary + primary programs, a current academic
/// year, sections, subjects, grading, fee heads and lookups, plus a tenant admin login.
/// </summary>
internal static class DemoDataSeeder
{
    public static async Task SeedAsync(BonzerDbContext db, UserManager<ApplicationUser> userManager,
        IConfiguration configuration, TimeProvider clock, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.Tenants.AnyAsync(t => t.Code == "DEMO", cancellationToken)) return;

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var startYear = today.Month >= 4 ? today.Year : today.Year - 1; // Indian academic year: April - March

        var tenant = new Tenant { Name = "Demo Education Trust", Code = "DEMO", Plan = SubscriptionPlan.Trial };
        db.Tenants.Add(tenant);

        var branch = new Branch
        {
            TenantId = tenant.Id, Name = "Demo Public School", Code = "DPS", InstitutionType = InstitutionType.School,
            Board = "CBSE", MediumOfInstruction = "English", WeeklyOffDays = WeekDays.Sunday,
            SaturdayOffPattern = SaturdayOffPattern.SecondAndFourth,
            Address = new Address { City = "Pune", State = "Maharashtra", Country = "India" }
        };
        branch.Tenant = tenant;
        db.Branches.Add(branch);

        var year = new AcademicYear
        {
            Name = $"{startYear}-{(startYear + 1) % 100:00}",
            StartDate = new DateOnly(startYear, 4, 1),
            EndDate = new DateOnly(startYear + 1, 3, 31),
            IsCurrent = true,
            Status = AcademicYearStatus.Active
        };
        Own(year, tenant, branch);
        db.AcademicYears.Add(year);

        AddProgram(db, tenant, branch, year, "Pre-Primary", "PP", EducationLevel.PrePrimary, ["Nursery", "LKG", "UKG"]);
        AddProgram(db, tenant, branch, year, "Primary", "PRI", EducationLevel.Primary, ["Std 1", "Std 2", "Std 3", "Std 4", "Std 5"]);

        foreach (var (name, code) in new[] { ("English", "ENG"), ("Mathematics", "MAT"), ("Environmental Studies", "EVS"), ("Hindi", "HIN"), ("Drawing", "DRW") })
            db.Subjects.Add(Own(new Subject { Name = name, Code = code }, tenant, branch));

        var scheme = Own(new GradingScheme { Name = "CBSE 8-point", Type = GradingType.Percentage, IsDefault = true }, tenant, branch);
        var bands = new (string Grade, decimal Min, decimal Max, bool Pass)[]
        {
            ("A1", 91, 100, true), ("A2", 81, 90.99m, true), ("B1", 71, 80.99m, true), ("B2", 61, 70.99m, true),
            ("C1", 51, 60.99m, true), ("C2", 41, 50.99m, true), ("D", 33, 40.99m, true), ("E", 0, 32.99m, false)
        };
        for (var i = 0; i < bands.Length; i++)
            scheme.Bands.Add(new GradeBand { TenantId = tenant.Id, Grade = bands[i].Grade, MinPercent = bands[i].Min, MaxPercent = bands[i].Max, IsPass = bands[i].Pass, SortOrder = i });
        db.GradingSchemes.Add(scheme);

        foreach (var name in new[] { "Unit Test", "Half Yearly", "Annual" })
            db.ExamTypes.Add(Own(new ExamType { Name = name }, tenant, branch));
        foreach (var name in new[] { "Theory", "Practical", "Oral", "Internal Assessment" })
            db.ExamComponentTypes.Add(Own(new ExamComponentType { Name = name }, tenant, branch));

        db.FeeHeads.AddRange(
            Own(new FeeHead { Name = "Admission Fee", Code = "ADM", Frequency = FeeFrequency.OneTime }, tenant, branch),
            Own(new FeeHead { Name = "Tuition Fee", Code = "TUI", Frequency = FeeFrequency.Term }, tenant, branch),
            Own(new FeeHead { Name = "Transport Fee", Code = "TRN", Frequency = FeeFrequency.Monthly, IsOptional = true }, tenant, branch));

        var lookups = new Dictionary<string, string[]>
        {
            [LookupCategories.BloodGroup] = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"],
            [LookupCategories.Religion] = ["Hindu", "Muslim", "Christian", "Sikh", "Buddhist", "Jain", "Other"],
            [LookupCategories.Category] = ["General", "OBC", "SC", "ST", "EWS"],
            [LookupCategories.LeaveType] = ["Sick", "Casual", "Family Function", "Other"]
        };
        foreach (var (category, values) in lookups)
            for (var i = 0; i < values.Length; i++)
                db.LookupValues.Add(new LookupValue { TenantId = tenant.Id, Category = category, Code = values[i].ToUpperInvariant(), Name = values[i], SortOrder = i, IsSystem = true });

        await db.SaveChangesAsync(cancellationToken);

        var password = configuration["Seed:DemoAdminPassword"];
        if (!string.IsNullOrWhiteSpace(password))
        {
            var admin = new ApplicationUser
            {
                UserName = "demo.admin", Email = "admin@demo.local", EmailConfirmed = true,
                FirstName = "Demo", LastName = "Admin", UserType = UserType.Staff, TenantId = tenant.Id
            };
            DbInitializer.EnsureSucceeded(await userManager.CreateAsync(admin, password), "create demo admin");
            DbInitializer.EnsureSucceeded(await userManager.AddToRoleAsync(admin, AppRoles.TenantAdmin), "assign TenantAdmin role");
        }

        logger.LogInformation("Seeded demo tenant DEMO (branch {BranchId})", branch.Id);
    }

    private static void AddProgram(BonzerDbContext db, Tenant tenant, Branch branch, AcademicYear year,
        string name, string code, EducationLevel level, string[] classNames)
    {
        var program = Own(new AcademicProgram { Name = name, Code = code, Level = level, Duration = classNames.Length }, tenant, branch);
        ClassLevel? previous = null;
        for (var i = 0; i < classNames.Length; i++)
        {
            var classLevel = Own(new ClassLevel { Name = classNames[i], SortOrder = i + 1 }, tenant, branch);
            classLevel.Sections.Add(Own(new Section { Name = "A", Capacity = 40, AcademicYear = year }, tenant, branch));
            program.ClassLevels.Add(classLevel);
            if (previous is not null) previous.NextClassLevel = classLevel;
            previous = classLevel;
        }
        db.Programs.Add(program);
    }

    private static T Own<T>(T entity, Tenant tenant, Branch branch) where T : BranchEntity
    {
        entity.Id = UuidV7.NewGuid();
        entity.TenantId = tenant.Id;
        entity.BranchId = branch.Id;
        return entity;
    }
}
