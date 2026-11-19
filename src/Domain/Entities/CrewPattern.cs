using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class CrewPattern : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CycleWeeks { get; set; } = 1;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool Active { get; set; } = true;

    public ICollection<PatternDay> Days { get; set; } = new List<PatternDay>();
    public ICollection<Crew> Crews { get; set; } = new List<Crew>();
}

public class PatternDay : IEntity
{
    public int Id { get; set; }
    public int CrewPatternId { get; set; }
    public CrewPattern? CrewPattern { get; set; }
    public int WeekIndex { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string ShiftName { get; set; } = "Vrij";
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int PauseMinutes { get; set; }
    public decimal NetHours { get; set; }
}
