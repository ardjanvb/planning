using Domain.Entities;
using Services.Logic;

namespace Services.Tests;

public class ShiftPatternHelperTests
{
    [Fact]
    public void ResolveForEmployee_uses_pattern_day()
    {
        var pattern = new CrewPattern
        {
            Name = "Dagdienst",
            CycleWeeks = 1,
            Days =
            [
                new PatternDay
                {
                    DayOfWeek = DayOfWeek.Monday,
                    WeekIndex = 0,
                    ShiftName = "Ochtend",
                    StartTime = new TimeOnly(6, 0),
                    EndTime = new TimeOnly(14, 0),
                    NetHours = 8
                }
            ]
        };
        var crew = new Crew { Name = "A", CrewPattern = pattern };
        var employee = new Employee { FirstName = "Jan", LastName = "J", PrimaryCrew = crew };

        var shift = ShiftPatternHelper.ResolveForEmployee(employee, new DateOnly(2026, 11, 23)); // Monday

        Assert.Equal("Ochtend", shift.ShiftName);
        Assert.Equal(new TimeOnly(6, 0), shift.StartTime);
        Assert.False(shift.IsFree);
    }

    [Fact]
    public void ResolveForEmployee_marks_free_day()
    {
        var pattern = new CrewPattern
        {
            Name = "Dagdienst",
            CycleWeeks = 1,
            Days =
            [
                new PatternDay
                {
                    DayOfWeek = DayOfWeek.Saturday,
                    WeekIndex = 0,
                    ShiftName = "Vrij",
                    NetHours = 0
                }
            ]
        };
        var employee = new Employee
        {
            FirstName = "Jan",
            LastName = "J",
            PrimaryCrew = new Crew { Name = "A", CrewPattern = pattern }
        };

        var shift = ShiftPatternHelper.ResolveForEmployee(employee, new DateOnly(2026, 11, 28)); // Saturday

        Assert.True(shift.IsFree);
        Assert.Equal("Vrij", shift.ShiftName);
    }

    [Fact]
    public void GetCycleWeekIndex_rotates_every_iso_week()
    {
        var pattern = new CrewPattern
        {
            Name = "3-ploegen",
            CycleWeeks = 3,
            EffectiveFrom = new DateOnly(2026, 1, 5) // Monday
        };

        Assert.Equal(0, ShiftPatternHelper.GetCycleWeekIndex(pattern, new DateOnly(2026, 1, 5)));
        Assert.Equal(1, ShiftPatternHelper.GetCycleWeekIndex(pattern, new DateOnly(2026, 1, 12)));
        Assert.Equal(2, ShiftPatternHelper.GetCycleWeekIndex(pattern, new DateOnly(2026, 1, 19)));
        Assert.Equal(0, ShiftPatternHelper.GetCycleWeekIndex(pattern, new DateOnly(2026, 1, 26)));
    }

    [Fact]
    public void ResolveForEmployee_uses_week_index_for_rotating_shift()
    {
        var pattern = new CrewPattern
        {
            Name = "3-ploegen",
            CycleWeeks = 3,
            EffectiveFrom = new DateOnly(2026, 1, 5),
            Days =
            [
                new PatternDay { WeekIndex = 0, DayOfWeek = DayOfWeek.Monday, ShiftName = "Ochtend", StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(14, 0), NetHours = 8 },
                new PatternDay { WeekIndex = 1, DayOfWeek = DayOfWeek.Monday, ShiftName = "Middag", StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(22, 0), NetHours = 8 },
                new PatternDay { WeekIndex = 2, DayOfWeek = DayOfWeek.Monday, ShiftName = "Nacht", StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0), NetHours = 8 }
            ]
        };
        var employee = new Employee
        {
            FirstName = "Jan",
            LastName = "J",
            PrimaryCrew = new Crew { Name = "A", CrewPattern = pattern }
        };

        Assert.Equal("Ochtend", ShiftPatternHelper.ResolveForEmployee(employee, new DateOnly(2026, 1, 5)).ShiftName);
        Assert.Equal("Middag", ShiftPatternHelper.ResolveForEmployee(employee, new DateOnly(2026, 1, 12)).ShiftName);
        Assert.Equal("Nacht", ShiftPatternHelper.ResolveForEmployee(employee, new DateOnly(2026, 1, 19)).ShiftName);
    }
}

