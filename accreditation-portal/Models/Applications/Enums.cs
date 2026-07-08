namespace accreditation_portal.Models.Applications
{
    public enum ApplicationType
    {
        Institute,
        QAB
    }

    // Deficient is terminal. AssessmentSubmitted now leads into TA-QEC review; what happens after
    // TaQecGraded (NAC approval/certificate) is a later module, not built here.
    public enum ApplicationStatus
    {
        Draft,
        Submitted,
        SelfAssessmentInProgress,
        SelfAssessmentSubmitted,
        UnderDeskReview,
        WorthyForVisit,
        Deficient,
        AssessmentAssigned,
        AssessmentInProgress,
        AssessmentSubmitted,
        UnderTaQecReview,
        TaQecGraded
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

    // NotStarted: team assigned, window not yet opened. WindowClosed is set by the background monitor
    // when WindowEndAt lapses without a submission - it is a dashboard/visibility flag only; the actual
    // write-blocking enforcement always re-checks DateTime.UtcNow against WindowEndAt directly (see
    // AssessmentService), never relies on this Status field, so it can't go stale mid-request.
    public enum AssessmentAssignmentStatus
    {
        NotStarted,
        WindowOpen,
        WindowClosed,
        FindingsSubmitted
    }

    public enum TaQecGrade
    {
        Pending,
        A,
        B,
        C,
        D,
        NotRejected
    }

    // Extensible: later modules (NAC) add their own action values here.
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
        DeskReviewDecisionMade,
        AssessmentAssigned,
        AssessmentWindowOpened,
        FindingRecorded,
        AssessmentEvidenceUploaded,
        AssessmentEvidenceDeleted,
        AssessmentWindowClosed,
        AssessmentSubmitted,
        TaQecReviewStarted,
        TaQecDiscussionNoteAdded,
        TaQecGradeLocked
    }
}
