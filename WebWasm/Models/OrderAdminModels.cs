namespace WebWasm.Models;

/// <summary>
/// SA-правки заказа (S5.2; контракты backend S4.2/S4.3, camelCase JSON).
/// SetOrderCalculationRequest: хотя бы одно поле обязательно; TotalCost пересчитывается на сервере,
/// правка пишется в audit (pricingAudit). SetPreferredDeliveryTimeRequest: время — регион-локальное
/// (конвенция CreateOrder); 409 = у назначенного водителя нет слота на новую дату.
/// </summary>
public record SetOrderCalculationRequest(decimal? MaterialCost, decimal? Commission);

/// <summary>Тело PUT /admin/orders/{id}/preferred-delivery-time (S5.2/S4.3).</summary>
public record SetPreferredDeliveryTimeRequest(DateTime PreferredDeliveryTime);
