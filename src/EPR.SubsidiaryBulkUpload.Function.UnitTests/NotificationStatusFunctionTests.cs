using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests;

[TestClass]
public class NotificationStatusFunctionTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _organisationId = Guid.NewGuid();

    private Mock<INotificationService> _notificationServiceMock;
    private NotificationStatusFunction _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _notificationServiceMock = new Mock<INotificationService>();

        _systemUnderTest = new NotificationStatusFunction(_notificationServiceMock.Object);
    }

    [TestMethod]
    public async Task NotificationStatusFunction_Calls_CsvService()
    {
        // Arrange
        var progressKey = $"{_userId}{_organisationId}Subsidiary bulk upload progress";
        var rowsAddedKey = $"{_userId}{_organisationId}Subsidiary bulk upload rows added";
        var errorKey = $"{_userId}{_organisationId}Subsidiary bulk upload errors";

        var status = "Working";
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

        _notificationServiceMock.Setup(x => x.GetStatus(progressKey))
            .ReturnsAsync(status);

        _notificationServiceMock.Setup(x => x.GetStatus(rowsAddedKey))
            .ReturnsAsync(rowsAdded);

        _notificationServiceMock.Setup(x => x.GetStatus(errorKey))
            .ReturnsAsync(errorStatus);

        // Act
        var result = await _systemUnderTest.Run(Mock.Of<HttpRequest>(), _userId, _organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<JsonResult>();

        _notificationServiceMock.Verify(x => x.GetStatus(progressKey), Times.Once);
        _notificationServiceMock.Verify(x => x.GetStatus(rowsAddedKey), Times.Once);
        _notificationServiceMock.Verify(x => x.GetStatus(errorKey), Times.Once);
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
    public async Task NotificationStatusFunction_Returns_NotFound_WhenExceptionIsThrown()
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
        ((StatusCodeResult)result).StatusCode.Should().Be(500);

        _notificationServiceMock.Verify(x => x.GetStatus(It.IsAny<string>()), Times.Once);
    }
}
