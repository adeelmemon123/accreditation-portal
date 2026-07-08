namespace accreditation_portal.Models.AdminViewModels
{
    public class EditUserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<RoleSelection> Roles { get; set; } = new();

        // Only meaningful (and only shown in the UI) when the TAQEC role is currently held.
        public bool IsChairperson { get; set; }
    }

    public class RoleSelection
    {
        public string RoleName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
