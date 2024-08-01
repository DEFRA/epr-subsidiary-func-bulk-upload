# EPR Subsidiary Bulk Upload

## Overview

Functions to handle bulk upload of subsidiary data.


## Running on a developer machine
To run locally, create a file `local.settings.json`. This file is in .gitignore.

```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "BlobStorage__ConnectionString": "UseDevelopmentStorage=true",
    "BlobStorage__SubsidiaryContainerName": "subsidiaries-upload-container"
  }
}
```
