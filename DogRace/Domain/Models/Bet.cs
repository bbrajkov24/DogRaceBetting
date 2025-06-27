using DogRace.Domain.Models.BetTypes;
using DogRace.Domain.Models.Common;

namespace DogRace.Domain.Models
{
	public abstract class Bet
	{
		public int Id { get; set; }
		public int PlayerId { get; set; }
		public int RaceId { get; set; }
		public Race Race { get; set; } = null!;
		public decimal Amount { get; set; }
		public decimal? Payout { get; set; }
		public BetType BetTypeKey { get; set; }
		public BetStatus Status { get; set; } = BetStatus.Pending;

		public abstract OperationResult ValidateParticipants(Race race);
		public abstract decimal CalculatePotentialPayout();
		public abstract string GetDetails();
		public abstract bool IsWinningBet(List<int> raceOfficialPlacements);

		public void Resolve(List<int> raceOfficialPlacements)
		{
			if (IsWinningBet(raceOfficialPlacements))
			{
				Payout = CalculatePotentialPayout();
				Status = BetStatus.Won;
			}
			else
			{
				Payout = 0m;
				Status = BetStatus.Lost;
			}
		}
	}
}
