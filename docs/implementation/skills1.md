# Project-agnostic rules extracted from the synchronization discussion

These are reusable rules extracted from this chat. Bookkeeping-specific and synchronization-specific business rules are excluded.

## Code style

1. Use explicit constructors instead of primary constructors.
2. Keep functions short—normally around **10–15 lines**. Treat this as a guideline, not a strict limit.
3. Follow **SLAP**: statements within a function should operate at the same abstraction level.
4. Use multiline conditional statements with braces. Follow the solution’s `.editorconfig`.

## Architecture and contracts

6. Keep domain-internal operations and their interfaces inside the domain’s implementation project.
10. Keep contracts and DTOs free of business behavior. Separate operations from their state.

## Shared services and state

14. Production consumers use `ISharedContext` through constructor injection. Access files and directories through `IFileService` and `IDirectoryService`; tests may use static filesystem APIs.
15. Helper functions receive the narrow functionality they need—for example, `IFileService`, not the whole `ISharedContext`.
16. Use `ILogService` through `ISharedContext` instead of introducing a separate logging abstraction in consumers.
17. Separate stateless factories and operations from stateful classes which are created. Factories read configuration and create instances containing the selected settings.

## Infrastructure and resource ownership

20. Authentication tokens belong to infrastructure. Validate them before invoking business services; pass business identity/context instead of raw tokens.
21. Store credentials through an appropriate platform abstraction. Provider implementations retrieve them when needed.
