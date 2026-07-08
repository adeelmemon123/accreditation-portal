using accreditation_portal.Models.TaQecViewModels;

namespace accreditation_portal.Services
{
    public interface ITaQecReportPdfGenerator
    {
        byte[] Generate(TaQecReportViewModel model);
    }
}
