namespace EPR.SubsidiaryBulkUpload.Application.Models;
public class Address
{
    public object subBuildingName { get; set; }

    public object buildingName { get; set; }

    public string buildingNumber { get; set; }

    public string street { get; set; }

    public object locality { get; set; }

    public object dependentLocality { get; set; }

    public string town { get; set; }

    public string county { get; set; }

    public string postcode { get; set; }

    public string country { get; set; }
}
