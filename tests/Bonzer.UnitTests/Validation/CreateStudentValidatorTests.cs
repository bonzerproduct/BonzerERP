using Bonzer.Application.Features.Students;
using Bonzer.Domain.Enums;

namespace Bonzer.UnitTests.Validation;

public class CreateStudentValidatorTests
{
    private readonly CreateStudentRequestValidator _validator = new(TimeProvider.System);

    private static CreateStudentRequest Valid(IReadOnlyList<GuardianInput>? guardians = null) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "Aarav", null, "Sharma", Gender.Male, new DateOnly(2018, 6, 1), null, null, null,
        null, null, null, null, null, null, null,
        guardians ?? [new GuardianInput(GuardianRelation.Father, "Rohit", "Sharma", "9876543210", null, null, true)]);

    [Fact]
    public void Valid_request_passes() => Assert.True(_validator.Validate(Valid()).IsValid);

    [Fact]
    public void Guardian_is_required() => Assert.False(_validator.Validate(Valid([])).IsValid);

    [Fact]
    public void Invalid_aadhaar_is_rejected()
    {
        var result = _validator.Validate(Valid() with { NationalIdNumber = "1234" });
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateStudentRequest.NationalIdNumber));
    }
}
