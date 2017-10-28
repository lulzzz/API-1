using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Aiursoft.API.Data.Migrations
{
    public partial class createnothing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LocalAppGrantId",
                table: "LocalAppGrant",
                newName: "AppGrantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AppGrantId",
                table: "LocalAppGrant",
                newName: "LocalAppGrantId");
        }
    }
}
