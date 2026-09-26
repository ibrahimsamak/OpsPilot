# Inventory reservation rules

Reserving stock takes units out of the available count so they cannot be sold to someone else. The Ops API tracks `onHand`, `reserved` and `available = onHand - reserved` per SKU.

## Who can reserve

Only users with the `ops.operator` or `ops.admin` role. Viewers can check stock but cannot reserve.

## Limits

- Maximum **50 units** per reservation.
- A reservation cannot exceed the available quantity; the Ops API rejects it.
- Every reservation needs a reason, and should reference an order id when it is for a specific order.

## When to reserve

- An order's payment failed for a reason that can be retried (for example `gateway_timeout`, or `insufficient_funds` with customer confirmation) and the item is low stock.
- During a payment gateway incident, for affected orders with low-stock items.
- For B2B quotes approved by sales, for up to 7 days.

Do not reserve stock for orders with `fraud_suspected`; those orders are on hold until the Risk team decides.

## Low stock

A SKU is **low stock** when 5 or fewer units are available. When you reserve a low-stock SKU, notify purchasing in the #inventory channel.

## Expiry

Reservations for failed payments expire after **48 hours**. If the payment has not succeeded by then, release the reservation and cancel the order after contacting the customer.
