namespace RefundPolicy;

public enum Decision { Refund, StoreCredit, Refused, ManualReview }

public record RefundRequest(decimal Price, int DaysSinceDelivery, bool Damaged, bool Electronics, bool Opened);

public static class Policy
{
    public static Decision Decide(RefundRequest request) => throw new NotImplementedException();
}
