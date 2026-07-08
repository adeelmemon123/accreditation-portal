using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace accreditation_portal.Migrations
{
    /// <inheritdoc />
    public partial class AddTaQecModuleSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No user currently holds the TAQECChairperson role (verified before this migration) - repurpose
            // it as the generic TAQEC committee-member role; IsChairperson (below) now distinguishes the chair.
            migrationBuilder.Sql(@"
                UPDATE [AspNetRoles]
                SET [Name] = 'TAQEC', [NormalizedName] = 'TAQEC'
                WHERE [Name] = 'TAQECChairperson';
            ");

            migrationBuilder.AddColumn<bool>(
                name: "IsChairperson",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TaQecReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RationaleRemarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LockedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaQecReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaQecReviews_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaQecReviews_AspNetUsers_LockedByUserId",
                        column: x => x.LockedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaQecDiscussionNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaQecReviewId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ChecklistItemId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaQecDiscussionNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaQecDiscussionNotes_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaQecDiscussionNotes_ChecklistItems_ChecklistItemId",
                        column: x => x.ChecklistItemId,
                        principalTable: "ChecklistItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaQecDiscussionNotes_TaQecReviews_TaQecReviewId",
                        column: x => x.TaQecReviewId,
                        principalTable: "TaQecReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaQecDiscussionNotes_AuthorUserId",
                table: "TaQecDiscussionNotes",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaQecDiscussionNotes_ChecklistItemId",
                table: "TaQecDiscussionNotes",
                column: "ChecklistItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TaQecDiscussionNotes_TaQecReviewId",
                table: "TaQecDiscussionNotes",
                column: "TaQecReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_TaQecReviews_ApplicationId",
                table: "TaQecReviews",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaQecReviews_LockedByUserId",
                table: "TaQecReviews",
                column: "LockedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaQecDiscussionNotes");

            migrationBuilder.DropTable(
                name: "TaQecReviews");

            migrationBuilder.DropColumn(
                name: "IsChairperson",
                table: "AspNetUsers");

            migrationBuilder.Sql(@"
                UPDATE [AspNetRoles]
                SET [Name] = 'TAQECChairperson', [NormalizedName] = 'TAQECCHAIRPERSON'
                WHERE [Name] = 'TAQEC';
            ");
        }
    }
}
