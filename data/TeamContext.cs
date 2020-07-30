using domain;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Linq;

namespace Data
{
  public class TeamContext : DbContext
  {
    public TeamContext(DbContextOptions<TeamContext> options) : base(options)
    {
    }

    public TeamContext()
    {
    }

    public DbSet<Team> Teams { get; set; }
    public DbSet<Manager> Managers { get; set; }
    public DbSet<ManagerTeamHistoryView> ManagerHistories { get; set; }

    public static readonly ILoggerFactory ConsoleLoggerFactory
    = LoggerFactory.Create(builder => { builder.AddDebug(); });

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
      {
        optionsBuilder.UseSqlite("Data Source=d:\\data\\TeamData.db")
                      .UseLoggerFactory(ConsoleLoggerFactory)
                      .EnableSensitiveDataLogging();
      }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      //key property is private, configure EF Core to find it
      modelBuilder.Entity<Team>()
        .HasKey("Id");

      //specify backing field for undiscoverable properties - TeamName has no getter or setter
        modelBuilder.Entity<Team>()
          .Property(b => b.TeamName)
          .HasField("_teamname");


      //Shadow properties
      modelBuilder.Entity<Team>().Property<DateTime>("Created");
      modelBuilder.Entity<Team>().Property<DateTime>("LastModified");

      //Keyless entity to map a read only view
      modelBuilder.Entity<ManagerTeamHistoryView>().HasNoKey().ToView("v_ManagerTeamHistory");

      //Value Objects ....map as owned entities of the types that use them as properties
      //there is also OwnedMany support
      modelBuilder.Entity<Player>().OwnsOne(p => p.NameFactory).Property(m => m.First).HasColumnName("FName");
      modelBuilder.Entity<Manager>().OwnsOne(p => p.NameFactory); 
      modelBuilder.Entity<Team>().OwnsOne(t => t.HomeColors, hc=>
      {
        //value conversions for the value object as owned by team
        hc.Property(u => u.Primary).HasConversion(c => c.Name, s => Color.FromName(s));
        hc.Property(u => u.Secondary).HasConversion(c => c.Name, s => Color.FromName(s));
      });

      //extra help here because of relationship to private backing field
      modelBuilder.Entity<Team>()
        .HasOne(typeof(Manager), "_manager").WithOne()
        .HasForeignKey(typeof(Manager), "CurrentTeamId")
        .OnDelete(DeleteBehavior.ClientSetNull);

      //m2m composite key...will be MUCH better in EF Core 5
      modelBuilder.Entity<ManagerTeamHistory>().HasKey(m => new { m.ManagerId, m.TeamId });

    }

    public override int SaveChanges()
    {
      //populating the shadow properties
      var timestamp = DateTime.Now;
      foreach (var entry in ChangeTracker.Entries()
              .Where(e => e.Entity is Team &&
                 (e.State == EntityState.Added || e.State == EntityState.Modified)
              ))
      {
        entry.Property("LastModified").CurrentValue = timestamp;
        if (entry.State == EntityState.Added)
        {
          entry.Property("Created").CurrentValue = timestamp;
        }
      }
      return base.SaveChanges();
    }
  }
}