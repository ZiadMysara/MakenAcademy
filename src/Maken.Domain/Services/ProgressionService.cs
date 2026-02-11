namespace Maken.Domain.Services;

/// <summary>
/// Domain service for determining lesson, course, and exam unlock status based on progression rules.
/// Implements Constitution §10.3 - Business rules in Domain layer.
/// </summary>
public class ProgressionService
{
    /// <summary>
    /// Determines if a lesson is unlocked for a student.
    /// Rules:
    /// - First lesson (Order=1) is always unlocked for enrolled students
    /// - If Free Flow mode is enabled, all lessons are unlocked
    /// - Otherwise, lesson N+1 is unlocked only if lesson N is completed AND its exam (if present) is passed
    /// </summary>
    /// <param name="lessonOrder">The order of the lesson to check (1-based).</param>
    /// <param name="freeFlowMode">Whether the course has Free Flow mode enabled.</param>
    /// <param name="previousLessonCompleted">Whether the previous lesson is completed.</param>
    /// <param name="previousLessonExamPassed">Whether the previous lesson's exam is passed (null if no exam).</param>
    /// <returns>True if the lesson is unlocked, false otherwise.</returns>
    public bool IsLessonUnlocked(
        int lessonOrder,
        bool freeFlowMode,
        bool previousLessonCompleted,
        bool? previousLessonExamPassed)
    {
        // First lesson is always unlocked
        if (lessonOrder == 1)
            return true;

        // Free Flow mode bypasses all progression rules
        if (freeFlowMode)
            return true;

        // For subsequent lessons, previous lesson must be completed
        if (!previousLessonCompleted)
            return false;

        // If previous lesson has an exam, it must be passed
        if (previousLessonExamPassed.HasValue && !previousLessonExamPassed.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Determines if a course is unlocked for a student based on prerequisite completion.
    /// Rules:
    /// - If course has no prerequisites, it's always unlocked
    /// - All prerequisite courses must be completed (all lessons and exams passed)
    /// </summary>
    /// <param name="hasPrerequisites">Whether the course has prerequisite courses.</param>
    /// <param name="allPrerequisitesCompleted">Whether all prerequisite courses are completed.</param>
    /// <returns>True if the course is unlocked, false otherwise.</returns>
    public bool IsCourseUnlocked(bool hasPrerequisites, bool allPrerequisitesCompleted)
    {
        if (!hasPrerequisites)
            return true;

        return allPrerequisitesCompleted;
    }

    /// <summary>
    /// Determines if an exam is unlocked for a student.
    /// Rules:
    /// - Exam is unlocked only after the lesson content is viewed (progress record exists)
    /// </summary>
    /// <param name="lessonViewed">Whether the lesson has been viewed (progress record exists).</param>
    /// <returns>True if the exam is unlocked, false otherwise.</returns>
    public bool IsExamUnlocked(bool lessonViewed)
    {
        return lessonViewed;
    }

    /// <summary>
    /// Calculates the completion percentage for a course.
    /// Completion = (completed lessons with passed exams) / (total lessons) * 100
    /// </summary>
    /// <param name="totalLessons">Total number of lessons in the course.</param>
    /// <param name="completedLessons">Number of lessons completed with exams passed.</param>
    /// <returns>Completion percentage (0-100).</returns>
    public decimal CalculateCourseCompletion(int totalLessons, int completedLessons)
    {
        if (totalLessons == 0)
            return 0;

        return Math.Round((decimal)completedLessons / totalLessons * 100, 2);
    }
}
