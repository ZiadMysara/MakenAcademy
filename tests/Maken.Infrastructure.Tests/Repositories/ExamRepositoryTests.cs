using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Infrastructure.Persistence;
using Maken.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Maken.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for ExamRepository.
/// Tests exam-specific query methods and relationships.
/// </summary>
public class ExamRepositoryTests : IDisposable
{
    private readonly MakenDbContext _context;
    private readonly ExamRepository _repository;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ExamRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MakenDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);

        _context = new MakenDbContext(options, mockTenantContext.Object);
        _repository = new ExamRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsExam()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Final Exam", 70);
        await _context.Exams.AddAsync(exam);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(exam.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(exam.Id, result.Id);
        Assert.Equal("Final Exam", result.Title);
        Assert.Equal(70, result.PassThreshold);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdWithQuestionsAsync_WithValidId_ReturnsExamWithQuestionsAndChoices()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Math Exam", 80);
        await _context.Exams.AddAsync(exam);
        await _context.SaveChangesAsync();

        var question1 = Question.Create(exam.Id, "What is 2+2?", 1);
        var question2 = Question.Create(exam.Id, "What is 3+3?", 2);
        await _context.Questions.AddRangeAsync(question1, question2);
        await _context.SaveChangesAsync();

        var choice1 = Choice.Create(question1.Id, "3", false);
        var choice2 = Choice.Create(question1.Id, "4", true);
        var choice3 = Choice.Create(question2.Id, "5", false);
        var choice4 = Choice.Create(question2.Id, "6", true);
        await _context.Choices.AddRangeAsync(choice1, choice2, choice3, choice4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithQuestionsAsync(exam.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(exam.Id, result.Id);
        Assert.Equal(2, result.Questions.Count);
        Assert.Equal(2, result.Questions.First().Choices.Count);
        Assert.Equal(2, result.Questions.Last().Choices.Count);
    }

    [Fact]
    public async Task GetByIdWithQuestionsAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdWithQuestionsAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByLessonIdAsync_WithValidLessonId_ReturnsExam()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Lesson Exam", 75);
        await _context.Exams.AddAsync(exam);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByLessonIdAsync(lessonId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(exam.Id, result.Id);
        Assert.Equal(lessonId, result.LessonId);
    }

    [Fact]
    public async Task GetByLessonIdAsync_WithInvalidLessonId_ReturnsNull()
    {
        // Arrange
        var invalidLessonId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByLessonIdAsync(invalidLessonId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetQuestionsByExamIdAsync_ReturnsQuestionsOrderedByOrder()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        await _context.Exams.AddAsync(exam);
        await _context.SaveChangesAsync();

        var question3 = Question.Create(exam.Id, "Question 3", 3);
        var question1 = Question.Create(exam.Id, "Question 1", 1);
        var question2 = Question.Create(exam.Id, "Question 2", 2);
        await _context.Questions.AddRangeAsync(question3, question1, question2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetQuestionsByExamIdAsync(exam.Id);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Order);
        Assert.Equal(2, result[1].Order);
        Assert.Equal(3, result[2].Order);
        Assert.Equal("Question 1", result[0].Text);
        Assert.Equal("Question 2", result[1].Text);
        Assert.Equal("Question 3", result[2].Text);
    }

    [Fact]
    public async Task GetQuestionsByExamIdAsync_WithNoQuestions_ReturnsEmptyList()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Empty Exam", 70);
        await _context.Exams.AddAsync(exam);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetQuestionsByExamIdAsync(exam.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetChoicesByQuestionIdAsync_ReturnsAllChoicesForQuestion()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        await _context.Exams.AddAsync(exam);
        await _context.SaveChangesAsync();

        var question = Question.Create(exam.Id, "What is 2+2?", 1);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        var choice1 = Choice.Create(question.Id, "3", false);
        var choice2 = Choice.Create(question.Id, "4", true);
        var choice3 = Choice.Create(question.Id, "5", false);
        await _context.Choices.AddRangeAsync(choice1, choice2, choice3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetChoicesByQuestionIdAsync(question.Id);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.Text == "3" && !c.IsCorrect);
        Assert.Contains(result, c => c.Text == "4" && c.IsCorrect);
        Assert.Contains(result, c => c.Text == "5" && !c.IsCorrect);
    }

    [Fact]
    public async Task GetChoicesByQuestionIdAsync_WithNoChoices_ReturnsEmptyList()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        await _context.Exams.AddAsync(exam);
        await _context.SaveChangesAsync();

        var question = Question.Create(exam.Id, "Question without choices", 1);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetChoicesByQuestionIdAsync(question.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_WithValidExam_AddsExam()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "New Exam", 65);

        // Act
        await _repository.AddAsync(exam);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Exams.FindAsync(exam.Id);
        Assert.NotNull(result);
        Assert.Equal("New Exam", result.Title);
        Assert.Equal(65, result.PassThreshold);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedExam_UpdatesExam()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Original Title", 70);
        await _context.Exams.AddAsync(exam);
        await _context.SaveChangesAsync();

        // Act
        exam.SetTitle("Updated Title");
        exam.SetPassThreshold(80);
        _repository.Update(exam);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Exams.FindAsync(exam.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal(80, result.PassThreshold);
    }

    [Fact]
    public async Task DeleteAsync_WithValidExam_SoftDeletesExam()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Exam to Delete", 70);
        await _context.Exams.AddAsync(exam);
        await _context.SaveChangesAsync();

        // Act
        exam.SoftDelete();
        _repository.Update(exam);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Exams.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == exam.Id);
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedExams()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var activeExam = Exam.Create(lessonId, _tenantId, "Active Exam", 70);
        var deletedExam = Exam.Create(lessonId, _tenantId, "Deleted Exam", 70);
        deletedExam.SoftDelete();

        await _context.Exams.AddRangeAsync(activeExam, deletedExam);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Exam", result.First().Title);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
