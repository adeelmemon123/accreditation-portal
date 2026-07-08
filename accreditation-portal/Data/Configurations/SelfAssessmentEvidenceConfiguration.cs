using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class SelfAssessmentEvidenceConfiguration : IEntityTypeConfiguration<SelfAssessmentEvidence>
    {
        public void Configure(EntityTypeBuilder<SelfAssessmentEvidence> builder)
        {
            builder.HasOne(e => e.SelfAssessmentResponse)
                .WithMany(r => r.Evidence)
                .HasForeignKey(e => e.SelfAssessmentResponseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
