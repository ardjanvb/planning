using Services.Logic;

namespace Services.Tests;

public class WeekHelperTests
{
    [Fact]
    public void DaysOfIsoWeek_returns_seven_days_starting_monday()
    {
        var days = WeekHelper.DaysOfIsoWeek(2026, 48);
        Assert.Equal(7, days.Count);
        Assert.Equal(DayOfWeek.Monday, days[0].DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, days[6].DayOfWeek);
    }
}
