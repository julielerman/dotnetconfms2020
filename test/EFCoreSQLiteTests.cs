using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Data;
using Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace test
{
    public class EFCoreSQLiteTests
    {
        private static Team TeamAjax => new Team("AFC Ajax", "The Lancers", "1900", "Amsterdam Arena");
        private readonly ITestOutputHelper output;

        public EFCoreSQLiteTests(ITestOutputHelper output)
        {
            using (var context = new TeamContext())
            {
                context.Database.EnsureDeleted();
                context.Database.Migrate();
            }
            this.output = output;
        }

        [Fact, Trait("", "ValueConverters")]
        public void CanStoreAndRetrieveHomeColors()
        {
            var team = TeamAjax;
            team.SpecifyHomeUniformColors(Color.Blue, Color.Red);

            using (var context = new TeamContext())
            {
                context.Teams.Add(team);
                context.SaveChanges();
            }

            using (var context = new TeamContext())
            {
                var storedTeam = context.Teams.Include(t => t.HomeColors).FirstOrDefault();

                var hc = storedTeam.HomeColors;
                Assert.Equal(new Color[] { hc.Primary, hc.Secondary }, new Color[] { Color.Blue, Color.Red });
            }
        }

        [Fact, Trait("", "Hidden 1:1 and also the *:*")]
        public void CanStoreAndRetrieveManagerTeamHistory()
        {
            var team = TeamAjax;
            var firstManager = new Manager("Marcel", "Keizer");
            team.ChangeManagement(firstManager);
            team.ChangeManagement(new Manager("Erik", "ten Hag"));

            using (var context = new TeamContext())
            {
                context.AddRange(team, firstManager);
                context.SaveChanges();

            }
            using (var context = new TeamContext())
            {
                var m1 = context.Managers.Include(m => m.PastTeams).FirstOrDefault(m => m.NameFactory.Last == "Keizer");
                var m2 = context.Managers.Include(m => m.PastTeams).FirstOrDefault(m => m.NameFactory.Last == "ten Hag");
                Assert.Equal(new { M1 = "Marcel Keizer", M1Count = 1, M2 = "Erik ten Hag", M2Count = 0 },
                    new { M1 = m1.Name, M1Count = m1.PastTeams.Count, M2 = m2.Name, M2Count = m2.PastTeams.Count });
            }

        }

        [Fact, Trait("", "Hidden properties")]
        public void CanStoreAndMaterializeImmutableTeamNameFromDataStore()
        {
            var team = TeamAjax;
            using (var context = new TeamContext())
            {
                context.Teams.Add(team);
                context.SaveChanges();
            }
            using (var context = new TeamContext())
            {
                var storedTeam = context.Teams.FirstOrDefault();
                Assert.Equal("AFC Ajax", storedTeam.TeamName);
            }
        }

        [Fact, Trait("", "Keyless Entities")]
        public void CanReadManagerHistoryView()
        {
            var team = TeamAjax;
            var firstManager = new Manager("Scott", "Hunter");
            team.ChangeManagement(firstManager);
            team.ChangeManagement(new Manager("Erik", "ten Hag"));
            var teamDotNet = new Team("dotNet", "The Nerds", "2002", "MS Campus");
            teamDotNet.ChangeManagement(firstManager);
            teamDotNet.ChangeManagement(new Manager("David", "Fowler"));
            using (var context = new TeamContext())
            {
                context.AddRange(team, teamDotNet, firstManager);
                context.SaveChanges();
            }

            using (var context2 = new TeamContext())
            {
                var histories = context2.ManagerHistories.ToList();
                int counter = 0;
                foreach (var history in context2.ManagerHistories.ToList())
                {
                    counter += 1;
                    output.WriteLine($"{history.Manager}: {history.Team}");
                }
                Assert.Equal(4, counter);
            }
        }
    }
}