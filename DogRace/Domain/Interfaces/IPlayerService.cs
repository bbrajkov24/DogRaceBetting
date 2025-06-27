using DogRace.Domain.Models;

namespace DogRace.Domain.Interfaces
{
	public interface IPlayerService
	{
		Task<Player> CreatePlayerAsync(string name);
		Task<Player?> GetPlayerByIdAsync(int id);
	}
}
