namespace Bonzer.Domain.Enums;

public enum SubscriptionPlan { Trial, Starter, Standard, Premium, Enterprise }

public enum InstitutionType { PreSchool, School, JuniorCollege, College, University, CoachingCentre, Other }

public enum DepartmentType { Academic, Administrative, Support }

/// <summary>Documents that get an auto-generated running number (see DocumentSequence).</summary>
public enum DocumentType
{
    Admission, Enrollment, Invoice, Receipt, Refund, TransferCertificate, BonafideCertificate,
    CharacterCertificate, HallTicket, StaffCode
}

[Flags]
public enum WeekDays
{
    None = 0, Sunday = 1, Monday = 2, Tuesday = 4, Wednesday = 8, Thursday = 16, Friday = 32, Saturday = 64
}

/// <summary>Which Saturdays are off when Saturday is not a full weekly off.</summary>
public enum SaturdayOffPattern { None, All, SecondAndFourth, FirstAndThird, SecondOnly, Alternate }
