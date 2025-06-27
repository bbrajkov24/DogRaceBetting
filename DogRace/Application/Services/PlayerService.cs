using DogRace.Domain.Interfaces;
using DogRace.Domain.Models;
using DogRace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DogRace.Application.Services
{
	public class PlayerService(DogRaceDbContext dbContext) : IPlayerService
	{
		public async Task<Player> CreatePlayerAsync(string name)
		{
			var player = new Player
			{
				Name = name,
				Balance = 100m
			};
			dbContext.Players.Add(player);
			await dbContext.SaveChangesAsync();
			return player;
		}

		public async Task<Player?> GetPlayerByIdAsync(int id)
		{
			return await dbContext.Players.FirstOrDefaultAsync(p => p.Id == id);
		}
	}
}
