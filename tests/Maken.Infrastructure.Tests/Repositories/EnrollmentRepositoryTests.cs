using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Infrastructure.Persistence;
using Maken.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Maken.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for EnrollmentRepository.
/// </summary>
public class EnrollmentRepositoryTests
{
    private readonly MakenDbContext _context;
    private readonly EnrollmentRepository _repository;
    private readonly Guid _tenantId = Guid.NewGuid();

    public EnrollmentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MakenDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);

        _context = new MakenDbContext(options, mockTenantContext.Object);
        _repository = new EnrollmentRepository(_context);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_WithValidIds_ReturnsEnrollment()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow
        };
        await _repository.AddAsync(enrollment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentAndCourseAsync(studentId, courseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(studentId, result.StudentId);
        Assert.Equal(courseId, result.CourseId);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_WithNonExistentIds_ReturnsNull()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByStudentAndCourseAsync(studentId, courseId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByStudentIdAsync_WithValidStudentId_ReturnsEnrollments()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var course1Id = Guid.NewGuid();
        var course2Id = Guid.NewGuid();

        // Create courses first (required for Include)
        var course1 = new Course(_tenantId, "Course 1", "Description 1", false, null);
        var course2 = new Course(_tenantId, "Course 2", "Description 2", false, null);
        _context.Set<Course>().Add(course1);
        _context.Set<Course>().Add(course2);
        await _context.SaveChangesAsync();

        var enrollment1 = new Enrollment
        {
            StudentId = studentId,
            CourseId = course1.Id,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-2)
        };
        var enrollment2 = new Enrollment
        {
            StudentId = studentId,
            CourseId = course2.Id,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-1)
        };

        await _repository.AddAsync(enrollment1);
        await _repository.AddAsync(enrollment2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentIdAsync(studentId);
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.Equal(enrollment1.Id, resultList[0].Id);
        Assert.Equal(enrollment2.Id, resultList[1].Id);
    }

    [Fact]
    public async Task GetByStudentIdAsync_WithNonExistentStudentId_ReturnsEmptyList()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByStudentIdAsync(studentId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByStudentIdAsync_OrdersByEnrolledAt()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Create courses first (required for Include)
        var course1 = new Course(_tenantId, "Course 1", "Description 1", false, null);
        var course2 = new Course(_tenantId, "Course 2", "Description 2", false, null);
        var course3 = new Course(_tenantId, "Course 3", "Description 3", false, null);
        _context.Set<Course>().Add(course1);
        _context.Set<Course>().Add(course2);
        _context.Set<Course>().Add(course3);
        await _context.SaveChangesAsync();

        var enrollment1 = new Enrollment
        {
            StudentId = studentId,
            CourseId = course1.Id,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-3)
        };
        var enrollment2 = new Enrollment
        {
            StudentId = studentId,
            CourseId = course2.Id,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-1)
        };
        var enrollment3 = new Enrollment
        {
            StudentId = studentId,
            CourseId = course3.Id,
            TenantId = _tenantId,
            EnrolledAt = DateTime.UtcNow.AddDays(-2)
        };

        await _repository.AddAsync(enrollment2);
        await _repository.AddAsync(enrollment3);
        await _repository.AddAsync(enrollment1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStudentIdAsync(studentId);
        var resultList = result.ToList();

        // Assert
        Assert.Equal(3, resultList.Count);
        Assert.Equal(enrollment1.Id, resultList[0].Id);
        Assert.Equal(enrollment3.Id, resultList[1].Id);
        Assert.Equal(enrollment2.Id, resultList[2].Id);
    }
}
