using Bonzer.Application.Features.Employees;
using Bonzer.Application.Security;
using Bonzer.Domain.Entities.HumanResources;
using Bonzer.Domain.Enums;
using Bonzer.Infrastructure.Identity;

namespace Bonzer.UnitTests.Employees;

public class EmployeeTests
{
    private readonly StaffProfileValidator _validator = new(TimeProvider.System);

    private static StaffProfile Valid() => new(
        "Priya", null, "Iyer", Gender.Female, new DateOnly(1990, 1, 15), "B+", MaritalStatus.Married, "Indian", null, null,
        "123412341234", "ABCDE1234F", "9876543210", null, "priya@school.edu", null, null, null, null,
        "Ravi Iyer", "Spouse", "9876500000", StaffType.Teaching, null, "TGT Mathematics", EmploymentType.Permanent,
        new DateOnly(2024, 6, 1), new DateOnly(2025, 5, 31), null, null, "M.Sc, B.Ed", "Mathematics", 4.5m, "CTET-2019-001",
        new BankDetails { AccountNumber = "123456789012", IfscCode = "HDFC0001234", BankName = "HDFC" },
        new StatutoryDetails { Uan = "100200300400", IsPfApplicable = true }, """{"busRoute":"R4"}""");

    [Fact]
    public void Valid_profile_passes() => Assert.True(_validator.Validate(Valid()).IsValid);

    [Theory]
    [InlineData("ABCD1234F")]
    [InlineData("abcde1234f")]
    public void Invalid_pan_is_rejected(string pan) =>
        Assert.Contains(_validator.Validate(Valid() with { PanNumber = pan }).Errors, e => e.PropertyName == nameof(StaffProfile.PanNumber));

    [Fact]
    public void Invalid_ifsc_is_rejected() =>
        Assert.False(_validator.Validate(Valid() with { Bank = new BankDetails { IfscCode = "HDFC1234" } }).IsValid);

    [Fact]
    public void Under_age_employee_is_rejected() =>
        Assert.False(_validator.Validate(Valid() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)) }).IsValid);

    [Fact]
    public void Custom_fields_must_be_a_json_object() =>
        Assert.False(_validator.Validate(Valid() with { CustomFields = "[1,2]" }).IsValid);

    [Fact]
    public void Exit_without_reason_is_rejected() =>
        Assert.False(new ChangeStaffStatusRequestValidator().Validate(new ChangeStaffStatusRequest(StaffStatus.Terminated, null, null)).IsValid);

    [Fact]
    public void Bank_account_inside_owned_type_is_encrypted()
    {
        using var db = TestDb.Create(new FakeCurrentUser { TenantId = Guid.NewGuid() });
        var bank = db.Model.FindEntityType(typeof(Staff))!.FindNavigation(nameof(Staff.Bank))!.TargetEntityType;

        Assert.NotNull(bank.FindProperty(nameof(BankDetails.AccountNumber))!.GetValueConverter());
        Assert.NotNull(db.Model.FindEntityType(typeof(Staff))!.FindProperty(nameof(Staff.PanNumber))!.GetValueConverter());
    }

    [Fact]
    public void Temporary_password_meets_policy()
    {
        for (var i = 0; i < 50; i++)
        {
            var password = UserAccountService.GenerateTemporaryPassword();
            Assert.Equal(12, password.Length);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
        }
    }

    [Fact]
    public void Every_built_in_role_has_a_description_and_permissions()
    {
        var roles = typeof(AppRoles).GetFields()
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.Equal(25, roles.Count);
        Assert.All(roles, r =>
        {
            Assert.True(AppRoles.Descriptions.ContainsKey(r), $"{r} has no description");
            Assert.True(AppRoles.DefaultPermissions.TryGetValue(r, out var p) && p.Length > 0, $"{r} has no permissions");
        });
    }

    [Fact]
    public void Only_super_admin_holds_platform_permissions()
    {
        foreach (var (role, permissions) in AppRoles.DefaultPermissions.Where(r => r.Key != AppRoles.SuperAdmin))
            Assert.DoesNotContain(permissions, p => Permissions.IsPlatformOnly(p));
    }

    [Fact]
    public void Drivers_cannot_see_fees_or_student_records()
    {
        var driver = AppRoles.DefaultPermissions[AppRoles.Driver];
        Assert.DoesNotContain(Permissions.Students.View, driver);
        Assert.DoesNotContain(Permissions.Fees.View, driver);
    }
}
