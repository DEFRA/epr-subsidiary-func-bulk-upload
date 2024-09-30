using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Options;

namespace EPR.SubsidiaryBulkUpload.Application.Extensions;

public static class OptionsExtensions
{
    public static TimeSpan ConvertToTimespan(this ApiOptions apiOptions, int time)
    {
        return apiOptions.ConvertToTimespan((double)time);
    }

    public static TimeSpan ConvertToTimespan(this ApiOptions apiOptions, double time)
    {
        return apiOptions.TimeUnits switch
        {
            TimeUnit.Seconds => TimeSpan.FromSeconds(time),
            TimeUnit.Milliseconds => TimeSpan.FromMilliseconds(time),
            TimeUnit.Minutes => TimeSpan.FromMinutes(time),
            _ => throw new NotImplementedException()
        };
    }
}
