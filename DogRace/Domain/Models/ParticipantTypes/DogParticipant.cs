namespace DogRace.Domain.Models.ParticipantTypes
{
	public class DogParticipant : RaceParticipant
	{
		public DogParticipant()
		{
			ParticipantTypeKey = ParticipantType.Dog;
		}
	}
}
