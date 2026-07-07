using accreditation_portal.Models;
using accreditation_portal.Models.Applications;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace accreditation_portal.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Application> Applications => Set<Application>();
        public DbSet<InstituteProfile> InstituteProfiles => Set<InstituteProfile>();
        public DbSet<QABProfile> QABProfiles => Set<QABProfile>();
        public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
        public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
