using WebWasm.Models;

namespace WebWasm.Helpers;

/// <summary>Colour tone of a status badge (design code, section 7). The only place that maps statuses to colours.</summary>
public enum Tone
{
	Neutral,
	Info,
	Warning,
	Success,
	Danger,
}

public static class StatusTone
{
	/// <summary>CSS classes of a badge: <c>badge badge-success</c>.</summary>
	public static string Badge(this Tone tone) => tone switch
	{
		Tone.Info => "badge badge-info",
		Tone.Warning => "badge badge-warning",
		Tone.Success => "badge badge-success",
		Tone.Danger => "badge badge-danger",
		_ => "badge badge-neutral",
	};

	public static Tone Of(OrderStatus status) => status switch
	{
		OrderStatus.Active or OrderStatus.PaymentInProgress or OrderStatus.Weighted => Tone.Info,
		OrderStatus.WaitingApprove or OrderStatus.PaymentPending => Tone.Warning,
		OrderStatus.Completed => Tone.Success,
		OrderStatus.CorruptedPayment or OrderStatus.Cancelled => Tone.Danger,
		_ => Tone.Neutral,
	};

	public static Tone Of(PaymentTransactionType type) => type switch
	{
		PaymentTransactionType.Authorized or PaymentTransactionType.Pending or PaymentTransactionType.Verification => Tone.Info,
		PaymentTransactionType.PartialRefunded or PaymentTransactionType.PartialVoided or PaymentTransactionType.Recurrent => Tone.Warning,
		PaymentTransactionType.Completed => Tone.Success,
		PaymentTransactionType.Declined or PaymentTransactionType.Failed or PaymentTransactionType.Voided
			or PaymentTransactionType.Refunded or PaymentTransactionType.Blocked or PaymentTransactionType.Cancelled => Tone.Danger,
		_ => Tone.Neutral,
	};

	/// <summary>Server log level as the server writes it: Error, Fatal, Warning, Information…</summary>
	public static Tone OfLogLevel(string? level) => level?.ToLowerInvariant() switch
	{
		"error" or "fatal" or "critical" => Tone.Danger,
		"warning" => Tone.Warning,
		_ => Tone.Neutral,
	};

	/// <summary>Yes/no states: active, verified, enabled.</summary>
	public static Tone Of(bool positive) => positive ? Tone.Success : Tone.Neutral;
}
