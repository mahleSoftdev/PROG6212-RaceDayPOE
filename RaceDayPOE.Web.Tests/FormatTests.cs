using RaceDayPOE.Web.Infrastructure;

namespace RaceDayPOE.Web.Tests;

//Display formatting is shared by every page, so it is worth pinning down.
public class FormatTests
{
    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(47, "47th")]
    [InlineData(111, "111th")]
    [InlineData(112, "112th")]
    [InlineData(312, "312th")]
    public void Ordinal_UsesTheCorrectSuffix(int number, string expected) => Assert.Equal(expected, Format.Ordinal(number));

    [Fact]
    public void FinishTime_ShowsMinutesAndSecondsUnderAnHour_AndHoursAfter()
    {
        Assert.Equal("47:03", Format.FinishTime(new TimeSpan(0, 47, 3)));
        Assert.Equal("1:05:09", Format.FinishTime(new TimeSpan(1, 5, 9)));
    }

    [Fact]
    public void FinishTimeInput_AlwaysUsesHhMmSs() => Assert.Equal("00:45:12", Format.FinishTimeInput(new TimeSpan(0, 45, 12)));

    [Fact]
    public void Rand_FormatsFeesAndShowsFreeForZero()
    {
        Assert.Equal("Free", Format.Rand(0));
        Assert.Equal("R 150.00", Format.Rand(150));
        Assert.Equal("R 1 250.50", Format.Rand(1250.5m));
    }

    [Fact]
    public void Km_DropsUnnecessaryDecimals()
    {
        Assert.Equal("10 km", Format.Km(10.00m));
        Assert.Equal("21.1 km", Format.Km(21.1m));
    }

    [Fact]
    public void Date_UsesTheSameDayMonthYearFormatEverywhere() => Assert.Equal("01 Dec 2026", Format.Date(new DateTime(2026, 12, 1)));
}
