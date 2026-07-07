using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace accreditation_portal.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfRegistrationProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cnic",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstituteName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileNo",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tehsil",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cnic",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InstituteName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MobileNo",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Tehsil",
                table: "AspNetUsers");
        }
    }
}
