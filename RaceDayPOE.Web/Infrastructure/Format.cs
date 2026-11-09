using System.Globalization;
using RaceDayPOE.Web.Models.Api;

namespace RaceDayPOE.Web.Infrastructure;

//Display formatting shared by every view so dates, money, times and badges look identical on all pages.
public static class Format
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string Date(DateTime date) => date.ToString("dd MMM yyyy", Culture);
    public static string LongDate(DateTime date) => date.ToString("dddd, d MMMM yyyy", Culture);
    public static string Day(DateTime date) => date.ToString("dd", Culture);
    public static string Month(DateTime date) => date.ToString("MMM", Culture);
    public static string Rand(decimal amount) => amount == 0 ? "Free" : "R " + amount.ToString("N2", Culture).Replace(",", " ");
    public static string Km(decimal km) => km.ToString("0.##", Culture) + " km";
    public static string FinishTime(TimeSpan time) => time.TotalHours >= 1 || time.Days > 0
        ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}" : $"{time.Minutes:00}:{time.Seconds:00}";
    public static string FinishTimeInput(TimeSpan time) => $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";

    //1 -> "1st", 2 -> "2nd", 11 -> "11th", 47 -> "47th".
    public static string Ordinal(int n)
    {
        var lastTwo = n % 100;
        var suffix = lastTwo is >= 11 and <= 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return n.ToString(Culture) + suffix;
    }

    public static string TypeClass(EventType type) => "type-" + type.ToString().ToLowerInvariant();
    public static string CoverClass(EventType type) => "cover-" + type.ToString().ToLowerInvariant();
    public static string TypeIcon(EventType type) => type switch { EventType.Run => "lightning-charge-fill", EventType.Walk => "person-walking", _ => "bicycle" };
    public static string StatusClass(EnrolmentStatus status) => "status-" + status.ToString().ToLowerInvariant();
    public static string StatusIcon(EnrolmentStatus status) => status switch { EnrolmentStatus.Confirmed => "check-circle-fill", EnrolmentStatus.Pending => "hourglass-split", _ => "x-circle-fill" };
}
