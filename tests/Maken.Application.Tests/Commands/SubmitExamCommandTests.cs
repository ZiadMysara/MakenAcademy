using FluentAssertions;
using Maken.Application.Commands.Exams;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Maken.Domain.Services;
using Maken.Domain.ValueObjects;
using Moq;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xunit;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for SubmitExamCommandHandler.
/// </summary>
public class SubmitExamCommandTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IRepository<Exam>> _mockExamRepository;
    private readonly Mock<IRepository<Question>> _mockQuestionRepository;
    private readonly Mock<IRepository<Choice>> _mockChoiceRepository;
    private readonly Mock<IRepository<Progress>> _mockProgressRepository;
    private readonly Mock<IRepository<Lesson>> _mockLessonRepository;
    private readonly ExamGradingService _gradingService;
    private readonly SubmitExamCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public SubmitExamCommandTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockExamRepository = new Mock<IRepository<Exam>>();
        _mockQuestionRepository = new Mock<IRepository<Question>>();
        _mockChoiceRepository = new Mock<IRepository<Choice>>();
        _mockProgressRepository = new Mock<IRepository<Progress>>();
        _mockLessonRepository = new Mock<IRepository<Lesson>>();
        _gradingService = new ExamGradingService();

        _mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);
        _mockUnitOfWork.Setup(x => x.Repository<Exam>()).Returns(_mockExamRepository.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Question>()).Returns(_mockQuestionRepository.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Choice>()).Returns(_mockChoiceRepository.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Progress>()).Returns(_mockProgressRepository.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Lesson>()).Returns(_mockLessonRepository.Object);

        _handler = new SubmitExamCommandHandler(_mockUnitOfWork.Object, _mockTenantContext.Object, _gradingService, new Mock<ILogger<SubmitExamCommandHandler>>().Object);
    }

    [Fact]
    public async Task Handle_ValidSubmission_PassingGrade_ReturnsPassedResult()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var question1 = Question.Create(examId, "What is 2+2?", 1);
        var question2 = Question.Create(examId, "What is 3+3?", 2);

        var choice1Correct = Choice.Create(question1.Id, "4", true);
        var choice1Wrong = Choice.Create(question1.Id, "5", false);
        var choice2Correct = Choice.Create(question2.Id, "6", true);
        var choice2Wrong = Choice.Create(question2.Id, "7", false);

        var lesson = Lesson.Create(courseId, _tenantId, "Test Lesson", "Description", Domain.Enums.ContentType.Video, "url", 1);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question1, question2 });
        _mockChoiceRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Choice> { choice1Correct, choice1Wrong, choice2Correct, choice2Wrong });
        _mockProgressRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());
        _mockLessonRepository.Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var command = new SubmitExamCommand(
            ExamId: examId,
            StudentId: studentId,
            Answers: new List<SubmitAnswerDto>
            {
                new SubmitAnswerDto(question1.Id, choice1Correct.Id),
                new SubmitAnswerDto(question2.Id, choice2Correct.Id)
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Passed.Should().BeTrue();
        result.Score.Should().Be(100);
        result.TotalQuestions.Should().Be(2);
        result.CorrectAnswers.Should().Be(2);

        _mockProgressRepository.Verify(x => x.AddAsync(It.IsAny<Progress>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidSubmission_FailingGrade_ReturnsFailedResult()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var question1 = Question.Create(examId, "What is 2+2?", 1);
        var question2 = Question.Create(examId, "What is 3+3?", 2);

        var choice1Correct = Choice.Create(question1.Id, "4", true);
        var choice1Wrong = Choice.Create(question1.Id, "5", false);
        var choice2Correct = Choice.Create(question2.Id, "6", true);
        var choice2Wrong = Choice.Create(question2.Id, "7", false);

        var lesson = Lesson.Create(courseId, _tenantId, "Test Lesson", "Description", Domain.Enums.ContentType.Video, "url", 1);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question1, question2 });
        _mockChoiceRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Choice> { choice1Correct, choice1Wrong, choice2Correct, choice2Wrong });
        _mockProgressRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());
        _mockLessonRepository.Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var command = new SubmitExamCommand(
            ExamId: examId,
            StudentId: studentId,
            Answers: new List<SubmitAnswerDto>
            {
                new SubmitAnswerDto(question1.Id, choice1Wrong.Id),
                new SubmitAnswerDto(question2.Id, choice2Wrong.Id)
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Passed.Should().BeFalse();
        result.Score.Should().Be(0);
        result.TotalQuestions.Should().Be(2);
        result.CorrectAnswers.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ExistingProgress_UpdatesProgressRecord()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var question = Question.Create(examId, "What is 2+2?", 1);
        var choiceCorrect = Choice.Create(question.Id, "4", true);
        var choiceWrong = Choice.Create(question.Id, "5", false);

        var lesson = Lesson.Create(courseId, _tenantId, "Test Lesson", "Description", Domain.Enums.ContentType.Video, "url", 1);
        var existingProgress = Progress.Create(studentId, lessonId, _tenantId);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question });
        _mockChoiceRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Choice> { choiceCorrect, choiceWrong });
        _mockProgressRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress> { existingProgress });

        var command = new SubmitExamCommand(
            ExamId: examId,
            StudentId: studentId,
            Answers: new List<SubmitAnswerDto>
            {
                new SubmitAnswerDto(question.Id, choiceCorrect.Id)
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Passed.Should().BeTrue();
        
        _mockProgressRepository.Verify(x => x.Update(existingProgress), Times.Once);
        _mockProgressRepository.Verify(x => x.AddAsync(It.IsAny<Progress>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var examId = Guid.NewGuid();
        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Exam?)null);

        var command = new SubmitExamCommand(
            ExamId: examId,
            StudentId: Guid.NewGuid(),
            Answers: new List<SubmitAnswerDto>()
        );

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
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

        var command = new SubmitExamCommand(
            ExamId: examId,
            StudentId: Guid.NewGuid(),
            Answers: new List<SubmitAnswerDto>()
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);
        var command = new SubmitExamCommand(
            ExamId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            Answers: new List<SubmitAnswerDto>()
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PartiallyCorrectAnswers_CalculatesCorrectScore()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 50);
        var examId = exam.Id;

        var question1 = Question.Create(examId, "Question 1", 1);
        var question2 = Question.Create(examId, "Question 2", 2);
        var question3 = Question.Create(examId, "Question 3", 3);
        var question4 = Question.Create(examId, "Question 4", 4);

        var choice1Correct = Choice.Create(question1.Id, "Correct", true);
        var choice1Wrong = Choice.Create(question1.Id, "Wrong", false);
        var choice2Correct = Choice.Create(question2.Id, "Correct", true);
        var choice2Wrong = Choice.Create(question2.Id, "Wrong", false);
        var choice3Correct = Choice.Create(question3.Id, "Correct", true);
        var choice3Wrong = Choice.Create(question3.Id, "Wrong", false);
        var choice4Correct = Choice.Create(question4.Id, "Correct", true);
        var choice4Wrong = Choice.Create(question4.Id, "Wrong", false);

        var lesson = Lesson.Create(courseId, _tenantId, "Test Lesson", "Description", Domain.Enums.ContentType.Video, "url", 1);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question1, question2, question3, question4 });
        _mockChoiceRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Choice> 
            { 
                choice1Correct, choice1Wrong, 
                choice2Correct, choice2Wrong,
                choice3Correct, choice3Wrong,
                choice4Correct, choice4Wrong
            });
        _mockProgressRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Progress>());
        _mockLessonRepository.Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        // Answer 2 out of 4 correctly (50%)
        var command = new SubmitExamCommand(
            ExamId: examId,
            StudentId: studentId,
            Answers: new List<SubmitAnswerDto>
            {
                new SubmitAnswerDto(question1.Id, choice1Correct.Id),
                new SubmitAnswerDto(question2.Id, choice2Wrong.Id),
                new SubmitAnswerDto(question3.Id, choice3Correct.Id),
                new SubmitAnswerDto(question4.Id, choice4Wrong.Id)
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Passed.Should().BeTrue(); // 50% meets 50% threshold
        result.Score.Should().Be(50);
        result.TotalQuestions.Should().Be(4);
        result.CorrectAnswers.Should().Be(2);
    }
}
