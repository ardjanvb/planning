using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class Employee : IEntity
{
    public int Id { get; set; }
    public string PersonnelNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? PrimaryJobFunctionId { get; set; }
    public JobFunction? PrimaryJobFunction { get; set; }
    public bool Active { get; set; } = true;
    public bool IsBhv { get; set; }
    public DateOnly? BhvValidUntil { get; set; }
    public decimal ContractHoursPerWeek { get; set; } = 40;
    public decimal Fte { get; set; } = 1;
    public int? PrimaryCrewId { get; set; }
    public Crew? PrimaryCrew { get; set; }
    public string? Remarks { get; set; }
    public DateOnly? EmploymentStart { get; set; }
    public DateOnly? EmploymentEnd { get; set; }
    public int? PrimaryWorkplaceId { get; set; }
    public Workplace? PrimaryWorkplace { get; set; }

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    public ICollection<EmployeeQualification> Qualifications { get; set; } = new List<EmployeeQualification>();
    public ICollection<EmployeeCrewMembership> CrewMemberships { get; set; } = new List<EmployeeCrewMembership>();
    public ICollection<Absence> Absences { get; set; } = new List<Absence>();
    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
    public ICollection<EmployeeChange> Changes { get; set; } = new List<EmployeeChange>();
}

public class EmployeeQualification : IEntity
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int QualificationId { get; set; }
    public Qualification? Qualification { get; set; }
    public DateOnly? ValidUntil { get; set; }
}

public class EmployeeCrewMembership : IEntity
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int CrewId { get; set; }
    public Crew? Crew { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
}

public class EmployeeChange : IEntity
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
