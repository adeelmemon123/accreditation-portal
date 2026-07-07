namespace accreditation_portal.Models.AdminViewModels
{
    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Province { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
