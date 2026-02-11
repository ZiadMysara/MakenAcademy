using FluentAssertions;
using Maken.Application.Commands.Exams;
using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;
using Moq;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xunit;

namespace Maken.Application.Tests.Commands;

/// <summary>
/// Unit tests for DeleteExamCommandHandler.
/// </summary>
public class DeleteExamCommandTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IRepository<Exam>> _mockExamRepository;
    private readonly Mock<IRepository<Question>> _mockQuestionRepository;
    private readonly Mock<IRepository<Choice>> _mockChoiceRepository;
    private readonly DeleteExamCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeleteExamCommandTests()
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

        _handler = new DeleteExamCommandHandler(_mockUnitOfWork.Object, _mockTenantContext.Object, new Mock<ILogger<DeleteExamCommandHandler>>().Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_SoftDeletesExam()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question>());

        var command = new DeleteExamCommand(examId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _mockExamRepository.Verify(x => x.Update(It.IsAny<Exam>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExamWithQuestionsAndChoices_CascadesSoftDelete()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        var examId = exam.Id;

        var question1 = Question.Create(examId, "Question 1", 1);
        var question2 = Question.Create(examId, "Question 2", 2);
        var choice1 = Choice.Create(question1.Id, "Choice 1", true);
        var choice2 = Choice.Create(question1.Id, "Choice 2", false);
        var choice3 = Choice.Create(question2.Id, "Choice 3", true);

        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);
        _mockQuestionRepository.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question1, question2 });
        
        // Setup choices for each question
        _mockChoiceRepository.SetupSequence(x => x.GetAllAsync(It.IsAny<Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Choice> { choice1, choice2 })
            .ReturnsAsync(new List<Choice> { choice3 });

        var command = new DeleteExamCommand(examId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _mockChoiceRepository.Verify(x => x.Update(It.IsAny<Choice>()), Times.Exactly(3));
        _mockQuestionRepository.Verify(x => x.Update(It.IsAny<Question>()), Times.Exactly(2));
        _mockExamRepository.Verify(x => x.Update(It.IsAny<Exam>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var examId = Guid.NewGuid();
        _mockExamRepository.Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Exam?)null);

        var command = new DeleteExamCommand(examId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
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

        var command = new DeleteExamCommand(examId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoTenantContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);
        var command = new DeleteExamCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
