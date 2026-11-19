using System.Globalization;
using Domain.Entities;

namespace Services.Logic;

public record ResolvedShift(string ShiftName, TimeOnly StartTime, TimeOnly EndTime, decimal NetHours, bool IsFree);

public static class ShiftPatternHelper
{
    /// <summary>
    /// Resolves the shift for an employee on a date using their crew pattern.
    /// Multi-week cycles rotate by ISO week (ochtend → middag → nacht).
    /// </summary>
    public static ResolvedShift ResolveForEmployee(Employee? employee, DateOnly date)
    {
        var pattern = employee?.PrimaryCrew?.CrewPattern;
        if (pattern?.Days is { Count: > 0 })
        {
            var weekIndex = GetCycleWeekIndex(pattern, date);
            var day = pattern.Days.FirstOrDefault(d => d.WeekIndex == weekIndex && d.DayOfWeek == date.DayOfWeek)
                      ?? pattern.Days.FirstOrDefault(d => d.DayOfWeek == date.DayOfWeek);

            if (day is not null)
            {
                var isFree = string.Equals(day.ShiftName, "Vrij", StringComparison.OrdinalIgnoreCase)
                             || day.NetHours <= 0
                             || day.StartTime is null
                             || day.EndTime is null;

                if (isFree)
                {
                    return new ResolvedShift("Vrij", new TimeOnly(6, 0), new TimeOnly(14, 0), 0, true);
                }

                return new ResolvedShift(
                    string.IsNullOrWhiteSpace(day.ShiftName) ? "Ochtend" : day.ShiftName,
                    day.StartTime!.Value,
                    day.EndTime!.Value,
                    day.NetHours > 0 ? day.NetHours : 8,
                    false);
            }
        }

        return new ResolvedShift("Ochtend", new TimeOnly(6, 0), new TimeOnly(14, 0), 8, false);
    }

    public static int GetCycleWeekIndex(CrewPattern pattern, DateOnly date)
    {
        if (pattern.CycleWeeks <= 1)
            return 0;

        var anchor = pattern.EffectiveFrom ?? new DateOnly(date.Year, 1, 4); // week containing Jan 4
        var anchorDt = anchor.ToDateTime(TimeOnly.MinValue);
        var dateDt = date.ToDateTime(TimeOnly.MinValue);

        var anchorYear = ISOWeek.GetYear(anchorDt);
        var anchorWeek = ISOWeek.GetWeekOfYear(anchorDt);
        var dateYear = ISOWeek.GetYear(dateDt);
        var dateWeek = ISOWeek.GetWeekOfYear(dateDt);

        // Approximate week delta across years via Monday dates
        var anchorMonday = DateOnly.FromDateTime(ISOWeek.ToDateTime(anchorYear, anchorWeek, DayOfWeek.Monday));
        var dateMonday = DateOnly.FromDateTime(ISOWeek.ToDateTime(dateYear, dateWeek, DayOfWeek.Monday));
        var weeks = (dateMonday.DayNumber - anchorMonday.DayNumber) / 7;
        var cycle = pattern.CycleWeeks;
        return ((weeks % cycle) + cycle) % cycle;
    }
}
