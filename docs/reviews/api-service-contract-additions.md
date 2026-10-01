# API/BFF service contract additions

Based on TRD 0.104. Scope: declarations only; no implementation, registration or wire protocol.

| Interface | TRD operations |
| --- | --- |
| IStartupService | OP-001–004 |
| ISynchronizationService | OP-043–049 |
| IDiagnosticsService | OP-050–052 |
| IReportsService | OP-037–040 |

## Concrete declaration choices

- CreateBooks and OpenBooks have separate inputs. Creation includes immutable AmountPrecision/RatePrecision (defaults 2/4; range 0–4). RequestId retains the TRD proposed setup-attempt identity.
- Synchronize is the local application-facing BFF action. Its input supplies the trigger; the implementation must obtain owner/registration, known revision and priority from current context and durably capture pending changes. No arbitrary object, persistent model or invented cloud change schema is exposed. This intentionally does not implement DTO-019 as a cloud upload contract. TQ-04 still requires a separate typed delta/ordering protocol before cloud synchronization implementation.
- SynchronizeInfo includes status and an optional transfer for the unresolved no-change case. The state must determine completion; null Transfer alone is not success.
- Download returns metadata plus a caller-owned Stream. A transport adapter streams bytes outside JSON; activation and acknowledgement remain separate.
- Expiry replacement returns the new LocalDatasetKey alongside its transfer, allowing the caller to retain the replacement registration.
- GetLatestSyncReport returns null when no session report exists. Diagnostic OperationId is named OperationKey because it is a string correlation key.
- Reports use typed selection/definition/row records. CalculateReport carries the current device TimeZoneKey; platform identifier mapping remains unresolved. Report JSON persistence format is not selected. SaveDefinition uses nullable Id for create/update, matching the logical TRD operation.
- Existing service contracts and authoritative BRD/TRD text are unchanged. These declaration choices do not resolve security, durable sync protocol, report serialization, rename-only selection preservation or other open technical questions.
