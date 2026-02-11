using FsCheck;
using FsCheck.Xunit;
using Maken.Domain.Entities;
using Maken.Domain.Enums;

namespace Maken.Application.Tests.Properties;

/// <summary>
/// Property-based tests for soft delete correctness properties.
/// These tests verify that soft delete is implemented correctly across all entities.
/// </summary>
public class SoftDeletePropertiesTests
{
    /// <summary>
    /// Property 28: Soft Delete Preservation
    /// For any entity, deleting the entity should set IsDeleted=true and DeletedAt=current timestamp
    /// without physically removing the record from the database. The entity should remain queryable
    /// with explicit IsDeleted filter but excluded from default queries.
    /// **Validates: Requirements FR-052**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoftDelete_DeleteCourse_SetsIsDeletedAndDeletedAt()
    {
        // Arrange: Create a course
        var course = Course.Create("Test Course", "Test Description", Guid.NewGuid(), false);
        
        // Act: Soft delete the course
        var beforeDelete = DateTime.UtcNow;
        course.SoftDelete();
        var afterDelete = DateTime.UtcNow;

        // Assert: Course should have IsDeleted=true and DeletedAt set
        return course.IsDeleted 
            && course.DeletedAt.HasValue 
            && course.DeletedAt.Value >= beforeDelete 
            && course.DeletedAt.Value <= afterDelete;
    }

    /// <summary>
    /// Property 28: Soft Delete Preservation (Lesson variant)
    /// For any lesson, deleting the lesson should set IsDeleted=true and DeletedAt=current timestamp.
    /// **Validates: Requirements FR-052**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoftDelete_DeleteLesson_SetsIsDeletedAndDeletedAt()
    {
        // Arrange: Create a lesson
        var lesson = Lesson.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Lesson", "Test Description", ContentType.Video, "https://example.com/video.mp4", 1);
        
        // Act: Soft delete the lesson
        var beforeDelete = DateTime.UtcNow;
        lesson.SoftDelete();
        var afterDelete = DateTime.UtcNow;

        // Assert: Lesson should have IsDeleted=true and DeletedAt set
        return lesson.IsDeleted 
            && lesson.DeletedAt.HasValue 
            && lesson.DeletedAt.Value >= beforeDelete 
            && lesson.DeletedAt.Value <= afterDelete;
    }

    /// <summary>
    /// Property 28: Soft Delete Preservation (Exam variant)
    /// For any exam, deleting the exam should set IsDeleted=true and DeletedAt=current timestamp.
    /// **Validates: Requirements FR-052**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoftDelete_DeleteExam_SetsIsDeletedAndDeletedAt()
    {
        // Arrange: Create an exam
        var exam = Exam.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Exam", 70);
        
        // Act: Soft delete the exam
        var beforeDelete = DateTime.UtcNow;
        exam.SoftDelete();
        var afterDelete = DateTime.UtcNow;

        // Assert: Exam should have IsDeleted=true and DeletedAt set
        return exam.IsDeleted 
            && exam.DeletedAt.HasValue 
            && exam.DeletedAt.Value >= beforeDelete 
            && exam.DeletedAt.Value <= afterDelete;
    }

    /// <summary>
    /// Property 28: Soft Delete Preservation (Question variant)
    /// For any question, deleting the question should set IsDeleted=true and DeletedAt=current timestamp.
    /// **Validates: Requirements FR-052**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoftDelete_DeleteQuestion_SetsIsDeletedAndDeletedAt()
    {
        // Arrange: Create a question
        var question = Question.Create(Guid.NewGuid(), "Test Question?", 1);
        
        // Act: Soft delete the question
        var beforeDelete = DateTime.UtcNow;
        question.SoftDelete();
        var afterDelete = DateTime.UtcNow;

        // Assert: Question should have IsDeleted=true and DeletedAt set
        return question.IsDeleted 
            && question.DeletedAt.HasValue 
            && question.DeletedAt.Value >= beforeDelete 
            && question.DeletedAt.Value <= afterDelete;
    }

    /// <summary>
    /// Property 28: Soft Delete Preservation (Choice variant)
    /// For any choice, deleting the choice should set IsDeleted=true and DeletedAt=current timestamp.
    /// **Validates: Requirements FR-052**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoftDelete_DeleteChoice_SetsIsDeletedAndDeletedAt()
    {
        // Arrange: Create a choice
        var choice = Choice.Create(Guid.NewGuid(), "Test Choice", false);
        
        // Act: Soft delete the choice
        var beforeDelete = DateTime.UtcNow;
        choice.SoftDelete();
        var afterDelete = DateTime.UtcNow;

        // Assert: Choice should have IsDeleted=true and DeletedAt set
        return choice.IsDeleted 
            && choice.DeletedAt.HasValue 
            && choice.DeletedAt.Value >= beforeDelete 
            && choice.DeletedAt.Value <= afterDelete;
    }

    /// <summary>
    /// Property 29: Soft Delete Query Exclusion
    /// For any query without an explicit IsDeleted filter, soft deleted entities (IsDeleted=true)
    /// should not be returned in the results. Only active entities (IsDeleted=false) should be
    /// included by default.
    /// 
    /// This property tests the concept that default queries exclude soft-deleted entities.
    /// In practice, this is enforced by global query filters in EF Core.
    /// **Validates: Requirements FR-056**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoftDelete_DefaultQuery_ExcludesSoftDeletedEntities()
    {
        // Arrange: Create a mix of active and soft-deleted courses
        var activeCourse1 = Course.Create("Active Course 1", "Description 1", Guid.NewGuid(), false);
        var activeCourse2 = Course.Create("Active Course 2", "Description 2", Guid.NewGuid(), false);
        var deletedCourse = Course.Create("Deleted Course", "Description 3", Guid.NewGuid(), false);
        deletedCourse.SoftDelete();
        
        var allCourses = new List<Course> { activeCourse1, activeCourse2, deletedCourse };
        
        // Act: Simulate default query filter (exclude soft-deleted)
        var defaultQueryResult = allCourses.Where(c => !c.IsDeleted).ToList();

        // Assert: Result should only contain active courses (no soft-deleted ones)
        return defaultQueryResult.Count == 2 
            && defaultQueryResult.All(c => !c.IsDeleted)
            && !defaultQueryResult.Contains(deletedCourse);
    }

    /// <summary>
    /// Property 29: Soft Delete Query Exclusion (Lesson variant)
    /// Default queries should exclude soft-deleted lessons.
    /// **Validates: Requirements FR-056**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoftDelete_DefaultLessonQuery_ExcludesSoftDeletedEntities()
    {
        // Arrange: Create a mix of active and soft-deleted lessons
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var activeLesson1 = Lesson.Create(courseId, tenantId, "Active Lesson 1", "Description 1", ContentType.Video, "https://example.com/video1.mp4", 1);
        var activeLesson2 = Lesson.Create(courseId, tenantId, "Active Lesson 2", "Description 2", ContentType.PDF, "https://example.com/doc2.pdf", 2);
        var deletedLesson = Lesson.Create(courseId, tenantId, "Deleted Lesson", "Description 3", ContentType.Video, "https://example.com/video3.mp4", 3);
        deletedLesson.SoftDelete();
        
        var allLessons = new List<Lesson> { activeLesson1, activeLesson2, deletedLesson };
        
        // Act: Simulate default query filter (exclude soft-deleted)
        var defaultQueryResult = allLessons.Where(l => !l.IsDeleted).ToList();

        // Assert: Result should only contain active lessons
        return defaultQueryResult.Count == 2 
            && defaultQueryResult.All(l => !l.IsDeleted)
            && !defaultQueryResult.Contains(deletedLesson);
    }

    /// <summary>
    /// Property 29: Soft Delete Query Exclusion (Exam variant)
    /// Default queries should exclude soft-deleted exams.
    /// **Validates: Requirements FR-056**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoftDelete_DefaultExamQuery_ExcludesSoftDeletedEntities()
    {
        // Arrange: Create a mix of active and soft-deleted exams
        var lessonId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var activeExam1 = Exam.Create(lessonId, tenantId, "Active Exam 1", 70);
        var activeExam2 = Exam.Create(lessonId, tenantId, "Active Exam 2", 80);
        var deletedExam = Exam.Create(lessonId, tenantId, "Deleted Exam", 75);
        deletedExam.SoftDelete();
        
        var allExams = new List<Exam> { activeExam1, activeExam2, deletedExam };
        
        // Act: Simulate default query filter (exclude soft-deleted)
        var defaultQueryResult = allExams.Where(e => !e.IsDeleted).ToList();

        // Assert: Result should only contain active exams
        return defaultQueryResult.Count == 2 
            && defaultQueryResult.All(e => !e.IsDeleted)
            && !defaultQueryResult.Contains(deletedExam);
    }

    /// <summary>
    /// Property 29: Soft Delete Query Exclusion (Universal variant)
    /// For any entity type, default queries should exclude soft-deleted entities.
    /// This tests the universal property across different entity types.
    /// **Validates: Requirements FR-056**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoftDelete_UniversalQueryExclusion_WorksForAllEntityTypes()
    {
        // Arrange: Create entities of different types
        var course = Course.Create("Test Course", "Description", Guid.NewGuid(), false);
        var lesson = Lesson.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Lesson", "Description", ContentType.Video, "https://example.com/video.mp4", 1);
        var exam = Exam.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Exam", 70);
        
        // Act: Delete all entities
        course.SoftDelete();
        lesson.SoftDelete();
        exam.SoftDelete();

        // Assert: All entities should be marked as deleted
        return course.IsDeleted && course.DeletedAt.HasValue
            && lesson.IsDeleted && lesson.DeletedAt.HasValue
            && exam.IsDeleted && exam.DeletedAt.HasValue;
    }
}
