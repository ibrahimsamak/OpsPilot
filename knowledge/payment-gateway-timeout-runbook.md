# Runbook: payment gateway timeouts

Owner: Operations on-call. Applies when payments fail with `gateway_timeout`.

## Detect

Symptoms: several orders move to `PaymentFailed` with failure code `gateway_timeout` within a short window; customers report the payment "spinning" at checkout. Check the payment failures report for the last 1 to 2 hours.

## Severity

- **Sev3**: fewer than 5 `gateway_timeout` failures in 60 minutes. Handle during business hours and retry affected orders individually.
- **Sev2**: 5 or more `gateway_timeout` failures in 60 minutes, or more than 5% of all payment attempts in 15 minutes. Page the payments on-call engineer and open an incident channel.
- **Sev1**: checkout completely unavailable for more than 10 minutes. Page the incident commander.

## Mitigate

1. Check the gateway provider's status page.
2. If the provider reports an outage, post the pre-approved storefront banner "Payments are delayed; your order is saved".
3. Do not bulk-retry orders while the gateway is still timing out; it adds load and makes the outage worse.
4. When the gateway has recovered (three consecutive successful test payments), retry affected orders one at a time, using the idempotency check from the payment failure codes page.

## Protect stock

For low-stock items in affected orders, reserve the stock for the order while the payment is retried so the item is not sold to someone else. Use the reason "hold for order <id> during gateway incident". Reservations follow the inventory reservation rules.

## Communicate

- Customers with a failed order: send template PAY-DELAY-01 within 2 hours. It says they were not charged and that we will retry.
- Internal: update the incident channel every 30 minutes until resolved.

## Close

Close the incident when no new `gateway_timeout` failures occur for 30 minutes and every affected order is either paid or the customer has been contacted. Write a post-incident review within 3 business days for Sev1 and Sev2.
