public static class PathHelper
{
	public static string GetSolutionRoot()
	{
		var currentDirectory = AppContext.BaseDirectory;
		var directory = new DirectoryInfo(currentDirectory);

		while (directory != null && directory.GetFiles("*.sln").Length == 0)
		{
			if (directory.GetDirectories("SharedDatabase").Any())
			{
				break;
			}
			directory = directory.Parent;
		}

		if (directory == null)
		{
			throw new InvalidOperationException("Solution root (.sln file) or 'SharedDatabase' directory not found.");
		}

		return directory.FullName;
	}

	public static string GetSharedDatabasePath()
	{
		string solutionRoot = GetSolutionRoot();
		string sharedDatabaseDirectory = Path.Combine(solutionRoot, "SharedDatabase");

		if (!Directory.Exists(sharedDatabaseDirectory))
		{
			try
			{
				Directory.CreateDirectory(sharedDatabaseDirectory);
				Console.WriteLine($"Created SharedDatabase directory at: {sharedDatabaseDirectory}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error creating SharedDatabase directory at {sharedDatabaseDirectory}: {ex.Message}");
				throw new IOException($"Could not create the SharedDatabase directory: {sharedDatabaseDirectory}", ex);
			}
		}

		return sharedDatabaseDirectory;
	}

	public static string GetDatabaseFilePath(string dbName = "dogracebetting.db")
	{
		string sharedDatabasePath = GetSharedDatabasePath();
		return Path.Combine(sharedDatabasePath, dbName);
	}
}