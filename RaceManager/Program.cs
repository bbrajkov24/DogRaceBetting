using DogRace.Application.Services;
using DogRace.Domain.Interfaces;
using DogRace.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RaceManager.Services;

var host = CreateHostBuilder(args).Build();

using (var scope = host.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	var dbContext = services.GetRequiredService<DogRaceDbContext>();
	var raceManagerService = services.GetRequiredService<IRaceManagerService>();
	var logger = services.GetRequiredService<ILogger<Program>>();

	try
	{
		Console.WriteLine("Ensuring database is deleted and recreated...");
		await dbContext.Database.EnsureDeletedAsync();
		await dbContext.Database.EnsureCreatedAsync();
		Console.WriteLine("Database ready.");

		await raceManagerService.StartSimulationAsync();

		Console.WriteLine("\n\x1b[1m\x1b[35mRace Manager Started. Press '1' to Pause, '2' to Resume, '3' to Exit.\x1b[0m");

		while (true)
		{
			var key = Console.ReadKey(true);

			switch (key.Key)
			{
				case ConsoleKey.D1:
				case ConsoleKey.NumPad1:
					raceManagerService.PauseSimulation();
					Console.WriteLine("\x1b[33mRaces paused.\x1b[0m");
					break;

				case ConsoleKey.D2:
				case ConsoleKey.NumPad2:
					raceManagerService.ResumeSimulation();
					Console.WriteLine("\x1b[32mRaces resumed.\x1b[0m");
					break;

				case ConsoleKey.D3:
				case ConsoleKey.NumPad3:
					raceManagerService.StopSimulation();
					Console.WriteLine("\x1b[31mExiting Race Manager...\x1b[0m");
					return;

				default:
					break;
			}
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine($"\x1b[31mAn unhandled error occurred in the application: {ex.Message}\x1b[0m");
		Console.WriteLine("Press any key to exit.");
		Console.ReadKey();
	}
}

static IHostBuilder CreateHostBuilder(string[] args)
{
	return Host.CreateDefaultBuilder(args)
	   .ConfigureServices((context, services) =>
	   {
		   services.AddDbContext<DogRaceDbContext>();

		   services.AddScoped<IRaceService, RaceService>();
		   services.AddScoped<IBetService, BetService>();
		   services.AddScoped<IPlayerService, PlayerService>();
		   services.AddSingleton<IRaceManagerService, RaceManagerService>();
	   })
	   .ConfigureLogging(logging =>
	   {
			logging.ClearProviders();
			logging.AddConsole();
			logging.SetMinimumLevel(LogLevel.Warning);
	   });
}