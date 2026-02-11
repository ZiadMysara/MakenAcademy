using Maken.Application.DTOs;
using MediatR;

namespace Maken.Application.Queries.Organizations;

/// <summary>
/// Query to get all active organizations for public display on the landing page.
/// No authentication required - returns only non-sensitive public data.
/// </summary>
public sealed record GetPublicOrganizationsQuery() : IRequest<List<PublicOrganizationDto>>;
