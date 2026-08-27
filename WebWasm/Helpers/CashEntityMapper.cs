using WebWasm.Models;

namespace WebWasm.Helpers;

public static class CashEntityMapper
{
	extension(Vehicle[] vehicles)
	{
		public Vehicle[] MapDriversToVehicles((Company Company, Driver Driver)[] driverWithCompany, User[] users)
		{
			var driverDict = driverWithCompany.ToDictionary(d => d.Driver.Id);
			var userDict = users.ToDictionary(u => u.UserInfo.Id);

			return [.. vehicles.Select(vehicle =>
			{
				if (vehicle.DriverId != Guid.Empty && driverDict.TryGetValue(vehicle.DriverId, out var driverWithCompany))
				{
					// Enrich driver with phone from User/UserInfo
					var (company, driver) = driverWithCompany;
					var enrichedDriver = driver;
					if (driver.UserInfo?.Id != Guid.Empty &&
						driver.UserInfo != null &&
						userDict.TryGetValue(driver.UserInfo.Id, out var user))
					{
						enrichedDriver = new Driver(
								driver.Id,
								driver.Photo,
								[vehicle],
								new UserInfo(
									driver.UserInfo.Id,
									user.UserInfo.FirstName,
									user.UserInfo.MiddleName,
									user.UserInfo.LastName,
									user.UserInfo.MobilePhone,
									user.UserInfo.IsActive,
									company
								)
							);
					}

					// Create new vehicle instance with mapped driver
					return new Vehicle(
						vehicle.Id,
						vehicle.DriverId,
						vehicle.CompanyId,
						vehicle.Model,
						vehicle.RegistrationNumber,
						vehicle.VehicleWeight,
						vehicle.LoadCapacity,
						vehicle.Photo,
						enrichedDriver
					);
				}
				return vehicle;
			})];
		}
	}
}
