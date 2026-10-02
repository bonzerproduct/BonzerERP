namespace Bonzer.Domain.Enums;

public enum Gender { Male, Female, Other, NotSpecified }

/// <summary>Kind of login. A user has one type but may hold several roles.</summary>
public enum UserType { PlatformAdmin, Staff, Guardian, Student }

public enum StudentStatus { Applicant, Active, Alumni, TransferredOut, Dropped, Suspended }

public enum EnrollmentStatus { Active, Promoted, Detained, Completed, Withdrawn }

public enum GuardianRelation { Father, Mother, Guardian, Grandparent, Sibling, Uncle, Aunt, Other }

public enum EmploymentType { Permanent, Probation, Contract, PartTime, Visiting, Intern }

/// <summary>Broad staff group used for reports, payroll groups and UDISE+ returns.</summary>
public enum StaffType { Teaching, NonTeaching, Administrative, Support, Management }

/// <summary>Employment lifecycle. Exit statuses (Resigned, Terminated, Retired) and Suspended disable the login.</summary>
public enum StaffStatus { Active, OnLeave, OnNotice, Suspended, Resigned, Terminated, Retired }

public enum MaritalStatus { Single, Married, Divorced, Widowed, Separated }
