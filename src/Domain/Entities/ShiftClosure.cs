using Domain.Entities.Interfaces;

namespace Domain.Entities;

/// <summary>
/// Company-wide closure of a named shift (Ochtend/Middag/Nacht) on a specific date.
/// When present, no assignments may use that shift on that day.
/// </summary>
public class ShiftClosure : IEntity
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string? Remark { get; set; }
}
