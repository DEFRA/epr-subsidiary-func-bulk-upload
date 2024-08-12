using System.Diagnostics.CodeAnalysis;

namespace EPR.SubsidiaryBulkUpload.Application.Models;

[ExcludeFromCodeCoverage]
public class CompaniesHouseResponse
{
    public Accounts accounts { get; set; }

    public bool can_file { get; set; }

    public string company_name { get; set; }

    public string company_number { get; set; }

    public string company_status { get; set; }

    public ConfirmationStatement confirmation_statement { get; set; }

    public string date_of_creation { get; set; }

    public string etag { get; set; }

    public bool has_charges { get; set; }

    public bool has_insolvency_history { get; set; }

    public string jurisdiction { get; set; }

    public string last_full_members_list_date { get; set; }

    public Links links { get; set; }

    public RegisteredOfficeAddress registered_office_address { get; set; }

    public bool registered_office_is_in_dispute { get; set; }

    public List<string> sic_codes { get; set; }

    public string type { get; set; }

    public bool undeliverable_registered_office_address { get; set; }

    public bool has_super_secure_pscs { get; set; }

    public string proof_status { get; set; }

    public AddressModel Address { get; set; } = null!;
}