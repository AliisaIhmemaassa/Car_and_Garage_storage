// File: GarageRepository.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Car_project;

namespace Vehicle
{
	/// <summary>
	/// Manages persistence for multiple garages and a shared "lost cars" file.
	/// </summary>
	public sealed class GarageRepository
	{
		public string RootFolder { get; }
		public string GaragesFolder => Path.Combine(RootFolder, "garages");
		public string LostCarsFilePath => Path.Combine(RootFolder, "lost_cars.json");

		private readonly JsonSerializerOptions _json;

		public GarageRepository(string rootFolder)
		{
			if (string.IsNullOrWhiteSpace(rootFolder))
				throw new ArgumentException("Root folder must not be empty.", nameof(rootFolder));

			RootFolder = Path.GetFullPath(rootFolder);
			Directory.CreateDirectory(RootFolder);
			Directory.CreateDirectory(GaragesFolder);

			_json = new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNameCaseInsensitive = true,
				ReadCommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true,
				Converters = { new JsonStringEnumConverter() } // enums as strings ("AWD")
			};
		}

		// ---------- Utility: file naming ----------

		private static string SanitizeFileName(string name)
		{
			foreach (var ch in Path.GetInvalidFileNameChars())
				name = name.Replace(ch, '_');
			return name.Trim();
		}

		private string GarageFilePath(string garageName)
		{
			var fileName = $"{SanitizeFileName(garageName)}.garage.json";
			return Path.Combine(GaragesFolder, fileName);
		}

		// ---------- Lost cars operations ----------

		public async Task<List<Car>> LoadLostCarsAsync()
		{
			if (!File.Exists(LostCarsFilePath))
				return new List<Car>();

			await using var fs = File.OpenRead(LostCarsFilePath);
			var list = await JsonSerializer.DeserializeAsync<List<Car>>(fs, _json).ConfigureAwait(false);
			return list ?? new List<Car>();
		}

		public async Task SaveLostCarsAsync(List<Car> cars)
		{
			Directory.CreateDirectory(RootFolder);
			var temp = LostCarsFilePath + ".tmp";

			await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				await JsonSerializer.SerializeAsync(fs, cars ?? new List<Car>(), _json).ConfigureAwait(false);
			}

			if (File.Exists(LostCarsFilePath))
				File.Replace(temp, LostCarsFilePath, LostCarsFilePath + ".bak");
			else
				File.Move(temp, LostCarsFilePath);
		}

		/// <summary>
		/// Deletes a car (by plate) from the lost cars file.
		/// </summary>
		public async Task<bool> DeleteCarFromLostAsync(string licensePlate)
		{
			licensePlate = (licensePlate ?? "").Trim().ToUpperInvariant();
			if (string.IsNullOrEmpty(licensePlate)) return false;

			var lost = await LoadLostCarsAsync().ConfigureAwait(false);
			var idx = lost.FindIndex(c => c.LicensePlate.Equals(licensePlate, StringComparison.OrdinalIgnoreCase));
			if (idx < 0) return false;

			lost.RemoveAt(idx);
			await SaveLostCarsAsync(lost).ConfigureAwait(false);
			return true;
		}

		// ---------- Garage list / create / load / save ----------

		/// <summary>
		/// Returns (garageName, filePath) pairs for all garages found.
		/// </summary>
		public IEnumerable<(string Name, string Path)> ListGarages()
		{
			if (!Directory.Exists(GaragesFolder))
				yield break;

			foreach (var file in Directory.EnumerateFiles(GaragesFolder, "*.garage.json"))
			{
				var name = Path.GetFileNameWithoutExtension(file).Replace(".garage", "", StringComparison.OrdinalIgnoreCase);
				// Prefer reading the actual Name from file (in case filename differs)
				try
				{
					var json = File.ReadAllText(file);
					var g = JsonSerializer.Deserialize<Garage>(json, _json);
					if (g != null) name = g.Name;
				}
				catch { /* ignore corrupt file names - fallback to filename */ }

				yield return (name, file);
			}
		}

		public async Task<bool> GarageExistsAsync(string garageName)
		{
			var path = GarageFilePath(garageName);
			return File.Exists(path);
		}

		public async Task CreateGarageAsync(Garage garage)
		{
			if (garage is null) throw new ArgumentNullException(nameof(garage));
			var path = GarageFilePath(garage.Name);

			if (File.Exists(path))
				throw new InvalidOperationException($"Garage '{garage.Name}' already exists.");

			await SaveGarageAsync(garage).ConfigureAwait(false);
		}

		public async Task<Garage> LoadGarageAsync(string garageName)
		{
			var path = GarageFilePath(garageName);
			if (!File.Exists(path))
				throw new FileNotFoundException($"Garage file for '{garageName}' not found.", path);

			await using var fs = File.OpenRead(path);
			var garage = await JsonSerializer.DeserializeAsync<Garage>(fs, _json).ConfigureAwait(false)
						 ?? throw new InvalidDataException($"Failed to load garage '{garageName}' (null).");

			return garage;
		}

		public async Task SaveGarageAsync(Garage garage)
		{
			if (garage is null) throw new ArgumentNullException(nameof(garage));
			var path = GarageFilePath(garage.Name);
			Directory.CreateDirectory(GaragesFolder);

			var temp = path + ".tmp";
			await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				await JsonSerializer.SerializeAsync(fs, garage, _json).ConfigureAwait(false);
			}

			if (File.Exists(path))
				File.Replace(temp, path, path + ".bak");
			else
				File.Move(temp, path);
		}

		// ---------- Move cars between garage and lost ----------

		/// <summary>
		/// Removes a car from a garage and moves it into the lost cars file.
		/// Returns true if moved.
		/// </summary>
		public async Task<bool> MoveCarFromGarageToLostAsync(string garageName, string licensePlate)
		{
			if (string.IsNullOrWhiteSpace(garageName)) throw new ArgumentException(nameof(garageName));
			licensePlate = (licensePlate ?? "").Trim().ToUpperInvariant();
			if (licensePlate.Length == 0) return false;

			var garage = await LoadGarageAsync(garageName).ConfigureAwait(false);
			var removed = garage.RemoveCar(licensePlate);
			if (removed == null) return false;

			// Save garage first
			await SaveGarageAsync(garage).ConfigureAwait(false);

			// Add to lost cars (dedupe by plate)
			var lost = await LoadLostCarsAsync().ConfigureAwait(false);
			if (!lost.Any(c => c.LicensePlate.Equals(removed.LicensePlate, StringComparison.OrdinalIgnoreCase)))
				lost.Add(removed);

			await SaveLostCarsAsync(lost).ConfigureAwait(false);
			return true;
		}

		/// <summary>
		/// Moves a car (by plate) from lost cars into the specified garage.
		/// Returns true if moved.
		/// </summary>
		public async Task<bool> MoveCarFromLostToGarageAsync(string garageName, string licensePlate)
		{
			if (string.IsNullOrWhiteSpace(garageName)) throw new ArgumentException(nameof(garageName));
			licensePlate = (licensePlate ?? "").Trim().ToUpperInvariant();
			if (licensePlate.Length == 0) return false;

			var lost = await LoadLostCarsAsync().ConfigureAwait(false);
			var idx = lost.FindIndex(c => c.LicensePlate.Equals(licensePlate, StringComparison.OrdinalIgnoreCase));
			if (idx < 0) return false;

			var car = lost[idx];

			var garage = await LoadGarageAsync(garageName).ConfigureAwait(false);

			// Attempt to add to garage (capacity & duplicate validation inside)
			garage.AddCar(car);
			await SaveGarageAsync(garage).ConfigureAwait(false);

			// Remove from lost and save
			lost.RemoveAt(idx);
			await SaveLostCarsAsync(lost).ConfigureAwait(false);

			return true;
		}

		/// <summary>
		/// Adds a new car directly to a garage (and ensures it's removed from lost if present).
		/// </summary>
		public async Task AddCarToGarageAsync(string garageName, Car car)
		{
			var garage = await LoadGarageAsync(garageName).ConfigureAwait(false);

			// if same plate in lost cars, remove it
			var lost = await LoadLostCarsAsync().ConfigureAwait(false);
			var lostIdx = lost.FindIndex(c => c.LicensePlate.Equals(car.LicensePlate, StringComparison.OrdinalIgnoreCase));
			if (lostIdx >= 0)
			{
				lost.RemoveAt(lostIdx);
				await SaveLostCarsAsync(lost).ConfigureAwait(false);
			}

			garage.AddCar(car);
			await SaveGarageAsync(garage).ConfigureAwait(false);
		}
	}
}