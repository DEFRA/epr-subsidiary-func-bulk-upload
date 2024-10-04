using System.ComponentModel.DataAnnotations;

namespace EPR.SubsidiaryBulkUpload.Application.Models.Submission;

public enum SubmissionType
{
    [Display(Name = "pom")]
    Producer = 1,
    [Display(Name = "registration")]
    Registration = 2,
    [Display(Name = "subsidiary")]
    Subsidiary = 3,
    [Display(Name = "companiesHouse")]
    CompaniesHouse = 4,
}