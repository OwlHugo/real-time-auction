using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealTimeAuction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRowVersionToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Created",
                table: "Auctions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Auctions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModified",
                table: "Auctions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Auctions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RowVersion",
                table: "Auctions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Auctions");
        }
    }
}
