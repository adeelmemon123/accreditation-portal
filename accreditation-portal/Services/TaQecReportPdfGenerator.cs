using accreditation_portal.Models.TaQecViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace accreditation_portal.Services
{
    // "PowerPoint style" (per the workflow doc) is interpreted here as visually-segmented PDF sections
    // (clear header/stats/section breaks) built from the same TaQecReportViewModel as the in-app Razor
    // report - not a literal .pptx file. Confirmed with the user before adding QuestPDF.
    public class TaQecReportPdfGenerator : ITaQecReportPdfGenerator
    {
        public byte[] Generate(TaQecReportViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("TA-QEC Committee Report").FontSize(20).Bold();
                        col.Item().PaddingTop(4).Text($"Application #{model.Application.Id} - {model.ApplicantName}").FontSize(13).SemiBold();
                        col.Item().Text($"{model.Application.ApplicationType} | Province: {model.Province ?? "-"} | Sector: {model.Sector ?? "-"}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Avg Self-Score").FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(model.AverageSelfScore.HasValue ? model.AverageSelfScore.Value.ToString("0.0") : "-").FontSize(16).Bold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Avg Assessor-Recommended Score").FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(model.AverageRecommendedScore.HasValue ? model.AverageRecommendedScore.Value.ToString("0.0") : "-").FontSize(16).Bold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Desk Review Flags").FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{model.FlaggedItemCount} / {model.TotalItemCount}").FontSize(16).Bold();
                            });
                        });

                        foreach (var section in model.Sections)
                        {
                            col.Item().PaddingTop(6).Text(section.Title).FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                            foreach (var item in section.Items)
                            {
                                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(itemCol =>
                                {
                                    itemCol.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(item.Title).SemiBold();
                                        row.ConstantItem(160).AlignRight()
                                            .Text($"Self: {item.SelfScore?.ToString() ?? "-"}/{item.MaxScore}   Assessor: {item.RecommendedScore?.ToString() ?? "-"}/{item.MaxScore}")
                                            .FontSize(9);
                                    });

                                    if (item.DeskReviewFlagged)
                                    {
                                        itemCol.Item().PaddingTop(2).Text("Flagged by Desk Review").FontColor(Colors.Red.Darken1).FontSize(9).Bold();
                                    }

                                    if (!string.IsNullOrWhiteSpace(item.SelfComments))
                                    {
                                        itemCol.Item().PaddingTop(2).Text($"Applicant self-assessment: {item.SelfComments}").FontSize(9);
                                    }

                                    if (!string.IsNullOrWhiteSpace(item.DeskReviewComment))
                                    {
                                        itemCol.Item().PaddingTop(2).Text($"Desk Review note: {item.DeskReviewComment}").FontSize(9);
                                    }

                                    if (!string.IsNullOrWhiteSpace(item.AssessorFindings))
                                    {
                                        itemCol.Item().PaddingTop(2).Text($"Assessor findings: {item.AssessorFindings}").FontSize(9);
                                    }

                                    if (!string.IsNullOrWhiteSpace(item.AssessorStrengths))
                                    {
                                        itemCol.Item().PaddingTop(2).Text($"Strengths: {item.AssessorStrengths}").FontSize(9);
                                    }

                                    if (!string.IsNullOrWhiteSpace(item.AssessorWeaknesses))
                                    {
                                        itemCol.Item().PaddingTop(2).Text($"Weaknesses: {item.AssessorWeaknesses}").FontSize(9);
                                    }
                                });
                            }
                        }

                        if (model.DiscussionNotes.Count > 0)
                        {
                            col.Item().PaddingTop(6).Text("Committee Discussion").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                            foreach (var note in model.DiscussionNotes)
                            {
                                var itemTag = note.ChecklistItemTitle is not null ? $" (on '{note.ChecklistItemTitle}')" : string.Empty;
                                col.Item().Text($"{note.AuthorName}{itemTag} - {note.CreatedAt.ToLocalTime():dd MMM yyyy HH:mm}").FontSize(9).SemiBold();
                                col.Item().PaddingBottom(4).Text(note.Note).FontSize(9);
                            }
                        }

                        col.Item().PaddingTop(6).Text("Grading Decision").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        if (model.IsLocked)
                        {
                            col.Item().Text($"Grade: {model.Grade}").FontSize(13).Bold();
                            col.Item().Text($"Locked by {model.LockedByName} on {model.LockedAt!.Value.ToLocalTime():dd MMM yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            col.Item().PaddingTop(4).Text($"Rationale: {model.RationaleRemarks}").FontSize(10);
                        }
                        else
                        {
                            col.Item().Text("Pending - not yet graded.").FontSize(10).Italic();
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
