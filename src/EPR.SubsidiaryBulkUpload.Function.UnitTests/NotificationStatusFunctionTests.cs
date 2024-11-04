using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Models;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using EPR.SubsidiaryBulkUpload.Function.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests;

[TestClass]
public class NotificationStatusFunctionTests
{
    private const string TestStatus = "Working";

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _organisationId = Guid.NewGuid();

    private string _progressKey;
    private string _rowsAddedKey;
    private string _errorKey;

    private Mock<INotificationService> _notificationServiceMock;
    private NotificationStatusFunction _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _progressKey = $"{_userId}{_organisationId}Subsidiary bulk upload progress";
        _rowsAddedKey = $"{_userId}{_organisationId}Subsidiary bulk upload rows added";
        _errorKey = $"{_userId}{_organisationId}Subsidiary bulk upload errors";

        _notificationServiceMock = new Mock<INotificationService>();

        _systemUnderTest = new NotificationStatusFunction(_notificationServiceMock.Object);
    }

    [TestMethod]
    public async Task NotificationStatusFunction_Calls_NotificationStatusService()
    {
        // Arrange
        var rowsAdded = "3";
        var errorsJson =
            """
            {
              "errors": [
                {
                  "fileLineNumber": 2,
                  "fileContent": "123456,Test1,XXX LIMITED,11111111,Child,",
                  "message": "The parent Organisation is not valid. Child cannot be processed.",
                  "errorNumber": 125,
                  "isError": true
                },
                {
                  "fileLineNumber": 1,
                  "fileContent": "123456,,FAKE PARENT,34564000,Parent,",
                  "message": "Parent organisation is not found.",
                  "errorNumber": 123,
                  "isError": true
                }
              ]
            }
            """;
        var expectedErrors = new UploadFileErrorCollectionModel
        {
            Errors = new List<UploadFileErrorModel>
            {
                new()
                {
                    FileLineNumber = 1,
                    ErrorNumber = 123,
                    FileContent = "123456,,FAKE PARENT,34564000,Parent,",
                    IsError = true,
                    Message = "Parent organisation is not found."
                },
                new()
                {
                    FileLineNumber = 2,
                    ErrorNumber = 125,
                    FileContent = "123456,Test1,XXX LIMITED,11111111,Child,",
                    IsError = true,
                    Message = "The parent Organisation is not valid. Child cannot be processed."
                }
            }
        };

        _notificationServiceMock.Setup(x => x.GetStatus(_progressKey)).ReturnsAsync(TestStatus);
        _notificationServiceMock.Setup(x => x.GetStatus(_rowsAddedKey)).ReturnsAsync(rowsAdded);
        _notificationServiceMock.Setup(x => x.GetStatus(_errorKey)).ReturnsAsync(errorsJson);

        // Act
        var result = await _systemUnderTest.Run(Mock.Of<HttpRequest>(), _userId, _organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();

        var jsonResult = result as JsonResult;
        var statusResult = jsonResult.GetDynamicPropertyValue<string?>("Status");
        var rowsAddedResult = jsonResult.GetDynamicPropertyValue<int?>("RowsAdded");
        var errorsResult = jsonResult.GetDynamicPropertyValue<UploadFileErrorCollectionModel?>("Errors");

        statusResult.Should().Be(TestStatus);
        rowsAddedResult.Should().Be(3);
        errorsResult.Should().NotBeNull();
        errorsResult.Should().BeEquivalentTo(expectedErrors);

        _notificationServiceMock.Verify(x => x.GetStatus(_progressKey), Times.Once);
        _notificationServiceMock.Verify(x => x.GetStatus(_rowsAddedKey), Times.Once);
        _notificationServiceMock.Verify(x => x.GetStatus(_errorKey), Times.Once);
    }

    [TestMethod]
    public async Task NotificationStatusFunction_Calls_NotificationStatusService_WhenErrorsIsNull()
    {
        // Arrange
        var rowsAdded = "2";

        var statusJson =
            """
            {
                status = Working,
                rowsAdded = 2,
                errors =  null
            }
            """;

        _notificationServiceMock.Setup(x => x.GetStatus(_progressKey)).ReturnsAsync(TestStatus);
        _notificationServiceMock.Setup(x => x.GetStatus(_rowsAddedKey)).ReturnsAsync(rowsAdded);
        _notificationServiceMock.Setup(x => x.GetStatus(_errorKey)).ReturnsAsync(default(string?));

        // Act
        var result = await _systemUnderTest.Run(Mock.Of<HttpRequest>(), _userId, _organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();

        var jsonResult = result as JsonResult;
        var statusResult = jsonResult.GetDynamicPropertyValue<string?>("Status");
        var rowsAddedResult = jsonResult.GetDynamicPropertyValue<int?>("RowsAdded");
        var errorsResult = jsonResult.GetDynamicPropertyValue<UploadFileErrorCollectionModel?>("Errors");

        statusResult.Should().Be(TestStatus);
        rowsAddedResult.Should().Be(2);
        errorsResult.Should().BeNull();

        _notificationServiceMock.Verify(x => x.GetStatus(_progressKey), Times.Once);
        _notificationServiceMock.Verify(x => x.GetStatus(_rowsAddedKey), Times.Once);
        _notificationServiceMock.Verify(x => x.GetStatus(_errorKey), Times.Once);
    }

    [TestMethod]
    public async Task NotificationStatusFunction_Returns_NotFound_WhenKeyIsMissing()
    {
        // Arrange
        var key = $"{_userId}{_organisationId}Subsidiary bulk upload progress";
        _notificationServiceMock.Setup(x => x.GetStatus(It.IsAny<string>())).ReturnsAsync(default(string));

        // Act
        var result = await _systemUnderTest.Run(Mock.Of<HttpRequest>(), _userId, _organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NotFoundResult>();

        _notificationServiceMock.Verify(x => x.GetStatus(key), Times.Once);
    }

    [TestMethod]
    public async Task NotificationStatusFunction_Returns_InternalStatusError_WhenExceptionIsThrown()
    {
        // Arrange
        var exception = new Exception("Test Exception");
        _notificationServiceMock.Setup(x => x.GetStatus(It.IsAny<string>())).ThrowsAsync(exception);

        // Act
        var result = await _systemUnderTest.Run(Mock.Of<HttpRequest>(), _userId, _organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusCodeResult>();
        ((StatusCodeResult)result).StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        _notificationServiceMock.Verify(x => x.GetStatus(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task NotificationStatusDeleteFunction_Calls_NotificationStatusService()
    {
        // Arrange & // Act
        var result = await _systemUnderTest.Delete(Mock.Of<HttpRequest>(), _userId, _organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<AcceptedResult>();

        _notificationServiceMock.Verify(x => x.ClearRedisKeyAsync(_progressKey), Times.Once);
        _notificationServiceMock.Verify(x => x.ClearRedisKeyAsync(_rowsAddedKey), Times.Once);
        _notificationServiceMock.Verify(x => x.ClearRedisKeyAsync(_errorKey), Times.Once);
    }

    [TestMethod]
    public async Task NotificationStatusDeleteFunction_Returns_InternalStatusError_WhenExceptionIsThrown()
    {
        // Arrange
        var exception = new Exception("Test Exception");
        _notificationServiceMock.Setup(x => x.ClearRedisKeyAsync(It.IsAny<string>())).ThrowsAsync(exception);

        // Act
        var result = await _systemUnderTest.Delete(Mock.Of<HttpRequest>(), _userId, _organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusCodeResult>();
        ((StatusCodeResult)result).StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        _notificationServiceMock.Verify(x => x.ClearRedisKeyAsync(It.IsAny<string>()), Times.Once);
    }
}
