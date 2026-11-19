using System.Globalization;

namespace Services.Logic;

public static class WeekHelper
{
    private static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl-NL");

    public static (int Year, int Week) CurrentIsoWeek()
    {
        var today = DateTime.Today;
        var week = ISOWeek.GetWeekOfYear(today);
        var year = ISOWeek.GetYear(today);
        return (year, week);
    }

    public static IReadOnlyList<DateOnly> DaysOfIsoWeek(int year, int week)
    {
        var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(year, week, DayOfWeek.Monday));
        return Enumerable.Range(0, 7).Select(i => monday.AddDays(i)).ToList();
    }

    public static string DayLabel(DateOnly date) =>
        Dutch.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek);
}
