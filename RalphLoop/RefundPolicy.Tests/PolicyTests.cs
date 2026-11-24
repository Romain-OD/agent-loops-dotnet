using RefundPolicy;

public class PolicyTests
{
    [Fact]
    public void Within_30_days_is_a_refund() =>
        Assert.Equal(Decision.Refund, Policy.Decide(new(Price: 40, DaysSinceDelivery: 12, Damaged: false, Electronics: false, Opened: false)));

    [Fact]
    public void Day_30_is_still_a_refund() =>
        Assert.Equal(Decision.Refund, Policy.Decide(new(40, 30, false, false, false)));

    [Fact]
    public void After_30_days_is_store_credit() =>
        Assert.Equal(Decision.StoreCredit, Policy.Decide(new(40, 31, false, false, false)));

    [Fact]
    public void Damaged_within_90_days_is_a_refund() =>
        Assert.Equal(Decision.Refund, Policy.Decide(new(40, 75, true, false, false)));

    [Fact]
    public void Damaged_after_90_days_is_store_credit() =>
        Assert.Equal(Decision.StoreCredit, Policy.Decide(new(40, 91, true, false, false)));

    [Fact]
    public void Opened_electronics_after_14_days_are_refused() =>
        Assert.Equal(Decision.Refused, Policy.Decide(new(120, 20, false, true, true)));

    [Fact]
    public void Opened_electronics_that_arrived_damaged_are_refunded() =>
        Assert.Equal(Decision.Refund, Policy.Decide(new(120, 20, true, true, true)));

    [Fact]
    public void Over_500_always_goes_to_manual_review() =>
        Assert.Equal(Decision.ManualReview, Policy.Decide(new(899, 3, true, false, false)));
}
