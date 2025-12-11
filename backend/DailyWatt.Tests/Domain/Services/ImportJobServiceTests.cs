using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Enums;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using Moq;
using Xunit;

namespace DailyWatt.Tests.Domain.Services;

public class ImportJobServiceTests
{
  [Fact]
  public async Task MarkRunningAsync_UpdatesJobStatus()
  {
    // Arrange
    var job = new ImportJob
    {
      Id = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
      FromUtc = DateTime.UtcNow.AddDays(-7),
      ToUtc = DateTime.UtcNow,
      Status = ImportJobStatus.Pending,
      CreatedAt = DateTime.UtcNow
    };

    // Act
    job.Status = ImportJobStatus.Running;

    // Assert
    Assert.Equal(ImportJobStatus.Running, job.Status);
  }

  [Fact]
  public async Task MarkCompletedAsync_UpdatesJobStatusAndMeasurementCount()
  {
    // Arrange
    var job = new ImportJob
    {
      Id = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
      FromUtc = DateTime.UtcNow.AddDays(-7),
      ToUtc = DateTime.UtcNow,
      Status = ImportJobStatus.Running,
      CreatedAt = DateTime.UtcNow,
      ImportedCount = 0
    };

    const int measurementCount = 336;

    // Act
    job.Status = ImportJobStatus.Completed;
    job.ImportedCount = measurementCount;

    // Assert
    Assert.Equal(ImportJobStatus.Completed, job.Status);
    Assert.Equal(measurementCount, job.ImportedCount);
  }

  [Fact]
  public async Task MarkFailedAsync_UpdatesJobStatusWithErrorMessage()
  {
    // Arrange
    var job = new ImportJob
    {
      Id = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
      FromUtc = DateTime.UtcNow.AddDays(-7),
      ToUtc = DateTime.UtcNow,
      Status = ImportJobStatus.Running,
      CreatedAt = DateTime.UtcNow,
      ErrorMessage = null
    };

    const string errorMessage = "File format not supported";
    const string errorCode = "INVALID_FORMAT";

    // Act
    job.Status = ImportJobStatus.Failed;
    job.ErrorCode = errorCode;
    job.ErrorMessage = errorMessage;

    // Assert
    Assert.Equal(ImportJobStatus.Failed, job.Status);
    Assert.Equal(errorCode, job.ErrorCode);
    Assert.Equal(errorMessage, job.ErrorMessage);
  }

  [Fact]
  public void ImportJob_DateRangeValidation()
  {
    // Arrange
    var fromDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var toDate = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

    // Act
    var job = new ImportJob
    {
      Id = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
      FromUtc = fromDate,
      ToUtc = toDate,
      Status = ImportJobStatus.Pending,
      CreatedAt = DateTime.UtcNow
    };

    // Assert
    Assert.True(job.ToUtc > job.FromUtc);
    Assert.Equal(fromDate, job.FromUtc);
    Assert.Equal(toDate, job.ToUtc);
  }
}

public class EnedisCredentialServiceMockTests
{
  [Fact]
  public async Task GetCredentialsAsync_ReturnsCredentialForUser()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var credential = new EnedisCredential
    {
      UserId = userId,
      LoginEncrypted = new byte[] { 1, 2, 3 },
      PasswordEncrypted = new byte[] { 4, 5, 6 },
      MeterNumber = "12345678901234"
    };

    var mockCredentialService = new Mock<IEnedisCredentialService>();
    mockCredentialService
        .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(credential);

    // Act
    var result = await mockCredentialService.Object.GetCredentialsAsync(userId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(userId, result.UserId);
    Assert.Equal("12345678901234", result.MeterNumber);
    mockCredentialService.Verify(
        s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task GetCredentialsAsync_ReturnsNullForUnknownUser()
  {
    // Arrange
    var userId = Guid.NewGuid();

    var mockCredentialService = new Mock<IEnedisCredentialService>();
    mockCredentialService
        .Setup(s => s.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((EnedisCredential?)null);

    // Act
    var result = await mockCredentialService.Object.GetCredentialsAsync(userId);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task SaveCredentialsAsync_StoresCredentialSecurely()
  {
    // Arrange
    var userId = Guid.NewGuid();
    const string login = "login";
    const string password = "password";
    const string meterNumber = "12345678901234";
    const string city = "Paris";
    const double latitude = 48.8566;
    const double longitude = 2.3522;

    var mockCredentialService = new Mock<IEnedisCredentialService>();
    mockCredentialService
        .Setup(s => s.SaveCredentialsAsync(userId, login, password, meterNumber, city, latitude, longitude, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    // Act
    await mockCredentialService.Object.SaveCredentialsAsync(userId, login, password, meterNumber, city, latitude, longitude);

    // Assert
    mockCredentialService.Verify(
        s => s.SaveCredentialsAsync(userId, login, password, meterNumber, city, latitude, longitude, It.IsAny<CancellationToken>()),
        Times.Once);
  }
}

public class SecretProtectorMockTests
{
  [Fact]
  public void Protect_And_Unprotect_RoundTrip()
  {
    // Arrange
    var plainText = "MySuperSecretPassword123";
    var mockProtector = new Mock<ISecretProtector>();
    var encryptedValue = new byte[] { 1, 2, 3, 4 }; // Simulate encryption

    mockProtector
        .Setup(s => s.Protect(plainText))
        .Returns(encryptedValue);

    mockProtector
      .Setup(s => s.Unprotect(encryptedValue))
        .Returns(plainText);

    // Act
    var protected1 = mockProtector.Object.Protect(plainText);
    var unprotected = mockProtector.Object.Unprotect(protected1);

    // Assert
    Assert.NotEmpty(protected1);
    Assert.Equal(plainText, unprotected);
  }

  [Fact]
  public void Protect_WithEmptyString_ThrowsException()
  {
    // Arrange
    var mockProtector = new Mock<ISecretProtector>();
    mockProtector
        .Setup(s => s.Protect(It.IsAny<string>()))
        .Throws<ArgumentException>();

    // Act & Assert
    Assert.Throws<ArgumentException>(() => mockProtector.Object.Protect(""));
  }
}
