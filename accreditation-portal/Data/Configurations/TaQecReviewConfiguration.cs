using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class TaQecReviewConfiguration : IEntityTypeConfiguration<TaQecReview>
    {
        public void Configure(EntityTypeBuilder<TaQecReview> builder)
        {
            builder.Property(r => r.Grade).HasConversion<string>().HasMaxLength(20);
            builder.Property(r => r.RationaleRemarks).HasMaxLength(4000);

            builder.HasOne(r => r.Application)
                .WithOne(a => a.TaQecReview)
                .HasForeignKey<TaQecReview>(r => r.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => r.ApplicationId).IsUnique();

            // Restrict, not Cascade - same reasoning as DeskReview.Reviewer/AssessmentAssignment.Convener.
            builder.HasOne(r => r.LockedByUser)
                .WithMany()
                .HasForeignKey(r => r.LockedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
