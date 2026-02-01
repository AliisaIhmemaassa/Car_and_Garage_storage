// File: Garage.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Car_project;

namespace Vehicle
{
	/// <summary>
	/// Represents a garage that stores cars and supports common operations.
	/// </summary>
	public sealed class Garage
	{
		/// <summary>Display name of the garage (also used as file name base).</summary>
		public string Name { get; }

		/// <summary>Street address of the garage.</summary>
		public string Address { get; }

		/// <summary>Owner of the garage.</summary>
		public string Owner { get; }

		/// <summary>Maximum number of cars the garage can hold (0 means unlimited).</summary>
		public int Capacity { get; }

		/// <summary>True if the garage also operates as a workshop.</summary>
		public bool IsWorkshop { get; }

		/// <summary>Cars currently in this garage. Public for serialization; modify via Add/Remove methods.</summary>
		[JsonInclude]
		public List<Car> Cars { get; private set; } = new List<Car>();

		/// <summary>Current number of cars in the garage.</summary>
		[JsonIgnore]
		public int Count => Cars.Count;

		[JsonConstructor]
		public Garage(string name, string address, string owner, int capacity, bool isWorkshop)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must not be empty.", nameof(name));
			if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address must not be empty.", nameof(address));
			if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Owner must not be empty.", nameof(owner));
			if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");

			Name = name.Trim();
			Address = address.Trim();
			Owner = owner.Trim();
			Capacity = capacity;
			IsWorkshop = isWorkshop;
		}

		/// <summary>
		/// Adds a car to the garage. Enforces capacity and unique license plates.
		/// </summary>
		public void AddCar(Car car)
		{
			if (car is null) throw new ArgumentNullException(nameof(car));

			if (Capacity > 0 && Count >= Capacity)
				throw new InvalidOperationException($"Garage '{Name}' is full (capacity {Capacity}).");

			if (Cars.Any(c => c.LicensePlate.Equals(car.LicensePlate, StringComparison.OrdinalIgnoreCase)))
				throw new InvalidOperationException($"Car with plate '{car.LicensePlate}' already in '{Name}'.");

			Cars.Add(car);
		}

		/// <summary>
		/// Removes a car by license plate. Returns the removed car or null if not found.
		/// </summary>
		public Car? RemoveCar(string licensePlate)
		{
			if (string.IsNullOrWhiteSpace(licensePlate)) return null;

			var idx = Cars.FindIndex(c =>
				c.LicensePlate.Equals(licensePlate.Trim(), StringComparison.OrdinalIgnoreCase));

			if (idx < 0) return null;

			var removed = Cars[idx];
			Cars.RemoveAt(idx);
			return removed;
		}

		/// <summary>Gets a car by license plate, or null if not found.</summary>
		public Car? GetCar(string licensePlate)
		{
			if (string.IsNullOrWhiteSpace(licensePlate)) return null;

			return Cars.FirstOrDefault(c =>
				c.LicensePlate.Equals(licensePlate.Trim(), StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>Returns a snapshot of all cars currently in the garage.</summary>
		public IReadOnlyList<Car> GetAllCars() => Cars.AsReadOnly();

		public override string ToString()
			=> $"{Name} | {Address} | Owner={Owner} | Capacity={Capacity} | Workshop={IsWorkshop} | Cars={Count}";
	}
}