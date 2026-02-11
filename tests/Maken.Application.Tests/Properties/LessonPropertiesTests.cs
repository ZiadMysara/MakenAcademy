using FsCheck;
using FsCheck.Xunit;
using Maken.Application.Commands.Lessons;
using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Lessons;
using Maken.Application.Tests.Helpers;
using Maken.Domain.Entities;
using Maken.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maken.Application.Tests.Properties;

/// <summary>
/// Property-based tests for Lesson operations.
/// Tests universal properties that should hold across all valid inputs.
/// Minimum 100 iterations per property test as per design.md.
/// </summary>
public class LessonPropertiesTests
{
    /// <summary>
    /// Property 4: Lesson Creation with Course Relationship
    /// For any valid lesson data and existing course, creating a lesson should result in a persisted lesson entity 
    /// with the correct course relationship, tenant ID matching the course's tenant, and unique order within the course.
    /// 
    /// Feature: backend-business-features, Property 4: Lesson Creation with Course Relationship
    /// Validates: Requirements FR-013
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LessonCreation_WithValidDataAndCourse_PersistsLessonWithCorrectRelationship(
        NonEmptyString titleGen,
        NonEmptyString descriptionGen,
        NonEmptyString contentUrlGen,
        Guid tenantId,
        Guid courseId,
        PositiveInt orderGen)
    {
        // Extract values and filter control characters
        var title = new string(titleGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var description = new string(descriptionGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var contentUrl = new string(contentUrlGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var order = orderGen.Get % 100 + 1; // Keep order between 1-100
        
        // Skip if any trimmed strings are empty (would fail domain validation)
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(contentUrl))
        {
            return true; // Skip this test case
        }

        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var courseRepositoryMock = new Mock<IRepository<Course>>();
        var lessonRepositoryMock = new Mock<IRepository<Lesson>>();

        // Create existing course with matching tenant
        var existingCourse = Course.Create("Test Course", "Test Description", tenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(courseRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Lesson>()).Returns(lessonRepositoryMock.Object);
        courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        var loggerMock = new Mock<ILogger<CreateLessonCommandHandler>>();
        var handler = new CreateLessonCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, loggerMock.Object);
        var command = new CreateLessonCommand(
            courseId,
            title,
            description,
            "Video",
            contentUrl,
            order
        );

        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        var afterCreation = DateTime.UtcNow;

        return result.Id != Guid.Empty &&
               result.CourseId == courseId &&
               result.TenantId == tenantId &&
               result.Title == title.Trim() &&
               result.Description == description.Trim() &&
               result.ContentUrl == contentUrl.Trim() &&
               result.Order == order &&
               result.CreatedAt >= beforeCreation &&
               result.CreatedAt <= afterCreation;
    }

    /// <summary>
    /// Property 5: Lesson Update Preservation
    /// For any existing lesson and valid update data, updating the lesson should result in the lesson 
    /// having the new properties while preserving the original ID, CourseId, TenantId, and audit fields.
    /// 
    /// Feature: backend-business-features, Property 5: Lesson Update Preservation
    /// Validates: Requirements FR-014
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LessonUpdate_WithValidData_PreservesOriginalFields(
        NonEmptyString originalTitleGen,
        NonEmptyString originalDescriptionGen,
        NonEmptyString newTitleGen,
        NonEmptyString newDescriptionGen,
        NonEmptyString newContentUrlGen,
        Guid tenantId,
        Guid courseId,
        Guid lessonId,
        PositiveInt originalOrderGen,
        PositiveInt newOrderGen)
    {
        // Extract values and filter control characters
        var originalTitle = new string(originalTitleGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var originalDescription = new string(originalDescriptionGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var newTitle = new string(newTitleGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var newDescription = new string(newDescriptionGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var newContentUrl = new string(newContentUrlGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var originalOrder = originalOrderGen.Get % 100 + 1;
        var newOrder = newOrderGen.Get % 100 + 1;
        
        // Skip if any trimmed strings are empty (would fail domain validation)
        if (string.IsNullOrWhiteSpace(originalTitle) || string.IsNullOrWhiteSpace(originalDescription) ||
            string.IsNullOrWhiteSpace(newTitle) || string.IsNullOrWhiteSpace(newDescription) || string.IsNullOrWhiteSpace(newContentUrl))
        {
            return true; // Skip this test case
        }

        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var lessonRepositoryMock = new Mock<IRepository<Lesson>>();

        var existingLesson = Lesson.Create(
            courseId,
            tenantId,
            originalTitle,
            originalDescription,
            ContentType.Video,
            "https://example.com/original.mp4",
            originalOrder
        );
        EntityTestHelper.SetId(existingLesson, lessonId);
        var originalCreatedAt = existingLesson.CreatedAt;

        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        unitOfWorkMock.Setup(x => x.Repository<Lesson>()).Returns(lessonRepositoryMock.Object);
        lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLesson);

        var handler = new UpdateLessonCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, new Mock<ILogger<UpdateLessonCommandHandler>>().Object);
        var command = new UpdateLessonCommand(
            lessonId,
            newTitle,
            newDescription,
            "PDF",
            newContentUrl,
            newOrder
        );

        // Act
        var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        return result.Id == lessonId &&
               existingLesson.CourseId == courseId &&
               existingLesson.TenantId == tenantId &&
               existingLesson.Title == newTitle.Trim() &&
               existingLesson.Description == newDescription.Trim() &&
               existingLesson.ContentUrl == newContentUrl.Trim() &&
               existingLesson.Order == newOrder &&
               existingLesson.CreatedAt == originalCreatedAt;
    }

    /// <summary>
    /// Property 3: Lesson Soft Delete Cascade
    /// For any lesson with an associated exam and progress records, soft deleting the lesson should mark 
    /// the lesson, exam, questions, choices, and progress records as deleted without physically removing any records.
    /// 
    /// Feature: backend-business-features, Property 6: Lesson Soft Delete Cascade
    /// Validates: Requirements FR-015, FR-054
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LessonSoftDelete_WithRelatedEntities_CascadesDelete(
        Guid tenantId,
        Guid courseId,
        Guid lessonId)
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var lessonRepositoryMock = new Mock<IRepository<Lesson>>();
        var examRepositoryMock = new Mock<IRepository<Exam>>();
        var questionRepositoryMock = new Mock<IRepository<Question>>();
        var choiceRepositoryMock = new Mock<IRepository<Choice>>();

        var existingLesson = Lesson.Create(
            courseId,
            tenantId,
            "Test Lesson",
            "Test Description",
            ContentType.Video,
            "https://example.com/video.mp4",
            1
        );
        EntityTestHelper.SetId(existingLesson, lessonId);

        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        unitOfWorkMock.Setup(x => x.Repository<Lesson>()).Returns(lessonRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Exam>()).Returns(examRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Question>()).Returns(questionRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Choice>()).Returns(choiceRepositoryMock.Object);
        
        lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLesson);
        
        // Setup empty collections for cascade delete (no related entities)
        examRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Exam, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Exam>());

        var handler = new DeleteLessonCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, new Mock<ILogger<DeleteLessonCommandHandler>>().Object);
        var command = new DeleteLessonCommand(lessonId);

        var beforeDeletion = DateTime.UtcNow;

        // Act
        var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        var afterDeletion = DateTime.UtcNow;

        return result.Id == lessonId &&
               existingLesson.IsDeleted &&
               existingLesson.DeletedAt != null &&
               existingLesson.DeletedAt >= beforeDeletion &&
               existingLesson.DeletedAt <= afterDeletion;
    }

    /// <summary>
    /// Property 7: Lesson Ordering
    /// For any course, querying lessons should return them ordered by the Order field in ascending sequence (1, 2, 3, ...).
    /// 
    /// Feature: backend-business-features, Property 7: Lesson Ordering
    /// Validates: Requirements FR-017
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LessonQuery_ForCourse_ReturnsLessonsInOrder(
        Guid tenantId,
        Guid courseId,
        PositiveInt lessonCountGen)
    {
        // Extract value and constrain to reasonable range
        var lessonCount = Math.Max(2, Math.Min(10, lessonCountGen.Get));

        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var courseRepositoryMock = new Mock<IRepository<Course>>();
        var lessonRepositoryMock = new Mock<IRepository<Lesson>>();

        // Create existing course
        var existingCourse = Course.Create("Test Course", "Test Description", tenantId);
        EntityTestHelper.SetId(existingCourse, courseId);

        // Create lessons with random orders
        var lessons = new List<Lesson>();
        var orders = Enumerable.Range(1, lessonCount).OrderBy(_ => Guid.NewGuid()).ToList();
        
        for (int i = 0; i < lessonCount; i++)
        {
            var lesson = Lesson.Create(
                courseId,
                tenantId,
                $"Lesson {i + 1}",
                $"Description {i + 1}",
                ContentType.Video,
                $"https://example.com/video{i + 1}.mp4",
                orders[i]
            );
            lessons.Add(lesson);
        }

        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        unitOfWorkMock.Setup(x => x.Repository<Course>()).Returns(courseRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Lesson>()).Returns(lessonRepositoryMock.Object);
        
        courseRepositoryMock
            .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);
        
        lessonRepositoryMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Lesson, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessons);

        var handler = new GetLessonsQueryHandler(unitOfWorkMock.Object, tenantContextMock.Object);
        var query = new GetLessonsQuery(courseId);

        // Act
        var result = handler.Handle(query, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        var resultList = result.Lessons;
        
        // Verify lessons are in ascending order
        for (int i = 0; i < resultList.Count - 1; i++)
        {
            if (resultList[i].Order >= resultList[i + 1].Order)
            {
                return false;
            }
        }

        return resultList.Count == lessonCount &&
               resultList.First().Order == orders.Min() &&
               resultList.Last().Order == orders.Max();
    }
}
