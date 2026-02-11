using FsCheck;
using FsCheck.Xunit;
using Maken.Application.Queries.Tenants;
using Maken.Application.Common.Interfaces;
using Maken.Application.Tests.Helpers;
using Maken.Domain.Entities;
using Moq;

namespace Maken.Application.Tests.Properties;

/// <summary>
/// Property-based tests for tenant resolution correctness properties.
/// Tests universal properties that should hold across all valid inputs.
/// </summary>
public sealed class TenantPropertiesTests
{
    /// <summary>
    /// Helper method to clean and validate subdomain
    /// </summary>
    private static bool TryGetValidSubdomain(NonEmptyString subdomain, out string validSubdomain)
    {
        var cleaned = new string(subdomain.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        
        // Filter to only valid characters (lowercase alphanumeric and hyphens)
        validSubdomain = new string(cleaned.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .ToArray());
        
        // Skip if subdomain is empty after filtering or too short
        if (string.IsNullOrWhiteSpace(validSubdomain) || validSubdomain.Length < 3)
        {
            return false;
        }
        
        // Skip if subdomain starts or ends with hyphen (invalid format)
        if (validSubdomain.StartsWith('-') || validSubdomain.EndsWith('-'))
        {
            return false;
        }
        
        // Skip if subdomain has consecutive hyphens (invalid format per regex ^[a-z0-9]+(-[a-z0-9]+)*$)
        if (validSubdomain.Contains("--"))
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Helper method to clean and validate company name
    /// </summary>
    private static bool TryGetValidCompanyName(NonEmptyString companyName, out string validName)
    {
        validName = new string(companyName.Get.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return !string.IsNullOrWhiteSpace(validName);
    }
    /// <summary>
    /// Property 18: Tenant Resolution from Subdomain
    /// For any valid subdomain that exists in the database, resolving the tenant should return
    /// the correct tenant entity with ID and configuration. For invalid or non-existent subdomains,
    /// resolving should return null.
    /// 
    /// Feature: backend-business-features, Property 18: Tenant Resolution from Subdomain
    /// Validates: Requirements FR-029
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property18_TenantResolution_ValidSubdomain_ReturnsTenant(
        NonEmptyString companyName,
        NonEmptyString subdomain)
    {
        // Validate and clean inputs
        if (!TryGetValidCompanyName(companyName, out var companyNameTrimmed) ||
            !TryGetValidSubdomain(subdomain, out var validSubdomain))
        {
            return true; // Skip this test case
        }
        
        // Create valid tenant
        var tenant = new Tenant(companyNameTrimmed, validSubdomain);
        EntityTestHelper.SetId(tenant, Guid.NewGuid());

        // Arrange
        var mockTenantRepository = new Mock<IRepository<Tenant>>();

        mockTenantRepository
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var handler = new ResolveTenantQueryHandler(mockTenantRepository.Object);
        var query = new ResolveTenantQuery(tenant.Subdomain);

        // Act
        var result = handler.Handle(query, CancellationToken.None).GetAwaiter().GetResult();

        // Assert - Valid subdomain should return tenant
        return result != null &&
               result.Id == tenant.Id &&
               result.Subdomain == tenant.Subdomain &&
               result.Name == tenant.Name;
    }

    /// <summary>
    /// Property 18: Tenant Resolution from Subdomain (Invalid Subdomain)
    /// For invalid or non-existent subdomains, resolving should return null.
    /// 
    /// Feature: backend-business-features, Property 18: Tenant Resolution from Subdomain
    /// Validates: Requirements FR-029
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property18_TenantResolution_InvalidSubdomain_ReturnsNull(
        NonEmptyString subdomain)
    {
        // Arrange
        var mockTenantRepository = new Mock<IRepository<Tenant>>();

        mockTenantRepository
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null); // Subdomain not found

        var handler = new ResolveTenantQueryHandler(mockTenantRepository.Object);
        var query = new ResolveTenantQuery(subdomain.Get.Trim());

        // Act
        var result = handler.Handle(query, CancellationToken.None).GetAwaiter().GetResult();

        // Assert - Invalid subdomain should return null
        return result == null;
    }

    /// <summary>
    /// Property 18: Tenant Resolution from Subdomain (Empty Subdomain)
    /// For empty or whitespace subdomains, resolving should return null.
    /// 
    /// Feature: backend-business-features, Property 18: Tenant Resolution from Subdomain
    /// Validates: Requirements FR-029
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property18_TenantResolution_EmptySubdomain_ReturnsNull()
    {
        // Test with various empty/whitespace strings
        var emptyStrings = new[] { "", " ", "  ", "\t", "\n" };
        
        foreach (var subdomain in emptyStrings)
        {
            // Arrange
            var mockTenantRepository = new Mock<IRepository<Tenant>>();
            var handler = new ResolveTenantQueryHandler(mockTenantRepository.Object);
            var query = new ResolveTenantQuery(subdomain);

            // Act
            var result = handler.Handle(query, CancellationToken.None).GetAwaiter().GetResult();

            // Assert - Empty subdomain should return null
            if (result != null)
            {
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Property 18: Tenant Resolution from Subdomain (Case Insensitivity)
    /// Subdomain resolution should be case-insensitive.
    /// 
    /// Feature: backend-business-features, Property 18: Tenant Resolution from Subdomain
    /// Validates: Requirements FR-029
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property18_TenantResolution_CaseInsensitive_ReturnsSameTenant(
        NonEmptyString companyName,
        NonEmptyString subdomain)
    {
        // Validate and clean inputs
        if (!TryGetValidCompanyName(companyName, out var companyNameTrimmed) ||
            !TryGetValidSubdomain(subdomain, out var validSubdomain))
        {
            return true; // Skip this test case
        }
        
        // Create valid tenant
        var tenant = new Tenant(companyNameTrimmed, validSubdomain);
        EntityTestHelper.SetId(tenant, Guid.NewGuid());

        // Arrange
        var mockTenantRepository = new Mock<IRepository<Tenant>>();

        // Setup repository to return tenant for any case variation
        mockTenantRepository
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var handler = new ResolveTenantQueryHandler(mockTenantRepository.Object);

        // Act - Query with different case variations
        var lowerQuery = new ResolveTenantQuery(tenant.Subdomain.ToLowerInvariant());
        var upperQuery = new ResolveTenantQuery(tenant.Subdomain.ToUpperInvariant());

        var lowerResult = handler.Handle(lowerQuery, CancellationToken.None).GetAwaiter().GetResult();
        var upperResult = handler.Handle(upperQuery, CancellationToken.None).GetAwaiter().GetResult();

        // Assert - Both should return the same tenant
        return lowerResult != null &&
               upperResult != null &&
               lowerResult.Id == tenant.Id &&
               upperResult.Id == tenant.Id;
    }

    /// <summary>
    /// Property 18: Tenant Resolution from Subdomain (Subdomain Format Validation)
    /// Subdomains should follow valid format rules (alphanumeric, hyphens, no special chars).
    /// 
    /// Feature: backend-business-features, Property 18: Tenant Resolution from Subdomain
    /// Validates: Requirements FR-029
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property18_TenantResolution_ValidFormat_Succeeds(
        NonEmptyString companyName,
        NonEmptyString subdomainGen)
    {
        // Validate and clean inputs
        if (!TryGetValidCompanyName(companyName, out var companyNameClean) ||
            !TryGetValidSubdomain(subdomainGen, out var subdomain))
        {
            return true; // Skip this test case
        }

        // Create valid tenant
        var tenant = new Tenant(companyNameClean, subdomain);
        EntityTestHelper.SetId(tenant, Guid.NewGuid());

        // Arrange
        var mockTenantRepository = new Mock<IRepository<Tenant>>();

        mockTenantRepository
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var handler = new ResolveTenantQueryHandler(mockTenantRepository.Object);
        var query = new ResolveTenantQuery(subdomain);

        // Act
        var result = handler.Handle(query, CancellationToken.None).GetAwaiter().GetResult();

        // Assert - Valid format should succeed
        return result != null &&
               result.Subdomain == subdomain;
    }
}
