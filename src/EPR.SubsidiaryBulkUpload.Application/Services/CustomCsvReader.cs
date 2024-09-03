using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace EPR.SubsidiaryBulkUpload.Application.Services;

public class CustomCsvReader : CsvReader
{
    public CustomCsvReader(IParser parser)
        : base(parser)
    {
    }

    public CustomCsvReader(TextReader reader, IReaderConfiguration configuration)
        : base(reader, configuration)
    {
    }

    public CustomCsvReader(TextReader reader, CultureInfo culture, bool leaveOpen = false)
        : base(reader, culture, leaveOpen)
    {
    }

    public virtual MissingFieldFound MissingFieldMappingFound { get; set; }

    public virtual List<InvalidHeader> InvalidHeaders { get; set; }

    public virtual List<string> InvalidHeaderErrors { get; set; }

    protected void ValidateHeader(ClassMap map, List<InvalidHeader> invalidHeaders)
    {
        base.ValidateHeader(map, invalidHeaders);
        var validationErrors = new List<string>();

        if (invalidHeaders.Count == 0)
        {
            for (var i = 0; i < HeaderRecord.Length; i++)
            {
                var header = HeaderRecord[i];
                if (!isHeaderMapped(map, header, i))
                {
                    validationErrors.Add(header);
                }
            }

            if (validationErrors.Count != 0)
            {
                InvalidHeaderErrors = validationErrors;
            }
        }
    }

    private bool isHeaderMapped(ClassMap map, string header, int index)
    {
        var prepareHeaderForMatchArgs = new PrepareHeaderForMatchArgs(header, index);
        var headerName = Configuration.PrepareHeaderForMatch(prepareHeaderForMatchArgs);

        foreach (var parameter in map.ParameterMaps)
        {
            foreach (var name in parameter.Data.Names)
            {
                var prepareHForMatchArgs = new PrepareHeaderForMatchArgs(name, index);
                if (Configuration.PrepareHeaderForMatch(prepareHForMatchArgs) == headerName)
                {
                    return true;
                }
            }
        }

        foreach (var memberMap in map.MemberMaps)
        {
            foreach (var name in memberMap.Data.Names)
            {
                var prepareHForMatchArgs = new PrepareHeaderForMatchArgs(name, index);
                if (Configuration.PrepareHeaderForMatch(prepareHForMatchArgs) == headerName)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
