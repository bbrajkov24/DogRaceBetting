namespace DogRace.Domain.Models
{
	public class Race
	{
		public int Id { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public bool IsFinished { get; set; }
		public int? WinnerParticipantNumber { get; set; }
		public string ParticipantTypeKey { get; set; } = string.Empty;
		public List<RaceParticipant> Participants { get; set; } = [];
		public List<int> OfficialPlacements { get; set; } = [];
	}
}