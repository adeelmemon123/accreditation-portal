using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class ApplicationLogConfiguration : IEntityTypeConfiguration<ApplicationLog>
    {
        public void Configure(EntityTypeBuilder<ApplicationLog> builder)
        {
            builder.Property(l => l.Action).HasConversion<string>().HasMaxLength(30);

            builder.HasOne(l => l.Application)
                .WithMany(a => a.Logs)
                .HasForeignKey(l => l.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
