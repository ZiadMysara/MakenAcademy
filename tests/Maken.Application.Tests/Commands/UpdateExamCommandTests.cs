using FluentAssertions;
using Maken.Application.Commands.Exams;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Moq;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xunit;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for UpdateExamCommandHandler.
/// </summary>
public class UpdateExamCommandTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IRepository<Exam>> _mockExamRepository;
    private readonly Mock<IRepository<Question>> _mockQuestionRepository;
    private readonly Mock<IRepository<Choice>> _mockChoiceRepository;
    private readonly UpdateExamCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdateExamCommandTests()
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

        _handler = new UpdateExamCommandHandler(_mockUnitOfWork.Object, _mockTenantContext.Object, new Mock<ILogger<UpdateExamCommandHandler>>().Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesExam()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Old Title", 60);
        var examId = exam.Id;

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question>());

        var command = new UpdateExamCommand(
            Id: examId,
            Title: "Updated Title",
            PassThreshold: 80,
            Questions: new List<UpdateQuestionDto>
            {
                new UpdateQuestionDto(
                    Id: null,
                    Text: "What is 2+2?",
                    Order: 1,
                    Choices: new List<UpdateChoiceDto>
                    {
                        new UpdateChoiceDto(Id: null, Text: "3", IsCorrect: false),
                        new UpdateChoiceDto(Id: null, Text: "4", IsCorrect: true)
                    }
                )
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(examId);
        result.Title.Should().Be("Updated Title");
        result.PassThreshold.Should().Be(80);

        _mockQuestionRepository.Verify(x => x.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockChoiceRepository.Verify(x => x.AddAsync(It.IsAny<Choice>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var examId = Guid.NewGuid();
        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Exam?)null);

        var command = new UpdateExamCommand(
            Id: examId,
            Title: "Updated Title",
            PassThreshold: 80,
            Questions: new List<UpdateQuestionDto>
            {
                new UpdateQuestionDto(
                    Id: null,
                    Text: "Question?",
                    Order: 1,
                    Choices: new List<UpdateChoiceDto>
                    {
                        new UpdateChoiceDto(Id: null, Text: "A", IsCorrect: true),
                        new UpdateChoiceDto(Id: null, Text: "B", IsCorrect: false)
                    }
                )
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WrongTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, differentTenantId, "Title", 70);
        var examId = exam.Id;

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);

        var command = new UpdateExamCommand(
            Id: examId,
            Title: "Updated Title",
            PassThreshold: 80,
            Questions: new List<UpdateQuestionDto>
            {
                new UpdateQuestionDto(
                    Id: null,
                    Text: "Question?",
                    Order: 1,
                    Choices: new List<UpdateChoiceDto>
                    {
                        new UpdateChoiceDto(Id: null, Text: "A", IsCorrect: true),
                        new UpdateChoiceDto(Id: null, Text: "B", IsCorrect: false)
                    }
                )
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);
        var command = new UpdateExamCommand(
            Id: Guid.NewGuid(),
            Title: "Updated Title",
            PassThreshold: 80,
            Questions: new List<UpdateQuestionDto>
            {
                new UpdateQuestionDto(
                    Id: null,
                    Text: "Question?",
                    Order: 1,
                    Choices: new List<UpdateChoiceDto>
                    {
                        new UpdateChoiceDto(Id: null, Text: "A", IsCorrect: true),
                        new UpdateChoiceDto(Id: null, Text: "B", IsCorrect: false)
                    }
                )
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_QuestionWithNoCorrectAnswer_ThrowsInvalidOperationException()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Title", 70);
        var examId = exam.Id;

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);

        var command = new UpdateExamCommand(
            Id: examId,
            Title: "Updated Title",
            PassThreshold: 80,
            Questions: new List<UpdateQuestionDto>
            {
                new UpdateQuestionDto(
                    Id: null,
                    Text: "Question?",
                    Order: 1,
                    Choices: new List<UpdateChoiceDto>
                    {
                        new UpdateChoiceDto(Id: null, Text: "A", IsCorrect: false),
                        new UpdateChoiceDto(Id: null, Text: "B", IsCorrect: false)
                    }
                )
            }
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("must have exactly one correct answer");
    }

    [Fact]
    public async Task Handle_QuestionWithMultipleCorrectAnswers_ThrowsInvalidOperationException()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Title", 70);
        var examId = exam.Id;

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);

        var command = new UpdateExamCommand(
            Id: examId,
            Title: "Updated Title",
            PassThreshold: 80,
            Questions: new List<UpdateQuestionDto>
            {
                new UpdateQuestionDto(
                    Id: null,
                    Text: "Question?",
                    Order: 1,
                    Choices: new List<UpdateChoiceDto>
                    {
                        new UpdateChoiceDto(Id: null, Text: "A", IsCorrect: true),
                        new UpdateChoiceDto(Id: null, Text: "B", IsCorrect: true)
                    }
                )
            }
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("must have exactly one correct answer");
    }

    [Fact]
    public async Task Handle_DeletesExistingQuestionsAndChoices()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Title", 70);
        var examId = exam.Id;

        var existingQuestion = Question.Create(examId, "Old Question", 1);
        var existingChoice = Choice.Create(existingQuestion.Id, "Old Choice", true);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { existingQuestion });
        _mockChoiceRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Choice> { existingChoice });

        var command = new UpdateExamCommand(
            Id: examId,
            Title: "Updated Title",
            PassThreshold: 80,
            Questions: new List<UpdateQuestionDto>
            {
                new UpdateQuestionDto(
                    Id: null,
                    Text: "New Question",
                    Order: 1,
                    Choices: new List<UpdateChoiceDto>
                    {
                        new UpdateChoiceDto(Id: null, Text: "A", IsCorrect: true),
                        new UpdateChoiceDto(Id: null, Text: "B", IsCorrect: false)
                    }
                )
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockQuestionRepository.Verify(x => x.Update(It.IsAny<Question>()), Times.Once);
        _mockChoiceRepository.Verify(x => x.Update(It.IsAny<Choice>()), Times.Once);
    }
}
