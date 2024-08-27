using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services
{
    internal interface IParserClass
    {
#pragma warning disable SA1414 // Tuple types in signatures should have element names
        public (ResponseClass, List<CompaniesHouseCompany>) ParseWithHelper(string filePath);
#pragma warning restore SA1414 // Tuple types in signatures should have element names

        public List<CompaniesHouseCompany> ParseTests(string filePath);

        // public (ResponseClass, List<CompaniesHouseCompany>) ParseErrors(string filePath);
    }
}
