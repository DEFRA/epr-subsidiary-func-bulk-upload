﻿using EPR.SubsidiaryBulkUpload.Application.DTOs;

namespace EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;

public interface IRecordExtraction
{
    IEnumerable<ParentAndSubsidiaries> ExtractParentsAndChildren(IEnumerable<CompaniesHouseCompany> source);
}