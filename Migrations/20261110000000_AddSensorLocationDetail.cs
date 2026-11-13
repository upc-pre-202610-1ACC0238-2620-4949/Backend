using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGas.Api.Migrations;

public partial class AddSensorLocationDetail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LocationDetail",
            table: "Sensors",
            type: "text",
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "LocationDetail", table: "Sensors");
    }
}