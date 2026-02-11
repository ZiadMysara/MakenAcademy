using FluentAssertions;
using Maken.Application.Commands.Exams;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using Moq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for CreateExamCommandHandler.
/// </summary>
public class CreateExamCommandTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IRepository<Exam>> _mockExamRepository;
    private readonly Mock<IRepository<Question>> _mockQuestionRepository;
    private readonly Mock<IRepository<Choice>> _mockChoiceRepository;
    private readonly CreateExamCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateExamCommandTests()
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

        _handler = new CreateExamCommandHandler(_mockUnitOfWork.Object, _mockTenantContext.Object, new Mock<ILogger<CreateExamCommandHandler>>().Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesExamWithQuestionsAndChoices()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var command = new CreateExamCommand(
            LessonId: lessonId,
            Title: "Final Exam",
            PassThreshold: 70,
            Questions: new List<CreateQuestionDto>
            {
                new CreateQuestionDto(
                    Text: "What is 2+2?",
                    Order: 1,
                    Choices: new List<CreateChoiceDto>
                    {
                        new CreateChoiceDto(Text: "3", IsCorrect: false),
                        new CreateChoiceDto(Text: "4", IsCorrect: true),
                        new CreateChoiceDto(Text: "5", IsCorrect: false)
                    }
                )
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.LessonId.Should().Be(lessonId);
        result.TenantId.Should().Be(_tenantId);
        result.Title.Should().Be("Final Exam");
        result.PassThreshold.Should().Be(70);

        _mockExamRepository.Verify(x => x.AddAsync(It.IsAny<Exam>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockQuestionRepository.Verify(x => x.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockChoiceRepository.Verify(x => x.AddAsync(It.IsAny<Choice>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleQuestions_CreatesAllQuestionsAndChoices()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var command = new CreateExamCommand(
            LessonId: lessonId,
            Title: "Math Exam",
            PassThreshold: 80,
            Questions: new List<CreateQuestionDto>
            {
                new CreateQuestionDto(
                    Text: "What is 2+2?",
                    Order: 1,
                    Choices: new List<CreateChoiceDto>
                    {
                        new CreateChoiceDto(Text: "3", IsCorrect: false),
                        new CreateChoiceDto(Text: "4", IsCorrect: true)
                    }
                ),
                new CreateQuestionDto(
                    Text: "What is 3+3?",
                    Order: 2,
                    Choices: new List<CreateChoiceDto>
                    {
                        new CreateChoiceDto(Text: "5", IsCorrect: false),
                        new CreateChoiceDto(Text: "6", IsCorrect: true)
                    }
                )
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mockQuestionRepository.Verify(x => x.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockChoiceRepository.Verify(x => x.AddAsync(It.IsAny<Choice>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task Handle_NoTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);
        var command = new CreateExamCommand(
            LessonId: Guid.NewGuid(),
            Title: "Test Exam",
            PassThreshold: 70,
            Questions: new List<CreateQuestionDto>
            {
                new CreateQuestionDto(
                    Text: "Question?",
                    Order: 1,
                    Choices: new List<CreateChoiceDto>
                    {
                        new CreateChoiceDto(Text: "A", IsCorrect: true),
                        new CreateChoiceDto(Text: "B", IsCorrect: false)
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
        var command = new CreateExamCommand(
            LessonId: Guid.NewGuid(),
            Title: "Test Exam",
            PassThreshold: 70,
            Questions: new List<CreateQuestionDto>
            {
                new CreateQuestionDto(
                    Text: "Question?",
                    Order: 1,
                    Choices: new List<CreateChoiceDto>
                    {
                        new CreateChoiceDto(Text: "A", IsCorrect: false),
                        new CreateChoiceDto(Text: "B", IsCorrect: false)
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
        var command = new CreateExamCommand(
            LessonId: Guid.NewGuid(),
            Title: "Test Exam",
            PassThreshold: 70,
            Questions: new List<CreateQuestionDto>
            {
                new CreateQuestionDto(
                    Text: "Question?",
                    Order: 1,
                    Choices: new List<CreateChoiceDto>
                    {
                        new CreateChoiceDto(Text: "A", IsCorrect: true),
                        new CreateChoiceDto(Text: "B", IsCorrect: true)
                    }
                )
            }
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("must have exactly one correct answer");
    }
}
