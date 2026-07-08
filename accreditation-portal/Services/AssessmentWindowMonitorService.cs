using accreditation_portal.Data;
using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;

namespace accreditation_portal.Services
{
    // Visibility/dashboard only - flips AssessmentAssignment.Status to WindowClosed once WindowEndAt has
    // passed, and logs it, so the Admin queue can surface the "closed without submission" exception state.
    // The actual write-blocking enforcement lives in AssessmentService.EnsureWindowOpenForEditing, which
    // re-checks DateTime.UtcNow against WindowEndAt directly on every write - it never depends on this
    // job having already run, so a slow poll interval can't create a security gap, only a dashboard lag.
    public class AssessmentWindowMonitorService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AssessmentWindowMonitorService> _logger;

        public AssessmentWindowMonitorService(IServiceScopeFactory scopeFactory, ILogger<AssessmentWindowMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CloseExpiredWindowsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while closing expired assessment windows.");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Shutting down.
                }
            }
        }

        private async Task CloseExpiredWindowsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logService = scope.ServiceProvider.GetRequiredService<IApplicationLogService>();

            var now = DateTime.UtcNow;
            var expired = await context.AssessmentAssignments
                .Where(a => a.Status == AssessmentAssignmentStatus.WindowOpen && a.WindowEndAt < now)
                .ToListAsync(cancellationToken);

            if (expired.Count == 0)
            {
                return;
            }

            foreach (var assignment in expired)
            {
                assignment.Status = AssessmentAssignmentStatus.WindowClosed;

                await logService.LogAsync(
                    assignment.ApplicationId,
                    null,
                    ApplicationLogAction.AssessmentWindowClosed,
                    "Assessment window closed automatically (3-day limit reached) without a submission.",
                    null,
                    cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Closed {Count} expired assessment window(s).", expired.Count);
        }
    }
}
