using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class AssessmentFindingConfiguration : IEntityTypeConfiguration<AssessmentFinding>
    {
        public void Configure(EntityTypeBuilder<AssessmentFinding> builder)
        {
            builder.Property(f => f.Strengths).HasMaxLength(2000);
            builder.Property(f => f.Weaknesses).HasMaxLength(2000);
            builder.Property(f => f.Findings).HasMaxLength(2000);

            builder.HasOne(f => f.AssessmentAssignment)
                .WithMany(a => a.Findings)
                .HasForeignKey(f => f.AssessmentAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict, not Cascade - same reasoning as SelfAssessmentResponse/DeskReviewItemComment: a
            // checklist item that already has assessment findings shouldn't be hard-deletable later.
            builder.HasOne(f => f.ChecklistItem)
                .WithMany()
                .HasForeignKey(f => f.ChecklistItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.SubmittedByUser)
                .WithMany()
                .HasForeignKey(f => f.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(f => new { f.AssessmentAssignmentId, f.ChecklistItemId }).IsUnique();
        }
    }
}
