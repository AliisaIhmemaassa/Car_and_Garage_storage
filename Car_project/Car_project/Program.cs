namespace Car_project
{
	// File: Program.cs
	using System;
	using System.Linq;
	using System.Threading.Tasks;
	using Vehicle; // <- fix: your classes live in 'Vehicles'

	class Program
	{
		static async Task Main()
		{
			var repo = new GarageRepository("data"); // data folder will be created

			while (true)
			{
				Console.WriteLine("\n=== Main Menu ===");
				var garages = repo.ListGarages().ToList();
				if (garages.Count == 0) Console.WriteLine("No garages yet.");

				for (int i = 0; i < garages.Count; i++)
					Console.WriteLine($"{i + 1}) {garages[i].Name}");

				Console.WriteLine("N) New garage");
				Console.WriteLine("L) List lost cars");
				Console.WriteLine("Q) Quit");
				Console.Write("Choose: ");
				var choice = (Console.ReadLine() ?? "").Trim();

				if (choice.Equals("Q", StringComparison.OrdinalIgnoreCase)) break;

				if (choice.Equals("N", StringComparison.OrdinalIgnoreCase))
				{
					var name = AskText("Garage name");
					var addr = AskText("Address");
					var own = AskText("Owner");
					var cap = AskInt("Capacity (0 = unlimited)", 0, 100000);
					var ws = AskYesNo("Is workshop");

					var g = new Garage(name, addr, own, cap, ws);
					await repo.CreateGarageAsync(g);
					Console.WriteLine($"Created garage '{name}'.");
					continue;
				}

				if (choice.Equals("L", StringComparison.OrdinalIgnoreCase))
				{
					var lost = await repo.LoadLostCarsAsync();
					Console.WriteLine($"\nLost cars ({lost.Count}):");
					foreach (var c in lost) Console.WriteLine(" - " + c);

					Console.WriteLine("D) Delete a lost car by plate");
					Console.WriteLine("B) Back");
					Console.Write("Choose: ");
					var lc = (Console.ReadLine() ?? "").Trim();
					if (lc.Equals("D", StringComparison.OrdinalIgnoreCase))
					{
						var plate = AskText("Plate to delete");
						var ok = await repo.DeleteCarFromLostAsync(plate);
						Console.WriteLine(ok ? "Deleted." : "Not found.");
					}
					continue;
				}

				if (int.TryParse(choice, out int idx) && idx >= 1 && idx <= garages.Count)
				{
					var garageName = garages[idx - 1].Name;
					await GarageMenuAsync(repo, garageName);
				}
			}
		}

		static async Task GarageMenuAsync(GarageRepository repo, string garageName)
		{
			while (true)
			{
				var g = await repo.LoadGarageAsync(garageName);
				Console.WriteLine($"\n=== Garage: {g} ===");
				foreach (var c in g.GetAllCars()) Console.WriteLine(" - " + c);

				Console.WriteLine("A) Add car");
				Console.WriteLine("R) Remove car (moves to lost cars)");
				Console.WriteLine("M) Move car from lost cars to this garage");
				Console.WriteLine("B) Back");
				Console.Write("Choose: ");
				var ch = (Console.ReadLine() ?? "").Trim();

				if (ch.Equals("B", StringComparison.OrdinalIgnoreCase)) break;

				if (ch.Equals("A", StringComparison.OrdinalIgnoreCase))
				{
					var car = CreateCarFromPrompt();
					try
					{
						await repo.AddCarToGarageAsync(garageName, car);
						Console.WriteLine("Car added.");
					}
					catch (Exception ex)
					{
						Console.WriteLine("Error adding car: " + ex.Message);
					}
				}
				else if (ch.Equals("R", StringComparison.OrdinalIgnoreCase))
				{
					var plate = AskText("License plate to remove");
					var ok = await repo.MoveCarFromGarageToLostAsync(garageName, plate);
					Console.WriteLine(ok ? "Moved to lost cars." : "Plate not found.");
				}
				else if (ch.Equals("M", StringComparison.OrdinalIgnoreCase))
				{
					var plate = AskText("Plate to move from lost");
					try
					{
						var ok = await repo.MoveCarFromLostToGarageAsync(garageName, plate);
						Console.WriteLine(ok ? "Moved into garage." : "Plate not found in lost cars.");
					}
					catch (Exception ex)
					{
						Console.WriteLine("Error moving car: " + ex.Message);
					}
				}
			}
		}

		// ----------------------------
		// INPUT HELPERS (re-ask until valid)
		// ----------------------------

		static int AskInt(string question, int min, int max)
		{
			while (true)
			{
				Console.Write($"{question} [{min}-{max}]: ");
				if (int.TryParse(Console.ReadLine(), out int value) &&
					value >= min && value <= max)
					return value;

				Console.WriteLine("Invalid number, try again.");
			}
		}

		static double AskDouble(string question, double min, double max)
		{
			while (true)
			{
				Console.Write($"{question} [{min}-{max}]: ");
				if (double.TryParse(Console.ReadLine(), out double value) &&
					value >= min && value <= max)
					return value;

				Console.WriteLine("Invalid number, try again.");
			}
		}

		static string AskText(string question)
		{
			while (true)
			{
				Console.Write($"{question}: ");
				var input = Console.ReadLine();
				if (!string.IsNullOrWhiteSpace(input))
					return input.Trim();

				Console.WriteLine("Cannot be empty. Try again.");
			}
		}

		static Drivetrain AskDrivetrain()
		{
			while (true)
			{
				Console.Write("Drivetrain (FWD/RWD/AWD): ");
				string s = (Console.ReadLine() ?? "FWD").Trim().ToUpperInvariant();

				if (s == "FWD") return Drivetrain.FWD;
				if (s == "RWD") return Drivetrain.RWD;
				if (s == "AWD") return Drivetrain.AWD;

				Console.WriteLine("Invalid input. Write FWD, RWD or AWD.");
			}
		}

		static bool AskYesNo(string question, bool defaultYes = true)
		{
			while (true)
			{
				Console.Write($"{question} ({(defaultYes ? "Y/n" : "y/N")}): ");
				var s = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

				if (string.IsNullOrEmpty(s)) return defaultYes;
				if (s is "y" or "yes") return true;
				if (s is "n" or "no") return false;

				Console.WriteLine("Answer must be y or n.");
			}
		}

		// ----------------------------
		// Create car with validation + retry
		// ----------------------------
		static Car CreateCarFromPrompt()
		{
			var plate = AskText("Plate");
			var model = AskText("Model");
			var year = AskInt("Year", 1886, DateTime.UtcNow.Year + 1);
			var dt = AskDrivetrain();
			var tank = AskDouble("Tank size (L)", 1, 500);
			var eng = AskDouble("Engine size (L)", 0.1, 20);
			var hp = AskInt("Horsepower", 1, 2000);
			var top = AskInt("Top speed (km/h)", 1, 600);
			var front = AskYesNo("Engine in front?", defaultYes: true);
			var owner = AskText("Owner");

			try
			{
				return new Car(plate, model, year, dt, tank, eng, hp, top, front, owner);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Invalid car: " + ex.Message);
				Console.WriteLine("Let's try again.\n");
				return CreateCarFromPrompt(); // retry
			}
		}
	}
}