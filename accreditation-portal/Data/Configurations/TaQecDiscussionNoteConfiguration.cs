using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class TaQecDiscussionNoteConfiguration : IEntityTypeConfiguration<TaQecDiscussionNote>
    {
        public void Configure(EntityTypeBuilder<TaQecDiscussionNote> builder)
        {
            builder.Property(n => n.Note).HasMaxLength(2000);

            builder.HasOne(n => n.TaQecReview)
                .WithMany(r => r.DiscussionNotes)
                .HasForeignKey(n => n.TaQecReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.AuthorUser)
                .WithMany()
                .HasForeignKey(n => n.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict, not Cascade - same reasoning as other ChecklistItem references. No uniqueness
            // constraint here - this is an append-only discussion log, not an upsert-per-item note.
            builder.HasOne(n => n.ChecklistItem)
                .WithMany()
                .HasForeignKey(n => n.ChecklistItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
