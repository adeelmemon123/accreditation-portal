using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace accreditation_portal.Data.Configurations
{
    public class ChecklistTemplateConfiguration : IEntityTypeConfiguration<ChecklistTemplate>
    {
        public void Configure(EntityTypeBuilder<ChecklistTemplate> builder)
        {
            builder.Property(t => t.ApplicationType).HasConversion<string>().HasMaxLength(20);
            builder.Property(t => t.Name).HasMaxLength(200);

            // The active template for a given ApplicationType is resolved at runtime (IsActive=true) -
            // this partial-style index just speeds that lookup, it does not enforce single-active-per-type
            // since SQL Server has no partial unique index without a filtered index, which we skip for now.
            builder.HasIndex(t => new { t.ApplicationType, t.IsActive });
        }
    }
}
