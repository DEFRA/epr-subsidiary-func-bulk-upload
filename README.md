# EPR Subsidiary Bulk Upload

## Overview

Functions to handle bulk upload of subsidiary data.


## Environment Variables - deployed environments

The structure of the application settings can be found in the repository. Example configurations for the different environments can be found in [epr-app-config-settings](https://dev.azure.com/defragovuk/RWD-CPR-EPR4P-ADO/_git/epr-app-config-settings).

| Variable Name                                         | Description                                                                |
|-------------------------------------------------------|----------------------------------------------------------------------------|
| ApiConfig__SubsidiaryServiceBaseUrl                   | Accounts API base URL for getting/updating Subsidiary data                 |
| ApiConfig__Timeout                                    | Number of seconds before timing out request to Accounts API                |
| ApiConfig__CompaniesHouseLookupBaseUrl                | Url for the gateway to Companies House data                                |
| ApiConfig__AccountServiceClientId                     | Accounts API client ID                                                     | 
| ApiConfig__CompaniesHouseDirectBaseUri                | Url for direct access to Companies House data                              |
| ApiConfig__CompaniesHouseDirectApiKey                 | API key for the Companies House API - used for direct access only          |
| ApiConfig__UseDirectCompaniesHouseLookup              | Whether or not to access Companies House API directly                      |
| ApiConfig__RetryPolicyInitialWaitTime                 | The time to wait when calls to the Companies House API fail for the first time |
| ApiConfig__RetryPolicyMaxRetries                      | The number of times to retry failing calls to the Companies House API      |
| ApiConfig__RetryPolicyTooManyAttemptsWaitTime         | The time to wait when the Companies House API returns a 429 response for the first time       |
| ApiConfig__RetryPolicyTooManyAttemptsMaxRetries       | The number of times to retry retry when the Companies House API returns a 429 response for the first time       | 
| AntivirusApi__BaseUrl                                 | Antivirus API base URL                                                     |
| AntivirusApi__SubscriptionKey                         | Antivirus API APIM subscription key                                        |
| AntivirusApi__TenantId                                | Antivirus API APIM tenant ID                                               |
| AntivirusApi__ClientId                                | Antivirus API APIM client ID                                               |
| AntivirusApi__ClientSecret                            | Antivirus API APIM client secret                                           |
| AntivirusApi__Scope                                   | Antivirus API APIM scope                                                   |
| AntivirusApi__Timeout                                 | Number of seconds before timing out request to the Antivirus API           |
| AntivirusApi__CollectionSuffix                        | CollectionSuffix is appended to the collection name passed to the Antivirus API. It allows the Antivirus API to support message filtering on each subscription used in the different dev environments. |
| AntivirusApi__NotificationEmail                       | The email address that antivirus failure notifications should be sent to   |
| AntivirusApi__RetryPolicyMaxRetries                   | The number of times to retry failing calls to the Antivirus API            |
| AntivirusApi__RetryPolicyInitialWaitTime              | The time to wait when calls to the Antivirus API fail for the first time   |
| BlobStorage__ConnectionString                         | The connection string of the blob container on the storage account, where files will be stored    |
| BlobStorage__CompaniesHouseContainerName              | The name of the blob container on the storage account, where companies house files will be stored |
| BlobStorage__SubsidiaryContainerName                  | The name of the blob container on the storage account, where uploaded files will be stored        |
| CompaniesHouseDownload__Schedule                      | CRON expression with the schedule for Companies House downloads            | 
| CompaniesHouseDownload__CompaniesHouseDataDownloadUrl | URL for downloading Companies House data                                   |
| CompaniesHouseDownload__RetryPolicyMaxRetries         | The number of times to retry failing calls to the Companies House downloads         |
| CompaniesHouseDownload__RetryPolicyInitialWaitTime    | The time to wait when calls to the Companies House downloads fail for the first time       |
| Redis__ConnectionString                               | Connection string for Redis                                                |
| Redis__TimeToLiveInMinutes                            | Time to live (expiry) for Redis keysConnection string for Redis            |
| SubmissionApi__BaseUrl                                | The base URL for the Submission Status API WebApp                          |
| TableStorage__ConnectionString                        | The connection string of the table storage account, where companies house data will be stored     |
| TableStorage__CompaniesHouseOfflineDataTableName      | The name of the table in the storage account, where companies house data will be stored           |


## Retry policy

Polly is used to retry http requests. Policies are added to http clients in the in the `ConfigurationExtensions` class.

**Important** - if a timeout policy is used, the http client timeout should not be set in `AddHttpClient`, otherwise a `TaskCanceledException` will be thrown and not caught by the policy.
There is some good information on this [here](https://briancaos.wordpress.com/2020/12/16/httpclient-retry-on-http-timeout-with-polly-and-ihttpclientbuilder/)


## Running on a developer machine
To run locally, create a file `local.settings.json`. This file is in `.gitignore`.

```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ApiConfig__SubsidiaryServiceBaseUrl": "http://localhost:5000/",
    "ApiConfig__CompaniesHouseLookupBaseUrl": "https://integration-snd.azure.defra.cloud/ws/rest/DEFRA/v2.1/",
    "BlobStorage__ConnectionString": "UseDevelopmentStorage=true",
    "BlobStorage__SubsidiaryContainerName": "subsidiary-upload-container",
    "BlobStorage__CompaniesHouseContainerName": "subsidiary-companies-house-upload-container",
    "ApiConfig__AccountServiceClientId": "",

    "ApiConfig__CompaniesHouseDirectBaseUri": "https://api.company-information.service.gov.uk/",
    "ApiConfig__CompaniesHouseDirectApiKey": "",
    "ApiConfig__Timeout": 30
    "ApiConfig__RetryPolicyMaxRetries": "3",
    "ApiConfig__RetryPolicyInitialWaitTime": "1",
    "ApiConfig__RetryPolicyTooManyAttemptsMaxRetries": "4",
    "ApiConfig__RetryPolicyTooManyAttemptsWaitTime": "30",
    "ApiConfig__UseDirectCompaniesHouseLookup": "true",
    "AntivirusApi__BaseUrl": "",
    "AntivirusApi__SubscriptionKey": "",
    "AntivirusApi__TenantId": "",
    "AntivirusApi__ClientId": "",
    "AntivirusApi__ClientSecret": "",
    "AntivirusApi__Scope": "",
    "AntivirusApi__Timeout": 300,
    "AntivirusApi__CollectionSuffix": "",
    "AntivirusApi__NotificationEmail": "",
    "AntivirusApi__RetryPolicyInitialWaitTime": "5",
    "AntivirusApi__RetryPolicyMaxRetries": "4",
    "AntivirusApi__TimeUnits": "Seconds",
    "CompaniesHouseDownload__Schedule": "0 * 1 * * *"
    "CompaniesHouseDownload__CompaniesHouseDataDownloadUrl": "https://download.companieshouse.gov.uk/"
    "CompaniesHouseDownload__RetryPolicyInitialWaitTime": "10",
    "CompaniesHouseDownload__RetryPolicyMaxRetries": "3",
    "Redis__ConnectionString": "localhost:6379,abortConnect=false,connectTimeout=1500", 
    "Redis__TimeToLiveInMinutes": "720",   
    "SubmissionApi__BaseUrl": "https://localhost:7206",
    "TableStorage__ConnectionString": "UseDevelopmentStorage=true",
    "TableStorage__CompaniesHouseOfflineDataTableName": "CompaniesHouseData"
  }
}
```

There are some optional settings to control whether timeouts and retry times are in seconds, milliseconds or minutes. The value defaults to Seconds.

```
  "ApiConfig__TimeUnits": "Seconds",
  "AntivirusApi__TimeUnits": "Seconds",
  "CompaniesHouseDownload__TimeUnits": "Seconds",
```

The companies house API uses a gateway that cannot be accessed from `developer` machines. To work around this, set `ApiConfig__UseDirectCompaniesHouseLookup` to `true`, 
set `ApiConfig__CompaniesHouseDirectBaseUri` to `https://api.company-information.service.gov.uk/` and set `ApiConfig__CompaniesHouseDirectApiKey`
to a valid api key. 

You can get an api key by registering for a companies house account at https://developer.company-information.service.gov.uk/get-started and 
creating a new REST API application there.

To stop the companies house download function running all the time, set `CompaniesHouseDownload__Schedule` to `"0 0 5 31 2 *"` so it only
runs on the 31st of February - i.e. never.

The companies house download function can be triggered from Postman by setting up a `POST` request with url `http://localhost:7245/admin/functions/CompaniesHouseDownloadFunction`
and a header `x-functions-key` (value can be empty for local use, but the functions master key is needed if doing this for a function in Azure). The request also needs a 
json body which can be set to `{}`.

## Notification status

To check notifications you can call the following GET uri, where 
 - `devrwdwebfax408` should be replaced with the correct environment name
 - `{userId}` is the guid for the user in the accounts database 
 - `{organisationId}` is the external id (guid) for the organisation in the accounts database
 - `<function code>` is the function or master code found in the Azure portal

```
https://devrwdwebfax408.azurewebsites.net/api/notifications/status/{userId}/{organisationId}?code=<function code>
```

On local developer environment use
```
http://localhost:7245/api/notifications/status/{userId}/{organisationId}
```

To reset notifications, use the same url as DELETE. 

