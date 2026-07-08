using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;

namespace accreditation_portal.Data
{
    // Seeds one active checklist template per ApplicationType so the Self-Assessment flow is testable
    // end-to-end. There is no admin editor UI yet (see README "follow-up" note) - when that's built, this
    // seeder's shape (Template -> Sections -> Items) is exactly what the editor should produce/consume, so
    // convert it into an editor rather than replacing it.
    public static class ChecklistTemplateSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (!await context.ChecklistTemplates.AnyAsync(t => t.ApplicationType == ApplicationType.Institute))
            {
                context.ChecklistTemplates.Add(BuildInstituteTemplate());
            }

            if (!await context.ChecklistTemplates.AnyAsync(t => t.ApplicationType == ApplicationType.QAB))
            {
                context.ChecklistTemplates.Add(BuildQabTemplate());
            }

            await context.SaveChangesAsync();
        }

        private static ChecklistTemplate BuildInstituteTemplate()
        {
            var now = DateTime.UtcNow;

            return new ChecklistTemplate
            {
                Name = "Institute Self-Assessment v1",
                ApplicationType = ApplicationType.Institute,
                IsActive = true,
                CreatedAt = now,
                Sections = new List<ChecklistSection>
                {
                    new()
                    {
                        Title = "Governance & Management",
                        DisplayOrder = 1,
                        Items = new List<ChecklistItem>
                        {
                            new() { Title = "Institutional governance structure is clearly documented", Description = "Organogram / governance policy showing reporting lines and decision-making authority.", DisplayOrder = 1, IsEvidenceRequired = true, MaxScore = 5 },
                            new() { Title = "Written policies exist for admissions, grievance handling, and academic integrity", Description = "Attach the relevant policy documents.", DisplayOrder = 2, IsEvidenceRequired = true, MaxScore = 5 },
                            new() { Title = "Institute holds a valid registration/affiliation with a recognized authority", Description = null, DisplayOrder = 3, IsEvidenceRequired = true, MaxScore = 5 }
                        }
                    },
                    new()
                    {
                        Title = "Infrastructure & Facilities",
                        DisplayOrder = 2,
                        Items = new List<ChecklistItem>
                        {
                            new() { Title = "Classrooms/labs are adequate for enrolled student capacity", Description = "Photographs or floor plans with capacity figures.", DisplayOrder = 1, IsEvidenceRequired = true, MaxScore = 5 },
                            new() { Title = "Institute has a functional library/resource center", Description = null, DisplayOrder = 2, IsEvidenceRequired = true, MaxScore = 5 },
                            new() { Title = "Health & safety measures (fire extinguishers, emergency exits) are in place", Description = "Photographs of safety equipment and signage.", DisplayOrder = 3, IsEvidenceRequired = true, MaxScore = 5 }
                        }
                    },
                    new()
                    {
                        Title = "Faculty & Staff Qualifications",
                        DisplayOrder = 3,
                        Items = new List<ChecklistItem>
                        {
                            new() { Title = "Teaching staff meet the minimum qualification criteria for their subjects", Description = "CVs/certificates for a sample of teaching staff.", DisplayOrder = 1, IsEvidenceRequired = true, MaxScore = 5 },
                            new() { Title = "Staff-to-student ratio meets the recommended threshold", Description = null, DisplayOrder = 2, IsEvidenceRequired = false, MaxScore = 5 },
                            new() { Title = "Regular professional development/training is provided to faculty", Description = null, DisplayOrder = 3, IsEvidenceRequired = false, MaxScore = 5 }
                        }
                    },
                    new()
                    {
                        Title = "Curriculum & Academic Quality",
                        DisplayOrder = 4,
                        Items = new List<ChecklistItem>
                        {
                            new() { Title = "Curriculum aligns with the National Vocational Qualifications Framework (NVQF) or relevant standard", Description = null, DisplayOrder = 1, IsEvidenceRequired = true, MaxScore = 5 },
                            new() { Title = "Assessment and examination procedures are documented and consistently applied", Description = null, DisplayOrder = 2, IsEvidenceRequired = true, MaxScore = 5 }
                        }
                    },
                    new()
                    {
                        Title = "Safety & Compliance",
                        DisplayOrder = 5,
                        Items = new List<ChecklistItem>
                        {
                            new() { Title = "Institute complies with applicable labor, safety, and environmental regulations", Description = null, DisplayOrder = 1, IsEvidenceRequired = true, MaxScore = 5 }
                        }
                    }
                }
            };
        }

        private static ChecklistTemplate BuildQabTemplate()
        {
            var now = DateTime.UtcNow;

            return new ChecklistTemplate
            {
                Name = "QAB Self-Assessment v1",
                ApplicationType = ApplicationType.QAB,
                IsActive = true,
                CreatedAt = now,
                Sections = new List<ChecklistSection>
                {
                    new()
                    {
                        Title = "Governance & Independence",
                        DisplayOrder = 1,
                        Items = new List<ChecklistItem>
                        {
                            new() { Title = "QAB has a documented governance structure ensuring independence from assessed institutes", Description = null, DisplayOrder = 1, IsEvidenceRequired = true, MaxScore = 5 },
                            new() { Title = "A conflict-of-interest policy is documented and enforced for assessors", Description = null, DisplayOrder = 2, IsEvidenceRequired = true, MaxScore = 5 }
                        }
                    },
                    new()
                    {
                        Title = "Assessor Competence",
                        DisplayOrder = 2,
                        Items = new List<ChecklistItem>
                        {
                            new() { Title = "QAB maintains a roster of qualified, trained assessors", Description = "CVs/certifications for a sample of assessors.", DisplayOrder = 1, IsEvidenceRequired = true, MaxScore = 5 },
                            new() { Title = "Assessors undergo periodic competency evaluation", Description = null, DisplayOrder = 2, IsEvidenceRequired = false, MaxScore = 5 }
                        }
                    },
                    new()
                    {
                        Title = "Assessment Methodology",
                        DisplayOrder = 3,
                        Items = new List<ChecklistItem>
                        {
                            new() { Title = "QAB has a documented, standardized assessment methodology", Description = null, DisplayOrder = 1, IsEvidenceRequired = true, MaxScore = 5 },
                            new() { Title = "Assessment reports follow a consistent, quality-assured template", Description = null, DisplayOrder = 2, IsEvidenceRequired = false, MaxScore = 5 }
                        }
                    },
                    new()
                    {
                        Title = "Quality Assurance & Complaints",
                        DisplayOrder = 4,
                        Items = new List<ChecklistItem>
                        {
                            new() { Title = "QAB has an internal quality assurance mechanism for its own operations", Description = null, DisplayOrder = 1, IsEvidenceRequired = true, MaxScore = 5 },
                            new() { Title = "A documented complaints/appeals process exists for assessed institutes", Description = null, DisplayOrder = 2, IsEvidenceRequired = true, MaxScore = 5 }
                        }
                    },
                    new()
                    {
                        Title = "Scope & Track Record",
                        DisplayOrder = 5,
                        Items = new List<ChecklistItem>
                        {
                            new() { Title = "QAB's scope of awarding aligns with its demonstrated technical competence", Description = null, DisplayOrder = 1, IsEvidenceRequired = false, MaxScore = 5 }
                        }
                    }
                }
            };
        }
    }
}
