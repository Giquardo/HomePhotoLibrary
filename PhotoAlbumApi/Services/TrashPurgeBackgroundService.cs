namespace PhotoAlbumApi.Services;

// Runs once at startup, then every 24h (mirrors scripts/backup.sh's own
// "run once immediately, then nightly" cadence). Thin on purpose: all the
// actual purge logic lives in ITrashCleanupService so it can be unit tested
// without dealing with timer/cancellation timing.
public class TrashPurgeBackgroundService : BackgroundService
{
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoggingService _loggingService;

    public TrashPurgeBackgroundService(IServiceScopeFactory scopeFactory, ILoggingService loggingService)
    {
        _scopeFactory = scopeFactory;
        _loggingService = loggingService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PurgeInterval);

        do
        {
            await RunOnceAsync();
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task RunOnceAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var trashCleanupService = scope.ServiceProvider.GetRequiredService<ITrashCleanupService>();
            await trashCleanupService.PurgeExpiredTrashAsync(DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            // A failed pass (e.g. a transient DB blip) must not kill the loop -
            // there's always another chance tomorrow.
            _loggingService.LogError(ex, "Trash purge pass failed; will retry on the next interval.");
        }
    }
}
