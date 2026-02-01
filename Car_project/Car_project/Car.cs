// File: Car.cs
using System;

namespace Vehicle
{
	/// <summary>
	/// Represents drivetrain configuration.
	/// </summary>
	public enum Drivetrain
	{
		Unknown = 0,
		FWD = 1, // Front-Wheel Drive
		RWD = 2, // Rear-Wheel Drive
		AWD = 3  // All-Wheel Drive
	}

	/// <summary>
	/// Represents a car with core specifications.
	/// </summary>
	public sealed class Car
	{
		public string LicensePlate { get; }

		public string Model { get; }

		public int Year { get; }

		public Drivetrain Drivetrain { get; }

		public double TankSize { get; }

		public double EngineSize { get; }

		public int Horsepower { get; }

		public int TopSpeed { get; }

		public bool EngineInFront { get; }

		public string Owner { get; }

		public Car(
			string licensePlate,
			string model,
			int year,
			Drivetrain drivetrain,
			double tankSize,
			double engineSize,
			int horsepower,
			int topSpeed,
			bool engineInFront,
			string owner)
		{
			if (string.IsNullOrWhiteSpace(licensePlate))
				throw new ArgumentException("License plate must not be empty.", nameof(licensePlate));
			if (string.IsNullOrWhiteSpace(model))
				throw new ArgumentException("Model must not be empty.", nameof(model));
			if (string.IsNullOrWhiteSpace(owner))
				throw new ArgumentException("Owner must not be empty.", nameof(owner));

			var currentYear = DateTime.UtcNow.Year + 1; // allow one model year ahead
			if (year < 1886 || year > currentYear)
				throw new ArgumentOutOfRangeException(nameof(year), $"Year must be between 1886 and {currentYear}.");

			if (drivetrain == Drivetrain.Unknown)
				throw new ArgumentException("Drivetrain must be FWD, RWD, or AWD.", nameof(drivetrain));
			if (tankSize <= 0) throw new ArgumentOutOfRangeException(nameof(tankSize));
			if (engineSize <= 0) throw new ArgumentOutOfRangeException(nameof(engineSize));
			if (horsepower <= 0) throw new ArgumentOutOfRangeException(nameof(horsepower));
			if (topSpeed <= 0) throw new ArgumentOutOfRangeException(nameof(topSpeed));

			LicensePlate = licensePlate.Trim().ToUpperInvariant();
			Model = model.Trim();
			Year = year;
			Drivetrain = drivetrain;
			TankSize = tankSize;
			EngineSize = engineSize;
			Horsepower = horsepower;
			TopSpeed = topSpeed;
			EngineInFront = engineInFront;
			Owner = owner.Trim();
		}

		public override string ToString()
		{
			return $"{LicensePlate} | {Model} ({Year}) | {Drivetrain} | {EngineSize:0.0}L | {Horsepower} hp | " +
				   $"{TopSpeed} km/h | Tank {TankSize:0.0} L | EngineInFront={EngineInFront} | Owner={Owner}";
		}
	}
}