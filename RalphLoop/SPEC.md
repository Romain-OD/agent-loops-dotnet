# Refund policy

`Policy.Decide(RefundRequest)` returns one `Decision`. Apply the rules in this order; the first that matches wins.

1. Price over 500: `ManualReview`, whatever the other rules say.
2. Damaged on arrival, 90 days or less since delivery: `Refund`.
3. Opened electronics, more than 14 days since delivery: `Refused`.
4. 30 days or less since delivery: `Refund`.
5. Anything else: `StoreCredit`.
