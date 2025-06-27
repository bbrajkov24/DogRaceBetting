namespace DogRace.Domain.Models.Common
{
	public class OperationResult
	{
		public bool Success { get; }
		public string? Error { get; }

		private OperationResult(bool success, string? error = null)
		{
			Success = success;
			Error = error;
		}

		public static OperationResult Ok() => new(true);
		public static OperationResult Fail(string error) => 
			new(false, string.IsNullOrWhiteSpace(error) ? "An error occurred." : error);
	}
}
