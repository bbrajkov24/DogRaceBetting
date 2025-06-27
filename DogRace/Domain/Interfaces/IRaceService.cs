using DogRace.Domain.Models;
using DogRace.Domain.Models.Common;
using DogRace.Domain.Models.ParticipantTypes;

namespace DogRace.Domain.Interfaces
{
	public interface IRaceService
	{
		Task<List<Race>> GetActiveRacesAsync();
		Task<Race?> GetRaceByIdAsync(int id);
		Task<Race> CreateRaceAsync(DateTime startTime, ParticipantType participantType);
		Task<OperationResult> CompleteRaceAsync(int raceId);
		Task<int> GetUnfinishedRacesCountAsync();
		Task<List<Race>> GetUnfinishedRacesAsync();
		Task<DateTime> GetNextAvailableRaceStartTimeAsync(int minSecondsUntilRace, int maxSecondsUntilRace);
	}
}
