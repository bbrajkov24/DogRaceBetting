using DogRace.Domain.Models.ParticipantTypes;

namespace DogRace.Domain.Models
{
	public abstract class RaceParticipant
	{
		public int Id { get; set; }
		public int Number { get; set; }
		public string Name { get; set; } = string.Empty;
		public int RaceId { get; set; }
		public Race Race { get; set; } = null!;
		public bool IsWinner { get; set; }
		public ParticipantType ParticipantTypeKey { get; set; }
	}
}