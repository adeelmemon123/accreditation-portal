using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class ApplicationDocumentConfiguration : IEntityTypeConfiguration<ApplicationDocument>
    {
        public void Configure(EntityTypeBuilder<ApplicationDocument> builder)
        {
            builder.Property(d => d.DocumentType).HasConversion<string>().HasMaxLength(30);

            builder.HasOne(d => d.Application)
                .WithMany(a => a.Documents)
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // "Replace" overwrites this row rather than appending a new one - ApplicationLog carries the
            // DocumentReplaced audit trail, so this table only ever holds the current document per type.
            builder.HasIndex(d => new { d.ApplicationId, d.DocumentType }).IsUnique();
        }
    }
}
