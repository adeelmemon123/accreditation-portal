using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class SelfAssessmentResponseConfiguration : IEntityTypeConfiguration<SelfAssessmentResponse>
    {
        public void Configure(EntityTypeBuilder<SelfAssessmentResponse> builder)
        {
            builder.Property(r => r.Comments).HasMaxLength(2000);

            builder.HasOne(r => r.Application)
                .WithMany(a => a.SelfAssessmentResponses)
                .HasForeignKey(r => r.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict, not Cascade - a checklist item that already has applicant responses shouldn't be
            // hard-deletable by a future template editor (it would silently erase self-assessment history).
            builder.HasOne(r => r.ChecklistItem)
                .WithMany()
                .HasForeignKey(r => r.ChecklistItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => new { r.ApplicationId, r.ChecklistItemId }).IsUnique();
        }
    }
}
