using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EPR.SubsidiaryBulkUpload.Application.Clients.Interfaces;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests;

public class CompaniesHouseImportFunctionTests
{
    private Mock<IAntivirusApiClient> _antivirusApiClientMock;
    private Mock<IConfiguration> _configurationMock;
    private Mock<ICsvProcessor> _csvProcessor;

    private Mock<ILogger<CompaniesHouseImportFunction>> _loggerMock;
    private CompaniesHouseImportFunction _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _antivirusApiClientMock = new Mock<IAntivirusApiClient>();
        _configurationMock = new Mock<IConfiguration>();
        _csvProcessor = new Mock<ICsvProcessor>();

        _loggerMock = new Mock<ILogger<CompaniesHouseImportFunction>>();
        _systemUnderTest = new CompaniesHouseImportFunction(_antivirusApiClientMock.Object, _configurationMock.Object, _csvProcessor.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task CompaniesHouseImportFunction_Accepts_Blob()
    {
        var content = "header1,header2\nval1,val2";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var mockBlobClient = new Mock<BlobClient>();
        var responseMock = new Mock<Response>();
        mockBlobClient
            .Setup(m => m.GetPropertiesAsync(null, CancellationToken.None).Result)
            .Returns(Response.FromValue<BlobProperties>(new BlobProperties(), responseMock.Object));
        mockBlobClient
            .SetupGet(m => m.BlobContainerName)
            .Returns("test_container");
        mockBlobClient
            .SetupGet(m => m.Name)
            .Returns("test_blob");
        mockBlobClient
            .Setup(m => m.GetPropertiesAsync(null, CancellationToken.None).Result)
            .Returns(Response.FromValue<BlobProperties>(new BlobProperties(), responseMock.Object));

        await _systemUnderTest.Run(mockBlobClient.Object);
    }
}
