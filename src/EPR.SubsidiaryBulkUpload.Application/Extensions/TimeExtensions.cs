using EPR.SubsidiaryBulkUpload.Application.Models;

namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

public static class TimeExtensions
{
    public static TimeSpan ToTimespan(this int time, TimeUnit timeUnits)
    {
        return ((double)time).ToTimespan(timeUnits);
    }

    public static TimeSpan ToTimespan(this double time, TimeUnit timeUnits)
    {
        return timeUnits switch
        {
            TimeUnit.Seconds => TimeSpan.FromSeconds(time),
            TimeUnit.Milliseconds => TimeSpan.FromMilliseconds(time),
            TimeUnit.Minutes => TimeSpan.FromMinutes(time),
            _ => throw new NotImplementedException()
        };
    }
}
