namespace accreditation_portal.Models.Applications
{
    public enum ApplicationType
    {
        Institute,
        QAB
    }

    // WorthyForVisit and Deficient are terminal for this module - what happens after WorthyForVisit
    // (On-Site Assessment/Assessor) is a later module, not built here.
    public enum ApplicationStatus
    {
        Draft,
        Submitted,
        SelfAssessmentInProgress,
        SelfAssessmentSubmitted,
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

    public enum DeskReviewDecision
    {
        Pending,
        WorthyForVisit,
        Deficient
    }

    // Extensible: later modules (Assessor, TA-QEC, NAC) add their own action values here.
    public enum ApplicationLogAction
    {
        Created,
        ProfileUpdated,
        DocumentUploaded,
        DocumentReplaced,
        DocumentDeleted,
        Submitted,
        SelfAssessmentStarted,
        ChecklistItemScored,
        EvidenceUploaded,
        EvidenceDeleted,
        SelfAssessmentSubmitted,
        EvidenceViewedByReviewer,
        DeskReviewStarted,
        ItemCommented,
        ItemFlagged,
        DeskReviewDecisionMade
    }
}
