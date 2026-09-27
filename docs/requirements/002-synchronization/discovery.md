# Requirements Discovery: Synchronization

## Status and Sources

- Feature folder: `docs/requirements/002-synchronization/`.
- Version: 0.21.
- Status: Draft; non-authoritative working record.
- Recorded: 2026-09-26; updated: 2026-09-27.
- Approval decision: None for this exact document version. Conversational confirmations are recorded below; they are not BRD/TRD approval.
- Approval provenance: No formal artifact approver supplied.
- Separate discovery-to-business promotion decision: None. User explicitly postponed BRD/TRD rework until after brainstorming.
- Sources: This task's synchronization discussion through the user's 2026-09-27 request "write all statements in documents"; [core discovery](../001-personal-bookkeeping/discovery.md); legacy [BRD](../BRD.md) and [TRD](../TRD.md); repository AGENTS.md; GL Analysis discovery skill, artifact contract, and template.
- Source precedence: Later user corrections supersede earlier statements in this discussion. Core discovery supersedes outdated legacy BRD/TRD wording. Proposed details and open questions below are not confirmed requirements.
- Authority: Discovery records input and decisions only. It does not approve a BRD, authorize a TRD, or authorize implementation.

## Original Request

> Let's continue in this chat out analysis. Use [@GL Analysis](plugin://gl-analysis@git-plugins) .
> Now we start discussion about "Synchronization" feature in this chat.
> Let's describe:
> \-- User can have possibility to use app without internet connection. In another words he can work on his device Android, Apple, Windows desktop, browser(probably???)  and save information in local SqLite db. During synchronization all data from Local db is merged with master db.
> \-- The master DB is stored on the Cloud (Azure in the first version, AWS maybe later).
> \-- Each local DB can synchronize only with master DB.
> \-- Master DB can have additional fields for sync feature (or can't if we find another approach)
> \-- For conflict situation user can choose the correct version. MasterDB or LocalDb.
> \-- Probably in make sense to register all LocalDbs in some table in MasterDb to simplify sync process. E.g. physically remove rows, when all LocalDb receive this info.
> \-- Transaction and Template are aggregate. They sync as whole entity. We don't need to sync each TransactionEntry. Only the whole Transaction with entries

Current save instruction:

> You are forgetting my answers. At first make current statements to the discovery doc. Then we can continue

Latest save instruction, 2026-09-27:

> write all statements in documents

## Problem or Opportunity

Interpretation: allow personal bookkeeping offline on registered devices, then reconcile complete business data through a cloud master without violating accounting, dependency, uniqueness, or hierarchy rules. Preserve recoverability when communication stops between the two database commits.

## Intended Outcomes

- Offline bookkeeping using local SQLite, with a mandatory cloud MasterDb relationship.
- Bidirectional synchronization of complete business datasets.
- Predictable priority-based conflict selection with silent business-rule-preserving corrections.
- Simple local deletion handling, device expiration, and recoverable interrupted synchronization.
- A concise latest-sync report and detailed diagnostic logs for unresolved failures.

## Stakeholders

- One owner account per MasterDb and all its LocalDbs; no separate collaborative accounts/roles within that set.
- Device user works locally without application-opening authentication.
- Requesting user supplies discovery decisions. Formal artifact approval identity remains unspecified.

## Raw Ideas

- Initially user named Android, Apple, Windows desktop, and possibly browser clients. Later explicitly selected Android and Windows desktop for the first release; Apple and browser support are deferred.
- Azure is the first cloud target; AWS may follow later.
- User requested additional fields on synchronized entities, e.g. CreatingTime, EditingTime, DeletingTime. Exact schema and change-version mechanism remain technical design topics.
- Initial timestamp comparison evolved into SyncId/revision/acknowledgement tracking; exact-result replay was subsequently replaced by versioned SQLite publication and full download of the latest MasterDb.

## Alternatives Considered

- Entry-level or field-level merge versus whole versions: whole entities and whole aggregates selected.
- Interactive per-conflict selection versus configured priority: configured Local/Master priority selected, with silent corrections and report.
- Comparing business values to suppress equal-value conflicts: rejected; use priority when both sides changed, regardless of value equality.
- Immediate master hard deletion versus deletion propagation: soft deletion until every non-expired LocalDb acknowledges selected.
- Indefinite offline registration versus expiration: fixed 90-day expiration selected.
- Recovery alternatives evolved from exact-result replay to full download of the latest published MasterDb. Resolve uncertain publication by SyncId before discarding non-expired local outgoing changes. Expiry separately requires disposal and replacement.
- Equal timestamps alone versus operation identity: use SyncId, published revision/outcome, and acknowledgement. Retaining the exact original change payload is superseded.
- Global all-or-nothing rollback versus two atomic database commits with recovery: the latter accepted after explaining the network failure window.

## Assumptions

- No technical implementation has been selected beyond explicitly stated storage, metadata direction, and accepted logical recovery flow.
- Sync metadata is distinct from user configuration: business data synchronizes; user configuration stays local.
- The earlier confirmation that an account remains after losing all references does not prohibit explicit user deletion of an unused account. Core deletion rules still apply.
- No assumption of browser or Apple support in the first release: both were explicitly deferred.

## Open Questions

Do not re-ask confirmed decisions below. Continue one question at a time. These gaps are not approved rules.

1. Publication and acknowledgement: specify durable SyncId outcomes, atomic Admin.db active-version changes, safe local activation, and repeatable acknowledgements after lost responses. Coordinate snapshot cleanup with active downloads. Recovery uses the latest version, not exact-result replay.
2. Administrative storage: first-release SQLite is settled; hosting, safe access, and exact placement of LocalDb registrations/acknowledgements/expiration metadata remain design details. Multi-user SQL Server is deferred.
3. Local snapshot preparation: reconcile whole-database download with Master soft-deletion markers, master-only metadata, and local hard deletion after successful sync. Preserve local identity and configuration. Do not assume an unmodified byte-for-byte Master file is already an appropriate active LocalDb.
4. Change tracking: exact timestamp fields, stable entity identifiers, revision mechanism, both-sided change detection, and preservation of account currency locks remain design details. User requested additional sync fields such as CreatingTime, EditingTime, DeletingTime.
5. Currency merging: same-code currency merge and account remapping are settled. Work out remapping of other currency references and initial-rate collisions without violating core currency rules.
6. Remaining conflict cases: analyze complex combinations of hierarchy changes, restoration, and naming collisions for silent valid fixes. Explain unresolvable cases and write detailed logs; do not invent relaxed business rules.
7. Logs: exact log-folder location, contents, retention, and rotation remain unspecified.
8. Reports after full replacement/recovery: calculate local created/updated/deleted counts and important conflict/correction information without treating every downloaded row as newly created. Keep only the last attempt's user report in memory.
9. No automatic retry trigger on later Wi-Fi availability has been confirmed. Startup without Wi-Fi shows a message and permits ordinary offline work; pending recovery remains blocked.
10. Formal promotion/approval and measurable acceptance criteria remain pending. All release-scope questions already answered are recorded as decisions, not open questions.

## Decisions and Rationale

All decisions below are from the requesting user in this task on 2026-09-26 unless individually dated otherwise, including direct acceptance of examples and the proposed corrected recovery flow. No document approval is implied.

### Release scope

- Synchronization is included in the first application release (explicit user confirmation, 2026-09-26). This supersedes the earlier synchronization deferral in core discovery. Azure is the first cloud target.

- First-release clients: Android and Windows desktop only. Browser and Apple support are postponed (explicit user confirmation, 2026-09-26).

### Ownership, authentication, and initial creation

- One account owns each MasterDb and its LocalDbs. No several-account access model within one MasterDb is needed.
- First version: the single user has only one MasterDb. Second version: the user may create several separate databases and choose one at application startup (explicit user confirmation, 2026-09-26).
- Creating MasterDb requires authentication/owner identification. Sync authenticates to reject access to another owner's MasterDb.
- Opening and using the local app requires no authentication.
- MasterDb is mandatory. A LocalDb cannot originate as an independent database to be linked later.
- Creating LocalDb requires internet, registration with MasterDb, and an initial download of its business data.
- Each LocalDb contains the complete bookkeeping dataset and synchronizes only with MasterDb, never directly with another LocalDb.
- MasterDb is cloud-hosted, Azure first; AWS is a possible later target.

### Synchronized scope and local configuration

- Upload local business changes and download master business changes.
- Saved report definitions are business data and synchronize.
- Unbalanced draft transactions synchronize.
- Configuration does not synchronize. Conflict priority and sync trigger are separate per LocalDb.
- Configuration survives replacement of an expired LocalDb (user answered "I think yes", subsequently carried forward without reversal).
- Transaction and Template are the two aggregates: sync each complete aggregate including entries.
- For all other synchronized business entities, select the whole entity version. No field-level merge.
- A change on only one side is not a conflict and propagates regardless of priority.
- When both sides changed the same entity, priority selects the whole version. Do not compare business-field equality to alter that rule.

### Sync with no business changes

- Skip business-data transfer only when neither LocalDb nor MasterDb has business changes since their last completed sync (user confirmation, 2026-09-27).
- If MasterDb changed, LocalDb must receive its current business data even if LocalDb has no outgoing changes.
- A successful no-change sync still resets the 90-day expiration period. Administrative bookkeeping still occurs.

### Priority and silent conflict resolution

- User can change the local configuration's conflict priority: prefer Master or prefer Local.
- Default priority is Master. The setting applies to all sync processes for that LocalDb.
- No interactive question for each conflict. Resolve silently and expose important outcomes in the report.
- Priority is subordinate to business rules. Override it when necessary to preserve a valid dataset.
- Analyze conflict cases and define silent repairs. If no valid repair exists, explain the situation and how to fix it; write a detailed diagnostic log into the log folder. Do not silently invent or relax business rules.
- Example: shared category Food becomes Groceries locally and Meals on Master. Local priority chooses Groceries; Master priority chooses Meals.
- Account-currency immutability/locking follows the already documented core rule and cannot be overridden by sync priority. The permanent lock survives removal of all transaction/template references.

### Deletion, restoration, and dependencies

- Use soft deletion to propagate deletions.
- Master hard-deletes only after every remaining non-expired registered LocalDb has received/acknowledged the deletion.
- Before sync, Master removes expired registrations and performs hard deletion newly eligible after those removals.
- LocalDb uses soft deletion only for sync. After the first successful sync confirming deletion, hard-delete locally. Failed/cancelled sync retains pending deletion information unless its outcome is being recovered through the accepted committed-sync flow.
- For delete-versus-edit conflicts, apply priority if business rules permit: Master deletion wins under Master priority; Local edited version survives under Local priority.
- If deletion violates a reference or another business rule, retain/restore the entity regardless of priority and report the reason.
- Applies to all protected deletions: accounts, used currencies, nonempty groups, classifications, and other protected entities.
- Example: Master deleted an unused account; offline Local created a transaction using it. Restore the account and keep the transaction, even under Master priority.
- Restore required dependencies automatically: currency, groups, and full deleted parent chain up to root.
- Example: Master merged group A into B and deleted A; another LocalDb added an item to A offline. Restore A and keep the new item in A. Report restoration.
- Sync result must be a consistent dataset: no transaction without required accounts/currencies/groups.

### Uniqueness and hierarchy conflicts

- Independently created currencies with the same ISO code merge into one currency. Update affected accounts' foreign keys. Other differing values follow configured priority.
- Two rates for the same currency and date resolve to one rate using priority.
- Distinct same-name entities subject to uniqueness remain distinct. Preferred version keeps the name; rename the other by appending `_1`, repeatedly until unique, following core rules (e.g. name_1_1).
- Applies to unique group, category, project, correspondent, and template names. Account names may duplicate under core rules.
- Renaming is silent and reported.
- Break hierarchy cycles silently according to priority and report the correction.
- Example: Local has A under B; Master has B under A. Under Master priority, retain B under A and restore A's parent from Master. Preserve the preferred valid hierarchy.

### LocalDb display names

- First version does not support user-assigned LocalDb names. Naming LocalDbs is planned for the second version (user confirmation, 2026-09-27). Registration still requires technical identity; this decision concerns user-facing names only.

### Triggers, locking, cancellation, and timeout

- Exactly one configured trigger: manual only, on start, or on exit.
- Default: manual only. This supersedes the earlier "both by default" statement.
- Sync button is always available regardless of trigger setting.
- On Android, Wi-Fi is required to start automatic sync or automatic pending-sync recovery. Once started on Wi-Fi, either may continue over mobile data if Wi-Fi disappears (user clarification, 2026-09-27). This supersedes the earlier Wi-Fi-only-for-the-entire-operation wording.
- Manually initiated sync or recovery may start on either Wi-Fi or mobile data. Pending recovery still blocks business-data access until completed; mobile-data availability alone does not authorize starting automatic recovery.
- No concurrent editing during any sync, including automatic sync, conflict processing, or waiting for Master lock.
- Pessimistic locking: one sync at a time per MasterDb. Second LocalDb waits; inform its user.
- Lock wait is a normal queue, not a communication failure. It has no time limit; user can cancel through a Cancel button. The 30-second timeout indicates technical communication problems (for example internet connection issues), not elapsed time in this queue. Exact connection-health detection is a technical design detail; do not infer failure merely because the lock is not yet available.
- Fixed, hardcoded inactivity timeout: stop activity after 30 seconds without a response. This is not a limit on total sync duration. Incoming download data resets the timer, so a large initial or replacement database download can continue while data arrives. Lock waiting remains exempt. This clarification supersedes the earlier total-duration wording.
- During long sync processing, MasterDb sends periodic "still working" responses to prevent a healthy connection from triggering the 30-second communication timeout. These responses reset the inactivity timer. Exact response interval remains a technical detail (user confirmation, 2026-09-26).
- User can cancel sync. Timeout also stops an attempt, subject to post-commit recovery semantics below.
- Startup sync with no internet/failure shows an error and allows ordinary offline work, except when recovery is pending.
- If configured on-start sync cannot start because Wi-Fi is unavailable, show a message and let the user continue working offline (user confirmation, 2026-09-27). This does not override the existing access block for pending recovery. User did not specify a new automatic trigger when Wi-Fi later becomes available; do not infer one.
- Failed or cancelled on-exit sync allows app closure and retains unsynced local data, subject to explicit expiration disposal rules.

### Report and diagnostic log

- A button opens the latest sync report; no automatic report-opening requirement.
- Only the latest sync attempt's report is retained, in memory until the app closes. No session history or persistent report storage.
- Report important items: conflicts, corrections/restorations/renames, errors, and entity-type counts.
- Counts are from LocalDb's point of view and only describe business changes received from Master and applied locally, not uploads.
- Split counts by created, updated, deleted for each type (Categories, CategoryGroups, Transactions, etc.). Omit zero values.
- Latest-attempt report rule also applies around recovery/replacement; no separate retained earlier report.
- Detailed logs for unresolved critical situations are separate from the in-memory user report.

### Expiration and replacement

- Fixed expiry: 90 days after last successful sync, calculated by MasterDb clock. "Three months" was explicitly clarified to 90 days, not calendar months.
- Successful initial registration/download starts this period. Every successful sync resets it, even with zero changes.
- Future configurable expiry was mentioned as a possibility, not selected for current scope.
- Remove expired LocalDb from Master's sync registry. Reject its sync and show information/error indicating a fresh LocalDb is required.
- Once expiration is confirmed, no offline viewing/export of the expired LocalDb; delete its database file automatically.
- Expiration overrides pending recovery. Discard expired local changes regardless of whether they were uploaded. User has no option to preserve the expired database through this workflow.
- Offer a fresh LocalDb from the same MasterDb. Download starts only when user clicks **Download**, not automatically.
- Preserve local configuration.
- The temporary "Cancel or Wait" answer concerned a misunderstanding of expiry and was superseded by the explicit 90-day clarification. Do not reinstate it as an expiry option.

### Commit boundary and recovery rules

Current direction is copy-and-publish on Master followed by full LocalDb replacement, described below. The earlier in-place Master transaction, per-entity local replay, and retained exact change-result flow are superseded. The original blanket requirement that both databases roll back on any failure was corrected: after Master publication, Master remains committed and Local must recover.

- Before Master publication, preserve original business data and discard an unsuccessful candidate. Local outgoing changes remain available for retry.
- After Master publication, never roll Master back because Local installation failed. Recovery downloads the latest published MasterDb, including later changes from other devices.
- Persist operation identity (SyncId), publication outcome/revision, and local installation state. If response is lost, determine whether Master published before re-uploading or discarding local changes. Equal timestamps alone do not establish completion.
- When local installation succeeded but acknowledgement was lost, repeat acknowledgement safely; do not upload or apply the original batch twice.
- Local acknowledges the revision actually installed. Record successful-sync time using Master's clock. Only acknowledged receipt counts toward deletion propagation. The exact bookkeeping is a technical design detail.
- If Master cannot be reached and outcome is unknown, preserve pending state; do not claim rollback or success.
- Cancellation/timeout after publication means recovery is pending, not that Master rolled back.
- While recovery is pending, block all local business-data access, including viewing and reports. This is incomplete synchronization, not necessarily physical SQLite corruption.
- On startup, initiate pending recovery regardless of configured sync trigger, subject to Android's Wi-Fi start rule. User may start recovery manually on Wi-Fi or mobile data.
- Recovery uses the same communication-inactivity timeout, Cancel control, and lock-wait rules. Cancellation/failure leaves access blocked.
- Non-expired local data must not be discarded while it may contain changes Master never received. Expiration is the explicit exception: discard the old database regardless of local changes, then require Download.
- Configured conflict priority intentionally discards losing versions; preservation of all historical edits is not promised.

### Versioned SQLite publication — latest direction, 2026-09-26

This is the current storage and recovery direction. Earlier exact-result replay is retained only as historical context in Alternatives Considered.

User proposed and confirmed direction:

- MasterDb is a SQLite database file stored in Azure storage under a unique name such as `{GUID}.db`.
- Make changes in a copy of MasterDb; do not modify its currently published version during preparation.
- Publish the completed copy by changing the record identifying the active database version. User also mentioned renaming; active-version publication is the discussed preferred approach, with atomicity still a technical requirement to design.
- After Master publication, download a complete new local database and substitute it for the old LocalDb, rather than applying a retained per-entity recovery result.
- Recovery downloads the latest published MasterDb, including changes committed by other LocalDbs after the interrupted sync (user confirmation, 2026-09-27). It does not require the original interrupted sync snapshot. Resolve the original SyncId outcome before discarding local outgoing changes; do not upload an already published batch again. A download uses a consistent selected revision even if a later version is published during transfer.
- User proposed an Admin.db registry recording users, database names, old versions, new versions, and related administration information. First-release administrative storage is SQLite, as confirmed below. Exact schema, hosting, access serialization, and publication mechanism remain design details.

Analysis constraints explained in the conversation (not all independently approved implementation choices):

- Before publication, discard a failed candidate; the previous active business dataset remains authoritative.
- Publication is the Master commit point. After it, do not roll Master back because Local failed; Local must recover by downloading/installing a valid published snapshot.
- Publish only a fully persisted and validated candidate. Switching the active-version record must be atomic and reject a stale expected previous version.
- Persist SyncId and publication outcome together so a lost response can be resolved without duplicate application. Exact per-entity recovery payload retention is no longer the proposed recovery mechanism.
- Download to a temporary local database, validate it, safely activate it, then acknowledge completion. Never activate a partial download or delete the old non-expired file before a successful switch.
- Interrupted database downloads restart from the beginning; discard partial download data rather than resume it (user confirmation, 2026-09-27). On recovery, select the latest published MasterDb as already agreed.
- Preserve local configuration and LocalDb identity. Local business snapshots must not expose master-only registrations or retain master deletion markers contrary to local hard-deletion rules.
- Keep a published snapshot available while an active download needs it. Pending recovery alone does not require retaining its original version, because recovery downloads the latest MasterDb (clarification, 2026-09-27). Delete superseded MasterDb versions once no download or actual recovery dependency needs them; no backup/history retention. Publication outcome records must remain available independently to resolve SyncId uncertainty. Cleanup and acknowledgements need coordination.
- Azure object storage holds SQLite files; processing needs an application service that obtains a consistent database copy and uses SQLite. Admin.db is a logical registry proposal, not approval to open a blob URL as a live SQLite database or to allow concurrent writers to independent registry copies.
- Expiration and blocked pending recovery rules remain unchanged.

Confirmed admin storage options (user, 2026-09-26):

- Use Microsoft SQL Server for administration in a deployment serving several users.
- Use SQLite for administration in a single-user deployment.
- An administrative UserId field may be present but is not used in single-user mode. Exact nullability/default and schema are unspecified.
- These deployment modes do not change the confirmed ownership rule: one owner account per MasterDb and its LocalDbs. Several service users do not imply several owners sharing one MasterDb.
- Master business database versions remain SQLite files; the SQL Server option concerns administrative storage only.
- Existing sync authentication and ownership protection remain in force; an unused administrative UserId field is not a decision to remove authentication.
- First release supports single-user mode only, using SQLite for administrative storage (user confirmation, 2026-09-26). Multi-user mode with SQL Server administration is deferred.

Remaining design questions are consolidated in Open Questions above. No BRD/TRD promotion or implementation approval is implied.

## Rejected Ideas

- Multiple owner accounts/roles within a MasterDb and its LocalDbs.
- Authentication just to open the local app.
- Synchronizing configuration.
- Field-level or entry-level merging.
- Per-conflict interactive selection or business-value equality checks changing priority resolution.
- Automatic deletion that violates business rules.
- Combined on-start and on-exit trigger settings; automatic default sync.
- Timeout for waiting on Master lock.
- Keeping all reports or persisting user reports across app closure.
- Viewing/exporting the expired LocalDb or retaining its file after confirmed expiration.
- Automatic fresh download without clicking Download.
- Using timestamp equality alone to establish commit completion.
- Treating a post-Master-commit cancellation as a rollback of Master.
- Access to business data while recovery is pending.

## Deferred Topics

- User-assigned LocalDb names: second version, not first (confirmed 2026-09-27).

- Multiple separate databases per user and choosing a database at app startup: second version. First version permits only one MasterDb.

- Multi-user deployment with SQL Server administration: deferred beyond the first release. First release uses single-user SQLite administration.

- AWS: possible later cloud target; no commitment/date.
- Browser and Apple support: explicitly postponed beyond the first release. Revisit timing and exact Apple platforms later.
- Configurable expiry: possible future addition; current rule is fixed 90 days.
- BRD/TRD format rework: after brainstorming and applicable promotion/approval steps, per user instruction.
- Implementation, schema, algorithms, and tests: not authorized by discovery.

## Promotion Readiness

- Ready to seek discovery-to-business promotion: Not yet requested; continue brainstorming from this saved record without repeating settled questions.
- Questions blocking a safe draft: No identified behavior gap necessarily prevents a labeled draft, but remaining recovery constraints must stay explicit; no promotion instruction has been given.
- Questions that may remain in a draft: Open Questions above, clearly identified as unresolved and not approved implementation rules.
- Promotion decision reference: None recorded.
- Next recommended phase: Continue synchronization discovery one question at a time. Later draft/rework BRD when explicitly authorized; TRD follows approved BRD.

