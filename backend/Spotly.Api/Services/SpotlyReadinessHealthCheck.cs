using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Spotly.Infrastructure.Persistence;

namespace Spotly.Api.Services;

public sealed class SpotlyReadinessHealthCheck(IDbContextFactory<SpotlyDbContext> factory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var canRead = await db.Restaurants.AnyAsync(cancellationToken);
        return canRead
            ? HealthCheckResult.Healthy("Spotly dependencies ready.")
            : HealthCheckResult.Unhealthy("Restaurant seed data unavailable.");
    }
}
