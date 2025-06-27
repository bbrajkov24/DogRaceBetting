using DogRace.Domain.Models.Common;

namespace DogRace.Domain.Models.BetTypes
{
	public class WinBet : Bet
	{
		public int ParticipantNumber { get; set; }

		public WinBet()
		{
			BetTypeKey = BetType.Win;
		}

		public override OperationResult ValidateParticipants(Race race)
		{
			if (!race.Participants.Any(p => p.Number == ParticipantNumber))
			{
				return OperationResult.Fail("Participant not found in race for Win Bet.");
			}
			return OperationResult.Ok();
		}

		public override decimal CalculatePotentialPayout()
		{
			return Amount * 2m;
		}

		public override bool IsWinningBet(List<int> raceOfficialPlacements)
		{
			return raceOfficialPlacements.Count != 0 && raceOfficialPlacements[0] == ParticipantNumber;
		}

		public override string GetDetails()
		{
			return $"Participant: #{ParticipantNumber}";
		}
	}
}
