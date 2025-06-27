using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace DogRace.Infrastructure.Persistence
{
	public class SqlitePragmaInterceptor : DbConnectionInterceptor
	{
		public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
		{
			using var command = connection.CreateCommand();

			command.CommandText = "PRAGMA journal_mode=WAL;";
			command.ExecuteNonQuery();

			command.CommandText = "PRAGMA busy_timeout = 5000;";
			command.ExecuteNonQuery();

			command.CommandText = "PRAGMA foreign_keys = ON;";
			command.ExecuteNonQuery();
		}
	}
}
