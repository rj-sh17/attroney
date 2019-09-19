using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AttorneyJournal.Migrations
{
    public partial class RemoveDefaultValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "AssignedToAttorneyId",
                table: "AspNetUsers",
                nullable: true,
                oldClrType: typeof(Guid),
                oldNullable: true,
                oldDefaultValue: new Guid("4d8ef8a6-5d24-468d-8b53-f29bc45a20fc"))
                .OldAnnotation("MySql:ValueGeneratedOnAdd", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "AssignedToAttorneyId",
                table: "AspNetUsers",
                nullable: true,
                defaultValue: new Guid("4d8ef8a6-5d24-468d-8b53-f29bc45a20fc"),
                oldClrType: typeof(Guid),
                oldNullable: true)
                .Annotation("MySql:ValueGeneratedOnAdd", true);
        }
    }
}
