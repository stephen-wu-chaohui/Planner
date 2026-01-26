namespace Planner.BlazorApp.Services;

static public class TimeFormat {
    public static string Readable(double minutesFromStart) {
        var startTime = new DateTime(2025, 1, 1, 8, 30, 0); // 8:30 AM
        var arrival = startTime.AddMinutes(minutesFromStart);
        return arrival.ToString("hh:mm tt");
    }

    public static string FormatDuration(double totalMinutes) {
        var hours = (int)(totalMinutes / 60);
        var minutes = (int)(totalMinutes % 60);

        if (hours > 0)
            return $"{hours}H {minutes}M";
        else
            return $"{minutes}M";
    }

}
