# EPR Subsidiary Bulk Upload

## Overview

Functions to handle bulk upload of subsidiary data.


## Running on a developer machine
To run locally, create a file `local.settings.json`. This file is in `.gitignore`.

```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "BlobStorage__ConnectionString": "UseDevelopmentStorage=true",
    "BlobStorage__SubsidiaryContainerName": "subsidiary-upload-container",
    "BlobStorage__CompaniesHouseContainerName": "subsidiary-companies-house-upload-container",
    "TableStorage__ConnectionString": "UseDevelopmentStorage=true",
    "TableStorage__CompaniesHouseOfflineDataTableName": "CompaniesHouseData",
    "Redis__ConnectionString": "localhost",
    "CompaniesHouseDownload__Schedule": "0 */5 * * * *"
    "ApiConfig__SubsidiaryServiceBaseUrl": "http://localhost:5000/",
    "ApiConfig__CompaniesHouseLookupBaseUrl": "https://integration-snd.azure.defra.cloud/ws/rest/DEFRA/v2.1/",
    "ApiConfig__AddressLookupBaseUrl": "https://integration-snd.azure.defra.cloud/ws/rest/DEFRA/v1/address/",
    "CompaniesHouseApi__BaseUri": "https://api.company-information.service.gov.uk/",
    "CompaniesHouseApi__ApiKey": "",
    "ApiConfig__StorageConnectionString": "",
    "ApiConfig__DeveloperMode": true,
    "ApiConfig__Timeout": 30
    "ApiConfig__CompaniesHouseDataDownloadUrl": "https://download.companieshouse.gov.uk/",
    "ApiConfig__SystemUserId": "59F5EE8E-690C-4D54-B285-4B04AC8124E3",
    "ApiConfig__SystemOrganisationId": "7CD04AD0-55FD-4295-8F8A-93D7477486DE",
    "SubmissionApi__BaseUrl": "https://localhost:7206"
  }
}
```
