# Payment failure codes

ShopCo takes card payments through the payment gateway (provider `stripe-sim` in the Ops API). Every failed attempt stores a `failureCode` on the payment. This page is the source of truth for what each code means, whether we may retry, and what to tell the customer.

## Summary

| Code | Meaning | Retry allowed? | Owner |
|---|---|---|---|
| `card_declined` | The card issuer refused the charge | No. The customer must use another card or call their bank | Support |
| `insufficient_funds` | Not enough balance or credit | Only after the customer confirms funds are available | Support |
| `expired_card` | The card's expiry date has passed | No. A new card is required | Support |
| `gateway_timeout` | The gateway did not answer within 30 seconds | Yes, after the idempotency check | Operations on-call |
| `fraud_suspected` | The risk engine blocked the charge | Never | Risk team |

## Attempt limit

An order may have at most **3 payment attempts** in total, whatever the code. After the third failure, ask the customer for a new payment method and cancel the order if none arrives within 48 hours.

## card_declined

The issuing bank refused the transaction without a specific reason (often "do not honor"). ShopCo cannot see why. Do not retry the same card: repeated declines can get the card blocked by the bank.

Tell the customer: "Your bank declined the payment. Please try a different card or contact your bank." Do not suggest the bank made a mistake.

## insufficient_funds

The card does not have enough balance or available credit. A retry is allowed only after the customer confirms in writing (chat or email) that funds are now available. Record the confirmation in the order notes before retrying.

## expired_card

The card on file has expired. Retrying will always fail. Send the customer the "update payment method" link from the order page.

## gateway_timeout

The payment gateway did not respond within 30 seconds. The gateway only captures funds after it responds, so a timeout means **the customer was not charged**. It is a ShopCo-side or gateway-side problem, not the customer's.

Before retrying:
1. Open the order and confirm there is **no** payment with status `Succeeded` (idempotency check).
2. Confirm the order has fewer than 3 attempts.
3. Retry once. If the retry also times out, stop and follow the payment gateway timeout runbook.

If several orders show `gateway_timeout` at the same time, treat it as an incident and follow the payment gateway timeout runbook.

## fraud_suspected

The risk engine blocked the charge because its risk score was above the threshold. **Never retry**, and never tell the customer the risk score or use the words "fraud" or "risk engine" with them. Route the order to the Risk team using the fraud review procedure.
