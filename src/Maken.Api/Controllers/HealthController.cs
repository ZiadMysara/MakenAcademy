using System.Diagnostics;
using Maken.Api.Models;
using Maken.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maken.Api.Controllers;

/// <summary>
/// Health check endpoints.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly MakenDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(MakenDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Returns service health status.
    /// </summary>
    /// <returns>Health status with individual checks</returns>
    /// <response code="200">Service is healthy</response>
    /// <response code="503">Service is unhealthy</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        var checks = new List<HealthCheck>();
        var overallHealthy = true;

        // Database health check
        var dbCheck = await CheckDatabaseHealthAsync();
        checks.Add(dbCheck);
        if (dbCheck.Status != "healthy")
        {
            overallHealthy = false;
        }

        var response = new HealthResponse
        {
            Status = overallHealthy ? "healthy" : "unhealthy",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow,
            Checks = checks
        };

        return overallHealthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    private async Task<HealthCheck> CheckDatabaseHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Simple query to check database connectivity
            await _dbContext.Database.CanConnectAsync();
            stopwatch.Stop();

            return new HealthCheck
            {
                Name = "database",
                Status = "healthy",
                Duration = $"{stopwatch.ElapsedMilliseconds}ms"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database health check failed");

            return new HealthCheck
            {
                Name = "database",
                Status = "unhealthy",
                Duration = $"{stopwatch.ElapsedMilliseconds}ms"
            };
        }
    }
}
