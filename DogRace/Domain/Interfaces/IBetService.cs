using DogRace.Domain.Models;
using DogRace.Domain.Models.Common;

namespace DogRace.Domain.Interfaces
{
	public interface IBetService
	{
		Task<OperationResult> PlaceBetAsync(Bet bet);
		Task<OperationResult> MarkBetAsActiveAsync(int betId);
		Task<OperationResult> ResolveBetsForRaceAsync(int raceId);
		Task<List<Bet>> GetBetsByPlayerAsync(int playerId);
		Task<List<Bet>> GetAllPendingBetsAsync();
	}
}
