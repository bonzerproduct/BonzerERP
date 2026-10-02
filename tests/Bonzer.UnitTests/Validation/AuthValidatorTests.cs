using Bonzer.Application.Features.Auth;
using Bonzer.Infrastructure.Identity;

namespace Bonzer.UnitTests.Validation;

public class AuthValidatorTests
{
    [Theory]
    [InlineData("token-value", null, true)]
    [InlineData(null, "123456", true)]
    [InlineData(null, null, false)]          // neither
    [InlineData("token-value", "123456", false)] // both
    [InlineData(null, "12345", false)]       // code must be 6 digits
    public void Reset_requires_exactly_one_of_token_or_code(string? token, string? code, bool valid) =>
        Assert.Equal(valid, new ResetPasswordRequestValidator().Validate(new ResetPasswordRequest("user", token, code, "NewPass123")).IsValid);

    [Theory]
    [InlineData("short1A", false)]
    [InlineData("alllowercase1", false)]
    [InlineData("ALLUPPERCASE1", false)]
    [InlineData("NoDigitsHere", false)]
    [InlineData("Good1Password", true)]
    public void New_password_must_be_strong(string password, bool valid) =>
        Assert.Equal(valid, new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest("Old1Password", password)).IsValid);

    [Fact]
    public void Security_stamp_hash_changes_with_the_stamp_and_hides_it()
    {
        var a = TokenService.StampHash("stamp-a");
        Assert.Equal(16, a.Length);
        Assert.NotEqual(a, TokenService.StampHash("stamp-b"));
        Assert.Equal(a, TokenService.StampHash("stamp-a"));
    }
}
