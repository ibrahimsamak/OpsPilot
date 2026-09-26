# Fraud review procedure

Owner: Risk team. Applies to orders whose payment failed with `fraud_suspected`.

## Rules for support and operations

- **Never retry** the payment of a `fraud_suspected` order, even if the customer insists.
- Never tell the customer the risk score, and do not use the words "fraud" or "risk engine" with the customer.
- Do not reserve stock for the order.
- Do not cancel the order yourself; the Risk team decides.

## What to tell the customer

"Your payment needs an additional verification step. Our team will contact you by email within one business day." Nothing more.

## Routing

1. Add the order to the Risk review queue with the order id and the time of the failed attempt.
2. The Risk team reviews within **2 business hours** during business hours (9:00 to 18:00 Eastern).
3. The Risk team either approves (the customer receives a new secure payment link) or rejects (the order is cancelled and the customer receives template RISK-02).

## Escalation

If the customer is a known B2B account or the order value is over $2,000, also notify the Risk team lead directly.
