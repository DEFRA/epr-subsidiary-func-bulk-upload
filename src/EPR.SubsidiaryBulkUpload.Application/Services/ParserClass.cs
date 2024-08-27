using CsvHelper;
using CsvHelper.Configuration;
using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    internal class ParserClass : IParserClass
    {
        // With CsvHelper
#pragma warning disable SA1414 // Tuple types in signatures should have element names
        public (ResponseClass, List<CompaniesHouseCompany>) ParseWithHelper(string filePath)
#pragma warning restore SA1414 // Tuple types in signatures should have element names
        {
            var response = new ResponseClass() { isDone = false, Messages = "None" };
            var rows = new List<CompaniesHouseCompany>();

            try
            {
                rows = ParseTests(filePath);
                response = new ResponseClass() { isDone = true, Messages = "All Done!" };
            }
            catch (Exception e)
            {
                response = new ResponseClass() { isDone = false, Messages = e.Message };
            }

            return (response, rows);
        }

        public List<CompaniesHouseCompany> ParseTests(string filePath)
        {
            var rows = new List<CompaniesHouseCompany>();
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified file was not found.", filePath);
            }

            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);

            config.Delimiter = ",";
            config.MissingFieldFound = null;
            config.TrimOptions = TrimOptions.Trim;
            config.HeaderValidated = null;
            config.BadDataFound = null;

            IList<string> readRow = new List<string>();

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<CompaniesHouseCompanyMap>();
                rows = csv.GetRecords<CompaniesHouseCompany>().ToList();
            }

            return rows;
        }
    }
}