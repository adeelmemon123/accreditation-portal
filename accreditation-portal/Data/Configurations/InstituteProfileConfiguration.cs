using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class InstituteProfileConfiguration : IEntityTypeConfiguration<InstituteProfile>
    {
        public void Configure(EntityTypeBuilder<InstituteProfile> builder)
        {
            builder.HasKey(p => p.ApplicationId);

            builder.HasOne(p => p.Application)
                .WithOne(a => a.InstituteProfile)
                .HasForeignKey<InstituteProfile>(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
