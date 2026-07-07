using accreditation_portal.Models.Applications;

namespace accreditation_portal.Services
{
    public interface IApplicationLogService
    {
        Task LogAsync(
            int applicationId,
            string? userId,
            ApplicationLogAction action,
            string description,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);
    }
}
