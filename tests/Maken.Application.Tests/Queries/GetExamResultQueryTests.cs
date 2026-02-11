using FluentAssertions;
using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Exams;
using Maken.Domain.Entities;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace Maken.Application.Tests.Queries;

/// <summary>
/// Unit tests for GetExamResultQueryHandler.
/// </summary>
public class GetExamResultQueryTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IRepository<Exam>> _mockExamRepository;
    private readonly Mock<IRepository<Progress>> _mockProgressRepository;
    private readonly Mock<IRepository<Question>> _mockQuestionRepository;
    private readonly GetExamResultQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetExamResultQueryTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockExamRepository = new Mock<IRepository<Exam>>();
        _mockProgressRepository = new Mock<IRepository<Progress>>();
        _mockQuestionRepository = new Mock<IRepository<Question>>();

        _mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);
        _mockUnitOfWork.Setup(x => x.Repository<Exam>()).Returns(_mockExamRepository.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Progress>()).Returns(_mockProgressRepository.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Question>()).Returns(_mockQuestionRepository.Object);

        _handler = new GetExamResultQueryHandler(_mockUnitOfWork.Object, _mockTenantContext.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsExamResult()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var progress = Progress.Create(studentId, lessonId, _tenantId);
        progress.RecordExamResult(passed: true, score: 85);

        var question1 = Question.Create(examId, "Question 1", 1);
        var question2 = Question.Create(examId, "Question 2", 2);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockProgressRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress> { progress });
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question1, question2 });

        var query = new GetExamResultQuery(examId, studentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ExamId.Should().Be(examId);
        result.StudentId.Should().Be(studentId);
        result.Passed.Should().BeTrue();
        result.Score.Should().Be(85);
        result.TotalQuestions.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NoProgressRecord_ReturnsNull()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockProgressRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());

        var query = new GetExamResultQuery(examId, studentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ProgressWithoutExamScore_ReturnsNull()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var progress = Progress.Create(studentId, lessonId, _tenantId);
        // Don't record exam result - progress exists but no exam score

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockProgressRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress> { progress });

        var query = new GetExamResultQuery(examId, studentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ExamNotFound_ReturnsNull()
    {
        // Arrange
        var examId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Exam?)null);

        var query = new GetExamResultQuery(examId, studentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WrongTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var differentTenantId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, differentTenantId, "Test Exam", 70);
        var examId = exam.Id;

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);

        var query = new GetExamResultQuery(examId, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);
        var query = new GetExamResultQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FailedExam_ReturnsFailedResult()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var progress = Progress.Create(studentId, lessonId, _tenantId);
        progress.RecordExamResult(passed: false, score: 45);

        var question1 = Question.Create(examId, "Question 1", 1);
        var question2 = Question.Create(examId, "Question 2", 2);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockProgressRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress> { progress });
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question1, question2 });

        var query = new GetExamResultQuery(examId, studentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Passed.Should().BeFalse();
        result.Score.Should().Be(45);
    }

    [Fact]
    public async Task Handle_CalculatesCorrectAnswersFromScore()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var progress = Progress.Create(studentId, lessonId, _tenantId);
        progress.RecordExamResult(passed: true, score: 75); // 75% of 4 questions = 3 correct

        var question1 = Question.Create(examId, "Question 1", 1);
        var question2 = Question.Create(examId, "Question 2", 2);
        var question3 = Question.Create(examId, "Question 3", 3);
        var question4 = Question.Create(examId, "Question 4", 4);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockProgressRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress> { progress });
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question1, question2, question3, question4 });

        var query = new GetExamResultQuery(examId, studentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.TotalQuestions.Should().Be(4);
        result.CorrectAnswers.Should().Be(3); // 75% of 4 = 3
        result.Score.Should().Be(75);
    }
}
