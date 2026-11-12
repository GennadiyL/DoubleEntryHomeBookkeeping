# Bugs

Current contract: TRD 0.242, including the 2026-11-12 Master consistency ownership clarification. Earlier snapshot-only Local cleanup and ModificationType.Deletion decisions are superseded.

## BUG-001 - Currency aggregate deletion and restoration

- Status: Fixed; regression verification recorded in the review report.
- Severity: P1.
- Origin: [Master and Synchronization review](reviews/2026-11-11-sync/review.md).

Currency owns all rates, including its initial rate and every dated rate. Local Delete soft-deletes Currency and every retained rate. Restore before capture clears their deletion markers, preserving identities, rate values, and prior Content/Order flags. Independent catalog normalization/re-append still records actual Order changes.

Capture includes all deleted aggregate rows in Deleted with full content, even new rows with null wire EditRevision. Acceptance physically removes the aggregate only after securing its JSON in the same transaction. Failure rolls both JSON and deletion back. Once capture commits, Local restore is unavailable; immutable batches retain the old changes.

Master restoration of a retained deleted Currency restores all retained rates, including previously individually deleted rates. This also applies to restoration required by a live dependency. Unchanged live rates also restore their deleted Currency; this no longer depends on incoming rate content. A dedicated Master IRestoreConsistencyOperation traverses live dependencies before batch revision stamping and publication. Missing/purged history cannot be reconstructed. A downloaded restored aggregate is unmodified Local data with its accepted identities/revisions.

## BUG-002 - Candidate retains Master deletion markers

- Status: Fixed; Candidate cleanup remains required under the final lifecycle.
- Severity: P1.

Prepare Candidate removes every soft-deleted business row across all 15 tracked tables before cumulative rebuilding. It preserves Local configuration and reconciled pending JSON. Preparation occurs in one transaction; installation follows only after successful preparation. Master restores deleted parents required by live dependents before publishing the snapshot; Candidate performs no duplicate dependency-validation query. Owned entries are removed with their aggregate.

This includes deletion markers accepted from other Locals, so Local hard deletion during acceptance does not replace Candidate cleanup. Master and published snapshot marker retention are unchanged.

## BUG-003 - Local acceptance does not produce a clean editing state

- Status: Fixed under the final 2026-11-12 clarification.
- Earlier classification of capture-time hard deletion as a defect is superseded.

Capture, immutable JSON persistence and Local acceptance share one transaction. Acceptance hard-deletes captured Local deletions in dependency order, clears surviving Content/Order flags and assigns sentinel EditRevision 0 to surviving new rows. A failed acceptance rolls back JSON, earlier deletes and metadata together. A later network failure retains the immutable JSON and the accepted clean Local state.

DeleteRevision 0 is sufficient to select deletion even when ModificationType is None. ModificationType contains only None=0, Content=1 and Order=2; Deletion=4 was removed. Delete/restore before capture preserves those edit flags. HasDeleted and the cleanup-only download path are unnecessary.

## BUG-004 - Non-owned references physically cascade-delete data

- Status: Fixed in fresh-schema SQL and matching EF configuration.
- Severity: P1.

Currency-to-Account, Account-to-TransactionEntry and Account-to-TemplateEntry use RESTRICT. The base-Currency-to-SystemConfig reference also uses RESTRICT. Existing service validation continues to block parent deletion while any dependent row physically exists, including soft-deleted rows.

Aggregate cascades remain Currency-to-CurrencyRate, Transaction-to-TransactionEntry and Template-to-TemplateEntry. Existing group hierarchy cascades remain guarded by the business rule forbidding deletion with live children/elements; this change does not introduce recursive business deletion of live members.

Editing fresh-schema scripts does not migrate existing database files. Such files require an explicit migration/rebuild before relying on the changed physical FK behavior.

## Verification

See [verification report](reviews/2026-11-11-sync/bugfix-verification.md) for dated results and execution limitations. Tests cover pre-capture tracking transitions, deletion-only batches, immutable retry after physical Local deletion, atomic rollback after an injected hard-delete failure, Currency aggregate replay/restore, Candidate cleanup, and SQL/EF RESTRICT behavior. No full-solution test success is implied.
