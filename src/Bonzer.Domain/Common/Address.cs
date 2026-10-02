namespace Bonzer.Domain.Common;

/// <summary>Value object stored inside the owning table (columns prefixed <c>address_</c>).</summary>
public class Address
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
}
