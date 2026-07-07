using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
    {
        public void Configure(EntityTypeBuilder<Application> builder)
        {
            builder.Property(a => a.ApplicationType).HasConversion<string>().HasMaxLength(20);
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            // Restrict (not Cascade) because ApplicationLog also FKs to AspNetUsers - letting both cascade
            // from the user would create multiple cascade paths, which SQL Server rejects.
            builder.HasOne(a => a.ApplicantUser)
                .WithMany()
                .HasForeignKey(a => a.ApplicantUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new { a.ApplicantUserId, a.Status });
        }
    }
}
