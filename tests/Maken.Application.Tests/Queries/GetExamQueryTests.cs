using FluentAssertions;
using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Exams;
using Maken.Domain.Entities;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace Maken.Application.Tests.Queries;

/// <summary>
/// Unit tests for GetExamQueryHandler.
/// </summary>
public class GetExamQueryTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IRepository<Exam>> _mockExamRepository;
    private readonly Mock<IRepository<Question>> _mockQuestionRepository;
    private readonly Mock<IRepository<Choice>> _mockChoiceRepository;
    private readonly GetExamQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetExamQueryTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockExamRepository = new Mock<IRepository<Exam>>();
        _mockQuestionRepository = new Mock<IRepository<Question>>();
        _mockChoiceRepository = new Mock<IRepository<Choice>>();

        _mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);
        _mockUnitOfWork.Setup(x => x.Repository<Exam>()).Returns(_mockExamRepository.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Question>()).Returns(_mockQuestionRepository.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Choice>()).Returns(_mockChoiceRepository.Object);

        _handler = new GetExamQueryHandler(_mockUnitOfWork.Object, _mockTenantContext.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsExamDto()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var question = Question.Create(examId, "What is 2+2?", 1);
        var choice1 = Choice.Create(question.Id, "3", false);
        var choice2 = Choice.Create(question.Id, "4", true);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question });
        _mockChoiceRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Choice> { choice1, choice2 });

        var query = new GetExamQuery(examId, IncludeAnswers: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(examId);
        result.Title.Should().Be("Test Exam");
        result.PassThreshold.Should().Be(70);
        result.Questions.Should().HaveCount(1);
        result.Questions[0].Text.Should().Be("What is 2+2?");
        result.Questions[0].Choices.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_IncludeAnswersTrue_ReturnsCorrectAnswers()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var question = Question.Create(examId, "What is 2+2?", 1);
        var choice1 = Choice.Create(question.Id, "3", false);
        var choice2 = Choice.Create(question.Id, "4", true);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question });
        _mockChoiceRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Choice> { choice1, choice2 });

        var query = new GetExamQuery(examId, IncludeAnswers: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Questions[0].Choices[0].IsCorrect.Should().Be(false);
        result.Questions[0].Choices[1].IsCorrect.Should().Be(true);
    }

    [Fact]
    public async Task Handle_IncludeAnswersFalse_HidesCorrectAnswers()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var question = Question.Create(examId, "What is 2+2?", 1);
        var choice1 = Choice.Create(question.Id, "3", false);
        var choice2 = Choice.Create(question.Id, "4", true);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question });
        _mockChoiceRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Choice> { choice1, choice2 });

        var query = new GetExamQuery(examId, IncludeAnswers: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Questions[0].Choices[0].IsCorrect.Should().BeNull();
        result.Questions[0].Choices[1].IsCorrect.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MultipleQuestions_OrdersByOrderProperty()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var question1 = Question.Create(examId, "Question 3", 3);
        var question2 = Question.Create(examId, "Question 1", 1);
        var question3 = Question.Create(examId, "Question 2", 2);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question1, question2, question3 });
        _mockChoiceRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Choice>());

        var query = new GetExamQuery(examId, IncludeAnswers: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Questions.Should().HaveCount(3);
        result.Questions[0].Text.Should().Be("Question 1");
        result.Questions[1].Text.Should().Be("Question 2");
        result.Questions[2].Text.Should().Be("Question 3");
    }

    [Fact]
    public async Task Handle_ExamNotFound_ReturnsNull()
    {
        // Arrange
        var examId = Guid.NewGuid();
        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Exam?)null);

        var query = new GetExamQuery(examId, IncludeAnswers: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WrongTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, differentTenantId, "Test Exam", 70);
        var examId = exam.Id;

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);

        var query = new GetExamQuery(examId, IncludeAnswers: true);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);
        var query = new GetExamQuery(Guid.NewGuid(), IncludeAnswers: true);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
