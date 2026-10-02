namespace Bonzer.Application.Common;

/// <summary>Masks sensitive identifiers (Aadhaar, PAN, bank account) unless the caller may see them.</summary>
public static class Masking
{
    public static string? Mask(string? value, bool reveal, int visible = 4)
    {
        if (string.IsNullOrEmpty(value) || reveal) return value;
        if (value.Length <= visible) return new string('X', value.Length);
        return new string('X', value.Length - visible) + value[^visible..];
    }
}
