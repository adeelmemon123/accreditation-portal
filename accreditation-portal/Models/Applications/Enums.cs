namespace accreditation_portal.Models.Applications
{
    public enum ApplicationType
    {
        Institute,
        QAB
    }

    // UnderDeskReview, WorthyForVisit, and Deficient are placeholders for the Desk Review module -
    // no logic in this module transitions an application into those states.
    public enum ApplicationStatus
    {
        Draft,
        Submitted,
        UnderDeskReview,
        WorthyForVisit,
        Deficient
    }

    public enum ApplicationDocumentType
    {
        RegistrationCertificate,
        AffiliationCertificate,
        FeeChallan
    }

    // Extensible: later modules (Desk Review, Assessor, TA-QEC, NAC) add their own action values here.
    public enum ApplicationLogAction
    {
        Created,
        ProfileUpdated,
        DocumentUploaded,
        DocumentReplaced,
        DocumentDeleted,
        Submitted
    }
}
