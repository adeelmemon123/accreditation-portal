using Microsoft.AspNetCore.Authorization;

namespace accreditation_portal.Authorization
{
    // Marker requirement for the "RequireTaQecChairperson" policy - see TaQecChairpersonHandler for the
    // actual check (TAQEC role + IsChairperson, read live from the DB rather than a cached claim).
    public class TaQecChairpersonRequirement : IAuthorizationRequirement
    {
    }
}
