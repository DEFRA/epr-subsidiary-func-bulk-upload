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

    public CustomCsvReader(TextReader reader, CsvConfiguration configuration)
        : base(reader, configuration)
    {
    }

    public CustomCsvReader(TextReader reader, CultureInfo culture, bool leaveOpen = false)
        : base(reader, culture, leaveOpen)
    {
    }

    public virtual MissingFieldFound MissingFieldMappingFound { get; set; }

    protected override void ValidateHeader(ClassMap map, List<InvalidHeader> invalidHeaders)
    {
        base.ValidateHeader(map, invalidHeaders);

        // We'll only run our validation if the base validation did not find any problems (otherwise we would need to throw
        // a single exception signalling both kinds of problems, which is hard to implement in a subclass)
        if (!invalidHeaders.Any())
        {
            var unexpectedHeaders = new List<string>();
            for (var i = 0; i < HeaderRecord.Length; i++)
            {
                var header = HeaderRecord[i];
                if (!isHeaderMapped(map, header, i))
                {
                    unexpectedHeaders.Add(header);
                }
            }

            if (unexpectedHeaders.Any())
            {
                // Adding headers to `invalidHeaders` causes a HeaderValidationException to be thrown later with a message that
                // implies that expected headers were not found, which is not the case (it's actually the other way around). Thus,
                // we throw a custom exception instead. Note that this will "escape" any custom handling set on `Configuration.HeaderValidated`
                throw new UnexpectedHeadersException(Context, unexpectedHeaders);
            }
        }

     /*   base.ValidateHeader(map, invalidHeaders);

        for (var i = 0; i < HeaderRecord.Length; i++)
        {
            var header = HeaderRecord[i];
            if (!isHeaderMapped(map, header, i))
            {
                MissingFieldMappingFound?.Invoke(new MissingFieldFoundArgs(new[] { header }, i, Context));
            }
        }*/
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

        /*var headerName = Configuration.PrepareHeaderForMatch(new PrepareHeaderForMatchArgs(header, index));

        if (map.ParameterMaps.Any(parameter => parameter.Data.Names.Any(name => name == headerName)))
        {
            return true;
        }

        return map.MemberMaps.Any(memberMap => memberMap.Data.Names.Any(name => name == headerName));

        // Not sure whether we should iterate `map.ReferenceMaps`*/
    }
}
