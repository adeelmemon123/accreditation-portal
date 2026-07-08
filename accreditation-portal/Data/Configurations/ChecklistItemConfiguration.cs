using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
    {
        public void Configure(EntityTypeBuilder<ChecklistItem> builder)
        {
            builder.Property(i => i.Title).HasMaxLength(300);
            builder.Property(i => i.Description).HasMaxLength(1000);

            builder.HasOne(i => i.ChecklistSection)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.ChecklistSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
