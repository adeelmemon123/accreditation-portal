using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class QABProfileConfiguration : IEntityTypeConfiguration<QABProfile>
    {
        public void Configure(EntityTypeBuilder<QABProfile> builder)
        {
            builder.HasKey(p => p.ApplicationId);

            builder.HasOne(p => p.Application)
                .WithOne(a => a.QABProfile)
                .HasForeignKey<QABProfile>(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
