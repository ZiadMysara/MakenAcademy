using MediatR;

namespace Maken.Application.Queries.Tenants;

/// <summary>
/// Query to resolve a tenant by subdomain.
/// </summary>
public sealed record ResolveTenantQuery(
    string Subdomain
) : IRequest<TenantResult?>;

/// <summary>
/// Result of a tenant resolution containing tenant information.
/// </summary>
public sealed record TenantResult(
    Guid Id,
    string Name,
    string Subdomain,
    bool IsActive
);
