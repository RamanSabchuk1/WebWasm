namespace WebWasm.Models;

public record Suggestion(Guid Id, Guid UserInfoId, string Name, Dictionary<string, string> Data, DateTime Created, DateTime? Applied);
public record DeviceToken(Guid Id, Guid UserInfoId, string Token, string Device, Dictionary<string, string>? AdditionalData);
public record BankAccount(string BankNumber, string BIC);
public record Location(double Longitude, double Latitude);
public record DriverSlot(Guid Id, Guid DriverId, TimeOnly StartTime, TimeOnly EndTime, DateOnly WorkingDay);
public record CalculationInfo(Guid Id, decimal MaterialCost, DeliveryInfo[] DeliveryInfo, decimal[] CommissionPercentages, decimal Commission, decimal TotalCost);
public record DeliveryInfo(Guid DeliveryId, decimal Cost, uint Weight);
public record CreditCardInfo(Guid Id, Guid UserInfoId, string MaskedCard, DateTime ExpirationDate, DateTime UnbindAt);
public record Role(Guid Id, RoleType Name, ICollection<string> Scopes);
public record User(Guid Id, string Login, bool UserVerified, bool IsActive, UserInfo UserInfo, ICollection<RoleType> Roles);
public record Company(Guid Id, string LegalName, string LegalAddress, string CorporateEmail, bool EmailVerified, bool IsActive, string UNP, CompanyInfo? CompanyInfo, BankAccount? BankAccount, ICollection<Vehicle> Vehicles, ICollection<Producer> Producers);
public record CompanyInfo(Guid Id, Guid RegionId, Location Location, string LegalType, TimeSpan StartWorkingHours, TimeSpan EndWorkingHours, string Photo, ICollection<string> ContactPhones);
public record Delivery(Guid Id, uint NetoWeight, double GrossWeight, double EstimatedDistance, TimeSpan EstimatedTime, string State, decimal DeliveryCost, Driver? Driver, ICollection<Document> Documents);
public record Document(Guid Id, string FileName, string PathOrId);
public record Driver(Guid Id, string Photo, Vehicle? Vehicle, UserInfo? UserInfo);
public record Level(Guid Id, byte CalculationAlgorithm, LevelType Type, ICollection<Triangle> Triangles, LevelInfo? Info);
public record LevelInfo(Guid Id, Guid LevelId, Dictionary<uint, PriceInfo> Info);
public record LoadingPlace(Guid Id, string Name, Location Location, decimal Cost, int Volume, MaterialType? MaterialType, Producer? Producer);
public record MaterialType(Guid Id, Guid? ParentId, string Name, string Description, string Solidity, string Photo);
public record Order(Guid Id, string Name, string Address, Guid RegionLevelId, double TotalWeight, double TotalKm, decimal Cost, DateTime PreferredDeliveryTime, TimeSpan Duration, Location Location, LoadingPlace? LoadingPlace, string State, OrderStatus Status, UserInfo? UserInfo, DateTime Created, ICollection<Delivery> Deliveries, ICollection<Transaction> Transactions, ICollection<Document> Documents);
public record Transaction(Guid Id, decimal Amount, DateTime Date, PaymentMethodType PaymentMethod, PaymentTransactionType Status);
public record Producer(Guid Id, Company? Company, string Name, ICollection<ProducerWorkingTime> ProducerWorkingTime, ICollection<LoadingPlace> LoadingPlaces);
public record Region(Guid Id, string Name, ICollection<Level> Levels);
public record Triangle(Location Point1, Location Point2, Location Point3);
public record UserInfo(Guid Id, string FirstName, string MiddleName, string LastName, string MobilePhone, Company? Company);
public record Vehicle(Guid Id, Guid DriverId, Guid CompanyId, string Model, string RegistrationNumber, uint VehicleWeight, uint LoadCapacity, string Photo, Driver? Driver) { public Driver? Driver { get; set; } = Driver; }
public record ActivityRecord(string Description, DateTime Date);

public record struct ProducerWorkingTime(TimeOnly StartLoadingHours, TimeOnly EndLoadingHours, TimeOnly StartWorkingHours, TimeOnly EndWorkingHours, DayOfWeek DayOfWeek);
public record struct PriceInfo(decimal MinPrice, decimal PricePerHour, decimal PricePerKm, decimal MaxPrice);

public record MutateLoadingPlace(string Name, Location Location, Guid MaterialTypeId, decimal Cost, int Volume);
public record CalculationInfoRequest(ICollection<Guid> OrderIds);
public record OrderCostRequest(Guid LoadingPlaceId, TimeSpan Duration, decimal Distance, int[] Weights);
public record OrderCostResponse(Guid LoadingPlaceId, decimal Cost);
public record MaterialTypeInfo(Guid? ParentId, string Name, string Description, string Solidity, string Photo);
public record UpdateProducer(ICollection<ProducerWorkingTime> ProducerWorkingTime);
public record UpdateRegion(string Name);
public record CreateCompany(Location Location, BankAccount BankAccount, TimeSpan StartWorkingHours, TimeSpan EndWorkingHours, string Photo, ICollection<string> ContactPhones, string LegalName, string LegalAddress, string CorporateEmail, string UNP, string LegalType);
public record CreateDriver(string Photo, string FirstName, string MiddleName, string LastName, string MobilePhone, Guid? CompanyId);
public record CreateDriverSlot(TimeOnly StartTime, TimeOnly EndTime, DateOnly WorkingDay);
public record CreateProducer(ICollection<ProducerWorkingTime> ProducerWorkingTime, string Name);
public record CreateLevel(LevelType Type, byte CalculationAlgorithm, ICollection<Location> Points, Dictionary<uint, PriceInfo> Info);
public record CreateUser(Guid CompanyId, string FirstName, string MiddleName, string LastName, string MobilePhone, RoleType[] Roles);
public record CreateVehicle(string Model, string RegistrationNumber, uint VehicleWeight, uint LoadCapacity, string Photo, Guid DriverId);
public record CreateRegion(string Name);
public record SetUserNames(string FirstName, string? MiddleName, string LastName)
{
	public string FirstName { get; set; } = FirstName;
	public string? MiddleName { get; set; } = MiddleName;
	public string LastName { get; set; } = LastName;
}

public enum LevelType
{
	Neighborhood,
	City
}

public enum RoleType
{
	Driver,
	TransportManager,
	OperatorProducer,
	AccountAdmin,
	SuperAdmin
}

public enum OrderStatus
{
	Draft,
	WaitingApprove,
	PaymentPending,
	Active,
	Completed,
	CorruptedPayment,
	Cancelled,
	Archived,
	Deleted
}

public enum PaymentMethodType
{
	None,
	Card,
	BindCard,
	TokenizedCard,
}

public enum PaymentTransactionType
{
	None = 0,
	Completed = 1,
	Declined = 2,
	Authorized = 4,
	PartialRefunded = 5,
	Voided = 7,
	Failed = 8,
	PartialVoided = 9,
	Recurrent = 10,
	Refunded = 11,
	Blocked = 12,
	Verification = 23,
	Pending = 24,
	Cancelled = 25
}