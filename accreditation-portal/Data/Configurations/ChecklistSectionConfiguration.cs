using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class ChecklistSectionConfiguration : IEntityTypeConfiguration<ChecklistSection>
    {
        public void Configure(EntityTypeBuilder<ChecklistSection> builder)
        {
            builder.Property(s => s.Title).HasMaxLength(200);

            builder.HasOne(s => s.ChecklistTemplate)
                .WithMany(t => t.Sections)
                .HasForeignKey(s => s.ChecklistTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
