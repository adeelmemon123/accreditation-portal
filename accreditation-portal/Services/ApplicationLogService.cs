using accreditation_portal.Data;
using accreditation_portal.Models.Applications;

namespace accreditation_portal.Services
{
    public class ApplicationLogService : IApplicationLogService
    {
        private readonly ApplicationDbContext _context;

        public ApplicationLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            int applicationId,
            string? userId,
            ApplicationLogAction action,
            string description,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            _context.ApplicationLogs.Add(new ApplicationLog
            {
                ApplicationId = applicationId,
                UserId = userId,
                Action = action,
                Description = description,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
