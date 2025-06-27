using DogRace.Domain.Interfaces;
using DogRace.Domain.Models;
using DogRace.Domain.Models.Common;
using DogRace.Domain.Models.ParticipantTypes;
using DogRace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DogRace.Application.Services
{
	public class RaceService(DogRaceDbContext dbContext) : IRaceService
	{
		private static readonly Random _random = new();
		private const int NUMBER_OF_PARTICIPANTS = 6;
		private const int RACE_RUNNING_DURATION_SECONDS = 5;
		private const int MIN_GAP_BETWEEN_RACES_SECONDS = 2;

		public async Task<List<Race>> GetActiveRacesAsync()
		{
			return await dbContext.Races
				.Where(r => !r.IsFinished && r.StartTime > DateTime.UtcNow)
				.Include(r => r.Participants)
				.OrderBy(r => r.StartTime)
				.ToListAsync();
		}

		public async Task<int> GetUnfinishedRacesCountAsync()
		{
			return await dbContext.Races.CountAsync(r => !r.IsFinished);
		}

		public async Task<List<Race>> GetUnfinishedRacesAsync()
		{
			return await dbContext.Races
				.Where(r => !r.IsFinished)
				.Include(r => r.Participants)
				.OrderBy(r => r.StartTime)
				.ToListAsync();
		}

		public async Task<DateTime> GetNextAvailableRaceStartTimeAsync(int minSecondsUntilRace, int maxSecondsUntilRace)
		{
			DateTime now = DateTime.UtcNow;

			var latestUnfinishedRace = await dbContext.Races
				.Where(r => !r.IsFinished)
				.OrderByDescending(r => r.StartTime)
				.FirstOrDefaultAsync();

			DateTime earliestPossibleStartTime = now.AddSeconds(minSecondsUntilRace);

			if (latestUnfinishedRace != null)
			{
				DateTime projectedEndTimeOfLatest = latestUnfinishedRace.StartTime.AddSeconds(RACE_RUNNING_DURATION_SECONDS);

				if (projectedEndTimeOfLatest.AddSeconds(MIN_GAP_BETWEEN_RACES_SECONDS) > earliestPossibleStartTime)
				{
					earliestPossibleStartTime = projectedEndTimeOfLatest.AddSeconds(MIN_GAP_BETWEEN_RACES_SECONDS);
				}
			}

			int randomOffset = _random.Next(0, maxSecondsUntilRace - minSecondsUntilRace + 1);
			return earliestPossibleStartTime.AddSeconds(randomOffset);
		}

		public async Task<Race> CreateRaceAsync(DateTime startTime, ParticipantType participantType)
		{
			var newRace = new Race
			{
				StartTime = startTime,
				IsFinished = false,
				ParticipantTypeKey = participantType.ToString(),
				Participants = []
			};

			for (int p = 0; p < NUMBER_OF_PARTICIPANTS; p++)
			{
				int participantNumber = p + 1;
				newRace.Participants.Add(new DogParticipant
				{
					Number = participantNumber,
					Name = RandomDogName,
					IsWinner = false
				});
			}
			dbContext.Races.Add(newRace);
			await dbContext.SaveChangesAsync();
			return newRace;
		}

		public async Task<OperationResult> CompleteRaceAsync(int raceId)
		{
			var race = await dbContext.Races
				.Include(r => r.Participants)
				.FirstOrDefaultAsync(r => r.Id == raceId && !r.IsFinished);

			if (race == null)
			{
				return OperationResult.Fail($"Race with ID {raceId} not found or already finished.");
			}

			race.IsFinished = true;
			race.EndTime = DateTime.UtcNow;

			var shuffledParticipantNumbers = race.Participants
				.Select(p => p.Number)
				.OrderBy(x => _random.Next())
				.ToList();

			race.OfficialPlacements = shuffledParticipantNumbers;
			race.WinnerParticipantNumber = race.OfficialPlacements.FirstOrDefault();

			var winner = race.Participants.FirstOrDefault(p => p.Number == race.WinnerParticipantNumber);
			if (winner != null)
			{
				winner.IsWinner = true;
			}

			await dbContext.SaveChangesAsync();
			return OperationResult.Ok();
		}

		public async Task<Race?> GetRaceByIdAsync(int id)
		{
			return await dbContext.Races
				.Include(r => r.Participants)
				.FirstOrDefaultAsync(r => r.Id == id);
		}

		private static string RandomDogName
		{
			get
			{
				var dogNames = new[]
				{
					"Max", "Luna", "Rex", "Bella", "Leo", "Sara", "Bubi", "Mira", "Duke", "Laki",
					"Brzopet", "Frkač", "Trkalica", "Pahulja", "Krepko", "Zvrk", "Pliško", "Brzopuh", "Mrcina",
					"Lule", "Trkačica", "Medo", "Raketko", "Šmeker", "Zeko", "Vjetrić", "Munja"
				};

				int index = _random.Next(dogNames.Length);
				return dogNames[index];
			}
		}
	}
}