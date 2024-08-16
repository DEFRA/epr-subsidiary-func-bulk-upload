using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class CompaniesHouseCsvProcessor(
    ILogger<CompaniesHouseCsvProcessor> logger) : ICompaniesHouseCsvProcessor
{
    private readonly ILogger<CompaniesHouseCsvProcessor> _logger = logger;

    public async Task<int> ProcessStream(Stream stream)
    {
        var rowCount = 0;

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            HasHeaderRecord = true,
        });

        await csv.ReadAsync();
        csv.ReadHeader();
        var header = csv.HeaderRecord;
        _logger.LogInformation("Found csv header of length {Length} - {Values}", header.Length, string.Join(',', header));

        while (await csv.ReadAsync())
        {
            rowCount++;

            var field = csv.GetField(0);
            if (rowCount % 1000 == 0)
            {
                _logger.LogInformation("Found csv field {RowCount}\t{Value}", rowCount, field);
            }
        }

        return rowCount;
    }

    public async Task<IEnumerable<T>> ProcessStreamToObject<T>(Stream stream, T streamObj)
    {
        List<T> records;

        using var streamReader = new StreamReader(stream);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Trim(),
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using (var csv = new CsvReader(streamReader, config))
        {
            records = csv.GetRecords<T>().ToList();
        }

        _logger.LogInformation("Found csv records {RecordsCount}", records.Count);

        return records;
    }
}
