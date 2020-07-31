using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SharedKernel;

namespace Domain
{
    public class Team
    {
        private Team()
        {
        }

        public Team(string teamName, string nickname, string yearFounded, string homeStadium)
        {
            TeamName = teamName;
            Nickname = nickname;
            YearFounded = yearFounded;
            HomeStadium = homeStadium;
            Id = Guid.NewGuid();
            _players = new List<Player>();
        }

        private Manager _manager;
        private readonly ICollection<Player> _players; // we can manipulate the ICollection locally

        public string ManagerName => _manager.Name;
        // EF Core recognizes IEnumerable, but as of EFC3,  backing field is default for read/write anyway
        //Players property is a "defensive copy", users can't modify the field
        public IEnumerable<Player> Players => _players.ToList();
        public string TeamName { get; }
        private Guid Id { get; }
        public UniformColors HomeColors { get; private set; }
        public string HomeStadium { get; }
        public string Nickname { get; }
        public string YearFounded { get; }

        public bool AddPlayer(string firstName, string lastname, out string response)
        {
            if (_players == null)
            {
                //this can only be tested with integration test against EF Core
                response = "You must first retrieve this team's existing list of players";
                return false;
            }

            var fullName = PersonFullName.Create(firstName, lastname).FullName;
            var foundPlayer = _players.Where(p => p.Name.Equals(fullName)).FirstOrDefault();
            if (foundPlayer == null)
            {
                _players.Add(new Player(firstName, lastname));
                response = "Player added to team";
                return true;
            }

            response = "Duplicate player";
            return false;
        }

        public void ChangeManagement(Manager newManager)
        {
            if (_manager is null || _manager.Name != newManager.Name)
            {
                _manager?.RemoveFromTeam(Id);
                newManager.BecameTeamManager(Id);
                _manager = newManager;
            }
        }

        public void SpecifyHomeUniformColors(Color primary, Color secondary)
        {
            //would be interesting in another aggregate in this same bounded context
            //(but not necessarily the same microservice)
            //to validate a rule ensuring no two teams have the same color pair
            HomeColors = new UniformColors(primary, secondary);
        }
    }
}