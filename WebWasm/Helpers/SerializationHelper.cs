using System.Text.Json;
using System.Text.Json.Serialization;
using WebWasm.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Helpers;

public static class SerializationHelper
{
	public static JsonSerializerOptions SerializerOptions() => new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		WriteIndented = false,
		TypeInfoResolver = AppJsonSerializerContext.Default
	};
}

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	WriteIndented = false,
	UseStringEnumConverter = true)]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Company))]
[JsonSerializable(typeof(Company[]))]
[JsonSerializable(typeof(DeviceToken))]
[JsonSerializable(typeof(DeviceToken[]))]
[JsonSerializable(typeof(ActivityRecord))]
[JsonSerializable(typeof(ActivityRecord[]))]
[JsonSerializable(typeof(Log))]
[JsonSerializable(typeof(Log[]))]
[JsonSerializable(typeof(MaterialType))]
[JsonSerializable(typeof(MaterialType[]))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(User[]))]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(UserInfo[]))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(Order[]))]
[JsonSerializable(typeof(CreditCardInfo))]
[JsonSerializable(typeof(CreditCardInfo[]))]
[JsonSerializable(typeof(Producer))]
[JsonSerializable(typeof(Producer[]))]
[JsonSerializable(typeof(Region))]
[JsonSerializable(typeof(Region[]))]
[JsonSerializable(typeof(Suggestion))]
[JsonSerializable(typeof(Suggestion[]))]
[JsonSerializable(typeof(Role))]
[JsonSerializable(typeof(Role[]))]
[JsonSerializable(typeof(Vehicle))]
[JsonSerializable(typeof(Vehicle[]))]
[JsonSerializable(typeof(Driver))]
[JsonSerializable(typeof(Driver[]))]
[JsonSerializable(typeof(DriverSlot))]
[JsonSerializable(typeof(DriverSlot[]))]
[JsonSerializable(typeof(MaterialTypeInfo))]
[JsonSerializable(typeof(CalculationInfoRequest))]
[JsonSerializable(typeof(CalculationInfo))]
[JsonSerializable(typeof(CalculationInfo[]))]
[JsonSerializable(typeof(MutateLevel))]
[JsonSerializable(typeof(Level))]
[JsonSerializable(typeof(Level[]))]
[JsonSerializable(typeof(CreateUser))]
[JsonSerializable(typeof(CreateCompany))]
[JsonSerializable(typeof(CreateProducer))]
[JsonSerializable(typeof(CreateVehicle))]
[JsonSerializable(typeof(CreateRegion))]
[JsonSerializable(typeof(CreateDriver))]
[JsonSerializable(typeof(CreateDriverSlot))]
[JsonSerializable(typeof(CreateDriverSlot[]))]
[JsonSerializable(typeof(MutateLoadingPlace))]
[JsonSerializable(typeof(UpdateProducer))]
[JsonSerializable(typeof(UpdateRegion))]
[JsonSerializable(typeof(SetUserNames))]
[JsonSerializable(typeof(OrderCostRequest))]
[JsonSerializable(typeof(OrderCostRequest[]))]
[JsonSerializable(typeof(OrderCostResponse))]
[JsonSerializable(typeof(OrderCostResponse[]))]
[JsonSerializable(typeof(Location))]
[JsonSerializable(typeof(BankAccount))]
[JsonSerializable(typeof(PriceInfo))]
[JsonSerializable(typeof(Triangle))]
[JsonSerializable(typeof(LoadingPlace))]
[JsonSerializable(typeof(Delivery))]
[JsonSerializable(typeof(Transaction))]
[JsonSerializable(typeof(Transaction[]))]
[JsonSerializable(typeof(CompanyInfo))]
[JsonSerializable(typeof(UpdateCompany))]
[JsonSerializable(typeof(LevelInfo))]
[JsonSerializable(typeof(DeliveryInfo))]
[JsonSerializable(typeof(CashedInfo))]
[JsonSerializable(typeof(DeviceTokenInfo))]
[JsonSerializable(typeof(ProducerWorkingTime))]
[JsonSerializable(typeof(Login.LoginModel))]
[JsonSerializable(typeof(Login.TokenResponse))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<uint, PriceInfo>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
