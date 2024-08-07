#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Copy everything
COPY NuGet.Config ./
COPY stylecop.ruleset ./
COPY Directory.Build.props ./

COPY EPR.SubsidiaryBulkUpload.Application/. ./EPR.SubsidiaryBulkUpload.Application/.
COPY EPR.SubsidiaryBulkUpload.Function/. ./EPR.SubsidiaryBulkUpload.Function/.

RUN dotnet publish EPR.SubsidiaryBulkUpload.Function/*.csproj --output /home/site/wwwroot

# Create a non-root user and set permissions
RUN groupadd -r dotnet && \
    useradd -r -g dotnet dotnet && \
    chown -R dotnet /home

# Switch to the non-root user for the final image
USER dotnet

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=build --chown=dotnet ["/home/site/wwwroot", "/home/site/wwwroot"]

