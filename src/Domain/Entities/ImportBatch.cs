using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class ImportBatch : IEntity
{
    public int Id { get; set; }
    public DateTime ImportedAt { get; set; }
    public string ImportedBy { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public int ProcessedRows { get; set; }
    public int ErrorCount { get; set; }
    public string? DiffSummary { get; set; }
    public bool Confirmed { get; set; }

    public ICollection<CapacityNeed> Needs { get; set; } = new List<CapacityNeed>();
}

public class CapacityNeed : IEntity
{
    public int Id { get; set; }
    public int ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    public int Year { get; set; }
    public int WeekNumber { get; set; }
    public int WorkplaceId { get; set; }
    public Workplace? Workplace { get; set; }
    public int RequiredShifts { get; set; }
    public decimal HoursPerShift { get; set; } = 8;
    public DateOnly? SpecificDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool IsActive { get; set; } = true;

    public decimal RequiredHours => RequiredShifts * HoursPerShift;
}
