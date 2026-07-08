using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class DeskReviewConfiguration : IEntityTypeConfiguration<DeskReview>
    {
        public void Configure(EntityTypeBuilder<DeskReview> builder)
        {
            builder.Property(r => r.Decision).HasConversion<string>().HasMaxLength(20);
            builder.Property(r => r.OverallComments).HasMaxLength(4000);

            builder.HasOne(r => r.Application)
                .WithOne(a => a.DeskReview)
                .HasForeignKey<DeskReview>(r => r.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => r.ApplicationId).IsUnique();

            // Restrict, not Cascade - same reasoning as Application.ApplicantUser and ApplicationLog.User:
            // deleting an Admin account shouldn't cascade-delete review audit history.
            builder.HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
