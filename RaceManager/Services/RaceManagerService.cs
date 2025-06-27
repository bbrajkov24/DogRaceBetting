using DogRace.Domain.Interfaces;
using DogRace.Domain.Models.ParticipantTypes;

namespace RaceManager.Services
{
	public class RaceManagerService(IBetService betService, IRaceService raceService) : IRaceManagerService, IDisposable
	{
		private Timer? _timer;
		private bool _isPaused;
		private bool _isRunning;
		private readonly SemaphoreSlim _simulationLock = new(1, 1);

		private bool _disposed = false;

		private const int MIN_ACTIVE_RACES = 5;
		private const int MIN_SECONDS_UNTIL_RACE = 30;
		private const int MAX_SECONDS_UNTIL_RACE = 60;
		private const int RACE_RUNNING_DURATION_SECONDS = 5;
		private const int RACE_ANNOUNCEMENT_THRESHOLD_SECONDS = 5;

		public Task StartSimulationAsync()
		{
			if (_isRunning) return Task.CompletedTask;

			_isRunning = true;
			_isPaused = false;
			_timer = new Timer(async (state) => await SimulateRacesLoop(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
			Console.WriteLine("Race simulation started.");
			return Task.CompletedTask;
		}

		public void PauseSimulation()
		{
			if (_isRunning)
			{
				_isPaused = true;
				Console.WriteLine("\x1b[33mRaces paused.\x1b[0m");
			}
		}

		public void ResumeSimulation()
		{
			if (_isRunning)
			{
				_isPaused = false;
				Console.WriteLine("\x1b[32mRaces resumed.\x1b[0m");
			}
		}

		public void StopSimulation()
		{
			_isRunning = false;
			Dispose();
			Console.WriteLine("\x1b[31mRace simulation stopped.\x1b[0m");
		}

		private async Task SimulateRacesLoop()
		{
			if (!_isRunning || _isPaused) return;

			await _simulationLock.WaitAsync();
			try
			{
				var unfinishedRacesCount = await raceService.GetUnfinishedRacesCountAsync();
				if (unfinishedRacesCount < MIN_ACTIVE_RACES)
				{
					int racesToCreate = MIN_ACTIVE_RACES - unfinishedRacesCount;
					for (int i = 0; i < racesToCreate; i++)
					{
						var startTime = await raceService.GetNextAvailableRaceStartTimeAsync(MIN_SECONDS_UNTIL_RACE, MAX_SECONDS_UNTIL_RACE);
						await raceService.CreateRaceAsync(startTime, ParticipantType.Dog);
					}
				}

				var now = DateTime.UtcNow;
				var unfinishedRaces = await raceService.GetUnfinishedRacesAsync();

				var pendingBets = await betService.GetAllPendingBetsAsync();
				foreach (var bet in pendingBets)
				{
					await betService.MarkBetAsActiveAsync(bet.Id);
				}

				foreach (var race in unfinishedRaces)
				{
					if (race.StartTime <= now && !race.IsFinished)
					{
						Console.WriteLine($"\x1b[36mRace #{race.Id} is running...\x1b[0m");

						TimeSpan timePassedSinceStart = now - race.StartTime;
						if (timePassedSinceStart < TimeSpan.FromSeconds(RACE_RUNNING_DURATION_SECONDS))
						{
							await Task.Delay(TimeSpan.FromSeconds(RACE_RUNNING_DURATION_SECONDS) - timePassedSinceStart);
						}

						Console.WriteLine($"\x1b[32;1mCompleting Race #{race.Id}...\x1b[0m");
						var completionResult = await raceService.CompleteRaceAsync(race.Id);
						if (!completionResult.Success)
						{
							Console.WriteLine($"\x1b[31mError completing race #{race.Id}: {completionResult.Error}\x1b[0m");
						}

						var resolveBetsResult = await betService.ResolveBetsForRaceAsync(race.Id);
						if (!resolveBetsResult.Success)
						{
							Console.WriteLine($"\x1b[31mError resolving bets for race #{race.Id}: {resolveBetsResult.Error}\x1b[0m");
						}

						var completedRace = await raceService.GetRaceByIdAsync(race.Id);
						var winner = completedRace?.Participants.FirstOrDefault(p => p.IsWinner);

						Console.WriteLine($"\x1b[32;1mRace #{race.Id} Finished! Winner: #{winner?.Number} {winner?.Name}\x1b[0m");
					}
					else if (race.StartTime > now)
					{
						var secondsUntilStart = (race.StartTime - now).TotalSeconds;
						if (secondsUntilStart <= RACE_ANNOUNCEMENT_THRESHOLD_SECONDS)
						{
							Console.WriteLine($"Race #{race.Id} starts in {secondsUntilStart:F1} seconds. Participants: {string.Join(", ", race.Participants.Select(p => $"#{p.Number} {p.Name}"))}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"\x1b[31mError in race simulation loop: {ex.Message}\x1b[0m");
			}
			finally
			{
				_simulationLock.Release();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed) return;

			if (disposing)
			{
				_timer?.Dispose();
				_simulationLock.Dispose();
			}

			_disposed = true;
		}
	}
}