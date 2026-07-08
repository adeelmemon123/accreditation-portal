using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class DeskReviewItemCommentConfiguration : IEntityTypeConfiguration<DeskReviewItemComment>
    {
        public void Configure(EntityTypeBuilder<DeskReviewItemComment> builder)
        {
            builder.Property(c => c.Comment).HasMaxLength(2000);

            builder.HasOne(c => c.DeskReview)
                .WithMany(r => r.ItemComments)
                .HasForeignKey(c => c.DeskReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict, not Cascade - a checklist item that already has reviewer notes shouldn't be
            // hard-deletable by a future template editor (same reasoning as SelfAssessmentResponse).
            builder.HasOne(c => c.ChecklistItem)
                .WithMany()
                .HasForeignKey(c => c.ChecklistItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // One note per item per review - the reviewer edits a single note/flag in place rather than
            // building a running thread; cheap to relax later if a history of notes is ever needed.
            builder.HasIndex(c => new { c.DeskReviewId, c.ChecklistItemId }).IsUnique();
        }
    }
}
