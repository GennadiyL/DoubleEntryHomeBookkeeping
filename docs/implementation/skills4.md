# Anemic Business Models and Stateless Operations

- Keep business models and auxiliary objects data-only.
- Place complex behavior in services or operations exposed through interfaces and resolved through dependency injection.
- Operations retain dependencies only; pass invocation-specific data through method parameters.
- Do not hide behavior in delegates stored inside supposedly data-only POCOs.
- Do not instantiate complex behavior-bearing objects with `new` inside a service.
- Simple derived properties are allowed.
- UI components in the entry point are exempt from these restrictions.

# Eliminating Magic Names and Magic Numbers

- Use `nameof` when a string represents a C# identifier and the required format permits it.
- Reference properties on the semantically correct type, even when properties on different types have identical names.
- Represent business limits and special values as meaningfully named constants.
- Do not automatically extend the `nameof` rule to SQL identifiers.
- Do not change an external contract solely to enable the use of `nameof`.
