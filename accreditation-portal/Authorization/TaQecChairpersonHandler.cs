using accreditation_portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace accreditation_portal.Authorization
{
    // IsChairperson is a plain bool column, not an Identity claim, so it can't be checked via a cached
    // auth-cookie claim without going stale the moment Admin toggles it for an already-logged-in user -
    // this handler re-reads it from the DB on every check instead, same "live over cached" principle used
    // for the Assessment window enforcement.
    public class TaQecChairpersonHandler : AuthorizationHandler<TaQecChairpersonRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public TaQecChairpersonHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TaQecChairpersonRequirement requirement)
        {
            if (!context.User.IsInRole(Roles.TAQEC))
            {
                return;
            }

            var user = await _userManager.GetUserAsync(context.User);
            if (user is { IsChairperson: true })
            {
                context.Succeed(requirement);
            }
        }
    }
}
