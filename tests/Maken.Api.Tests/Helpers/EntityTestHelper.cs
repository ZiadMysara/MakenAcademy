using System.Reflection;
using Maken.Domain.Common;

namespace Maken.Api.Tests.Helpers;

/// <summary>
/// Helper class for setting entity properties in tests using reflection.
/// </summary>
public static class EntityTestHelper
{
    /// <summary>
    /// Sets the Id property of an entity using reflection.
    /// This is only for testing purposes where we need to control entity IDs.
    /// </summary>
    public static void SetId<TEntity>(TEntity entity, Guid id) where TEntity : BaseEntity
    {
        var property = typeof(BaseEntity).GetProperty("Id");
        if (property == null)
        {
            throw new InvalidOperationException("Id property not found on BaseEntity");
        }

        // Use reflection to set the private setter
        property.SetValue(entity, id);
    }
}
