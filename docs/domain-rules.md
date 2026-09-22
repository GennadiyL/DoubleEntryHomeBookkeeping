# Domain rules

## Parent references for aggregate children

`TemplateEntry` and `TransactionEntry` intentionally keep both their parent navigation property and parent foreign-key property.

The application needs to navigate from either child entity to its parent without resolving the parent separately. This is a deliberate performance optimization and overrides the general rule that aggregate-owned children omit parent references.

This exception applies only to `TemplateEntry` and `TransactionEntry`. Their parent navigation and foreign-key values must always identify the same parent.
