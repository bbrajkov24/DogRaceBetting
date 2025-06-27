using DogRace.Domain.Models.Common;

namespace DogRace.Domain.Models
{
	public class Player
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public decimal Balance { get; set; } = 100m;

		public OperationResult Deposit(decimal amount)
		{
			if (amount <= 0)
			{
				return OperationResult.Fail("Deposit amount must be positive.");
			}
			Balance += amount;
			return OperationResult.Ok();
		}

		public OperationResult Withdraw(decimal amount)
		{
			if (amount <= 0)
				return OperationResult.Fail("Amount must be positive.");

			if (amount > Balance)
				return OperationResult.Fail("Insufficient funds.");

			Balance -= amount;
			return OperationResult.Ok();
		}

		public decimal GetBalance()
		{
			return Balance;
		}
	}
}
