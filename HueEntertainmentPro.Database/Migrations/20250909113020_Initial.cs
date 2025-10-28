using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HueEntertainmentPro.Database.Migrations
{
  /// <inheritdoc />
  public partial class Initial : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "Bridges",
          columns: table => new
          {
            Id = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
            Name = table.Column<string>(type: "TEXT", nullable: true),
            Ip = table.Column<string>(type: "TEXT", nullable: false),
            BridgeId = table.Column<string>(type: "TEXT", nullable: false),
            Username = table.Column<string>(type: "TEXT", nullable: false),
            StreamingClientKey = table.Column<string>(type: "TEXT", nullable: false),
            CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Bridges", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProAreas",
          columns: table => new
          {
            Id = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_ProAreas", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProAreaGroups",
          columns: table => new
          {
            Id = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
            ProAreaId = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
            BridgeId = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
            GroupId = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
            Name = table.Column<string>(type: "TEXT", nullable: true),
            CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_ProAreaGroups", x => x.Id);
            table.ForeignKey(
                      name: "FK_ProAreaGroups_Bridges_BridgeId",
                      column: x => x.BridgeId,
                      principalTable: "Bridges",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_ProAreaGroups_ProAreas_ProAreaId",
                      column: x => x.ProAreaId,
                      principalTable: "ProAreas",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_ProAreaGroups_BridgeId",
          table: "ProAreaGroups",
          column: "BridgeId");

      migrationBuilder.CreateIndex(
          name: "IX_ProAreaGroups_ProAreaId",
          table: "ProAreaGroups",
          column: "ProAreaId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "ProAreaGroups");

      migrationBuilder.DropTable(
          name: "Bridges");

      migrationBuilder.DropTable(
          name: "ProAreas");
    }
  }
}
