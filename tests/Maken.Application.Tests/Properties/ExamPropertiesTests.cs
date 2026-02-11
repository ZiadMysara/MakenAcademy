using FsCheck;
using FsCheck.Xunit;
using Maken.Application.Commands.Exams;
using Maken.Application.Common.Interfaces;
using Maken.Application.Queries.Exams;
using Maken.Application.Tests.Helpers;
using Maken.Domain.Entities;
using Moq;
using Microsoft.Extensions.Logging;

namespace Maken.Application.Tests.Properties;

/// <summary>
/// Property-based tests for exam operations.
/// **Validates: Requirements from design.md**
/// </summary>
public class ExamPropertiesTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Property 8: Exam Creation with Questions and Choices
    /// **Validates: Requirements 3.1, 3.2**
    /// 
    /// For all valid exam data with questions and choices:
    /// - Creating an exam persists it with all questions and choices
    /// - All relationships are maintained correctly
    /// - Each question has exactly one correct answer
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ExamCreation_PersistsWithQuestionsAndChoices(
        NonEmptyString titleGen,
        Guid lessonId,
        PositiveInt passThresholdGen,
        PositiveInt questionCountGen)
    {
        // Extract values and filter control characters
        var title = new string(titleGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var passThreshold = passThresholdGen.Get % 101; // 0-100
        var questionCount = Math.Max(1, Math.Min(5, questionCountGen.Get));
        
        // Skip if title is empty
        if (string.IsNullOrWhiteSpace(title))
        {
            return true; // Skip this test case
        }

        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var examRepositoryMock = new Mock<IRepository<Exam>>();
        var questionRepositoryMock = new Mock<IRepository<Question>>();
        var choiceRepositoryMock = new Mock<IRepository<Choice>>();

        tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        unitOfWorkMock.Setup(x => x.Repository<Exam>()).Returns(examRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Question>()).Returns(questionRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Choice>()).Returns(choiceRepositoryMock.Object);

        // Track added entities
        var addedQuestions = new List<Question>();
        var addedChoices = new List<Choice>();

        questionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()))
            .Callback<Question, CancellationToken>((q, _) => addedQuestions.Add(q))
            .Returns(Task.CompletedTask);

        choiceRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Choice>(), It.IsAny<CancellationToken>()))
            .Callback<Choice, CancellationToken>((c, _) => addedChoices.Add(c))
            .Returns(Task.CompletedTask);

        var handler = new CreateExamCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, new Mock<ILogger<CreateExamCommandHandler>>().Object);

        // Generate questions with exactly one correct answer each
        var questions = new List<CreateQuestionDto>();
        for (int i = 0; i < questionCount; i++)
        {
            var choices = new List<CreateChoiceDto>
            {
                new CreateChoiceDto($"Choice A for Q{i + 1}", true),
                new CreateChoiceDto($"Choice B for Q{i + 1}", false),
                new CreateChoiceDto($"Choice C for Q{i + 1}", false)
            };
            questions.Add(new CreateQuestionDto($"Question {i + 1}", i + 1, choices));
        }

        var command = new CreateExamCommand(lessonId, title, passThreshold, questions);

        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        var afterCreation = DateTime.UtcNow;

        // Verify each question has exactly one correct answer
        var questionsGroupedByExam = addedChoices
            .GroupBy(c => c.QuestionId)
            .Select(g => new { QuestionId = g.Key, CorrectCount = g.Count(c => c.IsCorrect) });

        return result.Id != Guid.Empty &&
               result.Title == title.Trim() &&
               result.PassThreshold == passThreshold &&
               result.TenantId == _tenantId &&
               result.LessonId == lessonId &&
               result.CreatedAt >= beforeCreation &&
               result.CreatedAt <= afterCreation &&
               addedQuestions.Count == questionCount &&
               questionsGroupedByExam.All(q => q.CorrectCount == 1);
    }

    /// <summary>
    /// Property 9: Exam Update Preservation
    /// **Validates: Requirements 3.3**
    /// 
    /// For any existing exam and valid update data:
    /// - Updating the exam preserves the original ID, LessonId, TenantId, and CreatedAt
    /// - The new title and pass threshold are correctly applied
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ExamUpdate_PreservesOriginalFields(
        NonEmptyString originalTitleGen,
        NonEmptyString newTitleGen,
        Guid lessonId,
        Guid examId,
        PositiveInt originalPassThresholdGen,
        PositiveInt newPassThresholdGen)
    {
        // Extract values and filter control characters
        var originalTitle = new string(originalTitleGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var newTitle = new string(newTitleGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var originalPassThreshold = originalPassThresholdGen.Get % 101;
        var newPassThreshold = newPassThresholdGen.Get % 101;
        
        // Skip if any title is empty
        if (string.IsNullOrWhiteSpace(originalTitle) || string.IsNullOrWhiteSpace(newTitle))
        {
            return true; // Skip this test case
        }

        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var examRepositoryMock = new Mock<IRepository<Exam>>();
        var questionRepositoryMock = new Mock<IRepository<Question>>();
        var choiceRepositoryMock = new Mock<IRepository<Choice>>();

        var existingExam = Exam.Create(lessonId, _tenantId, originalTitle, originalPassThreshold);
        EntityTestHelper.SetId(existingExam, examId);
        var originalCreatedAt = existingExam.CreatedAt;

        tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        unitOfWorkMock.Setup(x => x.Repository<Exam>()).Returns(examRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Question>()).Returns(questionRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Choice>()).Returns(choiceRepositoryMock.Object);
        
        examRepositoryMock
            .Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingExam);

        questionRepositoryMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question>());

        var handler = new UpdateExamCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, new Mock<ILogger<UpdateExamCommandHandler>>().Object);

        // Create update command with one question (exactly one correct answer)
        var updateQuestions = new List<UpdateQuestionDto>
        {
            new UpdateQuestionDto(
                null,
                "Updated Question",
                1,
                new List<UpdateChoiceDto>
                {
                    new UpdateChoiceDto(null, "Choice A", true),
                    new UpdateChoiceDto(null, "Choice B", false)
                })
        };

        var command = new UpdateExamCommand(examId, newTitle, newPassThreshold, updateQuestions);

        // Act
        var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        return result.Id == examId &&
               existingExam.LessonId == lessonId &&
               existingExam.TenantId == _tenantId &&
               existingExam.Title == newTitle.Trim() &&
               existingExam.PassThreshold == newPassThreshold &&
               existingExam.CreatedAt == originalCreatedAt;
    }

    /// <summary>
    /// Property 10: Exam Soft Delete Cascade
    /// **Validates: Requirements 3.4**
    /// 
    /// For any exam with questions and choices:
    /// - Soft deleting the exam marks it as deleted (IsDeleted=true)
    /// - All associated questions are also soft deleted
    /// - All associated choices are also soft deleted
    /// - No records are physically removed
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ExamSoftDelete_CascadesToQuestionsAndChoices(
        Guid lessonId,
        Guid examId,
        PositiveInt questionCountGen)
    {
        var questionCount = Math.Max(1, Math.Min(5, questionCountGen.Get));

        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var examRepositoryMock = new Mock<IRepository<Exam>>();
        var questionRepositoryMock = new Mock<IRepository<Question>>();
        var choiceRepositoryMock = new Mock<IRepository<Choice>>();

        var existingExam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        EntityTestHelper.SetId(existingExam, examId);

        // Create questions and choices
        var questions = new List<Question>();
        var allChoices = new List<Choice>();
        
        for (int i = 0; i < questionCount; i++)
        {
            var question = Question.Create(examId, $"Question {i + 1}", i + 1);
            questions.Add(question);

            var choices = new List<Choice>
            {
                Choice.Create(question.Id, "Choice A", true),
                Choice.Create(question.Id, "Choice B", false),
                Choice.Create(question.Id, "Choice C", false)
            };
            allChoices.AddRange(choices);
        }

        tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        unitOfWorkMock.Setup(x => x.Repository<Exam>()).Returns(examRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Question>()).Returns(questionRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Choice>()).Returns(choiceRepositoryMock.Object);
        
        examRepositoryMock
            .Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingExam);

        questionRepositoryMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(questions);

        // Setup choice repository to return choices for each question
        foreach (var question in questions)
        {
            var questionChoices = allChoices.Where(c => c.QuestionId == question.Id).ToList();
            choiceRepositoryMock
                .Setup(x => x.GetAllAsync(
                    It.Is<System.Linq.Expressions.Expression<Func<Choice, bool>>>(
                        expr => expr.Compile()(questionChoices.First())),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(questionChoices);
        }

        var handler = new DeleteExamCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, new Mock<ILogger<DeleteExamCommandHandler>>().Object);
        var command = new DeleteExamCommand(examId);

        var beforeDeletion = DateTime.UtcNow;

        // Act
        var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        var afterDeletion = DateTime.UtcNow;

        return existingExam.IsDeleted &&
               existingExam.DeletedAt != null &&
               existingExam.DeletedAt >= beforeDeletion &&
               existingExam.DeletedAt <= afterDeletion &&
               questions.All(q => q.IsDeleted) &&
               allChoices.All(c => c.IsDeleted);
    }

    /// <summary>
    /// Property 11: Exam Answer Visibility by Role
    /// **Validates: Requirements 3.5**
    /// 
    /// For any exam:
    /// - When IncludeAnswers=false, correct answers are hidden (IsCorrect=null)
    /// - When IncludeAnswers=true, correct answers are visible (IsCorrect has value)
    /// - All other exam data is visible regardless of role
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ExamAnswerVisibility_RespectsIncludeAnswersFlag(
        NonEmptyString titleGen,
        Guid lessonId,
        Guid examId,
        PositiveInt passThresholdGen)
    {
        // Extract values and filter control characters
        var title = new string(titleGen.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        var passThreshold = passThresholdGen.Get % 101;
        
        // Skip if title is empty
        if (string.IsNullOrWhiteSpace(title))
        {
            return true; // Skip this test case
        }

        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var examRepositoryMock = new Mock<IRepository<Exam>>();
        var questionRepositoryMock = new Mock<IRepository<Question>>();
        var choiceRepositoryMock = new Mock<IRepository<Choice>>();

        var existingExam = Exam.Create(lessonId, _tenantId, title, passThreshold);
        EntityTestHelper.SetId(existingExam, examId);

        // Create a question with choices
        var question = Question.Create(examId, "Test Question", 1);
        var choices = new List<Choice>
        {
            Choice.Create(question.Id, "Correct Answer", true),
            Choice.Create(question.Id, "Wrong Answer 1", false),
            Choice.Create(question.Id, "Wrong Answer 2", false)
        };

        tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        unitOfWorkMock.Setup(x => x.Repository<Exam>()).Returns(examRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Question>()).Returns(questionRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Choice>()).Returns(choiceRepositoryMock.Object);
        
        examRepositoryMock
            .Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingExam);

        questionRepositoryMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question });

        choiceRepositoryMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Choice, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(choices);

        var handler = new GetExamQueryHandler(unitOfWorkMock.Object, tenantContextMock.Object);

        // Act - Get exam without answers (student view)
        var queryWithoutAnswers = new GetExamQuery(examId, IncludeAnswers: false);
        var resultWithoutAnswers = handler.Handle(queryWithoutAnswers, CancellationToken.None).GetAwaiter().GetResult();

        // Act - Get exam with answers (admin view)
        var queryWithAnswers = new GetExamQuery(examId, IncludeAnswers: true);
        var resultWithAnswers = handler.Handle(queryWithAnswers, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        if (resultWithoutAnswers == null || resultWithAnswers == null)
        {
            return false;
        }

        var allChoicesWithoutAnswers = resultWithoutAnswers.Questions
            .SelectMany(q => q.Choices)
            .ToList();

        var allChoicesWithAnswers = resultWithAnswers.Questions
            .SelectMany(q => q.Choices)
            .ToList();

        return allChoicesWithoutAnswers.All(c => c.IsCorrect == null) &&
               allChoicesWithAnswers.All(c => c.IsCorrect != null) &&
               resultWithoutAnswers.Title == title.Trim() &&
               resultWithAnswers.Title == title.Trim();
    }

    /// <summary>
    /// Property 12: Exam Grading Correctness
    /// **Validates: Requirements 6.1, 6.2**
    /// 
    /// For any exam submission with correct/incorrect answers:
    /// - Score is calculated correctly as (correctAnswers / totalQuestions) * 100
    /// - Pass/fail is determined correctly based on score >= passThreshold
    /// - Result contains accurate counts of total questions and correct answers
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ExamGrading_CalculatesScoreAndPassFailCorrectly(
        PositiveInt totalQuestionsGen,
        PositiveInt passThresholdGen)
    {
        var totalQuestions = Math.Max(1, Math.Min(100, totalQuestionsGen.Get));
        var passThreshold = passThresholdGen.Get % 101; // 0-100

        // Arrange
        var gradingService = new Maken.Domain.Services.ExamGradingService();

        // Test all possible correct answer counts
        for (int correctAnswers = 0; correctAnswers <= totalQuestions; correctAnswers++)
        {
            // Act
            var result = gradingService.GradeExam(totalQuestions, correctAnswers, passThreshold);

            // Calculate expected score
            var expectedScore = (int)Math.Round((double)correctAnswers / totalQuestions * 100);
            var expectedPassed = expectedScore >= passThreshold;

            // Assert
            if (result.Score != expectedScore ||
                result.Passed != expectedPassed ||
                result.TotalQuestions != totalQuestions ||
                result.CorrectAnswers != correctAnswers)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Property 12 (Edge Case): Exam Grading with Zero Correct Answers
    /// **Validates: Requirements 6.1, 6.2**
    /// 
    /// For any exam with zero correct answers:
    /// - Score should be 0
    /// - Should fail unless passThreshold is 0
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ExamGrading_ZeroCorrectAnswers_ReturnsZeroScore(
        PositiveInt totalQuestionsGen,
        PositiveInt passThresholdGen)
    {
        var totalQuestions = Math.Max(1, Math.Min(100, totalQuestionsGen.Get));
        var passThreshold = passThresholdGen.Get % 101;

        // Arrange
        var gradingService = new Maken.Domain.Services.ExamGradingService();

        // Act
        var result = gradingService.GradeExam(totalQuestions, 0, passThreshold);

        // Assert
        return result.Score == 0 &&
               result.Passed == (passThreshold == 0) &&
               result.TotalQuestions == totalQuestions &&
               result.CorrectAnswers == 0;
    }

    /// <summary>
    /// Property 12 (Edge Case): Exam Grading with All Correct Answers
    /// **Validates: Requirements 6.1, 6.2**
    /// 
    /// For any exam with all correct answers:
    /// - Score should be 100
    /// - Should always pass
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ExamGrading_AllCorrectAnswers_ReturnsHundredScore(
        PositiveInt totalQuestionsGen,
        PositiveInt passThresholdGen)
    {
        var totalQuestions = Math.Max(1, Math.Min(100, totalQuestionsGen.Get));
        var passThreshold = passThresholdGen.Get % 101;

        // Arrange
        var gradingService = new Maken.Domain.Services.ExamGradingService();

        // Act
        var result = gradingService.GradeExam(totalQuestions, totalQuestions, passThreshold);

        // Assert
        return result.Score == 100 &&
               result.Passed == true &&
               result.TotalQuestions == totalQuestions &&
               result.CorrectAnswers == totalQuestions;
    }

    /// <summary>
    /// Property 12 (Boundary): Exam Grading at Pass Threshold
    /// **Validates: Requirements 6.2**
    /// 
    /// For any exam where score exactly equals passThreshold:
    /// - Should pass (score >= passThreshold)
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ExamGrading_ScoreEqualsThreshold_Passes(PositiveInt passThresholdGen)
    {
        var passThreshold = passThresholdGen.Get % 101;
        
        // Calculate total questions and correct answers to achieve exact threshold
        // For simplicity, use 100 questions and calculate correct answers
        var totalQuestions = 100;
        var correctAnswers = (int)Math.Round(passThreshold * totalQuestions / 100.0);

        // Arrange
        var gradingService = new Maken.Domain.Services.ExamGradingService();

        // Act
        var result = gradingService.GradeExam(totalQuestions, correctAnswers, passThreshold);

        // Assert - score should be >= passThreshold (passes)
        return result.Score >= passThreshold && result.Passed == true;
    }

    /// <summary>
    /// Property 22: Unlimited Exam Attempts
    /// **Validates: Requirements 6.4**
    /// 
    /// For any exam:
    /// - Students can retake the exam unlimited times
    /// - Each attempt updates the progress record with the latest result
    /// - Previous attempts do not block new attempts
    /// </summary>
    [Property(MaxTest = 100)]
    public bool UnlimitedExamAttempts_AllowsMultipleSubmissions(
        Guid examId,
        Guid lessonId,
        Guid studentId,
        PositiveInt attemptCountGen)
    {
        var attemptCount = Math.Max(1, Math.Min(10, attemptCountGen.Get));

        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var examRepositoryMock = new Mock<IRepository<Exam>>();
        var questionRepositoryMock = new Mock<IRepository<Question>>();
        var choiceRepositoryMock = new Mock<IRepository<Choice>>();
        var progressRepositoryMock = new Mock<IRepository<Progress>>();
        var lessonRepositoryMock = new Mock<IRepository<Lesson>>();
        var gradingService = new Maken.Domain.Services.ExamGradingService();

        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        EntityTestHelper.SetId(exam, examId);

        var question = Question.Create(examId, "Question 1", 1);
        var choices = new List<Choice>
        {
            Choice.Create(question.Id, "Correct", true),
            Choice.Create(question.Id, "Wrong", false)
        };

        var lesson = Lesson.Create(Guid.NewGuid(), _tenantId, "Test Lesson", "Description", Domain.Enums.ContentType.Video, "url", 1);
        EntityTestHelper.SetId(lesson, lessonId);

        tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        unitOfWorkMock.Setup(x => x.Repository<Exam>()).Returns(examRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Question>()).Returns(questionRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Choice>()).Returns(choiceRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Progress>()).Returns(progressRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Lesson>()).Returns(lessonRepositoryMock.Object);

        examRepositoryMock
            .Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);

        questionRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question });

        choiceRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(choices);

        lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        Progress? currentProgress = null;
        progressRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => currentProgress != null ? new List<Progress> { currentProgress } : new List<Progress>());

        progressRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Progress>(), It.IsAny<CancellationToken>()))
            .Callback<Progress, CancellationToken>((p, _) => currentProgress = p)
            .Returns(Task.CompletedTask);

        progressRepositoryMock
            .Setup(x => x.Update(It.IsAny<Progress>()))
            .Callback<Progress>(p => currentProgress = p);

        var handler = new SubmitExamCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, gradingService, new Mock<ILogger<SubmitExamCommandHandler>>().Object);

        // Act - Submit exam multiple times
        for (int i = 0; i < attemptCount; i++)
        {
            var command = new SubmitExamCommand(
                ExamId: examId,
                StudentId: studentId,
                Answers: new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto(question.Id, choices[0].Id) // Always correct answer
                });

            var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

            // Assert each attempt succeeds
            if (!result.Passed || result.Score != 100)
            {
                return false;
            }
        }

        // Assert progress was updated (not blocked)
        return currentProgress != null &&
               currentProgress.ExamPassed == true &&
               currentProgress.ExamScore == 100;
    }

    /// <summary>
    /// Property 22 (Retake): Unlimited Exam Attempts with Different Results
    /// **Validates: Requirements 6.4**
    /// 
    /// For any exam with multiple attempts:
    /// - Each attempt can have different results (pass/fail)
    /// - Latest attempt result is stored in progress
    /// - No limit on number of attempts
    /// </summary>
    [Property(MaxTest = 100)]
    public bool UnlimitedExamAttempts_UpdatesProgressWithLatestResult(
        Guid examId,
        Guid lessonId,
        Guid studentId)
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var tenantContextMock = new Mock<ITenantContext>();
        var examRepositoryMock = new Mock<IRepository<Exam>>();
        var questionRepositoryMock = new Mock<IRepository<Question>>();
        var choiceRepositoryMock = new Mock<IRepository<Choice>>();
        var progressRepositoryMock = new Mock<IRepository<Progress>>();
        var lessonRepositoryMock = new Mock<IRepository<Lesson>>();
        var gradingService = new Maken.Domain.Services.ExamGradingService();

        var exam = Exam.Create(lessonId, _tenantId, "Test Exam", 70);
        EntityTestHelper.SetId(exam, examId);

        var question = Question.Create(examId, "Question 1", 1);
        var choices = new List<Choice>
        {
            Choice.Create(question.Id, "Correct", true),
            Choice.Create(question.Id, "Wrong", false)
        };

        var lesson = Lesson.Create(Guid.NewGuid(), _tenantId, "Test Lesson", "Description", Domain.Enums.ContentType.Video, "url", 1);
        EntityTestHelper.SetId(lesson, lessonId);

        tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        unitOfWorkMock.Setup(x => x.Repository<Exam>()).Returns(examRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Question>()).Returns(questionRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Choice>()).Returns(choiceRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Progress>()).Returns(progressRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.Repository<Lesson>()).Returns(lessonRepositoryMock.Object);

        examRepositoryMock
            .Setup(x => x.GetByIdAsync(examId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exam);

        questionRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Question, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { question });

        choiceRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Choice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(choices);

        lessonRepositoryMock
            .Setup(x => x.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        Progress? currentProgress = null;
        progressRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Progress, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => currentProgress != null ? new List<Progress> { currentProgress } : new List<Progress>());

        progressRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Progress>(), It.IsAny<CancellationToken>()))
            .Callback<Progress, CancellationToken>((p, _) => currentProgress = p)
            .Returns(Task.CompletedTask);

        progressRepositoryMock
            .Setup(x => x.Update(It.IsAny<Progress>()))
            .Callback<Progress>(p => currentProgress = p);

        var handler = new SubmitExamCommandHandler(unitOfWorkMock.Object, tenantContextMock.Object, gradingService, new Mock<ILogger<SubmitExamCommandHandler>>().Object);

        // Act - First attempt: fail (wrong answer)
        var failCommand = new SubmitExamCommand(
            ExamId: examId,
            StudentId: studentId,
            Answers: new List<SubmitAnswerDto>
            {
                new SubmitAnswerDto(question.Id, choices[1].Id) // Wrong answer
            });

        var failResult = handler.Handle(failCommand, CancellationToken.None).GetAwaiter().GetResult();

        // Assert first attempt failed
        if (failResult.Passed || currentProgress?.ExamPassed != false)
        {
            return false;
        }

        // Act - Second attempt: pass (correct answer)
        var passCommand = new SubmitExamCommand(
            ExamId: examId,
            StudentId: studentId,
            Answers: new List<SubmitAnswerDto>
            {
                new SubmitAnswerDto(question.Id, choices[0].Id) // Correct answer
            });

        var passResult = handler.Handle(passCommand, CancellationToken.None).GetAwaiter().GetResult();

        // Assert second attempt passed and progress was updated
        return passResult.Passed &&
               currentProgress != null &&
               currentProgress.ExamPassed == true &&
               currentProgress.ExamScore == 100;
    }
}
