using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Support;

public record Order(string Id, string Item, decimal Price, DateOnly Delivered, bool Damaged);

public static class SupportTools
{
    public const string Instructions = "You are a support agent for an online shop. Use the tools. Refund only when the order is eligible.";

    static readonly Dictionary<string, Order> Orders = new()
    {
        ["1042"] = new("1042", "Ceramic mug", 24.90m, new DateOnly(2026, 11, 20), Damaged: true),
        ["1043"] = new("1043", "Headphones", 189m, new DateOnly(2026, 10, 1), Damaged: false),
    };

    public static bool RefundServiceDown { get; set; }

    [Description("Gets an order: the item, what the customer paid, when it was delivered.")]
    public static Order GetOrder(string orderId) =>
        Orders.TryGetValue(orderId, out var order) ? order : throw new KeyNotFoundException($"No order {orderId}");

    [Description("Tracks where a shipment is.")]
    public static string TrackShipment(string orderId) => $"Order {orderId}: delivered, signed for at the front door.";

    [Description("Checks refund eligibility.")]
    public static string CheckRefundEligibility(string orderId, string reason)
    {
        var order = GetOrder(orderId);
        var days = DateOnly.FromDateTime(DateTime.Today).DayNumber - order.Delivered.DayNumber;
        return order.Damaged || days <= 30 ? $"Eligible: full refund of {order.Price}" : "Not eligible: store credit only";
    }

    [Description("Issues a refund that was approved.")]
    public static string IssueRefund(string orderId) => RefundServiceDown
        ? throw new InvalidOperationException("Refund service unavailable (503)")
        : $"Refund of {GetOrder(orderId).Price} issued for order {orderId}";

    public static IList<AITool> All() =>
    [
        AIFunctionFactory.Create(GetOrder),
        AIFunctionFactory.Create(TrackShipment),
        AIFunctionFactory.Create(CheckRefundEligibility),
        AIFunctionFactory.Create(IssueRefund),
    ];
}
