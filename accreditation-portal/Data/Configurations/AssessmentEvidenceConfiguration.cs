using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class AssessmentEvidenceConfiguration : IEntityTypeConfiguration<AssessmentEvidence>
    {
        public void Configure(EntityTypeBuilder<AssessmentEvidence> builder)
        {
            builder.HasOne(e => e.AssessmentFinding)
                .WithMany(f => f.Evidence)
                .HasForeignKey(e => e.AssessmentFindingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.UploadedByUser)
                .WithMany()
                .HasForeignKey(e => e.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
