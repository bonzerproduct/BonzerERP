namespace Bonzer.Domain.Entities.HumanResources;

/// <summary>Salary account, stored in the staff table (columns prefixed <c>bank_</c>).</summary>
public class BankDetails
{
    public string? AccountHolderName { get; set; }

    [Encrypted]
    public string? AccountNumber { get; set; }
    public string? IfscCode { get; set; }
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
}

/// <summary>Statutory registrations for payroll (India: EPF / ESI), columns prefixed <c>statutory_</c>.</summary>
public class StatutoryDetails
{
    /// <summary>Universal Account Number (EPFO).</summary>
    public string? Uan { get; set; }
    public string? PfNumber { get; set; }
    public string? EsiNumber { get; set; }
    public bool IsPfApplicable { get; set; }
    public bool IsEsiApplicable { get; set; }
}
