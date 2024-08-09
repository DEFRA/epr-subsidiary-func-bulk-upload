using Azure;
using Azure.Data.Tables;
using CsvHelper.Configuration.Attributes;

public class CompanyHouseTableEntity : ITableEntity
{
    [Name("CompanyName")]
    public string CompanyName { get; set; }

    [Name("CompanyNumber")]
    public string CompanyNumber { get; set; }

    [Name("RegAddress.AddressLine1")]
    public string RegAddressAddressLine1 { get; set; }

    [Name("RegAddress.PostTown")]
    public string RegAddressPostTown { get; set; }

    [Name("RegAddress.County")]
    public string RegAddressCounty { get; set; }

    [Name("RegAddress.Country")]
    public string RegAddressCountry { get; set; }

    [Name("RegAddress.PostCode")]
    public string RegAddressPostCode { get; set; }

    [Name("CompanyStatus")]
    public string CompanyStatus { get; set; }

    [Name("CountryOfOrigin")]
    public string CountryOfOrigin { get; set; }

    [Name("IncorporationDate")]
    public string IncorporationDate { get; set; }

    public string PartitionKey { get; set; }

    public string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}