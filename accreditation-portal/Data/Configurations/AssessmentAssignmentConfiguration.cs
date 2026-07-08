using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class AssessmentAssignmentConfiguration : IEntityTypeConfiguration<AssessmentAssignment>
    {
        public void Configure(EntityTypeBuilder<AssessmentAssignment> builder)
        {
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            builder.HasOne(a => a.Application)
                .WithOne(app => app.AssessmentAssignment)
                .HasForeignKey<AssessmentAssignment>(a => a.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => a.ApplicationId).IsUnique();

            // Restrict, not Cascade - same reasoning as DeskReview.Reviewer: deleting an Admin account
            // shouldn't cascade-delete assignment audit history.
            builder.HasOne(a => a.Convener)
                .WithMany()
                .HasForeignKey(a => a.ConvenerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
