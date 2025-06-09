using FluentAssertions;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Location.Core.Infrastructure.Tests.Services
{
    [TestFixture]
    public class LoggingServiceTests
    {
        private LoggingService _loggingService;
        private DatabaseContext _context;
        private Mock<ILogger<LoggingService>> _mockLogger;
        private Mock<ILogger<DatabaseContext>> _mockContextLogger;
        private Mock<IDatabaseContext> _mockContext;
        private string _testDbPath;

        [SetUp]
        public async Task Setup()
        {
            _mockLogger = new Mock<ILogger<LoggingService>>();
            _mockContextLogger = new Mock<ILogger<DatabaseContext>>();
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            _context = new DatabaseContext(_mockContextLogger.Object, _testDbPath);
            await _context.InitializeDatabaseAsync();
            _loggingService = new LoggingService(_context, _mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();

            if (File.Exists(_testDbPath))
            {
                try
                {
                    File.Delete(_testDbPath);
                }
                catch
                {
                    // Ignore file deletion errors in tests
                }
            }
        }

        [Test]
        public async Task LogToDatabaseAsync_WithValidLogEntry_ShouldPersist()
        {
            // Arrange
            var level = LogLevel.Information;
            var message = "Test log message";

            // Act
            await _loggingService.LogToDatabaseAsync(level, message);

            // Assert
            var logs = await _context.Table<Log>().ToListAsync();
            logs.Should().HaveCount(1);
            logs[0].Level.Should().Be("Information");
            logs[0].Message.Should().Be(message);
            logs[0].Exception.Should().BeEmpty();
            logs[0].Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task LogToDatabaseAsync_WithException_ShouldIncludeExceptionDetails()
        {
            // Arrange
            var level = LogLevel.Error;
            var message = "Error occurred";
            var exception = new InvalidOperationException("Test exception");

            // Act
            await _loggingService.LogToDatabaseAsync(level, message, exception);

            // Assert
            var logs = await _context.Table<Log>().ToListAsync();
            logs.Should().HaveCount(1);
            logs[0].Level.Should().Be("Error");
            logs[0].Message.Should().Be(message);
            logs[0].Exception.Should().Contain("InvalidOperationException");
            logs[0].Exception.Should().Contain("Test exception");
        }

        [Test]
        public async Task LogToDatabaseAsync_WithDatabaseError_ShouldLogToStandardLogger()
        {
            // Arrange
            var level = LogLevel.Warning;
            var message = "Test warning";

            // Create a mock context that throws on InsertAsync
            _mockContext = new Mock<IDatabaseContext>();
            _mockContext.Setup(x => x.InsertAsync(It.IsAny<Log>()))
                .ThrowsAsync(new Exception("Database error"));

            var loggingServiceWithMock = new LoggingService(_mockContext.Object, _mockLogger.Object);

            // Act
            await loggingServiceWithMock.LogToDatabaseAsync(level, message);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once
            );
        }

        [Test]
        public async Task GetLogsAsync_WithMultipleLogs_ShouldReturnInDescendingOrder()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                await _loggingService.LogToDatabaseAsync(LogLevel.Information, $"Log message {i}");
                await Task.Delay(100); // Ensure different timestamps
            }

            // Act
            var logs = await _loggingService.GetLogsAsync();

            // Assert
            logs.Should().HaveCount(5);
            logs[0].Message.Should().Be("Log message 4"); // Most recent first
            logs[4].Message.Should().Be("Log message 0"); // Oldest last
        }

        [Test]
        public async Task GetLogsAsync_WithCountParameter_ShouldLimitResults()
        {
            // Arrange
            for (int i = 0; i < 10; i++)
            {
                await _loggingService.LogToDatabaseAsync(LogLevel.Information, $"Log message {i}");
            }

            // Act
            var logs = await _loggingService.GetLogsAsync(3);

            // Assert
            logs.Should().HaveCount(3);
        }

        [Test]
        public async Task GetLogsAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Act
            var logs = await _loggingService.GetLogsAsync();

            // Assert
            logs.Should().BeEmpty();
        }

        [Test]
        public async Task GetLogsAsync_WithDatabaseError_ShouldReturnEmptyListAndLogError()
        {
            // Arrange
            _mockContext = new Mock<IDatabaseContext>();
            _mockContext.Setup(x => x.Table<Log>())
                .Throws(new Exception("Database error"));

            var loggingServiceWithMock = new LoggingService(_mockContext.Object, _mockLogger.Object);

            // Act
            var logs = await loggingServiceWithMock.GetLogsAsync();

            // Assert
            logs.Should().BeEmpty();
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once
            );
        }

        [Test]
        public async Task ClearLogsAsync_WithLogs_ShouldDeleteAll()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                await _loggingService.LogToDatabaseAsync(LogLevel.Information, $"Log message {i}");
            }

            // Act
            await _loggingService.ClearLogsAsync();

            // Assert
            var logs = await _context.Table<Log>().ToListAsync();
            logs.Should().BeEmpty();

            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        [Test]
        public async Task ClearLogsAsync_WithDatabaseError_ShouldThrowAndLogError()
        {
            // Arrange
            _mockContext = new Mock<IDatabaseContext>();
            _mockContext.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<object[]>()))
                .ThrowsAsync(new Exception("Database error"));

            var loggingServiceWithMock = new LoggingService(_mockContext.Object, _mockLogger.Object);

            // Act
            Func<Task> act = async () => await loggingServiceWithMock.ClearLogsAsync();

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once
            );
        }

        [Test]
        public void Constructor_WithNullContext_ShouldThrowException()
        {
            // Act
            Action act = () => new LoggingService(null!, _mockLogger.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowException()
        {
            // Act
            Action act = () => new LoggingService(_context, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public async Task LogToDatabaseAsync_AllLogLevels_ShouldPersistCorrectly()
        {
            // Arrange
            var logLevels = new[]
            {
                LogLevel.Trace,
                LogLevel.Debug,
                LogLevel.Information,
                LogLevel.Warning,
                LogLevel.Error,
                LogLevel.Critical
            };

            // Act
            foreach (var level in logLevels)
            {
                await _loggingService.LogToDatabaseAsync(level, $"{level} message");
            }

            // Assert
            var logs = await _context.Table<Log>().ToListAsync();
            logs.Should().HaveCount(6);

            foreach (var level in logLevels)
            {
                logs.Should().Contain(log => log.Level == level.ToString());
            }
        }

        [Test]
        public async Task LogToDatabaseAsync_WithLongMessage_ShouldPersistCompletely()
        {
            // Arrange
            var longMessage = new string('x', 5000);

            // Act
            await _loggingService.LogToDatabaseAsync(LogLevel.Information, longMessage);

            // Assert
            var logs = await _context.Table<Log>().ToListAsync();
            logs.Should().HaveCount(1);
            logs[0].Message.Should().Be(longMessage);
        }

        [Test]
        public async Task LogToDatabaseAsync_WithNullException_ShouldHandleGracefully()
        {
            // Act
            await _loggingService.LogToDatabaseAsync(LogLevel.Error, "Error message", null);

            // Assert
            var logs = await _context.Table<Log>().ToListAsync();
            logs.Should().HaveCount(1);
            logs[0].Exception.Should().BeEmpty();
        }

        [Test]
        public async Task GetLogsAsync_WithLargeCount_ShouldHandleGracefully()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                await _loggingService.LogToDatabaseAsync(LogLevel.Information, $"Log {i}");
            }

            // Act
            var logs = await _loggingService.GetLogsAsync(1000);

            // Assert
            logs.Should().HaveCount(5); // Should return all available logs
        }
    }
}