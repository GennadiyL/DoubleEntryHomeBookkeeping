# Business Requirements Document: DoubleEntryHomeBookkeeping

## Document Control

- Artifact: `docs/requirements/BRD.md`; consolidated BRD for core bookkeeping, synchronization and first use.
- Status: **Draft**; version **0.48**, dated **2026-09-28**. Replaces legacy Draft 0.1 and its accumulated brainstorming updates.
- Business owner/approver: requesting user in this conversation (personal name not supplied).
- Drafting authorization: requesting user, this conversation, 2026-09-27: **“Cool. Work with BRD”**, following the proposal to consolidate core bookkeeping, synchronization and first use with unresolved Administration details marked open.
- Source status: discovery documents remain Draft. Individual user-confirmed decisions are evidence, not formal document approval. The current instruction authorizes this consolidated draft; no discovery approval is inferred. The requested consolidated scope/location takes precedence over the skill's default per-feature layout and preliminary approval workflow.
- Revision 0.39: extend favorites/filtering to currencies under the same ordinary content synchronization rules.
- Revision 0.38: retain favorite flags/filtering for groups and elements in the first release; synchronize favorites as content under configured conflict priority.
- Revision 0.37: mandatory Name and visible optional Description; remove Comment throughout, and remove hidden template Description and name-copy behavior. Template/transaction copying uses Description.
- Revision 0.36: Description is the required long name for groups/elements; Comment is optional. CurrencyRate retains its own optional Comment distinct from Currency.Comment.
- Revision 0.35: template retains Description and Comment; version 1 hides Description and copies final Name into it on every save. Comment continues to populate transaction Comment.
- Revision 0.34: user confirmed account currency becomes immutable at the first successful save; canceling creation creates nothing. Supersedes the first-use trigger. Prior 0.32 approval does not cover this revision.
- Revision 0.33: controlled revision requested 2026-09-28 for device-local calendar rate lookup and DateOnly rate dates; transaction timestamps remain UTC. Prior approval covers 0.32 only, not this revision.
- Revision 0.32: user requested Business Capability identifiers (BC) in the BRD and detailed Use Case identifiers (UC) in the TRD. Renamed the twelve headings and all internal references without changing their behavior. The previous BRD UC identifiers map one-to-one to BC identifiers with the same three-digit number.
- Revision 0.31: pre-approval consolidation against recorded answers. Closed stale Q-09; classified Q-11 as technical analysis under settled conflict policy. Q-01 now asks for exact-version acceptance, not another discovery round. No new business policy or approval inferred. Historical revision notes below describe earlier states; current rules and decision register take precedence.
- Revision 0.30: user reaffirmed that Open retries the initial download after successful master creation; Create stays disabled. Q-13 closed; this settled behavior must not be re-asked.
- Revision 0.29: user confirmed rejecting numeric-limit/overflow failures with an error and unchanged existing data; exact numeric limits belong in the TRD. Q-05 closed at business level.
- Revision 0.28: user confirmed first-returned Name/Symbol for each deduplicated ISO code; Q-04 closed.
- Revision 0.27: user confirmed one available currency entry per ISO code. No choice among differing regional Name/Symbol values was confirmed.
- Revision 0.26: user reaffirmed the previously supplied CultureInfo/RegionInfo currency catalog with its exact example. Currency source is settled and must not be re-asked as a new predefined-list choice. Duplicate-code/default selection remains unresolved.
- Revision 0.25: user specified a log folder for diagnostic logs; seven-day automatic cleanup retained. No separate Share/Export action selected. Q-12 closed at business level.
- Revision 0.24: user selected automatic diagnostic-log cleanup after a 7-day retention period. Latest sync report remains session-only.
- Revision 0.23: user established a general manual/automatic connection rule: manual actions use any connection, automatic actions require Wi-Fi. Initial setup is manual. Q-16 closed; existing Wi-Fi-start/continuation behavior is retained.
- Revision 0.22: user selected a new local copy after reinstallation; old registration follows existing expiry. Q-15 closed for the business behavior.
- Revision 0.21: user confirmed manual retries only after initial setup failure; show the error and do not retry automatically. This does not alter the separately agreed automatic pending-sync recovery rules.
- Revision 0.20: user selected required-fields-only validation for login/password in version one; no additional length/character/complexity rules. Q-14 closed.
- Revision 0.19: user confirmed remembering authorization after successful Create/Open, avoiding repeated credential entry during normal synchronization. No token, password-storage or authorization-expiry mechanism is selected by this decision.
- Revision 0.18: user deferred login/password changes and recovery to version two. Initial credentials and authenticated cloud access remain first-version requirements.
- Revision 0.17: user deferred backup/restore to version two and proposed Azure-side-only operation; Q-10 closed for version-one scope. Existing first-version synchronization recovery remains in scope.
- Revision 0.16: user confirmed default decimal display places = 2 in System configuration; calculation precision remains four places.
- Revision 0.15: user specified two configuration tables, System and Local. System synchronizes with master; Local does not. Decimal display precision belongs to System. This supersedes the earlier blanket rule that all configuration is local; previously agreed per-LocalDb settings remain Local. The two-table storage instruction is retained as technical input for later TRD.
- Revision 0.14: user confirmed applying saved report JSON as-is to the actual tree, ignoring missing identities and reapplying their saved choices when they return; Q-06 closed.
- Revision 0.13: user confirmed preserving all saved explicit report inclusions/exclusions after hierarchy moves, even redundant ones; only explicit user save recalculates included/excluded IDs.
- Revision 0.12: user confirmed a partial-selection mark (−) for report groups with only some descendants selected.
- Revision 0.11: user agreed default report-name format `yyyy-MM-dd HH:mm:ss`, rejection of empty/whitespace-only names, and duplicate-name allowance; Q-07 closed.
- Revision 0.10: user confirmed saved-report rename/edit/delete and on-demand calculation. Deletion removes only the JSON calculation instructions, never bookkeeping data; JSON is retained as technical input, not an additional business storage rule.
- Revision 0.9: user confirmed empty/single-entry template editing, disabled Save until the minimum account-bearing entries exist, and Cancel without saving. User expects single-entry templates to be the most frequent case; no measured frequency supplied.
- Revision 0.8: user agreed that a non-root group may merge into its parent, another ancestor or the root; Q-08 closed.
- Revision 0.7: user confirmed rejection of empty/whitespace-only mandatory Description after trimming; Q-02 closed.
- Revision 0.6: user confirmed initial order Correspondent, Category, Project and separator `/`; Q-03 closed.
- Revision 0.5: user refined format selection into all six classification orders plus a separately chosen single non-whitespace separator. Initial defaults are not yet confirmed.
- Revision 0.4: user selected predefined formats for `DefaultAccountName`; exact options and initial selection remain open.
- Revision 0.3: user confirmed per-LocalDb `DefaultAccountName` and no automatic renaming of existing accounts after classification renames. This supersedes the older whole-database-format wording in C.
- Source precedence: later explicit corrections supersede historical proposals. Synchronization is first-release scope. Multi-user administration is planned for version two.
- Approval: requesting user, 2026-09-27, current conversation: “I approve BRD. Lets start with TRD”. Applies to BRD 0.32, including its acceptance criteria and consolidated scope. Authorizes TRD drafting, not development.

| Key | Source/version | Decisions |
| --- | --- | --- |
| C | [Core discovery](001-personal-bookkeeping/discovery.md), Draft 0.1 | User decisions 2026-09-25–26: organization, transactions, currencies, templates, reports |
| S | [Synchronization discovery](002-synchronization/discovery.md), Draft 0.21 | User decisions 2026-09-26–27: ownership, offline work, conflicts, recovery, expiry |
| F | [First-use discovery](003-first-using/discovery.md), Draft 0.10 | User decisions 2026-09-27: startup, creation, additional devices, initial content |
| U | Current conversation, 2026-09-27 | First-use conclusion, consolidated drafting authorization and all subsequent BRD decisions through version 0.32 |

Table source references identify confirmed conversational decisions unless marked assumption, proposal or unresolved. Draft wording still requires review. Original BRD identifiers remain mapped below; discovery retains historical wording and corrections.

## Executive Summary

The owner records personal finances using strict double-entry bookkeeping, works offline on registered devices and synchronizes through a cloud master. Version one serves the requesting user on Android and Windows desktop, with one master dataset and complete local copies.

Setup requires internet. Ordinary local use requires neither internet nor sign-in. Pending synchronization recovery and confirmed expiration are explicit access exceptions. Active transactions balance exactly after per-entry APr rounding; unbalanced drafts do not affect calculations.

Version-one business decisions are consolidated below. Backup/restore and credential changes/recovery are version-two scope. This review version presents acceptance criteria for approval; technical mechanisms remain for the TRD.

## Business Problem or Opportunity

The stated need is personal accounting with consistent double-entry records, multiple currencies, reusable transactions, classification-based reports and offline work across devices. The existing manual process, baseline effort and quantified benefits were not supplied; this draft does not invent them.

## Objectives and Success Measures

The measures below are proposed acceptance criteria for this personal prototype, derived from the confirmed rules. Approval of this version accepts these criteria. No productivity baseline or new performance/uptime target is asserted; the recorded functional outcomes, 30-second inactivity timeout, 90-day expiry and 7-day log retention remain measurable requirements. ROLE-002 accepts the results; test evidence is produced during later verification.

| ID | Objective | Measure/calculation | Baseline/source | Target/timeframe | Evidence owner/status |
| --- | --- | --- | --- | --- | --- |
| SC-001 | Start/reconnect bookkeeping | Observe successful setup, initial content and local access | F; baseline unknown | Proposed: every acceptance scenario in BC-001/BC-002 passes for first release | Requesting user; proposed |
| SC-002 | Preserve accounting correctness | Check exact-zero rounded active totals and exclusion of drafts | C; baseline unknown | Proposed: every accounting scenario in BC-003–BC-005/BC-007 passes; no active transaction violates exact-zero rule | Requesting user; proposed |
| SC-003 | Reuse data and obtain intended reports | Compare template results and report totals/selections with agreed examples | C; baseline unknown | Proposed: every acceptance scenario in BC-006–BC-008 passes for first release | Requesting user; proposed |
| SC-004 | Work offline and reconcile safely | Observe offline work, conflict results, recovery and expiry | S/F; baseline unknown | Proposed: every acceptance scenario in BC-009–BC-012 passes for first release | Requesting user; proposed |

## Stakeholders and Roles

| ID | Role | Responsibility | Source/status |
| --- | --- | --- | --- |
| ROLE-001 | Personal bookkeeping owner | Own data, use registered devices, manage transactions/reports and sync | C/S/F; sole application user in version one |
| ROLE-002 | Requirements decision owner | Resolve business questions and approve exact document versions | Requesting user; formal approval identity open; not another application role |

## Scope

### In Scope

- First-use creation and connecting another device; one owner and one master dataset (F/S).
- Android and Windows desktop, ordinary offline use without sign-in (S/F).
- Five hierarchies, accounts, classifications, currencies and rates (C).
- Manual transactions, constrained unbalanced drafts, duplication, transaction templates, group merging (C).
- On-demand balances and configurable saved reports (C).
- First-release synchronization of all business data, conflict priority subordinate to validity, deletion propagation, recovery, expiry and latest-attempt reporting (S).

### Out of Scope

- Collaborative ownership of a master dataset; standalone offline-created books linked to cloud later (S, rejected).
- Unrequested automatic balancing adjustments, tolerance-based balance acceptance, field-level conflict merging, interactive per-conflict decisions (C/S, rejected).
- Account/Account Group report filters, relative report periods, drill-through and separate positive/negative subtotals (C, excluded).
- Future work is listed under Deferred Requirements. Backup/restore is deferred to version two; Azure-side-only backup/restore is the proposed direction.

## Current and Target Business Process

- Current process: not documented; product is defined from the owner's stated needs.
- Target: open existing local bookkeeping offline or complete online setup; organize data; record balanced activity or save unbalanced drafts; reuse templates; calculate reports; synchronize when requested/configured; complete required recovery/replacement before accessing affected data.
- **Open/editing mode** uses an existing local copy offline. **Creating mode** means no local copy and requires online setup. The setup **Open button** downloads from an existing cloud master; it is distinct from ordinary offline open mode.
- Setup retries, credentials, reinstallation and connectivity are settled in Q-13–Q-16. Remaining technical mechanisms do not change these business decisions.

## Business Rules

### Ownership and first use

| ID | Rule | Source | Related IDs |
| --- | --- | --- | --- |
| BR-001 | Version one has one owner per deployment and one master dataset. Each local copy belongs to that master; no direct device-to-device sync. Reinstalling the app registers a new local copy; the previous registration remains subject to the existing 90-day expiry rule. | S Ownership; F Decisions; U clarifications | BC-001, BC-002; FR-001, FR-002 |
| BR-002 | Startup selects mode by local-copy existence only. Existing copy opens without internet/sign-in; absent copy requires online creating mode and completed setup before bookkeeping. Recovery/expiry exceptions still apply. | F Startup; S Recovery | BC-001, BC-002; FR-001, FR-002 |
| BR-003 | Check master existence when setup opens. Keep Create disabled until absence confirmed, and disabled if master exists. Create takes login/password and base currency. Login and password are required fields only; no additional length, character or complexity rules in version one. Setup Open validates credentials and registers device before downloading existing business data. If initial setup fails, show the error and allow only user-initiated retries; no automatic setup retries. If master creation succeeded but the initial download failed, keep Create disabled and let the user select Open to retry downloading. No local copy means no bookkeeping access. | F Create/Open; U clarifications | BC-001, BC-002; FR-001, FR-002 |
| BR-004 | Initial content: five required roots, selected base currency at rate 1, and one rebalancing account in the Account root group using base currency; no transactions. System configuration selects this account for balancing. Other currencies added later. Base currency is immutable. | F Initial content; C Currency | BC-001; FR-001 |
| BR-005 | Authenticate cloud access and prevent another owner's data access. Remember authorization after successful Create/Open so normal synchronization does not require entering login/password again; the authorization mechanism and credential-storage design remain later technical work. Each local copy contains complete business data. Configuration has two scopes: System settings synchronize with master; Local settings do not synchronize and survive local-copy replacement. Decimal display places belong to System. `DefaultAccountName`, sync trigger and conflict priority belong to Local. | S Ownership; U configuration correction 2026-09-27 | BC-002, BC-009–BC-011; FR-002, FR-009–FR-011 |

### Organization, accounts, currency and accounting

| ID | Rule | Source | Related IDs |
| --- | --- | --- | --- |
| BR-006 | Accounts, Categories, Projects, Correspondents and Templates each have exactly one root hierarchy. Every element belongs to a group of its own type. Roots may directly contain elements; cannot be edited, renamed, moved or deleted. | C Legacy/Groups | BC-003; FR-003 |
| BR-007 | Rearrange non-root groups/elements within type, even if used. Reject cycles including merge into descendant. A non-root group may merge into its parent, another ancestor or the root; the root remains forbidden as merge source. Delete only groups without children/elements. Merge moves source children/elements to destination, resolves collisions and deletes empty source. | C Groups; U clarifications | BC-003; FR-003 |
| BR-008 | Trim names; reject empty; compare case-insensitively. Group names unique among siblings; element names unique in their group except Account duplicates allowed. Group/element scopes are separate. Reject individual forbidden collisions; bulk collisions repeatedly append `_1` to incoming name, retaining destination names. | C Names/Groups | BC-003, BC-004, BC-006; FR-003, FR-004, FR-006 |
| BR-009 | Name is mandatory on named catalogs, groups and elements; existing uniqueness rules remain. Description is optional, visible and independently editable wherever descriptive text is supported. No Comment field remains: currencies, rates, templates and transactions use optional Description. No hidden Template.Description and no automatic Name-to-Description copying on save. Omitting or leaving Description empty must not block saving. This does not add Name to transactions, rates or entries. | U clarification 2026-09-28 | BC-003–BC-006; FR-003–FR-006 |
| BR-010 | Account currency mandatory; Category, Project, Correspondent independently optional. Category is an activity; Project an investment/income purpose; Correspondent a person/legal entity. Historical reports use current account classifications. | C Legacy/Account lifecycle | BC-004, BC-007; FR-004, FR-007 |
| BR-011 | Account currency is selectable only during unsaved creation and becomes permanently immutable on the first successful account save, even before any transaction/template use. Canceling unsaved creation creates no account. Any transaction/template reference prevents account deletion, even at zero balance. Classifications used by accounts cannot be deleted. | C Account lifecycle; S Priority | BC-003–BC-006, BC-009; FR-003–FR-006, FR-009 |
| BR-012 | Each LocalDb has its own Local configuration value `DefaultAccountName`, e.g. `{Correspondent}/{Category}/{Project}`. User selects one of all six orders of Correspondent, Category and Project, and separately chooses a separator consisting of exactly one non-whitespace character. These choices determine the format; arbitrary free-text format entry is not supported. It generates a default using classification short names; it is not synchronized. Missing values retain positions/separators; all absent in slash format yields valid `//`. Account names are stored business values and synchronize normally. Allow manual override/Restore Default Name. Later classification renames or reassignment do not automatically change existing account names. Initial order is Correspondent, Category, Project; initial separator is `/`, producing `{Correspondent}/{Category}/{Project}`. Q-03 is resolved. | C Names; S Configuration; U clarification 2026-09-27 | BC-004; FR-004 |
| BR-013 | Currency ISO code unique dataset-wide and immutable; Name/Symbol editable/restorable from catalog defaults; Description optional. Delete only non-base currency unused by accounts. Populate available currency choices from system culture/region information, using ISO currency symbol as Code, currency symbol as Symbol and English currency name as Name, as in the user-provided example. Invalid region conversions are skipped. Available choices contain exactly one entry per ISO currency code. For repeated codes, retain the first returned Name and Symbol; these remain editable and restorable from source defaults. Q-04 is resolved. | C Currency; U clarifications | BC-004; FR-004 |
| BR-014 | Currency requires positive initial rate. Base rate always 1, never overridden. Initial rate has hidden system-controlled old date; value editable subject to base rule; cannot be deleted. One ordinary rate per currency per calendar date (DateOnly, without timezone conversion); ordinary rates editable/deletable. Each rate, including the initial rate, may have its own optional Description, separate from the currency Description. | C Currency/Precision | BC-004, BC-005; FR-004, FR-005 |
| BR-015 | Entry currency comes from account. Default rate is latest on/before the transaction calendar date in the current device/UI timezone with initial fallback. Stored entry rate independent of later rate changes. Non-base rate override allowed; Restore Rate reloads applicable current rate. Date changes do not alter stored rates automatically. | C Currency | BC-005, BC-006; FR-005, FR-006 |
| BR-016 | Every saved transaction, including drafts, needs at least two entries, each with account. Empty amount becomes zero; zero/repeated accounts allowed. Positive increases account balance; negative decreases it. Optional Description is transaction-level only. | C Transactions | BC-005; FR-005 |
| BR-017 | Base currency, Amount Precision (APr) and Rate Precision (RPr) are selected during dataset creation, stored in synchronized System configuration, and immutable afterwards. Amounts use APr fractional places; rates use RPr. Input controls enforce these precisions; excess input received outside them rounds to the applicable precision using midpoint-to-even. Rates must remain positive after rounding. Numeric overflow rejects the operation with an error and unchanged data. APr and RPr each allow integer values 0 through 4 inclusive; defaults are APr = 2 and RPr = 4. Display amounts and BaseAmount using APr and rates using RPr. No separate DisplayDecimalPlaces setting remains. SQLite storage uses fixed four-decimal scaling as specified in TRD. | U clarification 2026-09-28 | BC-001/004/005/009; FR-001/004/005/009 |
| BR-018 | Calculate each BaseAmount = Amount × Rate rounded to APr using midpoint-to-even; sum those rounded values. Confirmed transactions require exact zero, with no tolerance. For any nonzero difference, offer Add balancing entry. Only on user click, append an entry using the configured base-currency balancing account, Amount = negative current total, Rate = 1; recalculate and require zero. User may instead add an entry manually. The button has no small-rounding threshold and does not silently add entries or bypass other save validation. The balancing-account selection belongs to synchronized System configuration and is shared across devices. Initialization creates and selects the rebalancing account in the Account root group. The user can select another base-currency account in Settings. The account is subject to ordinary account deletion rules; selection alone does not protect it. If missing/deleted, Add balancing entry adds nothing and shows: Cannot add a balancing entry: the rebalancing account is missing. Please choose a rebalancing account in Settings. Manual balancing remains available. | U clarification 2026-09-28 | BC-005; FR-005 |
| BR-019 | Complete balanced save activates automatically. New unbalanced transaction may be draft, excluded from balances/reports. Active edit cannot save invalid or return to draft. Active transaction may be deleted. Calculate balances/reports on demand excluding drafts/deleted transactions. | C Transactions | BC-005, BC-007; FR-005, FR-007 |
| BR-020 | New occurrence defaults to now, editable. Duplicate copies accounts/amounts/rates/Description with time reset to now for review/save. Opening balances are ordinary balanced transactions; no opening-balance preset account created; the initialization rebalancing account is separate. | C Transactions | BC-005; FR-005 |
| BR-021 | Transaction time represents UTC, displays in device timezone; no separate timezone setting. Minimum 2001-01-01 00:00:00 UTC inclusive after local-input conversion. Ordinary DateOnly rate dates cannot precede 2001-01-01; initial date excepted. Rate lookup uses the transaction date in the current device/UI timezone. Report boundaries and calendar grouping use that same timezone, converted to UTC instants for timestamp filtering. | C Dates | BC-004, BC-005, BC-007; FR-004, FR-005, FR-007 |
| BR-022 | Template has group-unique Name, accounts/amounts and visible optional Description; each entry needs account. Empty, single-entry and unbalanced templates allowed. Applying opens transaction for review with applicable rates and copied Description; persists only on Save. Save stays disabled until there are at least two entries and every entry has an account; other transaction validation still applies. User adds missing entries or selects Cancel without saving the new transaction. Create from active transaction or draft by copying accounts/amounts/Description. | C Templates; U clarifications | BC-006; FR-006 |

#### Account-name order choices (BR-012)

1. Correspondent, Category, Project
2. Correspondent, Project, Category
3. Category, Correspondent, Project
4. Category, Project, Correspondent
5. Project, Correspondent, Category
6. Project, Category, Correspondent

Initial order is **Correspondent, Category, Project** and initial separator is **`/`**. The selected single non-whitespace separator goes between positions, including empty positions. For example, Correspondent/Category/Project with separator `-` produces `{Correspondent}-{Category}-{Project}`. This is a generated default only; existing stored account names do not change automatically.

### Reports

| ID | Rule | Source | Related IDs |
| --- | --- | --- | --- |
| BR-023 | Filter by Category, Project, Correspondent trees only. Within type, selected groups/elements/unassigned combine OR; types combine AND for matching accounts. New reports all unchecked. Empty selection in any type means empty report with warning, not unrestricted. | C Filtering | BC-007; FR-007 |
| BR-024 | Group selects descendants with explicit exclusions/inclusions. Inherit nearest saved ancestor state, default unchecked. Explicit element inclusion may override parent exclusion. Checking/unchecking group resets subtree and clears descendant overrides. When only some descendants are selected, the group checkbox shows a partial-selection mark (−). | C Saved selection; U clarifications | BC-007, BC-008; FR-007, FR-008 |
| BR-025 | Saved selections retain group/element identities, explicit inclusions/exclusions and unassigned choices. Apply all saved explicit choices after hierarchy moves: an explicitly included subgroup remains included under an excluded parent, and an explicit exclusion remains recorded even under another excluded group. Do not rewrite or remove redundant saved choices when opening/running a report or when hierarchy changes. Only when the user saves report settings again, recalculate included/excluded IDs from the current selection state, retaining only overrides differing from inherited parent state. Evaluate membership using current hierarchy; new members inherit applicable state, moved-out members stop matching their old group unless explicitly selected. Apply saved instructions as-is to the actual group tree. Ignore missing identities without removing their saved choices; if the same identity returns, apply its saved inclusion/exclusion again. Q-06 is resolved. Report references do not prevent otherwise valid deletion. | C Saved selection; U clarification 2026-09-27 | BC-008; FR-008 |
| BR-026 | Report matching account entries of active transactions only. Fixed optional From/To dates inclusive. Optional Currency first grouping, then one of Category, Project, Correspondent, day, week, month, year. Weeks Monday–Sunday; calendar periods in device timezone. | C Filtering/Grouping | BC-007; FR-007 |
| BR-027 | Signed net totals only. Without Currency grouping: one included currency shows own/base columns, multiple currencies base only. With Currency grouping: own/base totals per currency; mixed-currency grand total base only. Totals only, no drill-through. | C Grouping | BC-007; FR-007 |
| BR-028 | Save/reopen, rename, edit and delete report definitions containing selections, grouping and fixed optional dates. Calculate results on demand from current bookkeeping data; a saved report stores calculation instructions, not calculated results. Deletion removes only its definition and leaves bookkeeping data untouched. Default name uses `yyyy-MM-dd HH:mm:ss` and is user-replaceable. Reject empty or whitespace-only names; duplicates remain allowed. Q-07 is resolved. | C Saved definitions; U clarification 2026-09-27 | BC-008; FR-008 |

### Synchronization, recovery and expiry

| ID | Rule | Source | Related IDs |
| --- | --- | --- | --- |
| BR-029 | Synchronize all business data including drafts/saved reports and System configuration bidirectionally; exclude Local configuration. Whole items selected; transactions/templates include all entries, no field/entry merge. One-sided changes propagate regardless of priority. Both-sided changes use Local/Master priority even if values match. Default Master; preference local. | S Scope/Priority; U clarifications | BC-009; FR-009 |
| BR-030 | Business validity overrides priority. Restore required accounts/currencies/groups/ancestor chains. Preserve account currency immutability, references, accounting and hierarchy rules. Resolve silently where valid; report corrections. If no valid resolution exists, explain issue/remedy and record diagnostics. Combined-case mechanics are TRD analysis under these rules (Q-11). | S Priority/Dependencies | BC-009, BC-012; FR-009, FR-012 |
| BR-031 | Same-ISO currencies merge into one, remapping account use; differing values follow priority. Same currency/date rates resolve by priority. Distinct conflicting unique names remain separate: retain preferred name, repeatedly suffix other `_1`. Correct hierarchy cycles preserving preferred valid hierarchy; report. | S Uniqueness | BC-009; FR-009 |
| BR-032 | Master retains deletion information until all remaining non-expired registered copies acknowledge it, then permanently removes eligible data. Remove expired registrations/newly eligible deletions before sync. Local deletion permanent after successful confirming sync. Failed/cancelled attempts retain pending deletions, subject to recovery/expiry. | S Deletion | BC-009–BC-011; FR-009–FR-011 |
| BR-033 | Delete-versus-edit follows priority only if business rules allow. Restore protected dependencies regardless of priority. If master merged/deleted a group but offline copy added an item there, restore group and keep new item there; report restoration. | S Deletion | BC-009; FR-009 |
| BR-034 | Exactly one trigger: manual only (default), on start, on exit. Sync button always available. No editing during sync or waiting. One sync per master at a time; inform waiting user, allow Cancel, no queue-wait limit. | S Triggers | BC-009; FR-009 |
| BR-035 | General connectivity rule: user-initiated network actions may start on any available internet connection; automatic network actions require Wi-Fi to start. Initial setup is manual, so Wi-Fi or mobile data is allowed; the same applies to manual sync, recovery and Download. Retain the previously agreed behavior that automatic sync/recovery started on Wi-Fi may continue over mobile after Wi-Fi loss. Startup sync failure shows message and permits ordinary offline work except pending recovery. Failed/cancelled exit sync permits closure, retains unsynced work subject to expiry. No later-Wi-Fi automatic retry agreed. | S Triggers; U general rule 2026-09-27 | BC-001, BC-002, BC-009–BC-011; FR-001, FR-002, FR-009–FR-011 |
| BR-036 | Stop after 30 seconds communication inactivity, not total duration. Incoming data/still-working responses reset timer. Healthy queue wait unlimited. Cancel/timeout obey pre/post-publication rules. | S Timeout | BC-009, BC-010; FR-009, FR-010 |
| BR-037 | Skip business transfer only when neither side changed since last completed sync. Changed master data downloads even with no local changes. Successful no-change sync refreshes expiry. | S No changes | BC-009; FR-009 |
| BR-038 | Before master publication, failed attempts preserve original business data and outgoing local changes. After publication, local failure does not undo master results. Resolve uncertain outcome before replay/discard of non-expired outgoing changes; never apply same batch twice. If unreachable, preserve pending state without claiming success/rollback. | S Recovery | BC-010; FR-010 |
| BR-039 | Recovery obtains latest published master including later device changes, not original interrupted result. Block all local business access/viewing/reports while pending. Attempt on startup regardless of trigger, subject to Android Wi-Fi rule; manual recovery available. Failure/cancellation leaves access blocked. | S Recovery | BC-010; FR-010 |
| BR-040 | Confirm receipt of revision actually installed; repeat lost confirmation without duplicate business application. Interrupted downloads restart from beginning. Do not discard possibly unreceived non-expired outgoing changes while outcome uncertain. | S Recovery | BC-010; FR-010 |
| BR-041 | Expire registration 90 days after last successful sync by master clock. Successful initial registration/download starts interval; every successful sync resets it. Once confirmed expired, remove registration, reject old sync, notify need for fresh copy and automatically delete expired local data including unsynced changes; no viewing/export. Expiry overrides pending recovery. | S Expiry | BC-011; FR-011 |
| BR-042 | Offer fresh copy from same master after expiry; start only on owner's Download action. Preserve Local configuration. | S Expiry | BC-011; FR-011 |
| BR-043 | Button opens latest sync report. Keep only latest attempt until app closes, including recovery. Show conflicts/corrections/errors and received local changes by type and created/updated/deleted counts; omit zeros/uploads. Write detailed diagnostic logs for critical unresolved failures to a log folder. Retain logs for 7 days, then automatically clean them up. Exact platform-specific folder paths belong to technical design. No Share/Export logs action has been selected. Q-12 is resolved at business level. | S Report; U clarifications | BC-012; FR-012 |
| BR-044 | First release supports favorite flags on currencies, groups and grouped elements (accounts, categories, correspondents, projects, templates), with user filtering by favorites. IsFavorite defaults to false for newly created items. Favorite changes synchronize as ordinary entity content and use configured conflict priority. All five root groups always have IsFavorite = false and cannot be marked as favorites. Favorites filtering retains non-favorite ancestor groups, including the root when needed, only as navigation paths to favorite descendants. Ancestors remain non-favorites; showing a navigation path does not change any favorite flags. Opening a favorite group with the filter active still shows only favorite descendants and necessary ancestor navigation paths; it does not reveal all children. Favorite status does not cascade to descendants. | U clarification 2026-09-28 | BC-003/004/006/009; FR-003/004/006/009 |

## Business Capabilities

`BC-###` identifies a high-level Business Capability in this BRD. Detailed Use Cases belong in the TRD as `UC-###-##`, linked to their parent capability; for example, `BC-003` maps to `UC-003-01` for merging category groups. Each detailed use case describes the specific action, alternative flows and expected results, and links to API/BFF operations. Traceability is **Business Capability → Detailed Use Case → API/BFF operations → test scenarios**. The detailed use cases themselves are not created by this BRD revision.

All business capabilities are first-release scope; relative priority not separately ranked. All use **ROLE-001**. Given/When/Then statements are **proposed acceptance formulations of cited decisions**, pending draft review; no new business policy is implied.

### BC-001 — Start bookkeeping for the first time

- Source: F Startup/Create/Initial content; C base currency.
- Trigger: owner starts without local copy.
- Preconditions: creating mode, any available internet connection for manual setup, master absence confirmed before enabling Create.
- Main flow: (1) Show setup/check master. (2) Owner enters login/password and base currency. (3) Select Create. (4) Establish owner/master and register device. (5) Initialize five roots/base currency at 1 and the base-currency rebalancing account in Account root; select it in System configuration. (6) Obtain local data and enter ordinary use.
- Alternate/error flows: existing master disables Create; unknown existence keeps disabled. No internet/failed download leaves no bookkeeping access. On failure, show the error and let the user retry manually; no automatic setup retries. If master already exists after initial download failure, keep Create disabled and retry through Open. Missing required login/password prevents setup submission; no extra length/character rules.
- Outcome: initialized local bookkeeping with immutable base currency.
- Linked requirements: BR-001–BR-004; FR-001; NFR-001; SC-001.
- Proposed scenarios: Given no master/local copy, when Create succeeds with PLN, then five roots, PLN rate 1, one PLN rebalancing account in Account root selected in System configuration, and no transactions. Given mobile data without Wi-Fi, when the user starts initial setup, then that connection is allowed because setup is manual. Given existing master, when setup checks, then Create disabled. Given master creation succeeded but initial download failed, then show the error, keep normal access unavailable and Create disabled, and retry downloading only when the user selects Open. Given either required login or password is missing, when submitting setup, then submission is rejected; version one imposes no additional length/character/complexity rule.

### BC-002 — Open existing bookkeeping or add a device

- Source: F Startup/Open; S Ownership/Registration.
- Trigger: open app with local copy, or choose setup Open without one.
- Preconditions: local copy for ordinary use; any available internet connection and valid credentials for manual device setup.
- Main flow: (1) Check local existence only. (2) If present enter open/editing mode. (3) Otherwise show creating mode. (4) Enter credentials/select Open. (5) Authenticate, register device, download full business data. (6) Open local bookkeeping.
- Alternate/error flows: ordinary use offline without login; recovery/expiry use BC-010/BC-011. Invalid credentials do not authorize cloud access; show the error and allow manual retry through Open; no automatic retries. Required-fields-only credential validation is settled. No local copy means no normal access.
- Outcome: ordinary offline use or registered new local copy.
- Linked requirements: BR-001–BR-003, BR-005; FR-002; NFR-001, NFR-002; SC-001, SC-004.
- Proposed scenarios: Given existing copy/no pending recovery, when opening offline, then no sign-in or internet needed. Given successful setup Open, then registration precedes download and local use becomes available. Given the app was reinstalled, when setup completes, then register a new local copy rather than reuse the old registration; the old registration follows the 90-day expiry rule.

### BC-003 — Maintain groups and classifications

- Source: C Groups/Names/Account lifecycle.
- Trigger: owner creates/edits/moves/merges/deletes organizational data.
- Preconditions: local access; same-type hierarchy.
- Main flow: (1) Select operation. (2) Supply permitted names/descriptions/destination. (3) Enforce uniqueness/dependency rules. (4) Merge moves children/elements, renames incoming collisions and deletes empty source.
- Alternate/error flows: reject root changes, cycles, individual name collisions, nonempty group deletion or classification deletion while used. Merge into parent/another ancestor/root is allowed; root cannot be merge source.
- Outcome: valid organization with references preserved.
- Linked requirements: BR-006–BR-009, BR-011; FR-003; SC-002.
- Proposed scenarios: Given A contains B, when merging A into B, then reject. Given a non-root source and its parent, another ancestor or root as destination, when merging, then move source children/elements to destination, resolve naming collisions and delete the empty source. Given destination Travel, when bulk moving another unique Travel, then rename incoming Travel_1, repeat suffix if occupied. Given root, when renaming, then reject. Given Description is omitted or empty, saving remains allowed if other validation passes.

### BC-004 — Maintain currencies and accounts

- Source: C Currency/Account lifecycle/Names/Precision/Dates.
- Trigger: add/maintain currency, rate or account.
- Preconditions: local access; base currency established.
- Main flow: (1) Add currency/initial rate or maintain ordinary rates. (2) Create account with group/currency and optional classifications. (3) Generate/edit name and Description. (4) Maintain allowed values/restore defaults.
- Alternate/error flows: reject base changes, nonpositive rounded rate, duplicate ISO, protected deletion or currency change on an already saved account. Currency choices come from system culture/region data. One entry per ISO code is required, using the first returned Name/Symbol; account-name order/separator policy is resolved.
- Outcome: usable accounts and rates.
- Linked requirements: BR-008–BR-015, BR-017, BR-021; FR-004; SC-002.
- Proposed scenarios: Given repeated regional entries for one ISO code, when building currency choices, then show one entry with the first returned Name and Symbol. Given prior draft/template use, when references removed and account currency changed, then still reject. Given all classifications absent in slash format, then valid name // generated. Given `DefaultAccountName` is `{Correspondent}/{Category}/{Project}` and an account was generated as `Alice/Food/Home`, when Correspondent Alice is renamed to Bob, then that account name remains `Alice/Food/Home`. Different LocalDbs may use different default formats. Given new local configuration, then default order is Correspondent, Category, Project and separator is `/`. Given format settings, when choosing an order, then all six permutations are available. Given an empty, whitespace or multi-character separator, then it is invalid; a single non-whitespace character is valid. Given a rate that rounds to zero at configured RPr, reject. Given dataset creation, persist selected APr/RPr and base currency; later changes are rejected.

### BC-005 — Record, revise or duplicate transactions

- Source: C Transactions/Precision/Currency/Dates.
- Trigger: create/edit/duplicate/delete transaction.
- Preconditions: local access; at least two account-bearing entries to save.
- Main flow: (1) Set time/accounts/amounts/Description. (2) Default rates by transaction date in the current device/UI timezone, permit non-base override/restore. (3) Calculate rounded base amounts. (4) Save balanced complete transaction as active or new unbalanced transaction as draft. (5) Calculate balances on demand.
- Alternate/error flows: reject missing accounts, fewer than two entries, invalid rate/time or invalid active edit. Empty amount becomes zero. Duplicate resets time to now; active deletion removes contribution. Numeric-limit failures reject the operation with an error and unchanged existing data; exact limits belong in the TRD. Apply the established transaction requirements; Q-09 is closed.
- Outcome: active balanced record or excluded draft; account currency remains immutable from its first save.
- Linked requirements: BR-009, BR-011, BR-015–BR-021; FR-005; SC-002.
- Proposed scenarios: Given complete +100/-100, when saved, then active automatically. Given new +100/-99, when saved, then draft excluded from totals. Given 55555.5555 × 0.2222, then 12344.4444; opposite -12344.4444 at 1 balances. Given active record, when saving unbalanced edit, then reject. Given an amount or calculation exceeds supported numeric limits, when the operation is attempted, then reject it, show an error and leave existing data unchanged.

### BC-006 — Reuse transaction templates

- Source: C Templates.
- Trigger: define/apply template or create from transaction.
- Preconditions: local access; group-unique Name and account per entry.
- Main flow: (1) Define accounts/amounts/Description or copy active/draft transaction. (2) Save template. (3) Apply into transaction for review with applicable rates/copied Description. (4) Save under transaction rules.
- Alternate/error flows: empty/unbalanced templates allowed; applying does not save. For empty/single-entry templates, editor opens with those entries and Save remains disabled until at least two entries exist and every entry has an account. User adds missing entries or cancels without saving. Other established transaction save requirements still apply; Q-09 is closed.
- Outcome: reusable template and optionally reviewed saved transaction.
- Linked requirements: BR-008, BR-009, BR-011, BR-015, BR-022; FR-006; SC-003.
- Proposed scenarios: Given unbalanced draft, when making template, then copy accounts/amounts/Description. Given applied template, then transaction persists only on Save and uses date-applicable rates. Given a single-entry template, when applied, then Save is disabled until the user adds at least a second entry and every entry has an account. Given an empty template, then the editor opens empty with Save disabled under the same rule. Given Cancel before saving, then no new transaction is saved.

### BC-007 — Calculate balances and reports

- Source: C Filtering/Grouping/accounting/dates.
- Trigger: request balance or configure report.
- Preconditions: local access; report choices.
- Main flow: (1) Start unchecked. (2) Select classifications/unassigned/overrides. (3) Set dates/grouping. (4) Match accounts using OR within each type, AND across types. (5) Sum active entries, show signed totals with correct currency columns.
- Alternate/error flows: empty type selection gives empty report/warning. Drafts excluded; current classifications apply historically. Partially selected groups show a partial-selection mark (−). Saved explicit choices survive hierarchy moves. Missing identities are ignored; their saved choices apply again if they return.
- Outcome: on-demand selected totals with device-timezone boundaries.
- Linked requirements: BR-010, BR-019, BR-021, BR-023, BR-024, BR-026, BR-027; FR-007; SC-002, SC-003.
- Proposed scenarios: Given no Category selection, then empty with warning. Given A selected, child C excluded, X in C explicitly included, then A outside C plus X. Given mixed currencies/no Currency grouping, then base totals only. Given a group with only some descendants selected, then its checkbox shows a partial-selection mark (−).

### BC-008 — Save and reopen report settings

- Source: C Saved selection/definitions.
- Trigger: save/reopen, rename, edit or delete a saved report.
- Preconditions: local access; selections/grouping/optional fixed dates.
- Main flow: (1) Offer editable default name in `yyyy-MM-dd HH:mm:ss` format. (2) Save choices. (3) Reopen against current hierarchy. (4) Ignore missing references and apply inheritance/overrides; calculate results on demand from current bookkeeping data. (5) Allow renaming and editing saved instructions, or deleting the saved definition without deleting bookkeeping data.
- Alternate/error flows: duplicate names allowed; missing entities do not block execution. Preserve choices until resave. Reject empty or whitespace-only report names; duplicates allowed. Moves preserve saved explicit choices, including redundant overrides; only resaving recalculates stored IDs. A returning identity resumes its saved choice; Q-06 is resolved.
- Outcome: reusable report retaining selection intent as membership changes, updated definition, or deleted definition; bookkeeping data remains untouched by report deletion.
- Linked requirements: BR-024, BR-025, BR-028; FR-008; SC-003.
- Proposed scenarios: Given selected group gains member, then new member inherits unless overridden. Given a saved included/excluded ID is absent from the actual tree, when running the report, then ignore it without modifying saved instructions; if that same ID returns while its saved choice remains, apply that choice again. Given explicitly included subgroup B moves under an excluded parent, when the report runs, then B stays included. Given explicitly excluded B moves under another excluded group, then its saved exclusion remains recorded; only a user save recalculates IDs and may remove that now-redundant exclusion. Given selected Group1, excluded descendant Group2 and included deeper Group4, when Group2 checked again, then whole subtree selected and redundant overrides cleared. Given a saved report, when deleted, then only its calculation definition is removed and accounts/transactions remain unchanged. Given a saved report and changed bookkeeping data, when run again, then results are calculated from current data. Given a new report, then its default name follows `yyyy-MM-dd HH:mm:ss`. Given an empty or whitespace-only report name, when saving, then reject; an existing duplicate name alone does not prevent saving.

### BC-009 — Synchronize business changes

- Source: S Configuration/Conflicts/Deletion/Triggers/No changes.
- Trigger: Sync button or configured start/exit event.
- Preconditions: registered non-expired copy, authenticated cloud access, required connectivity.
- Main flow: (1) Prevent editing; wait/inform if another sync active. (2) Remove expired registrations/eligible deleted data. (3) Reconcile changes by priority. (4) Enforce validity with silent permitted corrections. (5) Publish master result, obtain local result and confirm receipt. (6) Reset expiry; provide latest report.
- Alternate/error flows: no changes either side skips business transfer. Cancel allowed; healthy wait unlimited; inactivity timeout separate. Ordinary startup failure permits offline work; post-publication uncertainty BC-010; expiry BC-011. Unresolvable conflicts explain/log; combined-case mechanics belong to TRD analysis under settled policy (Q-11).
- Outcome: reconciled business data retaining local configuration, or described failure/recovery state.
- Linked requirements: BR-005, BR-011, BR-029–BR-038; FR-009; NFR-001–NFR-004; SC-004.
- Proposed scenarios: Given Food renamed Groceries locally and Meals on master, when Master priority, then Meals. Given master-deleted account used in offline transaction, then restore dependency despite Master priority. Given neither side changed, then no business transfer but expiry renewed. Given System decimal display places changed on one device, when synchronization propagates that change, then other synchronized copies use that System value; their Local account-name formats remain unchanged. Given mobile-only Android, automatic sync does not start, manual may. Given authorization remembered after successful Create/Open, when normal synchronization runs, then no repeated login/password entry is required while cloud access remains authenticated.

### BC-010 — Recover interrupted synchronization

- Source: S Recovery/version direction.
- Trigger: interrupted/uncertain sync or startup with pending recovery.
- Preconditions: pending state, non-expired copy; connectivity to resolve outcome.
- Main flow: (1) Block business access. (2) Resolve master acceptance. (3) Preserve unreceived changes or avoid replaying accepted batch. (4) Obtain latest published master including later changes. (5) Confirm installed result and complete recovery.
- Alternate/error flows: unreachable preserves uncertainty/block. Lost receipt repeatable without duplicate application. Interrupted download starts over. Cancel/timeout leaves pending. Android automatic start requires Wi-Fi; manual may use mobile. Expiry supersedes recovery.
- Outcome: consistent usable local data or retained pending state; no rollback of already published master result.
- Linked requirements: BR-005, BR-035, BR-036, BR-038–BR-040; FR-010; NFR-002–NFR-004; SC-004.
- Proposed scenarios: Given lost publication response, then resolve before replay/discard. Given later device publication, when recovering, then latest version obtained. Given pending recovery/no connectivity, then viewing/reports blocked.

### BC-011 — Replace an expired local copy

- Source: S Expiry.
- Trigger: master confirms expiration.
- Preconditions: 90 days since successful sync by master clock.
- Main flow: (1) Remove registration/reject old sync. (2) Inform fresh copy required. (3) Delete old local data including unsynced changes; no viewing/export. (4) Preserve Local configuration/offer Download. (5) Owner selects Download to obtain fresh data from same master.
- Alternate/error flows: expiry overrides pending recovery; no preservation option in this flow. Failed download leaves no usable local copy; the existing user-triggered Download flow remains applicable.
- Outcome: fresh local data after successful download or waiting for completion.
- Linked requirements: BR-005, BR-032, BR-041, BR-042; FR-011; NFR-004; SC-004.
- Proposed scenarios: Given confirmed expiry/unsent edits, then old copy deleted without export. Given replacement offer, then no download before user action; Local configuration survives.

### BC-012 — Inspect latest sync result

- Source: S Report/diagnostics.
- Trigger: owner opens latest-report button.
- Preconditions: report available in current app session.
- Main flow: (1) Open latest attempt. (2) Show conflicts/corrections/errors and received created/updated/deleted counts by type. (3) Omit zero values/uploads.
- Alternate/error flows: newer attempt replaces report; closure discards it. Critical failures have separate diagnostic logs in a log folder, automatically cleaned up after 7 days. Exact platform-specific paths are technical design details. Full replacement must count business changes rather than every downloaded row as new; calculation is later design.
- Outcome: owner can inspect meaningful latest-attempt results.
- Linked requirements: BR-030, BR-043; FR-012; NFR-005; SC-004.
- Proposed scenarios: Given two categories received/three transactions uploaded, then category received count shown, uploads not counted. Given app closed/reopened, then previous session report not retained. Given a diagnostic log has reached the end of its 7-day retention period, then it is automatically cleaned up.

## Functional Requirements

All capabilities below are first-release decisions from the cited sources; their consolidated wording is Draft, not approved.

| ID | Capability and verifiable outcome | Priority/status | Source | Linked business capabilities/rules |
| --- | --- | --- | --- | --- |
| FR-001 | Select startup mode and initialize bookkeeping with fixed base currency/agreed content. | First release; In Review | F | BC-001; BR-001–BR-004 |
| FR-002 | Open locally offline or authenticate/register/download on new device. | First release; In Review | F/S | BC-002; BR-001–BR-003, BR-005 |
| FR-003 | Maintain valid groups/classifications, roots, moves/merges/deletions, naming and favorites filtering. | First release; In Review | C | BC-003; BR-006–BR-009, BR-011 |
| FR-004 | Maintain currencies/rates/accounts with defaults, currency immutability after first save and protected deletion. | First release; In Review | C | BC-004; BR-008–BR-015, BR-017, BR-021 |
| FR-005 | Record/duplicate/edit/delete transactions, enforce balancing/draft rules and calculate balances. | First release; In Review | C | BC-005; BR-009, BR-011, BR-015–BR-021 |
| FR-006 | Maintain/apply templates, create from active/draft transactions without bypassing save rules. | First release; In Review | C | BC-006; BR-008, BR-009, BR-011, BR-015, BR-022 |
| FR-007 | Calculate reports using agreed filters/dates/grouping/currency and active-only totals. | First release; In Review | C | BC-007; BR-010, BR-019, BR-021, BR-023, BR-024, BR-026, BR-027 |
| FR-008 | Save/reopen, rename, edit and delete report definitions; calculate on demand against current data/hierarchy with inherited selections and overrides. Deleting a definition does not delete bookkeeping data. | First release; In Review | C/U | BC-008; BR-024, BR-025, BR-028 |
| FR-009 | Configure/perform synchronization preserving validity and resolving/reporting conflicts. | First release; In Review | S | BC-009; BR-005, BR-011, BR-029–BR-038 |
| FR-010 | Recover uncertain/partly completed sync without duplicate application/premature loss. | First release; In Review | S | BC-010; BR-005, BR-035, BR-036, BR-038–BR-040 |
| FR-011 | Enforce expiry and owner-triggered fresh download, retaining Local configuration. | First release; In Review | S | BC-011; BR-005, BR-032, BR-041, BR-042 |
| FR-012 | Present latest sync report and diagnostics for unresolved critical failures. | First release; In Review | S/U | BC-012; BR-030, BR-043 |

## Business-Facing Non-Functional Requirements

| ID | Expectation | Measure/target/status | Source | Linked business capabilities |
| --- | --- | --- | --- | --- |
| NFR-001 | Offline availability | Ordinary local work without internet/sign-in on Android/Windows; setup/cloud sync need connectivity; recovery/expiry exceptions explicit | S/F confirmed | BC-001, BC-002, BC-009 |
| NFR-002 | Ownership/accounting integrity | No cross-owner cloud access; exact-zero active rounded totals; dependencies and locks preserved | C/S confirmed | BC-002–BC-007, BC-009, BC-010 |
| NFR-003 | Communication inactivity detection | 30 seconds without response/data, not total duration; healthy queue wait unlimited/cancellable | S confirmed | BC-009, BC-010 |
| NFR-004 | Predictable recovery/expiry | No duplicate application; pending recovery blocks access; fixed 90-day expiry/master clock; disposal as agreed | S confirmed | BC-009–BC-011 |
| NFR-005 | Session-limited report | Latest attempt only, retained until closure; separate diagnostic logs retained for 7 days, then automatically cleaned up | S/U confirmed | BC-012 |

No response-time, dataset-size, accessibility, uptime percentage or quantified performance target was supplied. Approval considers the stated functional acceptance criteria without inventing those targets; exact numeric bounds are reserved for the TRD under resolved Q-05.

## Assumptions and Dependencies

### Facts and decisions

| Statement | Source | Decision owner/date |
| --- | --- | --- |
| Core manual bookkeeping/templates/saved reports/merging first release | C Decisions | Requesting user, 2026-09-25–26 |
| Sync first release on Android/Windows; ordinary use offline | S Scope/Ownership | Requesting user, 2026-09-26–27 |
| First-use/seed content agreed; single-user personal prototype | F/U | Requesting user, 2026-09-27 |
| Multiple masters, local-copy names and multi-user administration version two | S/F Deferred | Requesting user, 2026-09-26–27 |
| Consolidated drafting authorized; no artifact approval inferred | U | Requesting user, 2026-09-27 |

### Assumptions

No remaining unconfirmed assumption is used as an active business rule. Earlier assumptions are resolved in the decision register.

### Decision register and approval item

Resolved rows preserve decisions and must not be re-asked. Q-01 is the remaining approval item. Technical analysis identified in Q-05/Q-11 is required during TRD work and cannot change the business rules without a new decision.

| ID | Question | Decision owner | Impact on draft/approval | Affected IDs |
| --- | --- | --- | --- | --- |
| Q-01 | Pending exact-version approval: accept BRD 0.32 and its proposed SC-001–SC-004 acceptance criteria, and identify the approver. No additional performance/capacity target was supplied; none is silently added. | Requesting user | Approval decision pending; not a missing functional requirement | SC-001–SC-004; all BCs |
| Q-02 | Resolved 2026-09-28: Description is optional and visible; previous mandatory-description rejection is superseded. | Requesting user, 2026-09-27 | Closed | BR-009; FR-003, FR-004 |
| Q-03 | Resolved: per-LocalDb `DefaultAccountName`; six field orders; one non-whitespace separator. Initial order: Correspondent, Category, Project. Initial separator: `/`. Existing account names do not change automatically after classification renames. | Requesting user, 2026-09-27 | Closed; no remaining format decision | BR-012; FR-004 |
| Q-04 | Resolved: use supplied CultureInfo/RegionInfo approach; keep one entry per ISO currency code using the first returned Name and Symbol. Name/Symbol remain editable and restorable from source defaults. | Requesting user, 2026-09-27 | Closed | BR-013; FR-004 |
| Q-05 | Resolved: APr/RPr are creation-time immutable System settings; calculations follow BR-017/018. APr/RPr ranges 0–4 and defaults 2/4 are confirmed. Display uses APr for amounts/BaseAmount and RPr for rates, without an independent display setting. Numeric overflow handling remains rejection without mutation; exact numeric bounds belong in TRD. | Requesting user, 2026-09-28 | Closed at business level; exact numeric bounds remain technical | BR-005/017/018; FR-001/004/005/009 |
| Q-06 | Resolved: partial checkbox for mixed selection; explicit choices survive moves, including redundant ones. Apply saved JSON as-is to the actual tree; ignore missing IDs, apply their saved choices if they return. Only user save recalculates stored included/excluded IDs. | Requesting user, 2026-09-27 | Closed | BR-024, BR-025; FR-007, FR-008 |
| Q-07 | Resolved: allow report rename/edit/delete; delete only calculation instructions. Default name is `yyyy-MM-dd HH:mm:ss`; reject empty/whitespace-only names; duplicates allowed. | Requesting user, 2026-09-27 | Closed | BR-028; FR-008 |
| Q-08 | Resolved: a non-root group may merge into its parent, another ancestor or the root. Root as source and descendant as destination remain forbidden. | Requesting user, 2026-09-27 | Closed | BR-007; FR-003 |
| Q-09 | Closed by existing decisions: every saved transaction has occurrence time within BR-021, at least two account-bearing entries, amounts defaulting to zero, valid rates and BR-017–BR-019 validation; Description optional. Empty/single-entry templates open for editing, with Save disabled until minimum account-bearing entries exist, or Cancel without saving. No additional required business field was specified. | C Transactions/Precision/Dates; U template confirmation | Closed; apply recorded rules without inventing more required fields | BR-015–BR-022; FR-005, FR-006 |
| Q-10 | Resolved for version one: backup/restore deferred to version two. Azure-side-only backup/restore is the user-proposed direction; detailed policy remains future work. | Requesting user, 2026-09-27 | Closed for version one; version-two design pending | BC-010, BC-011; SC-004 |
| Q-11 | Business policy already settled: whole-version priority is subordinate to validity; restore dependencies, resolve name/currency/rate conflicts under BR-029–BR-033, and explain/log cases with no valid resolution. Combined-case algorithms and initial-rate remapping belong to TRD analysis. Return only genuinely new business-policy conflicts for decision; never silently relax rules. | S Priority/Dependencies/Uniqueness | Closed as generic business blocker; technical analysis remains required | BR-029–BR-033; FR-009 |
| Q-12 | Resolved: write diagnostic logs to a log folder; retain for 7 days, then automatically clean up. Exact platform-specific folder paths belong to technical design. Share/Export logs was proposed but not selected. | Requesting user, 2026-09-27 | Closed at business level | BR-043; FR-012 |
| Q-13 | Resolved: initial setup failure shows an error; no local copy means no bookkeeping access. Retry only manually. If master creation succeeded but initial download failed, Create stays disabled and the user selects Open to retry downloading. Existing expiry replacement uses the previously agreed Download action. Exact error wording is a UI detail. | Requesting user, 2026-09-27; reaffirmed after repeated question | Closed; do not re-ask | BC-001, BC-002, BC-011 |
| Q-14 | Resolved: login and password are required fields only, with no additional length/character/complexity rules in version one. Remember authorization after successful Create/Open for normal sync. Login/password change and recovery deferred to version two. | Requesting user, 2026-09-27 | Closed | BR-003, BR-005; FR-001, FR-002, FR-009 |
| Q-15 | Resolved: reinstalling the app registers a new local copy; the old registration remains subject to the existing 90-day expiry rule. Retry duplicate-prevention mechanics remain later technical design. | Requesting user, 2026-09-27 | Closed for reinstallation behavior | FR-002, FR-010, FR-011 |
| Q-16 | Resolved: manual network actions may use any internet connection; automatic actions require Wi-Fi to start. Initial setup is manual and allows any connection. Existing ordinary offline use and previously agreed continuation after Wi-Fi loss remain unchanged. | Requesting user, 2026-09-27 | Closed | BC-001, BC-002, BC-009–BC-011 |

### Dependencies

| Dependency | Owner/source | Required by | Status |
| --- | --- | --- | --- |
| Exact-version approval and later technical analysis | Requesting user; C/S/F/U | TRD drafting and implementation handoff respectively | Approval pending; business decisions recorded |
| Internet/cloud service for setup/sync/recovery | S/F | BC-001, BC-002 setup, BC-009–BC-011 | Confirmed; uptime target unknown |
| Device timezone/currency defaults | C/U | BC-004, BC-005, BC-007 | Timezone and culture/region source settled; one entry per ISO code with first returned Name/Symbol confirmed |
| Later technical requirements and verification | TRD phase after BRD approval | Implementation handoff | Not authorized by draft |

Technical preferences remain in [core discovery](001-personal-bookkeeping/discovery.md), [sync discovery](002-synchronization/discovery.md), [first-use discovery](003-first-using/discovery.md) and unchanged [legacy TRD](TRD.md): SQLite local/master files, Azure Functions/hardcoded prototype address, SQLite Admin.db then Microsoft SQL Server administration in version two, version publication and SyncId/acknowledgement, parent references and saved-report representation. User additionally requires two configuration tables in the database, System and Local; System synchronizes and Local does not. These are design inputs, not new business requirements. Hosting, schema, serialization, atomic activation/publication, metadata isolation, snapshot cleanup, initial-rate sentinel, log storage and report-count calculation belong in later technical work. Startup existence-only selection is distinct from sync download/activation safeguards.

### User-supplied currency catalog example

Source: requesting user, current BRD review, 2026-09-27; reaffirmation of the earlier example. Preserved as technical input for later TRD, not newly generated implementation. It enumerates all cultures, skips neutral cultures, skips conversions that fail, and maps region currency values. The sample does not itself deduplicate repeated ISO codes. The user subsequently confirmed one available-list entry per ISO code; apply that requirement in addition to the supplied example. For each ISO code, retain the first returned Name and Symbol (user confirmation). BR-013 also requires uniqueness among currencies created in bookkeeping.

```csharp
private static IEnumerable<RegionInfo> GetRegionInfos()
{
    return CultureInfo.GetCultures(CultureTypes.AllCultures)
        .Where(c => !c.IsNeutralCulture)
        .Select(c =>
        {
            try
            {
                return new RegionInfo(c.Name);
            }
            catch
            {
                return null;
            }
        })
        .OfType<RegionInfo>();
}

public static List<CurrencyProfile> GetListOfAvailableCurrencyData()
{
    return [.. GetRegionInfos()
        .Select(ri => new CurrencyProfile
        {
            Code = ri.ISOCurrencySymbol,
            Symbol = ri.CurrencySymbol,
            Name = ri.CurrencyEnglishName
        })];
}
```

## Risks

| Risk | Business impact | Mitigation/question | Owner/status |
| --- | --- | --- | --- |
| Expiry deletes unsynced work | Lost edits after 90-day inactivity | Explicit user-selected expiry policy; backup/restore deferred to version two | Owner; policy confirmed |
| Conflict priority discards losing versions | Concurrent edits may be lost | Validity overrides/report; no edit-history promise | Owner; S confirmed |
| Unknown sync outcome while offline | Recovery blocks bookkeeping access | Preserve pending state until resolved | Owner; S confirmed |
| Current membership/classification affects historical reports | Earlier-period results can change | Intentional behavior; saved explicit choices retained and reapplied as agreed | Owner; confirmed |
| Lost credentials; no first-version backup/restore | Cloud reconnection/recovery may be unavailable | Credential change/recovery and backup/restore deferred to version two; required-fields-only initial validation and remembered authorization for normal sync are settled | Owner; deferrals confirmed |

## Deferred Requirements

| Candidate | Source/decision | Revisit trigger/owner | Status |
| --- | --- | --- | --- |
| Multiple separate masters/startup choice; local-copy display names | S | Version two; owner | Deferred |
| Multi-user administration | F refines S | Version two; owner; no shared ownership implied | Deferred |
| Account/Account Group report grouping | C 2026-09-26 correction | Next version; owner | Deferred |
| Database templates for initial setup | F | Future version unspecified | Deferred |
| Browser/Apple clients | S | Later version unspecified | Deferred |
| Backup and restore | U, 2026-09-27; Azure-side-only proposed | Version two; owner | Deferred; detailed policy pending |
| Login/password changes and recovery | U, 2026-09-27 | Version two; owner | Deferred |
| Bank imports, archive flags, other account presets (excluding the initialization rebalancing account) | C | Later version | Deferred |
| Bulk account copying/bulk default-name restoration/smarter missing-value formatting | C | Future work | Deferred |
| Alternative cloud provider/configurable expiry | S | Possible later consideration; no commitment | Deferred candidates |

Backup/restore is deferred to version two (user decision, 2026-09-27). User proposed Azure-side-only backup/restore; coverage, retention, restore behavior and treatment of existing local copies remain future-version decisions.

## Traceability Summary

| Outcome | Success | Business capability | Rules | Functional | NFR | Source/status |
| --- | --- | --- | --- | --- | --- | --- |
| Initialize bookkeeping | SC-001 | BC-001 | BR-001–BR-004 | FR-001 | NFR-001 | F; confirmed source/Draft wording |
| Open offline/add device | SC-001, SC-004 | BC-002 | BR-001–BR-003, BR-005 | FR-002 | NFR-001, NFR-002 | F/S |
| Valid organization | SC-002 | BC-003 | BR-006–BR-009, BR-011 | FR-003 | NFR-002 | C |
| Accounts/currencies | SC-002 | BC-004 | BR-008–BR-015, BR-017, BR-021 | FR-004 | NFR-002 | C |
| Correct transactions | SC-002 | BC-005 | BR-009, BR-011, BR-015–BR-021 | FR-005 | NFR-002 | C |
| Reuse templates | SC-003 | BC-006 | BR-008, BR-009, BR-011, BR-015, BR-022 | FR-006 | NFR-002 | C |
| Meaningful totals | SC-002, SC-003 | BC-007 | BR-010, BR-019, BR-021, BR-023, BR-024, BR-026, BR-027 | FR-007 | NFR-002 | C |
| Reusable reports | SC-003 | BC-008 | BR-024, BR-025, BR-028 | FR-008 | None separately specified | C |
| Reconcile work | SC-004 | BC-009 | BR-005, BR-011, BR-029–BR-038 | FR-009 | NFR-001–NFR-004 | S |
| Recover sync | SC-004 | BC-010 | BR-005, BR-035, BR-036, BR-038–BR-040 | FR-010 | NFR-002–NFR-004 | S |
| Replace expired copy | SC-004 | BC-011 | BR-005, BR-032, BR-041, BR-042 | FR-011 | NFR-004 | S |
| Inspect sync results | SC-004 | BC-012 | BR-030, BR-043 | FR-012 | NFR-005 | S |

### Legacy identifier continuity

Legacy IDs remain permanent aliases or explicitly superseded references; never reuse for unrelated meanings. New IDs normalize the consolidated template. Discovery preserves historical rules and corrections.

| Legacy ID | Current equivalent/disposition |
| --- | --- |
| BRD-GRP-001 | BR-006; five single-root hierarchies |
| BRD-GRP-002 | BR-006 business hierarchy; self-parent/foreign-key details retained in C for later TRD |
| BRD-GRP-003 | BR-006; child-collection representation remains technical source input |
| BRD-GRP-004 | BR-006; group membership/collection representation in C |
| BRD-GRP-005 | BR-006 mandatory same-type group; foreign-key representation in C |
| BRD-GRP-006 | BR-010 classification meanings |
| BRD-ACC-001 | BR-010 mandatory currency |
| BRD-ACC-002 | BR-010 independent optional classifications |
| BRD-ACC-003 | Superseded by BR-011 currency immutability after first successful account save; standalone sync lock trigger not reintroduced |
| BRD-REP-001 | FR-007/BR-023–BR-027; refined report rules |
| BRD-CUR-001 | BR-003/BR-004 base selection |
| BRD-CUR-002 | BR-004 immutable base |
| BRD-CUR-003 | BR-014/BR-015 relative rates; collection representation in C |
| BRD-CUR-004 | BR-014 base rate 1 |
| BRD-TXN-001 | FR-005 transaction entry |
| BRD-TXN-002 | BR-016/BR-020/BR-021 occurrence; optional text renamed Description |
| BRD-TXN-003 | BR-016 minimum two entries, including drafts |
| BRD-TXN-004 | BR-015–BR-017 account/currency/amount/rate |
| BRD-TXN-005 | BR-018 rounded base amount |
| BRD-TXN-006 | BR-018 exact zero for active transactions; drafts permitted |
| BRD-TXN-007 | Superseded BR-019: drafts excluded from calculations but may synchronize/create templates |

## Clarification Log

### 2026-09-28 — Local calendar dates with UTC timestamp storage

- Confirmed explanation: transactions remain stored in UTC; CurrencyRate.Date is DateOnly without timezone conversion; rate lookup and report calendar grouping use the current UI/device timezone.
- Accepted answer: “agree. Make corrections”, following clarification of the UTC+8 example.
- Source: requesting user in this chat. Explicit request authorizes correcting both BRD and TRD for this rule.
- Affected IDs: BR-014/015/021/026; BC-004/005/007.
- Example: January 1 04:00 at UTC+8 is stored as December 31 20:00 UTC but selects the rate applicable on January 1. January 1 reporting spans December 31 16:00 UTC inclusive to January 1 16:00 UTC exclusive.
- This controlled Draft 0.33 revision preserves the prior approval provenance below; the 0.32 approval does not cover this modified version.

### 2026-09-28 — Account currency fixed on first save

- Exact question: “Does closes the creating dialog mean successfully saves the new account? Canceling would create nothing.”
- Accepted answer: “yes”, following the request to remove CurrencyLocked and allow currency changes only during account creation.
- Source: requesting user in this chat.
- Decision: BR-011 now makes currency immutable on first successful account save; transaction/template use is no longer the trigger. Reference-based deletion protections remain unchanged.
- Affected IDs: BR-011/030; BC-004/005; FR-004.
- Draft 0.33 becomes Draft 0.34; prior BRD 0.32 approval provenance is retained and does not cover this revision.

### 2026-09-28 — Hidden template Description

- Exact question: “Should the app copy Name → Description on every template save, including renaming, or only when creating the template?”
- Accepted answer: “every”, following “Let's leave both. We will hide Template.Description form user in the first version (app makes copy name->description). After that I will think what to do with it”.
- Source: requesting user in this chat.
- Decision: BR-009/022 retain both fields; version 1 keeps Description hidden and copies final Name into it on every template save. Comment remains the transaction-comment source; future Description behavior is undecided.
- Affected IDs: BR-009/022; BC-006; FR-006.
- Draft 0.34 becomes Draft 0.35; prior 0.32 approval does not cover this revision.

### 2026-09-28 — Mandatory descriptions and optional rate comments

- Exact question: “Keep a comment on each rate, for notes such as bank exchange rate or manual estimate, separate from the currency's own comment?”
- Accepted answer: “yes. Discription - is mandatory field like long name. Comment - is optional. Rate, Template, Transaction comments are optional. For groups and elements description are mandatory”.
- Source: requesting user in this chat.
- Decision: BR-009 clarifies Description versus Comment; BR-014 includes optional per-rate Comment. The hidden, app-maintained Template.Description exception remains unchanged.
- Affected IDs: BR-009/014; BC-003/004/005/006; FR-003/004/005/006.
- Draft 0.35 becomes Draft 0.36; prior 0.32 approval does not cover this revision.

### 2026-09-28 — Visible optional Description replaces Comment

- Accepted direction: “Name - mandatory; Description - optional; Comment - remove at all. Remove in Template. Substitute with Description in Rate.”
- Clarification accepted: replace Transaction.Comment with optional Description and copy Template.Description to transactions; answer “yes”. Further correction: “no hidden”.
- Source: requesting user in this chat.
- Decision: BR-009/022 now require visible optional Description, no Comment fields, and no hidden/automatic name-copy template behavior. Existing named entities retain mandatory Name; no new Name field is added to rates, transactions or entries.
- Affected IDs: BR-009/013/014/016/020/022; BC-003–BC-006; Q-02.
- Draft 0.36 becomes Draft 0.37; previous 0.32 approval does not cover this revision.

### 2026-09-28 — Synchronized favorites

- User direction: “keep it. It's small feature. It's flag for favorite groups and elements. User can filter by it”.
- Exact follow-up: “Should favorites synchronize across devices as ordinary content changes, using ModificationType.Content and the configured conflict priority?” Accepted answer: “yes”.
- Source: requesting user in this chat.
- Decision: BR-044 records first-release favorites/filtering and content synchronization. Unspecified currency/root/filter presentation details are not inferred.
- Affected IDs: BR-044; BC-003/004/006/009; FR-003/004/006/009.
- Draft 0.37 becomes Draft 0.38; prior 0.32 approval remains historical.

### 2026-09-28 — Currency favorites

- Exact question: include currencies in favorites too? Your Currency model already has IsFavorite.
- Accepted answer: yes.
- Source: requesting user in this chat.
- Decision: currencies support favorites and filtering with the same ordinary Content synchronization and configured conflict-priority rules as groups/elements.
- Affected IDs: BR-044; BC-004/009; FR-004/009.
- Draft 0.38 becomes Draft 0.39; full-version approval remains outstanding.

### 2026-09-28 — Favorite default

- Exact question: should IsFavorite default to false for newly created items?
- Accepted answer: yes.
- Source: requesting user in this chat.
- Decision: new favorite-capable items default to IsFavorite = false; existing favorite values and synchronization rules are unchanged.
- Affected IDs: BR-044.
- Draft 0.39 becomes Draft 0.40; full-version approval remains outstanding.

### 2026-09-28 — Root groups remain non-favorite

- Exact question: may users mark root groups as favorites, or should roots always remain non-favorites?
- Accepted answer: non favorite.
- Source: requesting user in this chat.
- Decision: all five root groups have IsFavorite fixed to false; the general favorites feature does not relax root protection.
- Affected IDs: BR-044; BR-006 root protection.
- Draft 0.40 becomes Draft 0.41; full-version approval remains outstanding.

### 2026-09-28 — Ancestor paths in favorites filtering

- Exact question: when filtering favorites, show non-favorite parent groups only as navigation paths to favorite descendants? They would remain non-favorites.
- Accepted answer: yes.
- Source: requesting user in this chat.
- Decision: retain necessary ancestor paths without changing IsFavorite; do not treat navigation ancestors as favorite matches.
- Affected IDs: BR-044.
- Draft 0.41 becomes Draft 0.42; full-version approval remains outstanding.

### 2026-09-28 — Favorites-only children

- Exact question: when opening a favorite group with the filter active, show only favorite descendants, or all its children?
- Accepted answer: only favorite.
- Source: requesting user in this chat.
- Decision: keep the filter active inside favorite groups; retain only favorite descendants and their necessary navigation ancestors, without inherited favorite status.
- Affected IDs: BR-044.
- Draft 0.42 becomes Draft 0.43; full-version approval remains outstanding.

### 2026-09-28 — Immutable precision settings and balancing button

- Accepted sequence: separate APr/RPr; round each BaseAmount to APr; offer a base-currency balancing entry for any difference only when the user clicks Add balancing entry.
- Final answer: configured at the creating. Set in System config. User cannot change them. So user cannot change Base currency, APr, RPr.
- Source: requesting user in this chat.
- Decision: record creation-time immutable settings and the explicit balancing action; prior fixed-four-place/no-assisted-adjustment rules are superseded. Precision ranges/defaults, balancing-account configuration details and physical scaling remain unresolved.
- Affected IDs: BR-017/018; Q-05; BC-001/005/009.
- BRD Draft 0.43 becomes 0.44; full-version approval remains outstanding.

### 2026-09-28 — Precision bounds and defaults

- Exact question: what allowed ranges and defaults should APr and RPr have?
- Accepted answer: 0-4. ARr - 2 default, RPr - 4 default. ARr is interpreted as APr in context.
- Source: requesting user in this chat.
- Decision: both precisions accept integer values 0 through 4 inclusive; APr defaults to 2 and RPr defaults to 4. Creation-time immutability remains unchanged.
- Affected IDs: BR-017; Q-05.
- BRD 0.44 becomes Draft 0.45; full-version approval remains outstanding.

### 2026-09-28 — Display precision and SQLite scaling

- Exact question: remove DisplayDecimalPlaces and display amounts using APr and rates using RPr?
- Accepted answer: Yes. And for SqLite we use 4 digits scale.
- Source: requesting user in this chat, following scaled-integer SQLite storage discussion.
- Decision: remove independent display precision; fixed SQLite INTEGER scaling is 10,000 for Amount/Rate regardless of APr/RPr. Calculation rounding remains governed by APr/RPr; BaseAmount stays derived.
- Affected IDs: BR-017; Q-05.
- BRD 0.45 becomes Draft 0.46; full-version approval remains outstanding.

### 2026-09-28 — Shared balancing-account setting

- Exact question: store the balancing-account selection in System config, shared across devices?
- Accepted answer: yes.
- Source: requesting user in this chat.
- Decision: the base-currency balancing-account selection belongs to synchronized System configuration. Selection/default and unavailable-account behavior remain unresolved.
- Affected IDs: BR-018; System configuration BR-005.
- BRD 0.46 becomes Draft 0.47; full-version approval remains outstanding.

### 2026-09-28 — Initialize rebalancing account and handle its deletion

- User direction: create five roots plus one rebalancing account in the root group during initialization; if deleted, explain that automatic rebalancing cannot proceed and ask the user to choose a rebalancing account in Settings.
- Source: requesting user in this chat; replaces the suggestion to start with an empty selection.
- Decision: create/select an ordinary base-currency account in Account root; missing/deleted account blocks the assisted action without modifying the transaction and displays the Settings guidance. Ordinary reference-based deletion protection remains.
- Affected IDs: BR-004/018/020; BC-001/005; FR-001.
- BRD 0.47 becomes Draft 0.48; full-version approval remains outstanding.

## Approval

- Current version: **Draft 0.48**, not submitted for full-version approval. The date-rule and account-currency corrections are explicitly accepted; no full-document approval is inferred.

### Prior approved baseline — provenance

- **Approved 0.32**.
- Approver: requesting user in this conversation; no personal name supplied or inferred.
- Decision date: 2026-09-27.
- Source: explicit current user message, “I approve BRD. Lets start with TRD”.
- Scope: entire BRD 0.32, covering core bookkeeping, synchronization and first use, including proposed acceptance formulations. Q-01 approval is satisfied by this decision; preceding drafting/history text is retained as provenance.
- Substance and identifiers unchanged by this metadata-only approval record. Discovery documents remain Draft.
- Historical next step for 0.32: draft TRD. This prior approval does not cover BRD 0.33, approve TRD, or authorize implementation.

