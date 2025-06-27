namespace RaceManager.Services
{
	public interface IRaceManagerService
	{
		Task StartSimulationAsync();
		void PauseSimulation();
		void ResumeSimulation();
		void StopSimulation();
	}
}
