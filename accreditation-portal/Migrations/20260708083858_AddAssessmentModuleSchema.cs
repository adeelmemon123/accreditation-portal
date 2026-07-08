using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace accreditation_portal.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentModuleSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "QABProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "InstituteProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssessmentAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    ConvenerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WindowStartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WindowEndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentAssignments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentAssignments_AspNetUsers_ConvenerId",
                        column: x => x.ConvenerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentFindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentAssignmentId = table.Column<int>(type: "int", nullable: false),
                    ChecklistItemId = table.Column<int>(type: "int", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Strengths = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Weaknesses = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecommendedScore = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentFindings_AspNetUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentFindings_AssessmentAssignments_AssessmentAssignmentId",
                        column: x => x.AssessmentAssignmentId,
                        principalTable: "AssessmentAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentFindings_ChecklistItems_ChecklistItemId",
                        column: x => x.ChecklistItemId,
                        principalTable: "ChecklistItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentTeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentAssignmentId = table.Column<int>(type: "int", nullable: false),
                    AssessorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentTeamMembers_AspNetUsers_AssessorUserId",
                        column: x => x.AssessorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentTeamMembers_AssessmentAssignments_AssessmentAssignmentId",
                        column: x => x.AssessmentAssignmentId,
                        principalTable: "AssessmentAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentEvidence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentFindingId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentEvidence_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentEvidence_AssessmentFindings_AssessmentFindingId",
                        column: x => x.AssessmentFindingId,
                        principalTable: "AssessmentFindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAssignments_ApplicationId",
                table: "AssessmentAssignments",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAssignments_ConvenerId",
                table: "AssessmentAssignments",
                column: "ConvenerId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentEvidence_AssessmentFindingId",
                table: "AssessmentEvidence",
                column: "AssessmentFindingId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentEvidence_UploadedByUserId",
                table: "AssessmentEvidence",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentFindings_AssessmentAssignmentId_ChecklistItemId",
                table: "AssessmentFindings",
                columns: new[] { "AssessmentAssignmentId", "ChecklistItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentFindings_ChecklistItemId",
                table: "AssessmentFindings",
                column: "ChecklistItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentFindings_SubmittedByUserId",
                table: "AssessmentFindings",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTeamMembers_AssessmentAssignmentId_AssessorUserId",
                table: "AssessmentTeamMembers",
                columns: new[] { "AssessmentAssignmentId", "AssessorUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTeamMembers_AssessorUserId",
                table: "AssessmentTeamMembers",
                column: "AssessorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentEvidence");

            migrationBuilder.DropTable(
                name: "AssessmentTeamMembers");

            migrationBuilder.DropTable(
                name: "AssessmentFindings");

            migrationBuilder.DropTable(
                name: "AssessmentAssignments");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "QABProfiles");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "InstituteProfiles");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "AspNetUsers");
        }
    }
}
