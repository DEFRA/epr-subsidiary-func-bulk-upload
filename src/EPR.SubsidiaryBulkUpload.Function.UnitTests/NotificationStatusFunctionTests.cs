﻿using EPR.SubsidiaryBulkUpload.Application.Services.Interfaces;
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
        var key = $"{_userId}{_organisationId}Subsidiary bulk upload progress";

        var status = "Working";

        _notificationServiceMock.Setup(x => x.GetStatus(key))
            .ReturnsAsync(status);

        // Act
        var result = await _systemUnderTest.Run(Mock.Of<HttpRequest>(), _userId, _organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult).Value.Should().Be(status);

        _notificationServiceMock.Verify(x => x.GetStatus(key), Times.Once);
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
}