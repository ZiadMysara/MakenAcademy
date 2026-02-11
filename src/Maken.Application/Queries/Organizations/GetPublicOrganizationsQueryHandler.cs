using Maken.Application.Common.Interfaces;
using Maken.Application.DTOs;
using Maken.Domain.Entities;
using MediatR;

namespace Maken.Application.Queries.Organizations;

/// <summary>
/// Handler for GetPublicOrganizationsQuery that retrieves all active organizations
/// for public display on the landing page.
/// </summary>
public sealed class GetPublicOrganizationsQueryHandler 
    : IRequestHandler<GetPublicOrganizationsQuery, List<PublicOrganizationDto>>
{
    private readonly IRepository<Tenant> _tenantRepository;

    public GetPublicOrganizationsQueryHandler(IRepository<Tenant> tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<List<PublicOrganizationDto>> Handle(
        GetPublicOrganizationsQuery request, 
        CancellationToken cancellationToken)
    {
        // Get all active, non-deleted tenants
        // Note: IsDeleted filter is already applied by global query filter in DbContext
        var tenants = await _tenantRepository.GetAllAsync(
            filter: t => t.IsActive,
            cancellationToken: cancellationToken);

        // Map to DTOs and order by name
        var dtos = tenants
            .OrderBy(t => t.Name)
            .Select(t => new PublicOrganizationDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                LogoUrl = t.LogoUrl,
                PrimaryColor = t.PrimaryColor,
                SecondaryColor = t.SecondaryColor
            }).ToList();

        return dtos;
    }
}
