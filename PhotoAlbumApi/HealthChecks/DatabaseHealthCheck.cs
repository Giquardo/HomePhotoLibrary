using Microsoft.Extensions.Diagnostics.HealthChecks;
using PhotoAlbumApi.Data;

namespace PhotoAlbumApi.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly PhotoAlbumContext _context;

    public DatabaseHealthCheck(PhotoAlbumContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Cannot connect to the database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check threw an exception.", ex);
        }
    }
}
