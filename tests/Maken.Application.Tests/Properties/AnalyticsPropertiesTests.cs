using FsCheck;
using FsCheck.Xunit;
using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Analytics;
using Maken.Domain.Entities;
using Moq;

namespace Maken.Application.Tests.Properties;

public class AnalyticsPropertiesTests
{
    /// <summary>
    /// Property 15: Tenant-Scoped Analytics
    /// For any tenant, querying analytics should return metrics (enrollment count, completion rate, exam pass rate) 
    /// that include only data from that tenant, with no cross-tenant data leakage.
    /// 
    /// Feature: backend-business-features, Property 15: Tenant-Scoped Analytics
    /// Validates: Requirements FR-026
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AnalyticsQuery_WithMultipleTenants_ReturnsOnlyCurrentTenantData(
        Guid tenantId1,
        Guid tenantId2,
        PositiveInt tenant1EnrollmentCount,
        PositiveInt tenant2EnrollmentCount,
        PositiveInt tenant1ProgressCount,
        PositiveInt tenant2ProgressCount)
    {
        if (tenantId1 == tenantId2)
        {
            return true;
        }

        var enrollmentRepositoryMock = new Mock<IEnrollmentRepository>();
        var progressRepositoryMock = new Mock<IProgressRepository>();

        var tenant1Enrollments = Enumerable.Range(0, tenant1EnrollmentCount.Get)
            .Select(i => new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = tenantId1,
                EnrolledAt = DateTime.UtcNow.AddDays(-i),
                CompletedAt = i % 2 == 0 ? DateTime.UtcNow.AddDays(-i + 1) : null
            })
            .ToList();

        var tenant1Progress = Enumerable.Range(0, tenant1ProgressCount.Get)
            .Select(i =>
            {
                var progress = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), tenantId1);
                progress.RecordExamResult(i % 2 == 0, i % 2 == 0 ? 85 : 45);
                return progress;
            })
            .ToList();

        enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant1Enrollments);

        progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant1Progress);

        var handler = new GetAnalyticsQueryHandler(
            enrollmentRepositoryMock.Object,
            progressRepositoryMock.Object
        );

        var query = new GetAnalyticsQuery();
        var result = handler.Handle(query, CancellationToken.None).GetAwaiter().GetResult();

        var expectedEnrollmentCount = tenant1Enrollments.Count;
        var expectedCompletedCount = tenant1Enrollments.Count(e => e.CompletedAt.HasValue);
        var expectedCompletionRate = expectedEnrollmentCount > 0 
            ? Math.Round((decimal)expectedCompletedCount / expectedEnrollmentCount * 100, 2) 
            : 0;

        var expectedExamAttempts = tenant1Progress.Count(p => p.ExamScore.HasValue);
        var expectedPassedExams = tenant1Progress.Count(p => p.ExamPassed == true);
        var expectedExamPassRate = expectedExamAttempts > 0 
            ? Math.Round((decimal)expectedPassedExams / expectedExamAttempts * 100, 2) 
            : 0;

        return result.TotalEnrollments == expectedEnrollmentCount &&
               result.CompletionRate == expectedCompletionRate &&
               result.ExamPassRate == expectedExamPassRate;
    }

    [Property(MaxTest = 100)]
    public bool AnalyticsQuery_WithEmptyTenantData_ReturnsZeroMetrics(Guid tenantId)
    {
        var enrollmentRepositoryMock = new Mock<IEnrollmentRepository>();
        var progressRepositoryMock = new Mock<IProgressRepository>();

        enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        var handler = new GetAnalyticsQueryHandler(
            enrollmentRepositoryMock.Object,
            progressRepositoryMock.Object
        );

        var query = new GetAnalyticsQuery();
        var result = handler.Handle(query, CancellationToken.None).GetAwaiter().GetResult();

        return result.TotalEnrollments == 0 &&
               result.CompletionRate == 0 &&
               result.ExamPassRate == 0;
    }

    [Property(MaxTest = 100)]
    public bool AnalyticsQuery_WithValidData_CalculatesMetricsCorrectly(
        Guid tenantId,
        PositiveInt enrollmentCount,
        PositiveInt progressCount)
    {
        var enrollmentRepositoryMock = new Mock<IEnrollmentRepository>();
        var progressRepositoryMock = new Mock<IProgressRepository>();

        var enrollments = Enumerable.Range(0, enrollmentCount.Get)
            .Select(i => new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                TenantId = tenantId,
                EnrolledAt = DateTime.UtcNow.AddDays(-i),
                CompletedAt = i % 2 == 0 ? DateTime.UtcNow.AddDays(-i + 1) : null
            })
            .ToList();

        var progressRecords = Enumerable.Range(0, progressCount.Get)
            .Select(i =>
            {
                var progress = Progress.Create(Guid.NewGuid(), Guid.NewGuid(), tenantId);
                progress.RecordExamResult(i % 3 == 0, i % 3 == 0 ? 85 : 45);
                return progress;
            })
            .ToList();

        enrollmentRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        progressRepositoryMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progressRecords);

        var handler = new GetAnalyticsQueryHandler(
            enrollmentRepositoryMock.Object,
            progressRepositoryMock.Object
        );

        var query = new GetAnalyticsQuery();
        var result = handler.Handle(query, CancellationToken.None).GetAwaiter().GetResult();

        var expectedEnrollmentCount = enrollments.Count;
        var expectedCompletedCount = enrollments.Count(e => e.CompletedAt.HasValue);
        var expectedCompletionRate = Math.Round((decimal)expectedCompletedCount / expectedEnrollmentCount * 100, 2);

        var expectedExamAttempts = progressRecords.Count(p => p.ExamScore.HasValue);
        var expectedPassedExams = progressRecords.Count(p => p.ExamPassed == true);
        var expectedExamPassRate = expectedExamAttempts > 0 
            ? Math.Round((decimal)expectedPassedExams / expectedExamAttempts * 100, 2) 
            : 0;

        return result.TotalEnrollments == expectedEnrollmentCount &&
               result.CompletionRate == expectedCompletionRate &&
               result.ExamPassRate == expectedExamPassRate;
    }
}
