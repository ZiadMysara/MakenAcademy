using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Analytics;
using Maken.Domain.Entities;
using Moq;

namespace Maken.Application.Tests.Queries;

/// <summary>
/// Unit tests for GetAnalyticsQueryHandler.
/// Tests analytics computation with tenant isolation.
/// </summary>
public class GetAnalyticsQueryTests
{
    private readonly Mock<IEnrollmentRepository> _enrollmentRepositoryMock;
    private readonly Mock<IProgressRepository> _progressRepositoryMock;
    private readonly GetAnalyticsQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetAnalyticsQueryTests()
    {
        _enrollmentRepositoryMock = new Mock<IEnrollmentRepository>();
        _progressRepositoryMock = new Mock<IProgressRepository>();

        _handler = new GetAnalyticsQueryHandler(
            _enrollmentRepositoryMock.Object,
            _progressRepositoryMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithNoData_ReturnsZeroMetrics()
    {
        // Arrange
        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        _progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        var query = new GetAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalEnrollments);
        Assert.Equal(0, result.CompletionRate);
        Assert.Equal(0, result.ExamPassRate);
    }

    [Fact]
    public async Task Handle_WithEnrollmentsButNoCompletions_ReturnsZeroCompletionRate()
    {
        // Arrange
        var enrollments = new List<Enrollment>
        {
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-5),
                CompletedAt = null
            },
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-3),
                CompletedAt = null
            }
        };

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        _progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        var query = new GetAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalEnrollments);
        Assert.Equal(0, result.CompletionRate);
        Assert.Equal(0, result.ExamPassRate);
    }

    [Fact]
    public async Task Handle_WithSomeCompletedEnrollments_ReturnsCorrectCompletionRate()
    {
        // Arrange
        var enrollments = new List<Enrollment>
        {
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-10),
                CompletedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-8),
                CompletedAt = null
            },
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-5),
                CompletedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-3),
                CompletedAt = null
            }
        };

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        _progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        var query = new GetAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(4, result.TotalEnrollments);
        Assert.Equal(50.00m, result.CompletionRate); // 2 out of 4 = 50%
        Assert.Equal(0, result.ExamPassRate);
    }

    [Fact]
    public async Task Handle_WithExamAttempts_ReturnsCorrectExamPassRate()
    {
        // Arrange
        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var progress1 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress1.RecordExamResult(true, 85);

        var progress2 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress2.RecordExamResult(false, 45);

        var progress3 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress3.RecordExamResult(true, 90);

        var progress4 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress4.RecordExamResult(true, 75);

        var progress5 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress5.MarkAsCompleted(); // No exam taken

        var progressRecords = new List<Progress> { progress1, progress2, progress3, progress4, progress5 };

        _progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progressRecords);

        var query = new GetAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalEnrollments);
        Assert.Equal(75.00m, result.ExamPassRate); // 3 passed out of 4 attempts = 75%
    }

    [Fact]
    public async Task Handle_WithCompleteData_ReturnsAllMetrics()
    {
        // Arrange
        var enrollments = new List<Enrollment>
        {
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-10),
                CompletedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-8),
                CompletedAt = null
            },
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-5),
                CompletedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        var progress1 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress1.RecordExamResult(true, 85);

        var progress2 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress2.RecordExamResult(false, 45);

        var progressRecords = new List<Progress> { progress1, progress2 };

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        _progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progressRecords);

        var query = new GetAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalEnrollments);
        Assert.Equal(66.67m, result.CompletionRate); // 2 out of 3 = 66.67%
        Assert.Equal(50.00m, result.ExamPassRate); // 1 passed out of 2 = 50%
    }

    [Fact]
    public async Task Handle_WithAllEnrollmentsCompleted_Returns100PercentCompletionRate()
    {
        // Arrange
        var enrollments = new List<Enrollment>
        {
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-10),
                CompletedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = _tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-8),
                CompletedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        _progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        var query = new GetAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalEnrollments);
        Assert.Equal(100.00m, result.CompletionRate);
    }

    [Fact]
    public async Task Handle_WithAllExamsPassed_Returns100PercentPassRate()
    {
        // Arrange
        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var progress1 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress1.RecordExamResult(true, 85);

        var progress2 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress2.RecordExamResult(true, 90);

        var progressRecords = new List<Progress> { progress1, progress2 };

        _progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progressRecords);

        var query = new GetAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(100.00m, result.ExamPassRate);
    }

    [Fact]
    public async Task Handle_WithProgressRecordsWithoutExams_IgnoresThemInPassRate()
    {
        // Arrange
        _enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var progress1 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress1.RecordExamResult(true, 85);

        var progress2 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress2.MarkAsCompleted(); // No exam

        var progress3 = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), _tenantId);
        progress3.MarkAsCompleted(); // No exam

        var progressRecords = new List<Progress> { progress1, progress2, progress3 };

        _progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progressRecords);

        var query = new GetAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(100.00m, result.ExamPassRate); // Only 1 exam attempt, and it passed
    }
}
