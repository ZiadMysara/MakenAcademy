using Maken.Application.Common.Interfaces;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Tenants;

/// <summary>
/// Handler for ResolveTenantQuery that resolves a tenant by subdomain.
/// </summary>
public sealed class ResolveTenantQueryHandler : IRequestHandler<ResolveTenantQuery, TenantResult?>
{
    private readonly IRepository<Tenant> _tenantRepository;

    public ResolveTenantQueryHandler(IRepository<Tenant> tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantResult?> Handle(ResolveTenantQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Subdomain))
            return null;

        // Find tenant by subdomain (case-insensitive)
        var tenant = await _tenantRepository.GetFirstOrDefaultAsync(
            t => t.Subdomain == request.Subdomain.ToLowerInvariant(),
            cancellationToken);

        if (tenant == null)
            return null;

        return new TenantResult(
            Id: tenant.Id,
            Name: tenant.Name,
            Subdomain: tenant.Subdomain,
            IsActive: !tenant.IsDeleted
        );
    }
}
