using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class CalendarDay : IEntity
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public CalendarDayType Type { get; set; } = CalendarDayType.WorkDay;
    public string? Remark { get; set; }
}

public class WorkplaceCalendarException : IEntity
{
    public int Id { get; set; }
    public int WorkplaceId { get; set; }
    public Workplace? Workplace { get; set; }
    public DateOnly Date { get; set; }
    public CalendarDayType Type { get; set; }
    public string? Remark { get; set; }
}
