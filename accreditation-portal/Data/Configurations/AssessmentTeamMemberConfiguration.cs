using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class AssessmentTeamMemberConfiguration : IEntityTypeConfiguration<AssessmentTeamMember>
    {
        public void Configure(EntityTypeBuilder<AssessmentTeamMember> builder)
        {
            builder.HasOne(m => m.AssessmentAssignment)
                .WithMany(a => a.TeamMembers)
                .HasForeignKey(m => m.AssessmentAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.AssessorUser)
                .WithMany()
                .HasForeignKey(m => m.AssessorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => new { m.AssessmentAssignmentId, m.AssessorUserId }).IsUnique();
        }
    }
}
