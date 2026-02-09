namespace Maken.Domain.Common;

/// <summary>
/// Defines a progression rule that controls access to educational content.
/// This is a placeholder interface for future Course/Lesson progression logic.
/// </summary>
/// <remarks>
/// Constitution requirement (§5): Progression rules must be enforced by the backend.
/// Students cannot skip lessons, bypass exams, or access locked content.
/// 
/// <para><strong>Progression Rules (from Constitution):</strong></para>
/// <list type="bullet">
///   <item>Lesson unlock requires: (a) prior lesson completion, (b) exam passing (if exam exists)</item>
///   <item>Course unlock requires all prerequisite courses completed and passed</item>
///   <item>Level unlock requires all courses in prior level completed and passed</item>
///   <item>Free Flow mode is a per-course configuration option that relaxes progression</item>
///   <item>Exam attempts are unlimited</item>
/// </list>
/// 
/// <para><strong>Implementation Pattern:</strong></para>
/// <code>
/// // Example: LessonProgressionRule (to be implemented in Course/Lesson feature)
/// public class LessonProgressionRule : IProgressionRule
/// {
///     public async Task&lt;bool&gt; CanAccessAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken)
///     {
///         // Check if prior lesson is completed
///         // Check if exam is passed (if required)
///         // Check if course is in Free Flow mode
///         return true; // or false
///     }
/// 
///     public async Task&lt;string&gt; GetLockReasonAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken)
///     {
///         return "Complete Lesson 1 before accessing Lesson 2";
///     }
/// }
/// </code>
/// 
/// <para><strong>Usage in Application Layer:</strong></para>
/// <code>
/// // In a MediatR handler or use case
/// var canAccess = await _progressionRule.CanAccessAsync(userId, lessonId, cancellationToken);
/// if (!canAccess)
/// {
///     var reason = await _progressionRule.GetLockReasonAsync(userId, lessonId, cancellationToken);
///     throw new AccessDeniedException(reason);
/// }
/// </code>
/// </remarks>
public interface IProgressionRule
{
    /// <summary>
    /// Determines whether a user can access a specific resource (lesson, course, level).
    /// </summary>
    /// <param name="userId">The ID of the user requesting access.</param>
    /// <param name="resourceId">The ID of the resource being accessed (lesson, course, or level).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if access is allowed; otherwise, false.</returns>
    Task<bool> CanAccessAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a human-readable reason why access is denied.
    /// Should only be called when CanAccessAsync returns false.
    /// </summary>
    /// <param name="userId">The ID of the user requesting access.</param>
    /// <param name="resourceId">The ID of the resource being accessed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A message explaining why access is locked (e.g., "Complete Lesson 1 first").</returns>
    Task<string> GetLockReasonAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default);
}
