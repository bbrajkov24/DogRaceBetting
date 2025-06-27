using DogRace.Domain.Models;
using DogRace.Domain.Models.BetTypes;
using DogRace.Domain.Models.ParticipantTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace DogRace.Infrastructure.Persistence
{
	public class DogRaceDbContext(DbContextOptions<DogRaceDbContext> options) : DbContext(options)
	{
		public DbSet<Race> Races { get; set; } = null!;
		public DbSet<RaceParticipant> Participants { get; set; } = null!;
		public DbSet<Bet> Bets { get; set; } = null!;
		public DbSet<Player> Players { get; set; } = null!;

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Race>(b =>
			{
				b.HasKey(r => r.Id);
				b.Property(r => r.ParticipantTypeKey).IsRequired();
				b.HasMany(r => r.Participants).WithOne(p => p.Race).HasForeignKey(p => p.RaceId);

				var listIntConverter = new ValueConverter<List<int>, string>(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions?)null) ?? new List<int>()
				);
				var listIntComparer = new ValueComparer<List<int>>(
					(l1, l2) => l1!.SequenceEqual(l2!),
					l => l.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
					l => l.ToList()
				);

				b.Property(r => r.OfficialPlacements)
				 .HasConversion(listIntConverter)
				 .Metadata.SetValueComparer(listIntComparer); 
			});

			modelBuilder.Entity<RaceParticipant>(b =>
			{
				b.HasKey(p => p.Id);
				b.Property(p => p.Name).IsRequired();
				b.Property(p => p.Number).IsRequired();
				b.HasIndex(p => new { p.RaceId, p.Number }).IsUnique();
				b.HasDiscriminator<ParticipantType>("ParticipantTypeKey")
					.HasValue<DogParticipant>(ParticipantType.Dog);
			});

			modelBuilder.Entity<Bet>(b =>
			{
				b.HasKey(bet => bet.Id);
				b.HasOne(bet => bet.Race).WithMany().HasForeignKey(bet => bet.RaceId);
				b.HasDiscriminator<BetType>("BetTypeKey")
					.HasValue<WinBet>(BetType.Win);
			});

			modelBuilder.Entity<WinBet>().Property(w => w.ParticipantNumber).IsRequired();

			modelBuilder.Entity<Player>(b =>
			{
				b.HasKey(p => p.Id);
				b.Property(p => p.Name).IsRequired().HasMaxLength(100);
				b.Property(p => p.Balance).HasColumnType("decimal(18,2)").IsRequired();
			});
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseSqlite($"Data Source={PathHelper.GetDatabaseFilePath()}")
				  .AddInterceptors(new SqlitePragmaInterceptor());
			}
		}		
	}
}