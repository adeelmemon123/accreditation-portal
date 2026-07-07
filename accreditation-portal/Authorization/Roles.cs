namespace accreditation_portal.Authorization
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string ProvincialTEVTA = "ProvincialTEVTA";
        public const string SectorExpert = "SectorExpert";
        public const string TAQECChairperson = "TAQECChairperson";
        public const string NACChairman = "NACChairman";
        public const string Institute = "Institute";
        public const string QAB = "QAB";

        public static readonly string[] All =
        {
            Admin,
            ProvincialTEVTA,
            SectorExpert,
            TAQECChairperson,
            NACChairman,
            Institute,
            QAB
        };

        public static readonly string[] SelfRegisterable =
        {
            Institute,
            QAB
        };

        public static readonly string[] InternallyProvisioned =
        {
            Admin,
            ProvincialTEVTA,
            SectorExpert,
            TAQECChairperson,
            NACChairman
        };
    }
}
