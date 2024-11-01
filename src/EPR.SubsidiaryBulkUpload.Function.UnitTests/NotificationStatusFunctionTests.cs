using System.Net;
using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
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
        var rowsAdded = "1";
        var errorStatus = default(string);

        var statusJson =
            """
            {
                "status": "Working",
                "rowsAdded": 1,
                "errors": []
            }
            """;

        _notificationServiceMock.Setup(x => x.GetStatus(_progressKey))
            .ReturnsAsync(TestStatus);

        _notificationServiceMock.Setup(x => x.GetStatus(_rowsAddedKey))
            .ReturnsAsync(rowsAdded);

        _notificationServiceMock.Setup(x => x.GetStatus(_errorKey))
            .ReturnsAsync(errorStatus);

        // Act
        var result = await _systemUnderTest.Run(Mock.Of<HttpRequest>(), _userId, _organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();

        _notificationServiceMock.Verify(x => x.GetStatus(_progressKey), Times.Once);
        _notificationServiceMock.Verify(x => x.GetStatus(_rowsAddedKey), Times.Once);
        _notificationServiceMock.Verify(x => x.GetStatus(_errorKey), Times.Once);
    }

    [TestMethod]
    public async Task NotificationStatusFunction_Returns_NotFound_WhenKeyIsMissing()
    {
        // Arrange
        var key = $"{_userId}{_organisationId}Subsidiary bulk upload progress";
        _notificationServiceMock.Setup(x => x.GetStatus(It.IsAny<string>()))
            .ReturnsAsync(default(string));

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
        _notificationServiceMock.Setup(x => x.GetStatus(It.IsAny<string>()))
            .ThrowsAsync(exception);

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
        _notificationServiceMock.Setup(x => x.ClearRedisKeyAsync(It.IsAny<string>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _systemUnderTest.Delete(Mock.Of<HttpRequest>(), _userId, _organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusCodeResult>();
        ((StatusCodeResult)result).StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        _notificationServiceMock.Verify(x => x.ClearRedisKeyAsync(It.IsAny<string>()), Times.Once);
    }
}
