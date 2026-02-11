using MediatR;

namespace Maken.Application.Commands.Exams;

/// <summary>
/// Command to delete an exam.
/// </summary>
public sealed record DeleteExamCommand(Guid Id) : IRequest<Unit>;
