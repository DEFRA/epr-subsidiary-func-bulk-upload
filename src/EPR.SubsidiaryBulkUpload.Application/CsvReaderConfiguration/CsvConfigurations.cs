using System.Globalization;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.Services;

namespace EPR.SubsidiaryBulkUpload.Application.CsvReaderConfiguration;

public static class CsvConfigurations
{
    public static CsvConfiguration BulkUploadCsvConfiguration =>
        new(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(),
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            MissingFieldFound = null,
            Delimiter = ",",
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            HeaderValidated = args =>
            {
                if (args.Context?.Reader is CustomCsvReader csvReader && args.InvalidHeaders.Length > 0)
                {
                    csvReader.InvalidHeaderErrors = args.InvalidHeaders?.Select(x => x.Names[0]).ToList();
                }
            }
        };
}
