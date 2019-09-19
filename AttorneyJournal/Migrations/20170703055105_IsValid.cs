using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AttorneyJournal.Migrations
{
    public partial class IsValid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "Files",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Files",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "RegistrationCodes",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "RegistrationCodes",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "Attorneys",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Attorneys",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AssignedToAttorneyId",
                table: "AspNetUsers",
                nullable: true,
                defaultValue: new Guid("4d8ef8a6-5d24-468d-8b53-f29bc45a20fc"),
                oldClrType: typeof(Guid),
                oldNullable: true,
                oldDefaultValue: new Guid("58ceafa0-9ad9-468d-a16b-4ec8f2944d6f"))
                .Annotation("MySql:ValueGeneratedOnAdd", true)
                .OldAnnotation("MySql:ValueGeneratedOnAdd", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "RegistrationCodes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "RegistrationCodes");

            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "Attorneys");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Attorneys");

            migrationBuilder.AlterColumn<Guid>(
                name: "AssignedToAttorneyId",
                table: "AspNetUsers",
                nullable: true,
                defaultValue: new Guid("58ceafa0-9ad9-468d-a16b-4ec8f2944d6f"),
                oldClrType: typeof(Guid),
                oldNullable: true,
                oldDefaultValue: new Guid("4d8ef8a6-5d24-468d-8b53-f29bc45a20fc"))
                .Annotation("MySql:ValueGeneratedOnAdd", true)
                .OldAnnotation("MySql:ValueGeneratedOnAdd", true);
        }
    }
}
