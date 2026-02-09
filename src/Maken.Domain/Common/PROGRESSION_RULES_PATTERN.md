# Progression Rules Pattern

**For AI Developers: Claude Code, GLM4.7, AntiGravity**

This document explains the progression rule enforcement pattern used in the Maken platform.

---

## Constitution Requirements

From `.specify/memory/constitution.md` §5:

> **Progression rules MUST be enforced by the backend.** Students cannot skip lessons, bypass exams, or access locked content regardless of frontend manipulation.

### Core Rules

1. **Lesson unlock** requires:
   - Prior lesson completion
   - Exam passing (if exam exists)

2. **Course unlock** requires:
   - All prerequisite courses completed and passed

3. **Level unlock** requires:
   - All courses in prior level completed and passed

4. **Free Flow mode**:
   - Per-course configuration option
   - Relaxes progression rules for that course only

5. **Exam attempts**:
   - Unlimited attempts allowed

---

## Architecture Pattern

### 1. Interface Definition

The `IProgressionRule` interface defines the contract for all progression rules:

```csharp
public interface IProgressionRule
{
    Task<bool> CanAccessAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default);
    Task<string> GetLockReasonAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken = default);
}
```

### 2. Implementation (Future Features)

When implementing Course/Lesson features, create concrete implementations:

```csharp
// Example: LessonProgressionRule.cs (to be created in Course/Lesson feature)
public class LessonProgressionRule : IProgressionRule
{
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IRepository<UserProgress> _progressRepository;
    private readonly IRepository<Course> _courseRepository;

    public LessonProgressionRule(
        IRepository<Lesson> lessonRepository,
        IRepository<UserProgress> progressRepository,
        IRepository<Course> courseRepository)
    {
        _lessonRepository = lessonRepository;
        _progressRepository = progressRepository;
        _courseRepository = courseRepository;
    }

    public async Task<bool> CanAccessAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson == null) return false;

        // Check if course is in Free Flow mode
        var course = await _courseRepository.GetByIdAsync(lesson.CourseId, cancellationToken);
        if (course.IsFreeFlow) return true;

        // Check if this is the first lesson in the course
        if (lesson.Order == 1) return true;

        // Check if prior lesson is completed
        var priorLesson = await _lessonRepository.GetPriorLessonAsync(lessonId, cancellationToken);
        if (priorLesson == null) return true;

        var priorProgress = await _progressRepository.GetProgressAsync(userId, priorLesson.Id, cancellationToken);
        if (priorProgress == null || !priorProgress.IsCompleted) return false;

        // Check if prior lesson had an exam that needs to be passed
        if (priorLesson.HasExam)
        {
            if (!priorProgress.ExamPassed) return false;
        }

        return true;
    }

    public async Task<string> GetLockReasonAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        var priorLesson = await _lessonRepository.GetPriorLessonAsync(lessonId, cancellationToken);

        if (priorLesson == null) return "Unknown lock reason";

        var priorProgress = await _progressRepository.GetProgressAsync(userId, priorLesson.Id, cancellationToken);

        if (priorProgress == null || !priorProgress.IsCompleted)
        {
            return $"Complete '{priorLesson.Title}' before accessing this lesson";
        }

        if (priorLesson.HasExam && !priorProgress.ExamPassed)
        {
            return $"Pass the exam for '{priorLesson.Title}' before accessing this lesson";
        }

        return "Unknown lock reason";
    }
}
```

### 3. Application Layer Usage

Use progression rules in MediatR handlers or use cases:

```csharp
// Example: GetLessonContentQueryHandler.cs
public class GetLessonContentQueryHandler : IRequestHandler<GetLessonContentQuery, LessonContentDto>
{
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IProgressionRule _progressionRule;
    private readonly ITenantContext _tenantContext;

    public GetLessonContentQueryHandler(
        IRepository<Lesson> lessonRepository,
        IProgressionRule progressionRule,
        ITenantContext tenantContext)
    {
        _lessonRepository = lessonRepository;
        _progressionRule = progressionRule;
        _tenantContext = tenantContext;
    }

    public async Task<LessonContentDto> Handle(GetLessonContentQuery request, CancellationToken cancellationToken)
    {
        var userId = _tenantContext.UserId; // Get from authenticated context

        // CRITICAL: Check progression rule BEFORE returning content
        var canAccess = await _progressionRule.CanAccessAsync(userId, request.LessonId, cancellationToken);
        if (!canAccess)
        {
            var reason = await _progressionRule.GetLockReasonAsync(userId, request.LessonId, cancellationToken);
            throw new AccessDeniedException(reason);
        }

        // Access granted, return content
        var lesson = await _lessonRepository.GetByIdAsync(request.LessonId, cancellationToken);
        return MapToDto(lesson);
    }
}
```

### 4. API Layer (Controllers)

Controllers should NOT contain progression logic. They delegate to Application layer:

```csharp
// Example: LessonsController.cs
[ApiController]
[Route("api/lessons")]
[Authorize] // Require authentication
public class LessonsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LessonsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}/content")]
    public async Task<IActionResult> GetContent(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetLessonContentQuery { LessonId = id };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (AccessDeniedException ex)
        {
            // Return 403 Forbidden with lock reason
            return StatusCode(403, new { error = ex.Message });
        }
    }
}
```

---

## Testing Pattern

### Unit Tests

Test progression rules in isolation:

```csharp
public class LessonProgressionRuleTests
{
    [Fact]
    public async Task CanAccessAsync_FirstLesson_ShouldReturnTrue()
    {
        // Arrange
        var rule = new LessonProgressionRule(...);
        var userId = Guid.NewGuid();
        var firstLessonId = Guid.NewGuid();

        // Act
        var canAccess = await rule.CanAccessAsync(userId, firstLessonId);

        // Assert
        Assert.True(canAccess);
    }

    [Fact]
    public async Task CanAccessAsync_SecondLesson_PriorNotCompleted_ShouldReturnFalse()
    {
        // Arrange
        var rule = new LessonProgressionRule(...);
        var userId = Guid.NewGuid();
        var secondLessonId = Guid.NewGuid();

        // Act
        var canAccess = await rule.CanAccessAsync(userId, secondLessonId);

        // Assert
        Assert.False(canAccess);
    }
}
```

### Integration Tests

Test progression enforcement end-to-end:

```csharp
public class LessonAccessIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetLessonContent_LockedLesson_ShouldReturn403()
    {
        // Arrange
        var client = _factory.CreateClient();
        var lockedLessonId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/lessons/{lockedLessonId}/content");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Complete", content); // Lock reason message
    }
}
```

---

## AI Developer Checklist

When implementing features that involve progression rules:

- [ ] **NEVER** implement progression logic in the frontend
- [ ] **ALWAYS** enforce progression rules in the Application layer
- [ ] **ALWAYS** check `CanAccessAsync` before returning protected content
- [ ] **ALWAYS** return meaningful lock reasons via `GetLockReasonAsync`
- [ ] **ALWAYS** respect Free Flow mode configuration
- [ ] **NEVER** limit exam attempts (Constitution requires unlimited)
- [ ] **ALWAYS** write unit tests for progression rule logic
- [ ] **ALWAYS** write integration tests for end-to-end enforcement

---

## Common Mistakes to Avoid

### ❌ DON'T: Frontend-only checks

```typescript
// BAD: Frontend can be bypassed
if (lesson.isLocked) {
  showLockedMessage();
  return;
}
displayLessonContent(lesson);
```

### ✅ DO: Backend enforcement

```csharp
// GOOD: Backend enforces access control
var canAccess = await _progressionRule.CanAccessAsync(userId, lessonId);
if (!canAccess) throw new AccessDeniedException(...);
```

### ❌ DON'T: Hardcode progression logic in controllers

```csharp
// BAD: Business logic in controller
[HttpGet("{id}")]
public async Task<IActionResult> GetLesson(Guid id)
{
    var lesson = await _repository.GetByIdAsync(id);
    if (lesson.Order > 1) // Hardcoded logic
    {
        return Forbid();
    }
    return Ok(lesson);
}
```

### ✅ DO: Delegate to Application layer

```csharp
// GOOD: Controller delegates to Application layer
[HttpGet("{id}")]
public async Task<IActionResult> GetLesson(Guid id)
{
    var query = new GetLessonQuery { LessonId = id };
    var result = await _mediator.Send(query); // Progression checked in handler
    return Ok(result);
}
```

---

## References

- Constitution: `.specify/memory/constitution.md` §5
- Interface: `src/Maken.Domain/Common/IProgressionRule.cs`
- Validation Behavior: `src/Maken.Application/Common/Behaviors/ValidationBehavior.cs`
- Spec: `specs/001-global-rules/spec.md` (User Story 3)

---

**Last Updated**: 2026-02-09  
**Status**: Pattern established, awaiting Course/Lesson feature implementation
