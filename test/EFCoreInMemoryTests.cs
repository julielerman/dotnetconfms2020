using Data;
using Domain;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace test
{
  public class EFCoreInMemoryTests
  {
    private readonly ITestOutputHelper _output;

    public EFCoreInMemoryTests(ITestOutputHelper output)
    { _output = output; }

    private static Team TeamAjax => new Team("AFC Ajax", "The Lancers", "1900", "Amsterdam Arena");

    [Fact, Trait("", "Hidden properties")]
    public void CanStoreAndMaterializeImmutableTeamNameFromDataStore()
    {
      var team = TeamAjax;
      var options = new DbContextOptionsBuilder<TeamContext>().UseInMemoryDatabase("immutableTeamName").Options;
      using (var context = new TeamContext(options))
      {
        context.Teams.Add(team);
        context.SaveChanges();
      }
      using (var context = new TeamContext(options))
      {
        var storedTeam = context.Teams.FirstOrDefault();
        Assert.Equal("AFC Ajax", storedTeam.TeamName);
      }
    }

    [Fact, Trait("", "Protected collection")]
    public void CanStoreAndRetrieveTeamPlayers()
    {
      var team = TeamAjax;
      team.AddPlayer("André", "Onana", out string response);

      var options = new DbContextOptionsBuilder<TeamContext>().UseInMemoryDatabase("storeretrieveplayer").Options;
      using (var context = new TeamContext(options))
      {
        context.Teams.Add(team);
        context.SaveChanges();
      }
      using (var context = new TeamContext(options))
      {
        var storedTeam = context.Teams.Include(t => t.Players).FirstOrDefault();
        Assert.Single(storedTeam.Players);
      }
    }

    [Fact, Trait("", "Protected collection")]
    public void TeamPreventsAddingPlayersToExistingTeamWhenPlayersNotInMemory()
    {
      var team = TeamAjax;
      team.AddPlayer("André", "Onana", out string response);

      var options = new DbContextOptionsBuilder<TeamContext>().UseInMemoryDatabase("preventplayeronteamwithplayersnotloaded").Options;
      using (var context = new TeamContext(options))
      {
        context.Teams.Add(team);
        context.SaveChanges();
      }
      using (var context = new TeamContext(options))
      {
        var storedTeam = context.Teams.FirstOrDefault();
        storedTeam.AddPlayer("Matthijs", "de Ligt", out response);
        Assert.Equal("You must first retrieve", response.Substring(0, 23));
      }
    }

    [Fact, Trait("", "Protected collection")]
    public void TeamAllowsAddingPlayersToExistingTeamWhenPlayersAreLoaded()
    {
      var team = TeamAjax;
      team.AddPlayer("André", "Onana", out string response);

      var options = new DbContextOptionsBuilder<TeamContext>().UseInMemoryDatabase("allowplayeronteamwithplayersloaded").Options;
      using (var context = new TeamContext(options))
      {
        context.Teams.Add(team);
        context.SaveChanges();
      }
      using (var context = new TeamContext(options))
      {
        var storedTeam = context.Teams.Include(t => t.Players).ThenInclude(p => p.NameFactory).FirstOrDefault();
        storedTeam.AddPlayer("Matthijs", "de Ligt", out response);
        Assert.Equal(2, storedTeam.Players.Count());
      }
    }

    [Fact, Trait("", "Value Objects")]
    public void CanStoreAndRetrievePlayerName()
    {
      var team = TeamAjax;
      team.AddPlayer("André", "Onana", out string response);

      var options = new DbContextOptionsBuilder<TeamContext>().UseInMemoryDatabase("playername").Options;
      using (var context = new TeamContext(options))
      {
        context.Teams.Add(team);
        context.SaveChanges();
      }
      using (var context = new TeamContext(options))
      {
        var storedTeam = context.Teams.Include("Players").FirstOrDefault();
        Assert.Single(storedTeam.Players);
        Assert.Equal("André Onana", storedTeam.Players.First().Name);
      }
    }

    [Fact, Trait("", "Rich behavior, protected 1:1")]
    public void CanStoreAndRetrieveManagerTeamHistory()
    {
      var team = TeamAjax;
      var firstmanager = new Manager("Marcel", "Keizer");
      team.ChangeManagement(firstmanager);
      team.ChangeManagement(new Manager("Erik", "ten Hag"));

      var options = new DbContextOptionsBuilder<TeamContext>().UseInMemoryDatabase("storemanagerhistory").Options;
      using (var context = new TeamContext(options))
      {
        context.AddRange(team, firstmanager);
        context.SaveChanges();
      }
      using (var context = new TeamContext(options))
      {
        var M1 = context.Managers.Include(m => m.PastTeams).FirstOrDefault(m => m.NameFactory.Last == "Keizer");
        var M2 = context.Managers.Include(m => m.PastTeams).FirstOrDefault(m => m.NameFactory.Last == "ten Hag");
        Assert.Equal(new { M1 = "Marcel Keizer", M1Count = 1, M2 = "Erik ten Hag", M2Count = 0 },
            new { M1 = M1.Name, M1Count = M1.PastTeams.Count, M2 = M2.Name, M2Count = M2.PastTeams.Count });
      }
    }

    [Fact, Trait("", "Rich behavior, protected 1:1")]
    public void CanStoreAndRetrieveTeamManager()
    {
      var team = TeamAjax;
      var firstmanager = new Manager("Marcel", "Keizer");
      team.ChangeManagement(firstmanager);

      var options = new DbContextOptionsBuilder<TeamContext>().UseInMemoryDatabase("CanStoreAndRetrieveTeamManager").Options;
      using (var context = new TeamContext(options))
      {
        context.Teams.Add(team);
        context.SaveChanges();
      }
      using (var context = new TeamContext(options))
      {
        var storedTeam = context.Teams.Include("_manager").Include("_manager.NameFactory").FirstOrDefault();
        Assert.Equal(firstmanager.Name, storedTeam.ManagerName);
        var storedManager = context.Teams.Select(t => EF.Property<Manager>(t, "_manager")).FirstOrDefault();
        var privateId = typeof(Team).GetProperty("Id", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Equal(privateId.GetValue(storedTeam), storedManager.CurrentTeamId);
      }
    }

    [Fact, Trait("", "Value conversions")]
    public void CanStoreAndRetrieveHomeColors()
    {
      var team = TeamAjax;
      team.SpecifyHomeUniformColors(Color.White, Color.Red);
      var options = new DbContextOptionsBuilder<TeamContext>().UseInMemoryDatabase("storecolors").Options;

      using (var context = new TeamContext(options))
      {
        context.Teams.Add(team);
        context.SaveChanges();
      }
      using (var context2 = new TeamContext(options))
      {
        var hc = context2.Teams.FirstOrDefault().HomeColors;
        Assert.Equal(new Color[] { hc.Primary, hc.Secondary }, new Color[] { Color.White, Color.Red });
      }
    }
  }
}