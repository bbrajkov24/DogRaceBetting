using DogRace.Domain.Interfaces;
using DogRace.Domain.Models;
using DogRace.Domain.Models.Common;
using DogRace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DogRace.Application.Services
{
	public class BetService(DogRaceDbContext dbContext, IPlayerService playerService) : IBetService
	{
		private readonly IPlayerService _playerService = playerService;

		private const decimal MIN_BET_AMOUNT = 1m;
		private const decimal MAX_BET_AMOUNT = 50m;
		private const decimal MAX_POTENTIAL_PAYOUT = 200m;

		public async Task<OperationResult> PlaceBetAsync(Bet bet)
		{
			var race = await dbContext.Races
				.Include(r => r.Participants)
				.FirstOrDefaultAsync(r => r.Id == bet.RaceId);

			if (race == null)
			{
				return OperationResult.Fail($"Race with ID {bet.RaceId} not found.");
			}

			var validationResult = ValidateBet(bet, race);
			if (!validationResult.Success)
			{
				return validationResult;
			}

			var player = await _playerService.GetPlayerByIdAsync(bet.PlayerId);
			if (player == null || player.GetBalance() < bet.Amount)
			{
				return OperationResult.Fail("Insufficient funds in player wallet.");
			}

			var withdrawResult = player.Withdraw(bet.Amount);
			if (!withdrawResult.Success)
			{
				return OperationResult.Fail($"Failed to deduct funds: {withdrawResult.Error}");
			}

			bet.Status = BetStatus.Pending;

			dbContext.Bets.Add(bet);
			await dbContext.SaveChangesAsync();

			return OperationResult.Ok();
		}

		private static OperationResult ValidateBet(Bet bet, Race race)
		{
			if (race.IsFinished || race.StartTime <= DateTime.UtcNow)
				return OperationResult.Fail("Race not available for betting (finished or already started).");

			if (bet.Amount < MIN_BET_AMOUNT || bet.Amount > MAX_BET_AMOUNT)
				return OperationResult.Fail($"Invalid bet amount. Must be between {MIN_BET_AMOUNT} and {MAX_BET_AMOUNT}.");

			var participantValidationResult = bet.ValidateParticipants(race);
			if (!participantValidationResult.Success)
			{
				return participantValidationResult;
			}

			var potentialPayout = bet.CalculatePotentialPayout();
			if (potentialPayout > MAX_POTENTIAL_PAYOUT)
				return OperationResult.Fail($"Max winnings exceed allowed limit of {MAX_POTENTIAL_PAYOUT:C}.");

			return OperationResult.Ok();
		}

		private async Task<OperationResult> RevalidateBet(Bet? bet)
		{
			if (bet == null)
			{
				return OperationResult.Fail("Bet not found.");
			}

			if (bet.Status != BetStatus.Pending)
			{
				return OperationResult.Fail("Bet is not in Pending status.");
			}

			if (bet.Race == null || bet.Race.StartTime <= DateTime.UtcNow)
			{
				bet.Status = BetStatus.Rejected;
				bet.Payout = 0m;

				var player = await _playerService.GetPlayerByIdAsync(bet.PlayerId);
				if (player != null)
				{
					var depositResult = player.Deposit(bet.Amount);
					if (!depositResult.Success)
					{
						await dbContext.SaveChangesAsync();
						return OperationResult.Fail($"Failed to refund bet amount for Bet {bet.Id}: {depositResult.Error}");
					}
				}
				else
				{
					await dbContext.SaveChangesAsync();
					return OperationResult.Fail($"Bet {bet.Id} rejected: Race {(bet.Race == null ? "not found" : "already started")}, but player {bet.PlayerId} not found for refund.");
				}

				await dbContext.SaveChangesAsync();
				return OperationResult.Fail($"Bet {bet.Id} rejected: Race {(bet.Race == null ? "not found" : "already started")}.");
			}

			return OperationResult.Ok();
		}

		private async Task<OperationResult> FinalizeBet(Bet bet)
		{
			bet.Status = BetStatus.Success;
			await dbContext.SaveChangesAsync();
			return OperationResult.Ok();
		}

		public async Task<OperationResult> MarkBetAsActiveAsync(int betId)
		{
			var bet = await dbContext.Bets
				.Include(b => b.Race)
				.FirstOrDefaultAsync(b => b.Id == betId);

			var revalidationResult = await RevalidateBet(bet);
			if (!revalidationResult.Success)
			{
				return revalidationResult;
			}

			return await FinalizeBet(bet!);
		}

		public async Task<List<Bet>> GetBetsByPlayerAsync(int playerId)
		{
			return await dbContext.Bets
				.AsNoTracking()
				.Where(b => b.PlayerId == playerId)
				.Include(b => b.Race)
					.ThenInclude(r => r.Participants)
				.OrderByDescending(b => b.Id)
				.ToListAsync();
		}

		public async Task<List<Bet>> GetAllPendingBetsAsync()
		{
			return await dbContext.Bets
				.AsNoTracking()
				.Where(b => b.Status == BetStatus.Pending)
				.Include(b => b.Race)
				.ToListAsync();
		}

		public async Task<OperationResult> ResolveBetsForRaceAsync(int raceId)
		{
			var race = await dbContext.Races
				.Include(r => r.Participants)
				.FirstOrDefaultAsync(r => r.Id == raceId);

			if (race == null)
			{
				return OperationResult.Fail($"Race {raceId} not found.");
			}
			if (!race.IsFinished)
			{
				return OperationResult.Fail($"Race {raceId} is not finished yet.");
			}
			if (race.OfficialPlacements.Count == 0)
			{
				return OperationResult.Fail($"Race {raceId} has no official placements.");
			}

			var bets = await dbContext.Bets
				.Where(b => b.RaceId == raceId && b.Status == BetStatus.Success)
				.ToListAsync();

			if (bets.Count == 0)
			{
				return OperationResult.Ok();
			}

			List<string> resolutionErrors = [];

			foreach (var bet in bets)
			{
				bet.Resolve(race.OfficialPlacements);

				if (bet.Status == BetStatus.Won && bet.Payout.HasValue && bet.Payout.Value > 0)
				{
					var player = await _playerService.GetPlayerByIdAsync(bet.PlayerId);
					if (player != null)
					{
						var depositResult = player.Deposit(bet.Payout.Value);
						if (!depositResult.Success)
						{
							resolutionErrors.Add($"Failed to deposit payout for Bet {bet.Id}: {depositResult.Error}");
						}
					}
					else
					{
						resolutionErrors.Add($"Player {bet.PlayerId} not found for Bet {bet.Id} payout.");
					}
				}
			}

			await dbContext.SaveChangesAsync();

			if (resolutionErrors.Count != 0)
			{
				return OperationResult.Fail($"Some bets failed to resolve properly: {string.Join("; ", resolutionErrors)}");
			}

			return OperationResult.Ok();
		}
	}
}