using FsCheck;
using FsCheck.Xunit;
using Maken.Domain.Services;

namespace Maken.Application.Tests.Properties;

/// <summary>
/// Property-based tests for progression rules (course/lesson unlocking).
/// Tests Properties 19, 20, and 23 from design.md.
/// </summary>
public class ProgressionPropertiesTests
{
    private readonly ProgressionService _progressionService;

    public ProgressionPropertiesTests()
    {
        _progressionService = new ProgressionService();
    }

    /// <summary>
    /// Property 20: Course Prerequisite Enforcement
    /// **Validates: Requirements 1.4**
    /// 
    /// For any course C with prerequisites P1, P2, ..., Pn:
    /// - If all prerequisites are completed, course is unlocked
    /// - If any prerequisite is incomplete, course is locked
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CoursePrerequisiteEnforcement_AllPrerequisitesCompletedMeansUnlocked(bool hasPrerequisites)
    {
        // When all prerequisites are completed
        var allPrerequisitesCompleted = true;

        // Course should be unlocked
        var isUnlocked = _progressionService.IsCourseUnlocked(hasPrerequisites, allPrerequisitesCompleted);

        return isUnlocked;
    }

    [Property(MaxTest = 100)]
    public bool CoursePrerequisiteEnforcement_IncompletePrerequisitesMeansLocked(bool hasPrerequisites)
    {
        // When prerequisites exist but are not all completed
        if (!hasPrerequisites)
        {
            // Skip: no prerequisites means always unlocked
            return true;
        }

        var allPrerequisitesCompleted = false;

        // Course should be locked
        var isUnlocked = _progressionService.IsCourseUnlocked(hasPrerequisites, allPrerequisitesCompleted);

        return !isUnlocked;
    }

    [Property(MaxTest = 100)]
    public bool CoursePrerequisiteEnforcement_NoPrerequisitesMeansAlwaysUnlocked(bool allPrerequisitesCompleted)
    {
        // When course has no prerequisites
        var hasPrerequisites = false;

        // Course should always be unlocked regardless of completion status
        var isUnlocked = _progressionService.IsCourseUnlocked(hasPrerequisites, allPrerequisitesCompleted);

        return isUnlocked;
    }

    /// <summary>
    /// Property 23: Free Flow Mode Bypass
    /// **Validates: Requirements 1.5**
    /// 
    /// For any course with FreeFlowMode=true:
    /// - All lessons are unlocked regardless of completion status
    /// - Sequential progression rules are bypassed
    /// </summary>
    [Property(MaxTest = 100)]
    public bool FreeFlowModeBypass_AllLessonsUnlockedRegardlessOfCompletion(
        PositiveInt lessonOrderPos,
        bool previousLessonCompleted,
        bool? previousLessonExamPassed)
    {
        var lessonOrder = lessonOrderPos.Get;
        var freeFlowMode = true;

        // In free flow mode, all lessons should be unlocked
        var isUnlocked = _progressionService.IsLessonUnlocked(
            lessonOrder,
            freeFlowMode,
            previousLessonCompleted,
            previousLessonExamPassed);

        return isUnlocked;
    }

    [Property(MaxTest = 100)]
    public bool FreeFlowModeBypass_FirstLessonAlwaysUnlocked(
        bool freeFlowMode,
        bool previousLessonCompleted,
        bool? previousLessonExamPassed)
    {
        var lessonOrder = 1;

        // First lesson should always be unlocked
        var isUnlocked = _progressionService.IsLessonUnlocked(
            lessonOrder,
            freeFlowMode,
            previousLessonCompleted,
            previousLessonExamPassed);

        return isUnlocked;
    }

    [Property(MaxTest = 100)]
    public bool FreeFlowModeBypass_SequentialModeRequiresPreviousCompletion(
        PositiveInt lessonOrderPos,
        bool? previousLessonExamPassed)
    {
        var lessonOrder = lessonOrderPos.Get;
        if (lessonOrder <= 1)
        {
            // Skip: first lesson is always unlocked
            return true;
        }

        var freeFlowMode = false;
        var previousLessonCompleted = false;

        // In sequential mode, if previous lesson is not completed, current lesson is locked
        var isUnlocked = _progressionService.IsLessonUnlocked(
            lessonOrder,
            freeFlowMode,
            previousLessonCompleted,
            previousLessonExamPassed);

        return !isUnlocked;
    }

    /// <summary>
    /// Property 19: Sequential Lesson Unlock
    /// **Validates: Requirements FR-038**
    /// 
    /// For any lesson N+1 in a course (where Free Flow mode is disabled),
    /// the lesson should be locked (not accessible) until lesson N is completed
    /// AND its exam (if present) is passed. The first lesson (N=1) should always
    /// be unlocked for enrolled students.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SequentialLessonUnlock_FirstLessonAlwaysUnlocked(
        bool freeFlowMode,
        bool previousLessonCompleted,
        bool? previousLessonExamPassed)
    {
        var lessonOrder = 1;

        // First lesson should always be unlocked regardless of mode or previous completion
        var isUnlocked = _progressionService.IsLessonUnlocked(
            lessonOrder,
            freeFlowMode,
            previousLessonCompleted,
            previousLessonExamPassed);

        return isUnlocked;
    }

    [Property(MaxTest = 100)]
    public bool SequentialLessonUnlock_SubsequentLessonRequiresPreviousCompletion(
        PositiveInt lessonOrderPos,
        bool? previousLessonExamPassed)
    {
        var lessonOrder = lessonOrderPos.Get;
        if (lessonOrder <= 1)
        {
            // Skip: first lesson is always unlocked
            return true;
        }

        var freeFlowMode = false;
        var previousLessonCompleted = false;

        // In sequential mode, if previous lesson is not completed, current lesson is locked
        var isUnlocked = _progressionService.IsLessonUnlocked(
            lessonOrder,
            freeFlowMode,
            previousLessonCompleted,
            previousLessonExamPassed);

        return !isUnlocked;
    }

    [Property(MaxTest = 100)]
    public bool SequentialLessonUnlock_RequiresExamPassIfPresent(PositiveInt lessonOrderPos)
    {
        var lessonOrder = lessonOrderPos.Get;
        if (lessonOrder <= 1)
        {
            // Skip: first lesson is always unlocked
            return true;
        }

        var freeFlowMode = false;
        var previousLessonCompleted = true;
        var previousLessonExamPassed = false; // Exam exists but not passed

        // If previous lesson has an exam that's not passed, current lesson is locked
        var isUnlocked = _progressionService.IsLessonUnlocked(
            lessonOrder,
            freeFlowMode,
            previousLessonCompleted,
            previousLessonExamPassed);

        return !isUnlocked;
    }

    [Property(MaxTest = 100)]
    public bool SequentialLessonUnlock_UnlocksWhenPreviousCompletedAndExamPassed(PositiveInt lessonOrderPos)
    {
        var lessonOrder = lessonOrderPos.Get;
        if (lessonOrder <= 1)
        {
            // Skip: first lesson is always unlocked
            return true;
        }

        var freeFlowMode = false;
        var previousLessonCompleted = true;
        var previousLessonExamPassed = true; // Exam passed

        // If previous lesson is completed and exam passed, current lesson is unlocked
        var isUnlocked = _progressionService.IsLessonUnlocked(
            lessonOrder,
            freeFlowMode,
            previousLessonCompleted,
            previousLessonExamPassed);

        return isUnlocked;
    }

    [Property(MaxTest = 100)]
    public bool SequentialLessonUnlock_UnlocksWhenPreviousCompletedAndNoExam(PositiveInt lessonOrderPos)
    {
        var lessonOrder = lessonOrderPos.Get;
        if (lessonOrder <= 1)
        {
            // Skip: first lesson is always unlocked
            return true;
        }

        var freeFlowMode = false;
        var previousLessonCompleted = true;
        var previousLessonExamPassed = (bool?)null; // No exam

        // If previous lesson is completed and has no exam, current lesson is unlocked
        var isUnlocked = _progressionService.IsLessonUnlocked(
            lessonOrder,
            freeFlowMode,
            previousLessonCompleted,
            previousLessonExamPassed);

        return isUnlocked;
    }

    /// <summary>
    /// Property 21: Exam Unlock After Lesson View
    /// **Validates: Requirements 6.3**
    /// 
    /// For any exam associated with a lesson:
    /// - Exam is locked (not accessible) until the lesson is viewed
    /// - Once lesson is viewed (progress record exists), exam is unlocked
    /// - This rule applies regardless of course mode (Free Flow or Sequential)
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ExamUnlock_RequiresLessonViewed(bool lessonViewed)
    {
        // Exam unlock status should match lesson viewed status
        var isUnlocked = _progressionService.IsExamUnlocked(lessonViewed);

        return isUnlocked == lessonViewed;
    }

    [Property(MaxTest = 100)]
    public bool ExamUnlock_LockedWhenLessonNotViewed()
    {
        // When lesson is not viewed, exam should be locked
        var lessonViewed = false;
        var isUnlocked = _progressionService.IsExamUnlocked(lessonViewed);

        return !isUnlocked;
    }

    [Property(MaxTest = 100)]
    public bool ExamUnlock_UnlockedWhenLessonViewed()
    {
        // When lesson is viewed, exam should be unlocked
        var lessonViewed = true;
        var isUnlocked = _progressionService.IsExamUnlocked(lessonViewed);

        return isUnlocked;
    }
}
