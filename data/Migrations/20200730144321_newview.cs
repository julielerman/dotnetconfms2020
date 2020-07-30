using Microsoft.EntityFrameworkCore.Migrations;

namespace data.Migrations
{
    public partial class newview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          migrationBuilder.Sql(
            @"DROP VIEW IF EXISTS v_ManagerTeamHistory;
              CREATE VIEW IF NOT EXISTS v_ManagerTeamHistory 
              AS
              select M.NameFactory_First ||' ' || M.NameFactory_Last as Manager,TeamName ||' (Past)' as Team from managerteamhistory as H
              INNER JOIN Managers as M on H.ManagerId = M.Id
              INNER JOIN teams as T on H.TeamId=T.Id
              UNION
              select M.NameFactory_First ||' ' || M.NameFactory_Last ,TeamName ||' (Current)' 
              FROM Managers M
              INNER JOIN Teams as T on M.CurrentTeamId=T.Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
           migrationBuilder.Sql("DROP VIEW IF EXISTS v_ManagerTeamHistory");
        }
    }
}
