using DogRace.Application.Services;
using DogRace.Domain.Interfaces;
using DogRace.Domain.Models;
using DogRace.Domain.Models.BetTypes;
using DogRace.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = CreateHostBuilder(args).Build();

var scope = host.Services.CreateScope();
	
var services = scope.ServiceProvider;
var dbContext = services.GetRequiredService<DogRaceDbContext>();
var playerService = services.GetRequiredService<IPlayerService>();
var raceService = services.GetRequiredService<IRaceService>();
var betService = services.GetRequiredService<IBetService>();

int _currentPlayerId = 0;

Console.Clear();

try
{
	Console.WriteLine("Checking database connection...");
	if (await dbContext.Database.CanConnectAsync())
	{
		Console.WriteLine("Database connected.");
	}
	else
	{
		Console.WriteLine("\x1b[31mError: Could not connect to the database. Is the Race Manager running?\x1b[0m");
		return;
	}
}
catch (Exception ex)
{
	Console.WriteLine($"\x1b[31mAn error occurred while connecting to the database: {ex.Message}\x1b[0m");
	Console.WriteLine("\x1b[31mCould not connect to database. Is the Race Manager running?\x1b[0m");
	return;
}

// Automatically create player ID 1
Player? currentPlayer = await playerService.GetPlayerByIdAsync(1);
if (currentPlayer == null)
{
	currentPlayer = await playerService.CreatePlayerAsync("Default Player");
	_currentPlayerId = currentPlayer.Id;
	Console.WriteLine($"\x1b[32mDefault Player (ID: {_currentPlayerId}) created with initial balance {currentPlayer.GetBalance():C}.\x1b[0m");
}

while (true)
{
	await DisplayMainMenuAsync();
	var choice = Console.ReadLine();

	switch (choice)
	{
		case "1": await DisplayWalletBalanceAsync(); break;
		case "2": await ViewActiveRaces(raceService); break;
		case "3": await PlaceBet(betService, raceService); break;
		case "4": await ViewMyBets(betService); break;
		case "5": Console.WriteLine("\x1b[31mExiting client. Goodbye!\x1b[0m"); return;
		default: Console.WriteLine("\x1b[31mInvalid choice. Please try again.\x1b[0m"); break;
	}
	Console.WriteLine("\nPress any key to continue...");
	Console.ReadKey(true);
	Console.Clear();
}
	
static IHostBuilder CreateHostBuilder(string[] args) =>
	Host.CreateDefaultBuilder(args)
		.ConfigureServices((context, services) =>
		{
			services.AddDbContext<DogRaceDbContext>();
			services.AddScoped<IRaceService, RaceService>();
			services.AddScoped<IBetService, BetService>();
			services.AddSingleton<IPlayerService, PlayerService>();
		})
		.ConfigureLogging(logging =>
		{
			logging.ClearProviders();
			logging.AddConsole();
			logging.SetMinimumLevel(LogLevel.Warning);
		});

async Task DisplayMainMenuAsync()
{
	Console.WriteLine("\n\x1b[1m\x1b[35mPlayer Client Menu\x1b[0m");
	Console.WriteLine($"\x1b[34mCurrent Player ID: {_currentPlayerId}, Name: {(await playerService.GetPlayerByIdAsync(_currentPlayerId))?.Name ?? "Unknown"}\x1b[0m");
	Console.WriteLine("1. View Wallet Balance");
	Console.WriteLine("2. View Active Races");
	Console.WriteLine("3. Place Bet");
	Console.WriteLine("4. View My Bets");
	Console.WriteLine("5. Exit");
	Console.Write("\x1b[36mEnter your choice: \x1b[0m");
}

async Task DisplayWalletBalanceAsync()
{
	var player = await playerService.GetPlayerByIdAsync(_currentPlayerId);
	if (player != null)
	{
		Console.WriteLine($"\x1b[32mYour current balance: {player.GetBalance():C}\x1b[0m");
	}
	else
	{
		Console.WriteLine("\x1b[31mCurrent player not found.\x1b[0m");
	}
}

static async Task ViewActiveRaces(IRaceService raceService)
{
	Console.WriteLine("\n\x1b[1m\x1b[35mActive Races:\x1b[0m");
	var activeRaces = await raceService.GetActiveRacesAsync();

	if (activeRaces.Count == 0)
	{
		Console.WriteLine("No active races available at the moment. Please wait for the Race Manager to create some.");
		return;
	}

	foreach (var race in activeRaces)
	{
		Console.WriteLine($"\n\x1b[34mRace ID: {race.Id}\x1b[0m");
		Console.WriteLine($"  Starts: {race.StartTime:HH:mm:ss} (in {(race.StartTime - DateTime.UtcNow).TotalSeconds:F0} seconds)");
		Console.WriteLine($"  Type: {race.ParticipantTypeKey}");
		Console.WriteLine("  Participants:");
		foreach (var participant in race.Participants.OrderBy(p => p.Number))
		{
			Console.WriteLine($"    #{participant.Number}: {participant.Name}");
		}
	}
}

async Task PlaceBet(IBetService betService, IRaceService raceService)
{
	Console.Write("Enter Race ID to bet on: ");
	if (!int.TryParse(Console.ReadLine(), out int raceId))
	{
		Console.WriteLine("\x1b[31mInvalid Race ID.\x1b[0m");
		return;
	}

	var race = await raceService.GetRaceByIdAsync(raceId);
	if (race == null || race.IsFinished || race.StartTime <= DateTime.UtcNow)
	{
		Console.WriteLine("\x1b[31mRace is not available for betting.\x1b[0m");
		return;
	}

	Console.Write("Enter Bet Amount: ");
	if (!decimal.TryParse(Console.ReadLine(), out decimal amount) || amount <= 0)
	{
		Console.WriteLine("\x1b[31mInvalid bet amount.\x1b[0m");
		return;
	}

	var currentPlayer = await playerService.GetPlayerByIdAsync(_currentPlayerId);
	if (currentPlayer == null || currentPlayer.GetBalance() < amount)
	{
		Console.WriteLine("\x1b[31mInsufficient funds in wallet.\x1b[0m");
		return;
	}

	Console.WriteLine("\n\x1b[1m\x1b[35mPlace a Win Bet:\x1b[0m");
	Console.Write("Enter Participant Number for Win Bet: ");
	if (!int.TryParse(Console.ReadLine(), out int winParticipantNum))
	{
		Console.WriteLine("\x1b[31mInvalid participant number.\x1b[0m");
		return;
	}

	Bet? newBet = new WinBet
	{
		ParticipantNumber = winParticipantNum,
		PlayerId = _currentPlayerId,
		RaceId = raceId,
		Amount = amount
	};

	var placeBetResult = await betService.PlaceBetAsync(newBet);
	if (placeBetResult.Success)
	{
		Console.WriteLine($"\x1b[32mBet placed successfully! Amount: {amount:C}. Your new balance: {currentPlayer.GetBalance():C}\x1b[0m");
	}
	else
	{
		Console.WriteLine($"\x1b[31mFailed to place bet: {placeBetResult.Error}\x1b[0m");
	}
}

async Task ViewMyBets(IBetService betService)
{
	Console.WriteLine($"\n\x1b[1m\x1b[35mYour Bets (Player ID: {_currentPlayerId}):\x1b[0m");
	var playerBets = await betService.GetBetsByPlayerAsync(_currentPlayerId);

	if (playerBets.Count == 0)
	{
		Console.WriteLine("No bets found for this player.");
		return;
	}

	foreach (var bet in playerBets)
	{
		string betDetails = $"Bet ID: {bet.Id}, Race ID: {bet.RaceId}, Amount: {bet.Amount:C}, Type: {bet.BetTypeKey}, {bet.GetDetails()}";

		string status = bet.Status.ToString().ToUpper();
		string payout = bet.Payout.HasValue ? bet.Payout.Value.ToString("C") : "N/A";

		string statusColor = status switch
		{
			"WON" => "\x1b[32m",
			"LOST" => "\x1b[31m",
			"PENDING" => "\x1b[33m",
			_ => "\x1b[0m"
		};

		Console.WriteLine($"{betDetails}, Status: {statusColor}{status}\x1b[0m, Payout: {payout}");
	}
}