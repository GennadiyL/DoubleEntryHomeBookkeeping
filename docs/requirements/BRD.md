# Business Requirements Document: DoubleEntryHomeBookkeeping

## Document Control

- Revision 0.218, 2026-10-08: prioritize the complete Business-only Windows flow against an existing Local database; preserve other subdomains as deferred work. Confirm open-editor account deletion protection. Draft status retained.

- Revision 0.217, 2026-10-08: user accepted report failure handling for invalid saved JSON, calculation failure after date save, and CSV export failure. Existing report data is preserved as specified; Draft status retained.

- Revision 0.216, 2026-10-08: user reaffirmed Reports as the sixth catalog with the same shared group/element conventions and clarified that current development databases are initialized from scratch; no migration of old development reports is required. Draft status retained.

- Revision 0.215, 2026-10-08: user selected LocalConfig names AccountNameOrder, AccountNameSeparator and AccountNameAddCurrency, and confirmed currency is optional for name generation when the flag is off and required when on. Draft status retained.

- Revision 0.214, 2026-10-08: user corrected account-name generation to read LocalConfig.AddCurrencyToAccountName rather than a bool method argument; existing flat Settings read object retained and editable-only save input confirmed in TRD. Draft status retained.

- Controlled revision 0.213, 2026-10-08: requesting user explicitly authorized cleanup of Local default conflict priority, no Currency.Description (CurrencyRate.Description remains), AmountPrecision/RatePrecision for calculation and display, and unrestricted Draft entry count with individually valid entries and mandatory Account. Existing automatic Draft/Confirmed state derivation remains unchanged. Existing ordering and same-Local conflict exceptions are aligned from the previously recorded decisions. This is a draft revision, not full-document approval.

- Artifact: `docs/requirements/BRD.md`; consolidated BRD for core bookkeeping, synchronization and first use.
- Status: **Draft**; version **0.218**, dated **2026-10-08**. Replaces legacy Draft 0.1 and its accumulated brainstorming updates.
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

### Current delivery increment: Business UI — confirmed 2026-10-08

Prerequisite confirmed 2026-10-08: first align the existing persistent models, Business behavior, DAL and initial SQL/seed scripts with the accepted requirements, including ReportGroup and LocalConfig naming changes. Complete this backend alignment before starting the WinUI flow.

This increment completes the Windows Business flow using an existing, initialized, usable Local database. The user reports that the Business service layer is implemented. The current work connects that layer to the agreed WinUI interface; it does not restart discovery of already confirmed UI behavior.

1. Start the WinUI application and open the existing Local database without local-work authentication. Show Ledger with the newest 300 transactions, initially without an account filter.
2. Browse/filter the Ledger and add, edit, duplicate, delete or create transactions from templates using the established modal editor, validation and balancing rules. Account-filtered rows expose the account balance under the existing cumulative-amount rules.
3. Manage Accounts, Correspondents, Categories, Projects and Templates, including their groups, through the agreed trees, editors and pickers. Manage Currencies and exchange rates. Apply the existing selection, favorites, search, move, reorder and main-window-only merge rules.
4. Save through Business services, refresh affected views and preserve selection as already specified. Canceling a calling editor does not undo separately saved catalog changes. Close editors and exit using the agreed unsaved-change behavior.

Administration, Synchronization, Reporting and Setup are deferred from this increment and its discussion. Their existing decisions remain requirements for later increments. Reports, Synchronization, Settings and Help commands are unavailable in this increment. Database creation/registration, migration workflows and configuration editing are outside this flow; the supplied database already satisfies the required schema and initialization preconditions. Business still reads existing configuration for precision, account naming and balancing behavior.

Further discussion is limited to concrete Business integration gaps or contradictions encountered during implementation. Do not reopen settled UI choices or continue question batches for deferred domains. This scope decision is not approval of the entire BRD/TRD and does not remove future product requirements.


### In Scope

- First-use creation and connecting another device; one owner and one master dataset (F/S).
- Android and Windows desktop, ordinary offline use without sign-in (S/F).
- Six hierarchies, including ReportGroup for saved reports, accounts, classifications, currencies and rates (C/U).
- Manual transactions, incomplete or unbalanced drafts, duplication, transaction templates, group merging (C).
- On-demand balances and configurable saved reports, pre-run date selection with shortcuts, and saving generated results (C/U).
- First-release synchronization of all business data, conflict priority subordinate to validity, deletion propagation, recovery, expiry and latest-attempt reporting (S).

### Out of Scope

- Collaborative ownership of a master dataset; standalone offline-created books linked to cloud later (S, rejected).
- Unrequested automatic balancing adjustments, tolerance-based balance acceptance, field-level conflict merging, interactive per-conflict decisions (C/S, rejected).
- Account/Account Group report filters, drill-through and separate positive/negative subtotals (C, excluded).
- Future work is listed under Deferred Requirements. Backup/restore is deferred to version two; Azure-side-only backup/restore is the proposed direction.

## Current and Target Business Process

- Current process: not documented; product is defined from the owner's stated needs.
- Target: open existing local bookkeeping offline or complete online setup; organize data; record balanced activity or save incomplete or unbalanced drafts; reuse templates; calculate reports; synchronize when requested/configured; complete required recovery/replacement before accessing affected data.
- **Open/editing mode** uses an existing local copy offline. **Creating mode** means no local copy and requires online setup. The credentials-first setup attachment branch downloads from an existing cloud master; it is distinct from ordinary offline open mode.
- Setup retries, credentials, reinstallation and connectivity are settled in Q-13–Q-16. Remaining technical mechanisms do not change these business decisions.

## Business Rules

### Ownership and first use

| ID | Rule | Source | Related IDs |
| --- | --- | --- | --- |
| BR-001 | Version one has one owner per deployment and one master dataset. Each local copy belongs to that master; no direct device-to-device sync. Reinstalling the app registers a new local copy; the previous registration remains subject to the existing 90-day expiry rule. | S Ownership; F Decisions; U clarifications | BC-001, BC-002; FR-001, FR-002 |
| BR-002 | Startup selects mode by local-copy existence only. Existing copy opens without internet/sign-in; absent copy requires online creating mode and completed setup before bookkeeping. Recovery/expiry exceptions still apply. | F Startup; S Recovery | BC-001, BC-002; FR-001, FR-002 |
| BR-003 | Setup is credentials-first: the user enters login/password and selects Continue; the application determines whether Master exists and chooses the attachment or creation branch. If Master exists, authenticate and attach the new Local copy by registering the device and downloading existing business data. If Master does not exist, create it with the entered credentials and required base currency/amount-rate precision choices under BR-017, then establish the Local copy. Base currency starts unselected and must be explicitly chosen before creation. Login and password are required fields only; no additional length, character or complexity rules in version one. Show Checking Master / Master exists / No Master / Check failed, with manual Retry check after a failed check. Never create while Master existence or a previous creation outcome is unknown. A lost creation response requires manual Retry check; confirmed existence routes to attachment, confirmed absence allows creation subject to valid required input. If setup fails or ends cancelled, return to Setup with entered values preserved; show the error for failure and allow only user-initiated retries. No automatic setup retries. If Master was created but the initial download failed, preserve Master and retry through the attachment branch rather than creating it again. No local copy means no bookkeeping access. The credential step offers Continue / Exit. If Master is absent, the same dialog shows the currency/precision fields and Create. Before creation, Back returns to the credentials step with entered values preserved. | F Create/Open; U credentials-first routing clarification 2026-10-06 | BC-001, BC-002; FR-001, FR-002 |
| BR-004 | Initial content: six required roots, selected base currency at rate 1, and one account initially named Rebalancing in the Account root group using base currency; no transactions. System configuration selects this account for balancing. Other currencies added later. Base currency is immutable. | F Initial content; C Currency; U ReportGroup clarification 2026-10-05 | BC-001; FR-001 |
| BR-005 | Authenticate cloud access and prevent another owner's data access. Login matching is case-insensitive, so Gena and gena identify the same login. Remove leading and trailing spaces from the login before account lookup or creation. Passwords are case-sensitive and are not trimmed or otherwise normalized; preserve the entered password exactly. Existing required-field validation remains unchanged. Version one has no failed-login attempt limit, throttling cooldown or account lockout caused by repeated failed attempts. Remember authorization after successful Create/Open for synchronization with Master using a server-issued token, without storing the password. Local work requires no login or authorization in version one. A synchronization token is valid for 90 days from issue, without sliding renewal on synchronization; after expiration request login at the next synchronization. Token expiration is separate from Local-copy expiry. Master UTC time is authoritative for token expiration, independent of the Local computer clock. Each token authorizes only its associated owner, Master dataset and Local registration; Master validates these associations on every synchronization request. If synchronization authorization expires or is rejected, request login again without deleting the local database or pending changes. Each registered Local copy receives its own synchronization token; replacing one Local copy's authorization does not affect other devices. Reauthorization shows the existing login as read-only and an empty password field, and must authenticate the same Master account. Canceling that login cancels synchronization without discarding local work; offline use continues only where the existing expiry/recovery rules permit it. Existing recovery/expiry admission rules remain unchanged; Windows token protection belongs to TRD. If a saved token is missing or unreadable, request login when synchronization is attempted while retaining the existing Local registration and database; do not create a new Local copy. A network failure or unreachable Master does not constitute rejected authorization: retain the token and report the connection error. Configure the Master URL in a local configuration file; version one has no UI editor for that address. Read this URL at startup; configuration-file changes require an application restart. If the URL is missing or invalid, show a configuration error when synchronization is requested; existing local work remains available subject to the normal expiry/recovery rules. Require HTTPS for remote Master connections, allowing HTTP only for localhost development. Never send a saved token to a different Master address. Changing the Master address requires login again while preserving the local database and pending changes. After reauthorization to a different server address, verify that the server MasterDatasetKey matches the existing Local copy. A matching login alone does not permit merging into another dataset. Each local copy contains complete business data. Configuration has two scopes: System settings synchronize with master; Local settings do not synchronize and survive local-copy replacement. AmountPrecision and RatePrecision belong to System and govern calculation and display; no separate display-precision setting exists. `DefaultAccountName`, the Add currency naming option, sync trigger and conflict priority belong to Local. | S Ownership; U configuration correction 2026-09-27 | BC-002, BC-009–BC-011; FR-002, FR-009–FR-011 |

### Organization, accounts, currency and accounting

| ID | Rule | Source | Related IDs |
| --- | --- | --- | --- |
| BR-006 | Accounts, Categories, Projects, Correspondents, Templates and Reports each have exactly one root hierarchy; ReportGroup is the report hierarchy group type. Every element belongs to a group of its own type. Roots may directly contain elements; cannot be edited, renamed, moved or deleted. | C Legacy/Groups; U ReportGroup clarification 2026-10-05 | BC-003, BC-008; FR-003, FR-008 |
| BR-007 | Rearrange non-root groups/elements within type, even if used. Reject cycles including merge into descendant. A non-root group may merge into its parent, another ancestor or the root; the root remains forbidden as merge source. Delete only groups without children/elements. Merge moves source children/elements to destination, resolves collisions and deletes empty source. | C Groups; U clarifications | BC-003, BC-008; FR-003, FR-008 |
| BR-008 | Trim names; reject empty; compare case-insensitively. Group names unique among siblings; element names unique in their group except Account and Report duplicates allowed. Group/element scopes are separate. Reject individual forbidden collisions; bulk collisions repeatedly append `_1` to incoming name, retaining destination names. | C Names/Groups; U ReportGroup clarification 2026-10-05 | BC-003, BC-004, BC-006, BC-008; FR-003, FR-004, FR-006, FR-008 |
| BR-009 | Name is mandatory on named catalogs, groups and elements; existing uniqueness rules remain. Description is optional, visible and independently editable wherever descriptive text is supported. No Comment field remains. Currency has no Description; rates, templates and transactions retain optional Description. No hidden Template.Description and no automatic Name-to-Description copying on save. Omitting or leaving Description empty must not block saving. This does not add Name to transactions, rates or entries. | U clarification 2026-09-28 | BC-003–BC-006; FR-003–FR-006 |
| BR-010 | Account currency mandatory; Category, Project, Correspondent independently optional. Category is an activity; Project an investment/income purpose; Correspondent a person/legal entity. Historical reports use current account classifications. | C Legacy/Account lifecycle | BC-004, BC-007; FR-004, FR-007 |
| BR-011 | Account currency is selectable only during unsaved creation and becomes permanently immutable on the first successful account save, even before any transaction/template use. Canceling unsaved creation creates no account. Any transaction/template reference prevents account deletion, even at zero balance. Classifications used by accounts cannot be deleted. | C Account lifecycle; S Priority | BC-003–BC-006, BC-009; FR-003–FR-006, FR-009 |
| BR-012 | Each LocalDb has its own Local configuration value `DefaultAccountName`, e.g. `{Correspondent}/{Category}/{Project}`. User selects one of all six orders of Correspondent, Category and Project, and separately chooses a separator consisting of exactly one non-whitespace character. The separator textbox accepts at most one character. When focus leaves it or Save is clicked, replace empty or whitespace-only input with the default /. These choices determine the classification part of the format; arbitrary free-text format entry is not supported. Add an Add currency checkbox to the Local account-name preferences. When unchecked, generate the classification name alone, for example Store/Food/Life. When checked, append the account currency code in parentheses with no preceding space, for example Store/Food/Life(UAH) for an account in UAH. Apply this to every account currency, including the base currency. Add currency is initially unchecked. For the initial order and slash separator, the checked format is {Correspondent}/{Category}/{Project}({Account.CurrencyName}), using the account currency identifier shown in the user examples UAH/USD/EUR. Missing classifications keep their existing positions and separators; with all three empty, the checked slash format produces //(UAH) for a UAH account. It generates a default using classification short names; the naming preferences do not synchronize. Missing values retain positions/separators; all absent in slash format yields the valid classification part `//`. Account names are stored business values and synchronize normally. Allow manual override/Restore Default Name. Later classification renames or reassignment do not automatically change existing account names. Initial order is Correspondent, Category, Project; initial separator is `/`, producing `{Correspondent}/{Category}/{Project}`. Q-03 is resolved. | C Names; S Configuration; U clarification 2026-09-27 | BC-004; FR-004 |
| BR-013 | Currency ISO code unique dataset-wide and immutable; Name/Symbol editable/restorable from catalog defaults; Currency has no Description. Delete only non-base currency unused by accounts. Populate available currency choices from system culture/region information, using ISO currency symbol as Code, currency symbol as Symbol and English currency name as Name, as in the user-provided example. Invalid region conversions are skipped. Available choices contain exactly one entry per ISO currency code. For repeated codes, retain the first returned Name and Symbol; these remain editable and restorable from source defaults. Q-04 is resolved. | C Currency; U clarifications | BC-004; FR-004 |
| BR-014 | Currency requires positive initial rate. Base rate always 1, never overridden. Initial rate has hidden system-controlled old date; value editable subject to base rule; cannot be deleted. One ordinary rate per currency per calendar date (DateOnly, without timezone conversion); ordinary rates editable/deletable. Each rate, including the initial rate, may have its own optional Description. Currency itself has no Description. | C Currency/Precision | BC-004, BC-005; FR-004, FR-005 |
| BR-015 | Entry currency comes from account. Default rate is latest on/before the transaction calendar date in the current device/UI timezone with initial fallback. Stored entry rate independent of later rate changes. Non-base rate override allowed; Restore Rate reloads applicable current rate. Date changes do not alter stored rates automatically. | C Currency | BC-005, BC-006; FR-005, FR-006 |
| BR-016 | Confirmed transactions require at least two entries, each with an account. Drafts may contain any number of entries, including zero, and can be saved for later repair; there is no separate Draft entry-count limit. Every entry present in either state must be valid: it must have an existing account, a valid positive rate under BR-015/017 (base currency rate 1), and a numeric amount, including zero. An entry without an account or valid rate cannot be saved as Draft. Empty amount becomes zero; zero amounts and repeated accounts are allowed. Positive increases account balance and negative decreases it only when the transaction is Confirmed. Optional Description is transaction-level only. | U clarification 2026-10-01; C Transactions | BC-005; FR-005 |
| BR-017 | Base currency, Amount Precision (APr) and Rate Precision (RPr) are selected during dataset creation, stored in synchronized System configuration, and immutable afterwards. AmountPrecision (APr) governs amount and BaseAmount calculation and display; RatePrecision (RPr) governs rate precision for calculation and display. Input controls enforce these precisions; excess input received outside them rounds to the applicable precision using midpoint-to-even. Rates must remain positive after rounding. Numeric overflow rejects the operation with an error and unchanged data. APr and RPr each allow integer values 0 through 4 inclusive; defaults are APr = 2 and RPr = 4. Display amounts and BaseAmount using APr and rates using RPr. No separate DisplayDecimalPlaces setting remains. SQLite storage uses fixed four-decimal scaling as specified in TRD. | U clarification 2026-09-28 | BC-001/004/005/009; FR-001/004/005/009 |
| BR-018 | Calculate each BaseAmount = Amount × Rate rounded to APr using midpoint-to-even; sum those rounded values. Confirmed transactions require exact zero, with no tolerance. For any nonzero difference, offer Add balancing entry. Only on user click, append an entry using the configured base-currency balancing account, Amount = negative current total, Rate = 1; recalculate and require zero. User may instead add an entry manually. The button has no small-rounding threshold and does not silently add entries or bypass other save validation. The balancing-account selection belongs to synchronized System configuration and is shared across devices. Initialization creates and selects the rebalancing account in the Account root group. The user can select another base-currency account in Settings. The account is subject to ordinary account deletion rules; selection alone does not protect it. A missing/deleted configured balancing account appears as an empty field in Settings and does not prevent saving other settings. If missing/deleted, Add balancing entry adds nothing and shows: Cannot add a balancing entry: the rebalancing account is missing. Please choose a rebalancing account in Settings. Manual balancing remains available. | U clarification 2026-09-28 | BC-005; FR-005 |
| BR-019 | A complete, valid, exactly balanced save becomes Confirmed automatically. Any new or existing transaction may be saved as Draft when it has fewer than two entries or is unbalanced, including an edit of a previously Confirmed transaction. Every present entry must satisfy BR-016 and other date/numeric validation still applies. UI shows a persistent visible Draft warning until the transaction is fixed; fewer than two entries or imbalance alone does not disable Save; invalid entries still prevent saving. Drafts are unfinished work retained for future repair and contribute to no accounting balances, totals or reports. Saving a formerly Confirmed transaction as Draft removes its previous accounting contribution. Transactions may be deleted. Calculate balances/reports on demand using only Confirmed, non-deleted transactions. Draft synchronization remains governed by BR-029. | U clarification 2026-10-01 | BC-005, BC-007; FR-005, FR-007 |
| BR-020 | New occurrence defaults to now, editable. Duplicate copies accounts/amounts/rates/Description with time reset to now for review/save. Opening balances are ordinary balanced transactions; no opening-balance preset account created; the initialization rebalancing account is separate. | C Transactions | BC-005; FR-005 |
| BR-021 | Transaction time represents UTC, displays in device timezone; no separate timezone setting. Minimum 2001-01-01 00:00:00 UTC inclusive after local-input conversion. Ordinary DateOnly rate dates cannot precede 2001-01-01; initial date excepted. Rate lookup uses the transaction date in the current device/UI timezone. Report boundaries and calendar grouping use that same timezone, converted to UTC instants for timestamp filtering. | C Dates | BC-004, BC-005, BC-007; FR-004, FR-005, FR-007 |
| BR-022 | Template has group-unique Name, accounts/amounts and visible optional Description; each template entry needs an account. Empty, single-entry and unbalanced templates allowed. Applying opens a transaction for review with applicable rates and copied Description; persists only on Save. An incomplete or unbalanced applied transaction may be saved as Draft, including an empty or single-entry transaction, with the BR-019 warning. User may fix it now, save for later repair, or Cancel without saving. Creating a template from a Draft copies its valid account/amount entries; zero-entry and single-entry Drafts produce corresponding allowed templates. | C Templates; U clarifications 2026-10-01 | BC-006; FR-006 |

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
| BR-023 | Filter by Category, Project, Correspondent trees only. Within type, selected groups/elements/unassigned combine OR; types combine AND for matching accounts. New reports all unchecked. Empty selection in any type means empty report with warning, not unrestricted. Display the warning in the results popup and identify the classification filter with no selections. Empty results display No matching data. | C Filtering; U clarification 2026-10-05 | BC-007; FR-007 |
| BR-024 | Group selects descendants with explicit exclusions/inclusions. Inherit nearest saved ancestor state, default unchecked. Explicit element inclusion may override parent exclusion. Checking/unchecking group resets subtree and clears descendant overrides. When only some descendants are selected, the group checkbox shows a partial-selection mark (−). Use the report filter tree root checkbox to select/clear all groups and elements; do not add separate Select all/Clear all buttons. The filter picker has two independent first-level nodes in this order: Unassigned and Root. Root contains the groups/elements tree; its checkbox controls only that tree, not Unassigned. Selecting/clearing everything requires clicking both first-level checkboxes. | C Saved selection; U clarifications | BC-007, BC-008; FR-007, FR-008 |
| BR-025 | Saved selections retain group/element identities, explicit inclusions/exclusions and unassigned choices. Apply all saved explicit choices after hierarchy moves: an explicitly included subgroup remains included under an excluded parent, and an explicit exclusion remains recorded even under another excluded group. Do not rewrite or remove redundant saved choices when opening/running a report or when hierarchy changes. Only when the user saves report settings again, recalculate included/excluded IDs from the current selection state, retaining only overrides differing from inherited parent state. Evaluate membership using current hierarchy; new members inherit applicable state, moved-out members stop matching their old group unless explicitly selected. Apply saved instructions as-is to the actual group tree. Ignore missing identities without removing their saved choices; if the same identity returns, apply its saved inclusion/exclusion again. Q-06 is resolved. Report references do not prevent otherwise valid deletion. | C Saved selection; U clarification 2026-09-27 | BC-008; FR-008 |
| BR-026 | Report matching account entries of active transactions only. Fixed optional From/To dates inclusive. New reports default to the current calendar month; the pre-run dialog initializes from the report saved range. If From is later than To, disable Run including Enter and show an error beside the dates. Before running, show a modal date-range dialog with From/To fields, Current week/month/year and previous/next week/month/year controls. Previous/next shifts each populated date by the selected week/month/year unit, leaving blank boundaries blank; disable these controls when both dates are blank. Current week/month/year fills both boundaries with the complete calendar period in the device timezone. If a shifted month/year lacks the original day, use its last valid day; for example January 31 plus one month becomes February 28 in a non-leap year. The Currency checkbox enables Currency as the first grouping level. A second combo selects Category, Project, Correspondent, day, week, month or year; it controls the second grouping level when Currency is enabled and the single grouping level when Currency is disabled. New reports default to Currency off and Category grouping. Weeks Monday–Sunday; calendar periods in device timezone. | C Filtering/Grouping; U clarification 2026-10-05 | BC-007; FR-007 |
| BR-027 | Signed net totals only. Without Currency grouping: one included currency shows own/base columns, multiple currencies base only. With Currency grouping: own/base totals per currency; mixed-currency grand total base only. Totals only, no drill-through. | C Grouping | BC-007; FR-007 |
| BR-028 | Organize saved report definitions in ReportGroup folders with nested groups and reports under BR-006/007/008. Reuse shared group Add/Edit/Delete/Move/Merge and permitted drag/drop rules. Save/reopen, rename, edit, move and delete report definitions containing their group, favorite status, selections, grouping and fixed optional dates. Clicking Run or pressing Enter in the pre-run date dialog updates the current report only with the chosen date range, closes that dialog and opens the read-only results popup; it does not save changes to Name, grouping or filter selections. Calculate results on demand from current bookkeeping data; a saved report definition stores calculation instructions, not calculated results. Display generated results in a fully read-only modal popup with report name/date range, the calculated table and Save CSV/Close buttons. To run again with different dates, return to the saved-report list. Double-clicking a report or invoking Run opens the date-range popup; the report editor has no Run command. A new report must be saved before running. Canceling the pre-run date dialog changes nothing. CSV export uses a standard Save dialog for filename/location and contains the complete calculated table, including column headers, grouping labels, currency codes and totals. Delete from the report-list button/menu asks for confirmation, then removes only the report definition and leaves bookkeeping data untouched. Default name uses `yyyy-MM-dd HH:mm:ss` and is user-replaceable. Reject empty or whitespace-only names; duplicates remain allowed. Q-07 is resolved. | C Saved definitions; U clarifications 2026-09-27/2026-10-05 | BC-007, BC-008; FR-007, FR-008 |

### Synchronization, recovery and expiry

| ID | Rule | Source | Related IDs |
| --- | --- | --- | --- |
| BR-029 | Synchronize all business data including drafts/saved reports and System configuration bidirectionally; exclude Local configuration. Whole entity content is selected; transactions/templates include all entries, with no field/entry merge. Catalog ordering is synchronized separately: locally flagged Order changes win independently of content priority. One-sided content changes propagate regardless of priority. Both-sided content changes use Local/Master priority even if values match, except that an incoming change from the Local that produced the current accepted entity revision wins over that Local's earlier change. Business validity still overrides priority. Default Local; preference is local. | S Scope/Priority; U clarifications | BC-009; FR-009 |
| BR-030 | Business validity overrides priority. Restore required accounts/currencies/groups/ancestor chains. Preserve account currency immutability, references, accounting and hierarchy rules. Resolve silently where valid; report corrections. If no valid resolution exists, explain issue/remedy and record diagnostics. Combined-case mechanics are TRD analysis under these rules (Q-11). | S Priority/Dependencies | BC-009, BC-012; FR-009, FR-012 |
| BR-031 | Same-ISO currencies merge into one, remapping account use; differing values follow priority. Same currency/date rates resolve by priority. Distinct conflicting unique names remain separate: retain preferred name, repeatedly suffix other `_1`. Correct hierarchy cycles preserving preferred valid hierarchy; report. | S Uniqueness | BC-009; FR-009 |
| BR-032 | Master retains deletion information until all remaining non-expired registered copies acknowledge it, then permanently removes eligible data. Remove expired registrations/newly eligible deletions before sync. Local deletion permanent after successful confirming sync. Failed/cancelled attempts retain pending deletions, subject to recovery/expiry. | S Deletion | BC-009–BC-011; FR-009–FR-011 |
| BR-033 | Delete-versus-edit follows priority only if business rules allow. Restore protected dependencies regardless of priority. If master merged/deleted a group but offline copy added an item there, restore group and keep new item there; report restoration. | S Deletion | BC-009; FR-009 |
| BR-034 | Exactly one trigger: manual only (default), on start, on exit. Saving a changed trigger does not start synchronization immediately; on start applies at the next launch and on exit at the next actual exit. Sync button always available. The manual action is labeled Sync now when Ready and Retry recovery when Recovery required. Both open the same modal progress popup showing the current stage, including waiting for the server, with Cancel. Cancel, Esc or the popup X requests cancellation; keep the popup open until the service finishes handling cancellation, then close it. No editing during sync or waiting. One sync per master at a time; inform waiting user, allow Cancel, no queue-wait limit. | S Triggers; U UI clarification 2026-10-05 | BC-009; FR-009 |
| BR-035 | General connectivity rule: user-initiated network actions may start on any available internet connection; automatic network actions require Wi-Fi to start. Initial setup is manual, so Wi-Fi or mobile data is allowed; the same applies to manual sync, recovery and Download. Retain the previously agreed behavior that automatic sync/recovery started on Wi-Fi may continue over mobile after Wi-Fi loss. After automatic startup synchronization, proceed to Ledger only when business access is allowed, after closing any required result popup under BR-043. Startup failure/cancellation permits ordinary offline work except pending recovery or confirmed expiry. Failed/cancelled automatic exit sync shows the latest-result summary with Retry / Exit / Cancel exit. Retry starts synchronization or required recovery again; Exit closes the app; Cancel exit cancels the exit request and keeps the app open. Pending recovery continues to block business access. Closure retains unsynced work subject to expiry. No later-Wi-Fi automatic retry agreed. | S Triggers; U general rule 2026-09-27 and UI clarification 2026-10-05 | BC-001, BC-002, BC-009–BC-011; FR-001, FR-002, FR-009–FR-011 |
| BR-036 | Stop after 30 seconds communication inactivity, not total duration. Incoming data/still-working responses reset timer. Healthy queue wait unlimited. Cancel/timeout obey pre/post-publication rules. | S Timeout | BC-009, BC-010; FR-009, FR-010 |
| BR-037 | Skip business transfer only when neither side changed since last completed sync. Changed master data downloads even with no local changes. Successful no-change sync refreshes expiry. | S No changes | BC-009; FR-009 |
| BR-038 | Before master publication, failed attempts preserve original business data and outgoing local changes. After publication, local failure does not undo master results. Resolve uncertain outcome before replay/discard of non-expired outgoing changes; never apply same batch twice. If unreachable, preserve pending state without claiming success/rollback. | S Recovery | BC-010; FR-010 |
| BR-039 | Recovery obtains latest published master including later device changes, not original interrupted result. Block all local business access/viewing/reports while pending. Attempt on startup regardless of trigger, subject to Android Wi-Fi rule; manual recovery available. Failure/cancellation leaves access blocked. | S Recovery | BC-010; FR-010 |
| BR-040 | Confirm receipt of revision actually installed; repeat lost confirmation without duplicate business application. Interrupted downloads restart from beginning. Do not discard possibly unreceived non-expired outgoing changes while outcome uncertain. | S Recovery | BC-010; FR-010 |
| BR-041 | Expire registration 90 days after last successful sync by master clock. Successful initial registration/download starts interval; every successful sync resets it. Once confirmed expired, remove registration, reject old sync, notify need for fresh copy and automatically delete expired local data including unsynced changes; no viewing/export. Expiry overrides pending recovery. | S Expiry | BC-011; FR-011 |
| BR-042 | Offer fresh copy from same master after expiry; start only on owner's Download action. Preserve Local configuration. | S Expiry | BC-011; FR-011 |
| BR-043 | Latest result opens a read-only modal popup for the latest sync report, with a Save to file button exporting the displayed summary to a TXT file. After a manual synchronization attempt ends, close progress and automatically open this summary for success, failure or cancellation. For automatic startup synchronization, show the summary after failure or cancellation, or if conflicts or corrections occurred; routine success proceeds to Ledger under BR-035. After successful automatic exit synchronization, routine success closes the app; if conflicts or corrections occurred, show the summary with Save to file available, and closing the summary completes exit. After failed or cancelled automatic exit synchronization, show this summary with Retry / Exit / Cancel exit under BR-035. Keep only latest attempt until app closes, including recovery. Show conflicts/corrections/errors and received local changes by type and created/updated/deleted counts; omit zeros/uploads. Write detailed diagnostic logs for critical unresolved failures to a log folder. Retain logs for 7 days, then automatically clean them up. Exact platform-specific folder paths belong to technical design. No Share/Export diagnostic logs action has been selected. Q-12 is resolved at business level. | S Report; U clarifications | BC-012; FR-012 |
| BR-044 | First release supports favorite flags on currencies, groups and grouped elements (accounts, categories, correspondents, projects, templates, reports), with user filtering by favorites. IsFavorite defaults to false for newly created items. Favorite changes synchronize as ordinary entity content and use configured conflict priority. All six root groups always have IsFavorite = false and cannot be marked as favorites. Favorites filtering retains non-favorite ancestor groups, including the root when needed, only as navigation paths to favorite descendants. Ancestors remain non-favorites; showing a navigation path does not change any favorite flags. Opening a favorite group with the filter active still shows only favorite descendants and necessary ancestor navigation paths; it does not reveal all children. Favorite status does not cascade to descendants. | U clarification 2026-09-28 | BC-003/004/006/008/009; FR-003/004/006/008/009 |

## Main Navigation

Confirmed by the requesting user in this chat, 2026-10-04. The left navigation panel is docked on Windows and opens as a popup panel on Android.

| Group | Commands, in display order |
| --- | --- |
| Daily work | Ledger, Reports |
| Catalogs | Accounts, Correspondents, Categories, Projects, Templates, Currencies |
| Application | Synchronization, Settings, Help, Exit |

Accounts and Templates belong to Catalogs. Selecting a currency shows its exchange rates within the Currencies screen. These navigation choices organize existing capabilities; they do not change bookkeeping rules. Related capabilities: BC-003–BC-009 and BC-012. Help uses a PDF with pictures, to be written later, with modal Help/About dialogs and offline guide access under Help and About.

### First-start setup — dialog

Confirmed by the requesting user in this chat, 2026-10-06; related capabilities BC-001/002 and requirements FR-001/002, under BR-002/003/017.

- When no local copy exists, show one modal setup dialog. The credentials step contains Login, Password and Continue / Exit. Continue checks Master existence; if it exists, authenticate and start attaching the new Local copy automatically. No bookkeeping access is available until setup succeeds.
- If Master is absent, the same dialog shows the new-Master creation step with the currency/precision fields and a Create button. Creation starts when the user clicks Create with valid required input.
- Before creation, Back returns to the credentials step while preserving entered values.
- New-Master creation requires a base-currency picker and amount/rate precision combos. Base currency is initially unselected and must be explicitly chosen. Each precision combo offers 0 through 4 inclusive; defaults are amount precision 2 and rate precision 4 under BR-017. Existing-Master attachment uses downloaded System configuration.
- The credentials-first Continue flow replaces the previous manual Create/Open choice. Credentials and creation settings are steps of the same setup dialog; switching steps does not open another setup popup.
- Show the Master-check status as Checking Master / Master exists / No Master / Check failed. No Master means confirmed absence. A failed/unknown check prevents creation; offer manual Retry check.
- If a creation response is lost and the outcome is unknown, prevent another creation attempt until Retry check resolves it. Confirmed existence routes to attachment; confirmed absence permits creation with valid required input.
- Creation/attachment uses a modal progress popup showing the current stage and Cancel. Setup stays blocked while the operation runs; cancellation keeps progress open until the service handles the request.
- If a setup attempt fails or ends cancelled, return to Setup with entered values preserved. Show an error for failure; retries remain manual. A created Master followed by a failed initial download is preserved and retried through attachment under BR-003.
- When no operation is running, Exit or the setup dialog X closes the application. Successful setup closes the setup dialog and opens Ledger.

### Windows application and window state

Confirmed by the requesting user in this chat, 2026-10-06; applies to the Windows application shell and Main Navigation.

- The Windows application runs as a single instance. Launching it again brings the existing main window and its active modal popup to the front, preserving the existing modal interaction chain.
- Remember the main window's size, position and maximized state between launches. This is device-specific window state; main-screen selection/scroll/search/filter retention remains governed by the separate session-state rule below.
- Open each popup centered over its immediate parent. Do not add automatic popup resizing or repositioning to accommodate a smaller monitor; the user closes popups before maximizing or adjusting the main window.
- Only the main window has a minimize button; popups have none. The user must close all modal popups before operating the main window, including minimizing, maximizing or resizing it. Do not introduce a minimize/restore-whole-stack command.
- The main window has one normal Windows taskbar icon, allowing the user to return after minimizing it. Popups have no separate taskbar icons. The previously confirmed main-window-only minimize button and modal parent-blocking rules remain unchanged.
- Tree pickers, transaction/template editors and report-result popups are resizable. Simple forms, such as group-name editors, have a fixed size. Existing minimum usable dimensions remain applicable.
- Persist resizable popup dimensions locally per popup type, and separately per catalog/mode for tree pickers. Restore those dimensions when reopened, centering over the current parent rather than restoring the previous popup position.
- Store UI preferences for window geometry and column widths in a local JSON settings file under the Windows user profile. These UI preferences do not synchronize. Existing business Local/System configuration contracts remain unchanged.
- Persist column widths when Apply is clicked, resizable popup dimensions when the popup closes, and main-window geometry on application exit.
- If the UI settings file is missing or invalid, use default UI settings so the application can open. This fallback does not change bookkeeping data.
- No application theme feature is required. The proposed automatic Windows light/dark switching is not an approved application requirement. This answer does not specify a fixed light/dark appearance or custom palette.

### Main-window screen state

Confirmed by the requesting user in this chat, 2026-10-04; related capabilities BC-003–BC-009 and requirements FR-003–FR-009.

- When switching between main-window screens, preserve each screen selection, scroll position, search text and filters for the current application session. Returning to a screen restores that state.
- Modal pickers follow their separately confirmed opening rules, including opening with Favorites only off and selecting/revealing the current field item. Main-window state retention does not change picker initialization.

### Help and About

Confirmed by the requesting user in this chat, 2026-10-05; supporting UI under Main Navigation.

- The user guide is a PDF with pictures. Writing the PDF is deferred to a later task; this does not assign it to a later product release.
- An About button opens a modal popup showing the application name and version, with Close.
- The Help command opens a small modal dialog with User guide / About / Close. User guide is disabled until the PDF is available.
- User guide opens the PDF in the default Windows PDF viewer. Once written, the PDF is bundled with the application so the guide works offline.
- Guide contents and topic structure remain deferred until the PDF is written.

### Settings — main screen

Confirmed by the requesting user in this chat, 2026-10-05; related capabilities BC-001/004/005/009 and requirements FR-001/004/005/009, under BR-005/012/017/018/029/034.

- The Settings screen has two sections: This device and Shared settings.
- This device contains default account naming, the synchronization trigger and conflict priority. These are Local settings and do not synchronize under BR-005/012/029/034.
- Account naming uses a combo with all six classification orders, a single-character separator textbox and a live example of the resulting name. Include the Add currency checkbox under BR-012, initially unchecked. When checked, the live example appends the account currency identifier in parentheses for any currency, including the base currency, while retaining classification positions/separators even if all classifications are empty.
- Limit the separator textbox to one character. When focus leaves it or Save is clicked, replace empty or whitespace-only input with /. This normalization is not a validation error; the saved separator remains exactly one non-whitespace character under BR-012.
- The synchronization trigger uses three radio buttons: Manual only / On startup / On exit, with exactly one selected under BR-034. Saving a trigger change does not immediately start synchronization; On startup applies at the next launch and On exit at the next actual exit.
- Conflict priority uses two radio buttons: Prefer Master / Prefer this device, with the explanation Used when the same item changed on both sides. The existing priority and validity rules under BR-029/030 still apply.
- User-specified technical correction, 2026-10-08: store the checkbox in LocalConfig.AccountNameAddCurrency (bool, default false). IAccountService.GetDefaultName reads this saved Local setting instead of receiving a boolean parameter. The naming preference remains local and existing account names are unchanged.
- Shared settings contains the editable balancing-account selection under BR-018, and read-only base currency, amount precision and rate precision. The read-only values were fixed during bookkeeping creation under BR-017; no separate display-precision setting is introduced.
- Choosing the balancing account opens the usual account-tree picker. Only base-currency accounts may be chosen; other accounts remain visible. Disable Select when a group, a non-base-currency account or no item is selected. Other selection gestures must enforce the same eligibility rule.
- If the configured balancing account is missing/deleted, show an empty field and allow saving other Settings. Adding a balancing entry still requires choosing a replacement account under BR-018.
- Settings changes apply only after clicking Save. Cancel restores the saved values and keeps the Settings screen open. Changing a control alone does not persist its value.
- Navigating away or exiting with unsaved Settings changes opens a modal Save / Discard / Cancel choice. Save persists the changes and then continues the requested navigation or exit; Discard restores the saved values and continues; Cancel leaves Settings open with the pending values.
- If saving Settings fails, keep Settings open with the entered values and an error message. Previously saved settings remain unchanged. If Save was requested while navigating away or exiting, do not continue that action after the failure.

### Synchronization — screen and dialogs

Confirmed by the requesting user in this chat, 2026-10-05; related capabilities BC-009–BC-012 and requirements FR-009–FR-012, under BR-034 and BR-038–BR-043.

- The Synchronization screen shows Current status, the last successful synchronization date/time, a synchronization/recovery action and Latest result. Current status uses Ready for ordinary local use or Recovery required for pending recovery under BR-038–BR-040. Label the action Sync now when Ready and Retry recovery when Recovery required.
- Sync now and Retry recovery use the same modal progress popup showing the current stage, including waiting for the server, with Cancel. Cancel, Esc or the popup X requests cancellation; the popup stays open until the service finishes handling cancellation, then closes. Editing remains blocked during waiting, synchronization and cancellation handling under BR-034; existing cancellation/recovery rules remain applicable.
- Latest result opens a read-only modal summary of received additions/updates/deletions by entity type, plus conflicts, corrections and errors. Omit zero counts under BR-043. Include a Save to file button that saves the displayed synchronization summary to a TXT file.
- After a manual synchronization attempt ends, close the progress popup and automatically open the latest-result summary for success, failure or cancellation.
- After automatic startup synchronization, routine success opens Ledger. Failure, cancellation, conflicts or corrections show the latest-result summary first; after its closure, open Ledger only when business access is allowed under BR-039/041.
- After successful automatic exit synchronization, routine success closes the app. If conflicts or corrections occurred, show the latest-result summary first with Save to file available; closing this summary completes exit.
- After failed or cancelled automatic exit synchronization, show the latest-result summary with Retry / Exit / Cancel exit and Save to file. Retry starts synchronization or required recovery again; Exit closes the app; Cancel exit keeps the app open. Pending recovery still blocks business access; retained unsynced work remains subject to expiry.
- After Master confirms expiration during synchronization/recovery, follow the existing expired-copy removal and replacement rules under BR-041/042. Show an expiry message with Download for a fresh copy from the same Master; Expired is presented through this flow rather than as a permanent Current status value. Local elapsed time alone does not confirm expiry.

### Reports — main screen

Confirmed by the requesting user in this chat, 2026-10-05; related capabilities BC-007/008 and requirements FR-007/008, under BR-023–BR-028.

- The Reports screen displays saved reports in a ReportGroup tree with nested groups and report elements. Retain New, Edit, Delete and Run commands and report columns Name, Grouping, From and To; the Grouping label can show Currency → Category when Currency is the first level. Reports represent saved calculation instructions under BR-028; calculated results remain generated on demand from current bookkeeping data rather than stored in the saved definition. Report groups reuse the shared catalog tree actions, root protections, selection, movement, merging, drag/drop and favorite rules under BR-006/007/008/044. Double-clicking a group edits it; double-clicking a report invokes Run.
- New/Edit opens a modal report editor with header fields in this order: Name, Group picker, Favorite checkbox. Date range, grouping and Category/Project/Correspondent filters appear below. The editor offers Save/Cancel, with no Run command; shared editor Save/Cancel rules apply. Save a new report before running it from the saved-report list. Double-clicking a saved report invokes Run rather than Edit.
- Grouping uses a Currency checkbox and a Group by combo containing Category, Project, Correspondent, Day, Week, Month and Year. Currency is the first level when enabled; the combo then controls the second level. With Currency disabled, the combo controls the single grouping level. New reports default to Currency off and Category. Amount columns and mixed-currency totals follow BR-027.
- Category, Project and Correspondent filters each use a dedicated modal checkbox-tree picker, distinct from catalog element/group selection dialogs. These report filter pickers always show the full applicable tree and have no Favorites only toggle. Each picker has two independent first-level nodes: Unassigned, then Root. Root contains the groups/elements tree with checkboxes on groups and elements. Its checkbox selects/clears that tree without changing Unassigned; click both first-level checkboxes to select/clear everything. Users may change filter selections, but catalog Add/Edit/Delete operations are unavailable. Selection inheritance and partial-group marks follow BR-023–BR-025.
- No separate Select all/Clear all buttons are needed in the filter picker. OK closes the picker and returns its choices to the report editor; Cancel closes it and keeps the editor previous choices. Only Save in the report editor persists these filter changes. Canceling the report editor discards them under the shared editor cancellation rules.
- Double-clicking a report or invoking Run from the saved-report list opens a modal date-range dialog with From/To fields, Current week/month/year shortcuts and previous/next week/month/year controls. Current shortcuts select the full calendar period. Previous/next shifts each populated date by the chosen unit, leaves blank boundaries blank and is disabled when both dates are blank. Use the last valid destination day for month/year edge cases under BR-026. Clicking Run or pressing Enter updates only the current report date range, leaves its other saved settings unchanged under BR-028, closes the date popup and opens the results popup. Canceling the date popup changes nothing.
- A new report defaults to the current calendar month. The pre-run date popup initializes From/To from the report saved range.
- From/To remain optional under BR-026. If From is later than To, disable Run, prevent Enter from running and show an error beside the dates. With one entered boundary, previous/next shifts that date while leaving the other blank; with both blank, disable previous/next. Current week/month/year still fills both dates.
- The fully read-only modal results popup shows the report name and date range at the top, the calculated table using the chosen grouping, and Save CSV/Close buttons. It offers no Change dates, Run or editing commands. To run with a different range, close results and invoke Run again from the report list. Export the complete calculated table to CSV, including headers, grouping labels, currency codes and totals; a standard Save dialog lets the user choose filename/location. The saved report definition continues to store calculation instructions rather than calculated results.
- Deleting a report uses the Delete button/menu in the reports list. Ask for confirmation; after confirmation, delete the report definition under BR-028.
- Empty results display No matching data in the results popup. If a classification filter has no selections, show the existing BR-023 warning there and identify the filter.
- For an empty report result, keep Save CSV available and export column headers only. No matching data remains a results-popup message rather than a CSV data row. Existing empty-filter warnings still apply.
- Include a report group when it contains matching transactions even if its signed total is zero; omit groups with no matching transactions. This does not change the existing matching-entry and accounting-inclusion rules.
- Order report date groups chronologically. Category/Project/Correspondent groups follow their catalog tree order. CSV preserves the displayed report row order.
- Currency groups in report results follow the currency catalog order. When Unassigned has matching data, show it before named Category/Project/Correspondent groups within the applicable report grouping. In the grouping-label column of report results and CSV, use the full classification path, such as Home/Food or Travel/Food, to distinguish identical names under different groups. This does not change stored names or catalog/editor name display.
- When Currency grouping is enabled, display each currency as a section heading followed by its result rows. Keep all sections visible without expand/collapse controls. Show each currency subtotal after its section and the grand total at the bottom, using bold text for totals. Preserve BR-027 rules for own/base currency totals and mixed-currency grand totals. Display negative report amounts with a minus sign rather than parentheses; existing regional display formatting and separate CSV numeric formatting remain applicable.
- Category/Project/Correspondent report checkbox trees use Search/Next/Previous navigation across their existing group/element names, revealing matches inside collapsed groups. Searching does not change checkbox selections. Their Unassigned/Root selection structure remains under BR-024; ReportGroup organizes report definitions and does not become an additional classification filter.
- ReportGroup rows show the group Name with its favorite star; Grouping, From and To cells are blank. Report rows show their Name/favorite star and their own saved grouping/date values. The existing root favorite protection and shared inline-star behavior remain applicable.

### Report failure handling

Confirmed by the requesting user, 2026-10-08; BR-028, BC-007/008 and FR-007/008.

- If a saved report definition contains invalid JSON, show an error and leave the saved definition unchanged. Never silently substitute empty settings.
- If calculation fails after the selected date range was successfully saved, keep those saved dates, show an error and return to the report list. Calculation failure does not roll back the completed date-range save.
- If CSV export fails, keep the report-results popup open, show an error and allow the user to retry saving the same generated results.

### Currencies — layout

Confirmed by the requesting user in this chat, 2026-10-04; related capability BC-004 and requirement FR-004.

- In the main workspace, place the currency list on the left and the selected currency exchange-rate table on the right. Selecting a currency shows its exchange rates within this screen.
- Currency-list columns appear in this order: Code, Name, Symbol, Favorite. Display the favorite control as an empty star when false and a filled star when true; for example USD | US Dollar | $ | ★. Clicking the star follows the shared inline-favorite rule below. Existing currency favorite rules apply (BR-044).
- Editing a currency opens a modal dialog with read-only Code, editable Name and Symbol, a Favorite toggle, and Restore defaults for Name/Symbol using the existing system-catalog defaults under BR-013. Shared editor Save/Cancel and error-presentation rules apply.
- Add currency opens a modal editor with a picker for the existing system currency list. Choosing a currency fills its Name/Symbol defaults; the user can edit those values and must enter a valid initial rate before Save under BR-013/014/017. Existing currency creation, favorite-default and shared editor rules remain applicable.
- The Add currency system-list picker hides currency codes already present in the books, showing only currencies available to add. Existing dataset-wide code uniqueness validation still applies at Save under BR-013.
- Currency picker dialogs have no search textbox or Next/Previous search buttons. This includes the system-currency selection dialog used by Add currency. The tree-search rules apply to catalog tree pickers, not these currency lists.
- Exchange-rate table columns appear in this order: Date, Rate, Description. For the special initial rate, display Initial in the Date column instead of its internal system-controlled date; existing initial-rate and base-currency rules remain binding (BR-014).
- Display ordinary exchange rates newest first by date, with the special Initial rate fixed at the bottom of the rate list.
- Double-clicking an exchange-rate row or clicking Edit for the selected rate opens a modal rate editor. Add opens the same editor for a new rate. Apply the existing rate-date, precision, initial-rate and base-currency restrictions; shared editor Save/Cancel and error-presentation rules also apply.
- When adding an ordinary exchange rate, default Date to the current device-local calendar date (today). The user can choose another valid date before Save. The special Initial rate retains its separate system-controlled date rule.
- Delete rates opens a modal date-range dialog with From and To dates and shows the selected currency. Use the existing range-deletion operation. Silently skip the special Initial rate when applying the deletion; do not show a warning about skipping it. The Initial rate remains undeleted under BR-014.

### Accounts catalog — tree interaction

Confirmed by the requesting user in this chat, 2026-10-04; related capabilities BC-003/004 and requirements FR-003/004.

- The Accounts screen is for account/group management only. Do not show account-currency or base-currency balance columns or a balance-date selector there. Users view account balances in the Ledger filtered by account, under its existing cumulative-amount rules.
- Account rows have a separate read-only Currency column showing the account currency code in both the main Accounts tree and account-selection pickers that display elements, for example Cash | USD and Cash | EUR. Group rows have no currency value. The column helps users distinguish accounts when names repeat; account-name uniqueness and other restrictions remain unchanged.
- Double-clicking an editable group opens a modal group editor containing Name, Parent group with a picker button, and a Favorite checkbox. Add group uses the same editor. The Parent group picker is Select only groups. Root groups remain uneditable under existing protection.
- When adding a group or account, default its Parent group or Group to the group where the user made the selection in the originating tree; if no group context is available, use the catalog root group. The user may choose another valid group in the unsaved editor. After a successful Save, select the newly created item and reveal it in the tree, expanding its ancestor path and scrolling as needed.
- Before deleting a catalog group or element, ask for confirmation showing its name. This applies in both the main-window catalog and Edit-select popups; deletion proceeds only after confirmation and remains subject to existing saved-reference, open-editor and group-content restrictions.
- After successfully deleting a group or account, select its parent group in the Accounts tree. Existing deletion restrictions remain unchanged.
- Double-clicking an account opens its account editor in a modal dialog; it does not open the account-filtered Ledger.
- When saved AccountNameAddCurrency is false, name generation allows an empty currency. When true, require a valid selected currency and disable Restore default name until one is chosen. Account Save still requires a valid currency regardless of this naming preference.
- Place a Restore default name button beside Name in the account editor. On explicit click, generate the default name from the current Correspondent/Category/Project selections and existing Local naming settings under BR-012, and put it in the unsaved Name field. Persist the resulting name only through the ordinary account Save.
- For a new account, preserve a manually entered Name. If Name is empty after the existing trimming rule when the user saves the dialog, fill it with the default generated from the current Correspondent/Category/Project selections and Local naming settings before ordinary save validation. This is the default-name fill when saving a new account, not an implicit save on Cancel; existing shared Cancel behavior still applies.
- Changing classifications does not automatically update Name, including after an explicit Restore default name click. Restore uses the classifications current at the time of that click. A filled or restored name remains until the user edits it, clicks Restore again, or clears it and saves the new account under the empty-name rule. This new-account default rule does not change required-name validation for existing accounts.
- The Accounts tree uses single-selection mode. A single click selects one group or one account, replacing the previous selection; it does not open a dialog or navigate to another screen.
- Right-clicking a tree item selects it and opens a context menu with the same actions and enabled/disabled rules as the toolbar. Apply the current window mode, selected item type and existing business restrictions consistently to both command surfaces; Select-only pickers still expose no modification actions.
- Buttons and menu items update to match the selected item type. With a group selected, actions on existing accounts are disabled; Add account remains available to create an account inside that group. With an account selected, group-specific actions are disabled. Applicable actions remain subject to existing business restrictions, including root-group protection.
- Merge 2 groups is available only in the docked main-window catalog with an eligible group selected and is disabled when an account is selected. It is unavailable in every popup. The selected group is the source; the user chooses the destination group in a modal tree dialog.
- After destination selection, show a confirmation warning identifying both groups and explaining that the source group will be removed by the merge, for example: Merge 'Source' into 'Destination'? 'Source' will be removed. Perform the merge only after confirmation, under the existing group-merge rules.
- After a successful merge and closing the popup, select the destination group in the catalog tree and scroll it into the visible area, expanding its ancestor path as needed to make it visible.
- These rules describe the docked Accounts catalog and are shared by the other grouped catalogs as specified under Catalog windows and selection modes. Modal pickers use double-click-to-choose behavior rather than double-click-to-edit.

### Templates — entry editing

Confirmed by the requesting user in this chat, 2026-10-04; related capability BC-006 and requirement FR-006, under BR-022.

- Above the template entries table, place Name, Group with a picker button, optional single-line Description and a Favorite checkbox, in that order. Existing name, group, description and favorite rules remain applicable (BR-006/008/009/022/044).
- In the template editing dialog, entry-table columns appear in this order: Account, Amount, Currency. Amount is editable; Currency is read-only and comes from the entry account.
- Each Account field has a selection button on the right that opens the account-tree popup under the existing catalog-picker routing rules. Double-clicking an eligible account or selecting it and clicking OK assigns the account to the template entry and closes the picker.
- Add entry appends an empty row to the template entries. Remove entry removes the selected template-entry row. These changes stay unsaved until the template is saved, under the existing entry-validity and editor Save/Cancel rules.

### Catalog trees — move and drag-and-drop

Confirmed by the requesting user in this chat, 2026-10-04; related capabilities BC-003/004/006 and requirements FR-003/004/006.

- Move acts on the selected group or element. It opens a modal tree dialog for choosing the destination group. After a successful move, keep the moved item selected and visible, expanding its destination/ancestor path and scrolling as needed.
- Drag a group onto another group to move the dragged group into the target group.
- Drag an element onto a group to move the element into the target group.
- In the docked main-window catalog only, Shift-drag a group onto another group to merge the dragged source group into the target destination group. This gesture is unavailable in every popup. Show the same source/destination confirmation warning as the Merge action before making changes. After a successful merge, select the destination group and scroll it into view.
- Ordinary drag-and-drop moves use the same post-success selection/visibility behavior as Move. All these interactions obey the existing same-type hierarchy, root protection, cycle, merge and naming rules (BR-006/007/008); drag-and-drop does not bypass validation.
- While dragging an item, holding it over a collapsed group expands that group so nested destinations can be reached. Hover expansion alone does not move or merge the item.
- Dropping between sibling rows reorders the dragged item within that sibling sequence: groups among groups, elements among elements. Show an insertion line for reorder placement. Dropping onto a group retains the existing move-into-group behavior and main-window Shift-merge exception. Existing catalog/mode permissions remain binding.
- Favorites only does not disable sibling reordering or permitted moves into another group. Dropping between two visible siblings places the dragged item immediately after the preceding visible sibling in the complete sibling sequence, including hidden items; the user example is insertion after Order 5 and before visible Order 9 at Order 6. Dropping at the start places the item first in the complete sibling sequence. First position retains the existing zero-based stored Order = 0; the numbering convention does not change. Keep the established separate group/element sequences and mode permissions.
- Show a not-allowed cursor for invalid drop destinations and make no change, including when a group is dragged into its own descendant. Existing domain validation remains authoritative.
- While dragging near the top or bottom edge of the tree viewport, automatically scroll so destinations outside the visible area can be reached.
- Escape during an active drag cancels only that drag, without closing the current popup or changing stored data. Groups expanded by hover during the drag remain open after a drop or cancellation.
- Drag-and-drop gestures beyond the explicitly confirmed rules are not yet specified.

### Catalog windows and selection modes

Source: requesting user, this chat, 2026-10-04. The user explicitly requested recording these decisions in BRD/TRD. This records the confirmed scope without approving either complete document. Related capabilities BC-003–BC-006 and requirements FR-003–FR-006.

The tree interactions above also apply to Correspondents, Categories, Projects and Templates: single selection, type-dependent actions, modal editing, move/merge, drag-and-drop and selection/visibility after changes. Each catalog retains its own element editor and business restrictions. Creation actions remain available for the selected parent group.

Use the following five catalog window/popup types. Edit-select refers to either of the first two tree types, replacing the earlier regular/selection-big terminology. Transaction editing is a separate dialog. There is no Select only groups-and-elements mode in the first version.

| Type | Presentation and purpose | Modification actions |
| --- | --- | --- |
| Edit-select groups+elements | Docked catalog for management, or modal groups-and-elements picker for an element choice | Group/element Add, edit, delete, move and permitted drag-and-drop actions, subject to catalog rules; merge/combination only in the docked main-window catalog |
| Edit-select groups | Modal groups-only picker for a group choice | Group Add, edit, delete, move and permitted ordinary drag-and-drop; no element commands, group Merge or element combination |
| Select only groups | Modal tree showing groups only, for choosing a group; favorite stars are read-only indicators | None, including no favorite-status changes |
| Edit element | Modal add/edit form for the catalog's element, including a Favorite checkbox | Edit the element's fields; reference fields may open pickers; favorite changes take effect only on successful Save |
| Edit group | Modal add/edit group form containing Name, Parent group with a picker button, and a Favorite checkbox | Edit Name, choose Parent group and edit Favorite; apply changes only on successful Save, subject to root/hierarchy protection |

- In a docked Edit-select catalog, double-click edits a group or element, except that double-clicking a report invokes its Run flow. In a picker, double-clicking an eligible item selects it and closes the popup; selecting an eligible item and clicking OK also completes selection. Editing inside an Edit-select picker uses its editing action rather than the selection double-click.
- Single click changes the single selected row. Buttons and menus reflect the selected type, caller's allowed selection and existing business restrictions.
- Click the group's expand/collapse arrow to toggle expansion. Clicking the rest of the row selects it without toggling expansion; existing double-click edit/select behavior remains applicable.
- When the tree row area has keyboard focus, Up/Down selects the previous/next visible row. Right Arrow expands a closed group; on an expanded group it selects the first visible child, if any. Left Arrow collapses an expanded group; otherwise it selects the parent, if any. At the root, the parent-navigation action does nothing. Navigation uses the actual hierarchy even beyond the visual indentation cap and preserves existing picker eligibility rules.
- With a tree row focused, Enter invokes the same action as double-click: edit in management views, select an eligible item in selection pickers, or run a saved report. Existing mode-specific eligibility and disabled-action rules apply. Home/End selects the first/last visible row. In report checkbox trees, Space toggles the selected row checkbox; a partially checked group becomes fully checked, following the existing report checkbox propagation rules. These row shortcuts do not override text-entry or other focused controls.
- After saved catalog changes, refresh affected open screens and pickers when they become active again. This includes returning from a nested picker canceled after independently saving catalog changes.
- During tree refresh, preserve expanded groups, selection and scroll position where possible. Previously confirmed rules for revealing added/moved items and other explicit selection/visibility outcomes take priority.
- If refresh fails, keep the previous rows visible, show an error with Retry, and disable editing and selection confirmation until refresh succeeds. Cancel/Close remains available.
- Ordinary local SQLite loading and editor saving use synchronous execution from the UI action, as requested by the user. Do not add progress indicators, progress popups or temporary busy-state input/Save/close guards for these short operations. Existing validation, error reporting, transaction rollback and explicit refresh-failure rules remain applicable. This decision does not change synchronization or first-use creation/attachment progress and cancellation flows. Expected short duration is a design assumption, not a measured performance guarantee.
- Collapsing a group hides all descendants while retaining each nested group's own open/closed state. Reopening reveals only rows whose entire ancestor path is open, subject to the current mode and filter. Expanding/collapsing does not change report checkbox choices.
- If collapsing a group hides the selected descendant, select the collapsed group instead. A selection outside that subtree remains unchanged. Picker eligibility rules still apply; selecting a group does not make it an eligible element choice.
- Combined tree/table views show column headers that remain visible during vertical scrolling; clicking a header does not sort. Rows remain single-line, with overflowing text shortened by an ellipsis and full text available in a tooltip. Do not provide horizontal scrolling. Configure column widths through a Columns popup with editable width values and Apply/Cancel buttons; do not resize columns by dragging their borders. Save applied widths locally per view, separately for main screens and selection popups, and restore them on reopening. Restore defaults resets the popup values to predefined column widths; Apply commits them and Cancel discards pending changes. Column widths are percentages of the available row width and must total 100%; preserve those proportions when the viewport changes. Prevent widths that hide essential row controls, including expanders, checkboxes and favorite stars. In version one all columns remain visible in their fixed order; Columns changes widths only. Enforce a minimum window/popup width so it cannot be resized below the width needed for usable columns. Exact minimum dimensions remain a layout detail. Names and descriptions are left-aligned; amounts and rates are right-aligned; stars and checkboxes are centered within their allotted space. The currency-name text in the Account tree is centered. Cap visual indentation at eight steps, using a fixed constant of 8. Rows deeper than level 8 use the same visual indentation as level 8. Preserve actual hierarchy depth, parent relationships and expand/collapse behavior; this is a display limit only.
- Tree contents and caller eligibility are distinct. An Edit-select groups+elements account picker displays groups for navigation but accepts accounts only. Edit-select groups and Select only groups accept eligible groups only.
- For a group selection, open that catalog's groups-only Edit-select popup when no Edit-select tree or Edit group editor for that same catalog is already active. An Edit element editor alone does not trigger the fallback: it may open the groups-only Edit-select popup, which has no element commands that could request a second element editor. If either presentation of that catalog's Edit-select tree or its Edit group editor is already active, open Select only groups. The docked main-window catalog counts as active even while blocked by a modal editor. Nested parent/destination group choices from a groups-only Edit-select popup therefore use Select only groups. This rule applies to Accounts, Categories, Correspondents, Projects, Templates and Reports; instances for another catalog do not trigger the fallback.
- Element selection uses Edit-select groups+elements, accepting eligible elements only. Nested group choices use the group-selection routing above; no groups-and-elements select-only fallback is provided.
- In a new group editor, default Parent group to the group where the user made the selection in the originating tree, or the catalog root if there is no group context. In an existing group editor, initialize Parent group to its current parent. The picker uses Select only groups and accepts only valid same-catalog parents; self/descendant choices and forbidden root operations remain rejected under BR-006/007. Choosing another parent changes only the unsaved editor. For an existing group, apply the move only on successful Save with the edited Name and ordinary naming/hierarchy validation; Cancel leaves the persisted group name and parent unchanged under the shared discard rules.
- The active interaction chain may contain only one instance of each catalog/window-type pair. Accounts and Categories may each have their own Edit-select or Edit element instance simultaneously. The docked catalog counts as an existing instance.
- Select only groups cannot add, edit, delete, move, merge, change favorite status or modify by drag-and-drop, and cannot open further editors or pickers. Its favorite stars are read-only; the Favorites only view toggle still follows the shared filtering rules. Modal parents are blocked while their child is open. Therefore the approved flows cannot reopen an already active Select only groups dialog; no additional duplicate-dialog interaction is required.
- A nested choice returns to its immediate caller. Existing post-action tree rules still apply: after Move select/reveal the moved item, after Merge select/reveal the destination, after Add select/reveal the new item, and after Delete select the parent group.
- Every Add/Edit group and element editor in Accounts, Categories, Correspondents, Projects, Templates and Reports includes a Favorite checkbox. Initialize it to the current value for an existing item and false for a new item under BR-044. Checkbox changes stay in the unsaved editor and take effect only on successful Save; Cancel discards them under the shared editor rules. Existing root protection and the fixed false root favorite flag remain unchanged. After Save, the agreed favorites-filter and selection rules apply.
- All Edit element and Edit group dialogs use the transaction editor Save/Cancel rules: Save closes only after a successful save; a failed save leaves the dialog open with entered data. Cancel or closing the dialog asks for confirmation if unsaved changes exist. Confirming discards those unsaved changes and closes the editor; declining keeps it open. With no unsaved changes, it closes without discard confirmation. Independently completed catalog operations from nested pickers remain saved under the separate catalog-activity rule.
- Catalog management inside an Edit-select popup is an independent activity provided for user convenience. Successfully saved additions/edits and completed delete/move actions remain saved even if the user later cancels the picker or its calling editor. Canceling the calling transaction discards only unsaved changes in that transaction editor; it does not undo catalog changes. Example: add and save an account from the transaction account picker, then cancel the transaction; the new account remains.

Confirmed flow 1:

1. Transaction editor requests an account: open Accounts Edit-select groups+elements popup.
2. Move an account: Accounts Edit-select is already active, so open Accounts Select only groups. Choose the destination, complete Move and close that picker, returning to Accounts Edit-select.
3. Add an account: open Accounts Edit element. Choosing its group opens Accounts Select only groups because Accounts Edit-select remains active; close the group picker after choosing.
4. Choose the account's category: no Categories Edit-select is active, so open Categories Edit-select groups+elements.
5. Add a category: open Categories Edit element. Choosing its group opens Categories Select only groups because Categories Edit-select remains active.

Confirmed flow 2:

1. Accounts Edit-select groups+elements is docked in the main window.
2. Add an account: open Accounts Edit element.
3. Choose its group: open Accounts Select only groups because the docked Accounts Edit-select already counts as active. After selection, return to the account editor.

Confirmed flow 3:

1. Ledger Create template opens Templates Edit element directly, without a Templates Edit-select ancestor.
2. Choosing its group opens Templates Edit-select showing groups only, because no Templates Edit-select tree or Edit group editor is active.
3. This group picker allows group Add/Edit/Delete/Move under ordinary restrictions; it has no template-element commands or Merge. Nested group choices open Templates Select only groups. Accepting a group returns it to the template editor.

These rules supersede proposals that every picker opened beneath another picker must be select-only, that Move/Merge alone determines picker mode, or that an Edit element editor always forces its own group picker to be select-only. The group-selection decision now depends on an active same-catalog Edit-select tree or Edit group editor. The earlier routing model in TRD, Catalog window routing and modal nesting, requires alignment with the revised groups-only presentation and fallback before implementation.

### Catalog changes affecting an open editor

Source: requesting user, this chat, 2026-10-04; related capabilities BC-004/005/006 and requirements FR-004/005/006. Existing saved-reference deletion restrictions remain binding.

- When a picker is canceled after its currently referenced item was renamed or moved, retain that item identity in the calling field if it still exists and refresh its displayed name/path. Cancel dismisses the selection request without choosing a replacement; it does not undo saved catalog changes. Deleted references follow the separate optional/mandatory rules below.
- If an optional reference selected in an open editor is deleted through independent catalog activity, clear the field without a warning. This does not permit deletion of an entity protected by saved references.
- While editing an existing transaction or template, accounts used by entries in that editor cannot be deleted through nested catalog activity. This also protects current unsaved account selections; persisted-reference restrictions still apply independently.
- For a new unsaved transaction or template, if an account selected in its entries is otherwise eligible for deletion and is deleted through catalog activity, remove the entries using that deleted account from the transaction/template editor. Canceling the account picker does not restore the deleted account or those entries. Other entries remain unchanged.
- If the selected currency of a new unsaved account is deleted through independent catalog activity, clear Currency in the account editor. A valid currency is required before Save; the user must choose another currency or cancel the account dialog. No replacement currency is chosen automatically. Existing accounts retain their immutable currency and existing currency-deletion protection.
- All popups are modal. Only the top popup in the active stack can be used or closed; lower popups and the main window remain blocked until it closes.
- A catalog element editor's group picker follows the revised routing above. Select only groups cannot delete or merge groups; groups-only Edit-select can delete otherwise eligible groups. If the selected Group of any new unsaved catalog element (Account, Category, Correspondent, Project, Template or Report) is deleted through this independent catalog activity, clear Group without a warning and require another valid group before Save. Canceling the picker does not restore the deleted group or its reference. No automatic reassignment to root is made. Cancel can discard the new unsaved element under the shared editor Cancel/discard rules; a missing Group blocks Save, not Cancel. Existing group-content restrictions still protect groups containing saved elements.
- Combining catalog elements (accounts, categories and other element types that support combination) is available only from the docked catalog in the main window. Combination commands are unavailable in every popup, including Edit-select popups. The main window is blocked while any modal editor/picker is open, so an open transaction/template editor cannot initiate element combination through nested catalog activity. No source-to-destination replacement of unsaved editor entries is required for that interaction.
- Group Merge, like element combination, is available only in the docked main-window catalog. It is unavailable in every popup through buttons, context menus or Shift-drag. Ordinary permitted group/element moves remain available in Edit-select popups; Select-only popups still permit no modifications. Existing confirmation, hierarchy and post-merge selection rules remain applicable to main-window Group Merge.

### Windows display and input formats

Confirmed by the requesting user in this chat, 2026-10-04.

- Application-generated technical text in reports, logs and similar outputs is English, including column headings, status labels and diagnostic messages. Business text, such as account/category names and user-entered descriptions, remains in the user's language exactly as stored; do not translate it. This language rule does not change the separately confirmed numeric/date formats.
- Date format and formatted numeric display use Windows regional settings. Amount/rate editing uses the separate plain-text rule below.
- While editing amount/rate text, allow unfinished values such as a minus sign or a number ending with the decimal separator without immediately reporting an error. Validate on focus loss and on Save. Apply configured precision and the existing rounding rule at those boundaries, not after every keystroke. If validation fails, preserve the entered text, show an error beside that field and prevent saving until corrected. Existing empty-value and caller-specific amount/rate rules remain binding.
- On entering an amount/rate field, select its entire value for replacement and use plain, unformatted editing text without thousands grouping or regional formatting. Apply display formatting after focus leaves a valid field. Use a dot as the decimal separator while editing, regardless of Windows regional settings; for example, edit 1234.56 and format it regionally only after focus loss. Pasted input uses the same plain dot-decimal format: ignore surrounding whitespace, but reject currency symbols and thousands separators under the existing field-validation rules. The numeric keypad decimal key inserts a dot regardless of Windows settings. Accept simple decimal numbers only; calculator expressions and scientific notation are unsupported. If Save encounters invalid input, focus the first invalid field and preserve the other entered values.
- Transaction time is displayed and edited in 24-hour HH:mm, without seconds, regardless of the regional clock format. This user-confirmed change supersedes the earlier HH:mm:ss UI requirement. For new transactions or when the user changes an existing transaction time, save seconds as 00. If an existing transaction time stays unchanged, preserve its stored seconds; opening the minute-only editor or editing other fields must not clear them.
- Amount/base-amount fractional precision follows AmountPrecision, and rate fractional precision follows RatePrecision under the existing configuration rules (BR-017). Regional separators do not change these precision rules or the established date/time storage semantics.

### Editor error presentation

Confirmed by the requesting user in this chat, 2026-10-04; related capabilities BC-003–BC-006 and requirements FR-003–FR-006.

- Show field validation errors beside the affected field.
- Show general save errors in a banner inside the editor dialog.
- Preserve entered data and keep the dialog open after a failed save so the user can correct or retry it. Existing validation and transaction rollback rules remain binding.

### Selection controls

Confirmed by the requesting user in this chat, 2026-10-04.

- Use modal selection dialogs for complex structures such as account trees. Account, template and other catalog pickers follow the Edit-select/select-only routing rules above.
- Use combo boxes only for simple, small, flat lists. No numeric item-count threshold is established by this decision.
- In a groups-only tree, show the hidden-element count beside each group name, for example Store (4). Count elements directly assigned to that group only; do not count child groups or elements in subgroups. Each subgroup shows its own direct-element count. Group deletion still checks actual child groups and elements under BR-007; hiding elements does not make a nonempty group deletable.
- Catalog trees and tree pickers provide a search textbox with Next and Previous buttons. Match search text anywhere within group and element names, case-insensitively, across the whole applicable catalog tree, including collapsed branches; Select only groups still contains groups only. For example, bank matches both Bank account and My bank savings.
- Search navigates to matching rows in the tree instead of filtering the tree to matching items. Next and Previous navigate forward/backward between matches, revealing the matching row by expanding its ancestor path and scrolling as needed. Navigating to a match does not complete a picker selection or change catalog data or favorite flags; caller eligibility still applies.
- Search navigation wraps: Next from the last matching row selects and reveals the first matching row; Previous from the first matching row selects and reveals the last matching row.
- If search finds no matches, keep the current tree selection and show No matches beside the search textbox.
- In catalog trees and catalog tree pickers, search is available only when Favorites only is off, and then covers the whole applicable tree. Turning Favorites only on disables the search textbox and its Next/Previous buttons; search navigation is unavailable while the favorites filter is active. Turning Favorites only off re-enables search. The existing favorites selection-restoration rule still applies. The earlier search-filtering proposal and restriction to searching within favorites are superseded.
- When a picker opens for a populated field, select its current item and scroll it into view, expanding its ancestor groups as needed.
- Optional reference fields have a Clear (×) button beside the picker button. Clear empties the field without opening the picker. Mandatory reference fields have no Clear button. Clearing a reference does not delete the referenced catalog item.
- Every catalog tree picker opens with Favorites only off and the whole applicable tree visible, regardless of the toggle state in a previous opening. For a populated field, select and reveal its current item under the rule above.
- Catalog grids/tree rows show an empty star for IsFavorite = false and a filled star for IsFavorite = true. Where catalog modification is allowed, clicking the star toggles and saves that item's favorite status directly, without opening its editor. This applies to favorite-capable groups, elements and currencies under BR-044. Within catalog tree popups, stars are clickable only in Edit-select groups+elements and Edit-select groups; Select only groups shows read-only stars. Existing root protection still applies. A star click does not accept a picker choice or close the picker; a completed favorite change is independent catalog activity and is not undone by canceling the picker or its calling editor.
- When Favorites only is active and a star click switches an element or currency to non-favorite, remove its row from the filtered grid and keep Favorites only on. For groups, apply BR-044: a non-favorite group remains visible only when still required as an ancestor navigation path to favorite descendants. This inline action does not use the Add/Edit-dialog rule that turns the filter off to reveal the saved item. Selection after this inline change follows the rule below.
- If saving an inline star change fails, keep or restore the original favorite status, row and selection, keep the filter state unchanged, and show an error banner in the current catalog screen or Edit-select picker. The successful-change selection fallback does not apply to a failed change; the picker remains open under the existing modal rules.
- After a successful inline favorite change, if the selected row disappears from the active filtered set or is no longer selectable for the caller, select the first selectable item in the remaining filtered tree/list and reveal it, expanding its ancestor path and scrolling as needed. If no selectable item remains, clear selection and disable the picker's OK button. If the current selection remains visible and selectable, keep it. The Favorites only filter remains on; moving the selection does not accept a picker choice.
- Every catalog tree picker has a Favorites only toggle, applying the existing BR-044 filtering rule: show favorites and retain necessary ancestor groups as navigation paths. Ancestors do not become favorites, and filtering does not relax the picker eligibility or modification restrictions.
- In a groups-only tree picker, Favorites only uses group favorite flags only. Retain non-favorite ancestor groups when needed as navigation paths to favorite groups under BR-044. Favorite elements that are hidden from this groups-only tree do not make their groups appear in this filter.
- When Favorites only is turned on, remember the current whole-tree selection. If that item is hidden by filtering, select the first visible item eligible for the caller. If it remains visible and eligible, keep it selected. If there is no eligible visible item, there is no accepted selection.
- When the user turns Favorites only off, restore the whole applicable tree and the remembered selection, revealing it under the existing selection-visibility rule. If the remembered item has been deleted during catalog activity, the existing deleted-reference rules apply; restoration does not recreate it. Changing the filter or highlighted row alone does not complete the selection request.
- After successfully saving a newly added catalog group or element through its Add editor, if Favorites only would hide that new item, automatically turn the filter off, restore the whole applicable tree, and select/reveal the new item under the existing post-Add rule. This applies to docked catalog trees and Edit-select pickers. In this automatic filter-off case, selection of the newly created item takes precedence over restoring the earlier remembered selection. If the new item is already visible, keep the filter state and follow the ordinary post-Add selection rule. Saving the item does not itself complete a picker selection request.
- When Favorites only is on and an existing catalog group or element is edited with Favorite cleared in its Edit dialog, after successful Save automatically turn the filter off, restore the whole applicable tree, and keep the edited item selected and visible, expanding its ancestor path and scrolling as needed. This applies to docked catalog trees and Edit-select pickers. In this automatic filter-off case, retaining the edited item takes precedence over restoring the earlier remembered selection. Saving the edit does not itself complete a picker selection request; ordinary shared Save/Cancel rules still apply.

### Ledger — account selector

Confirmed by the requesting user in this chat, 2026-10-04; related capabilities BC-004/005 and requirements FR-004/005.

- Place an account selector above the Ledger list, initially showing All accounts.
- Its picker button opens a modal account-tree selection dialog. Choosing an account switches the Ledger to transactions for that account.
- Clear removes the account selection and returns the Ledger to All accounts.
- Account-filtered Ledger columns appear in this order: Date/time, Description, Amount, Cumulative amount, Currency. Currency is a separate final column showing the selected account's currency.
- Amount is the signed sum of the selected account's entry amounts within that transaction, expressed in the account currency. This is a display total only; the underlying entries remain separate. This account-filtered Amount rule differs from the nonnegative base-currency amount used in the all-transactions view. Cumulative amount remains the last matching entry's cumulative amount under the rule below.
- In the account-filtered Ledger, show one row per transaction even when the selected account occurs in multiple entries. Preserve the separate entries, including their individual amounts and rates; do not combine them.
- The displayed cumulative amount is the cumulative amount of the last entry for the selected account in that transaction. The accounting order is transaction date/time, then transaction GUID, then entry order within the transaction. Use the last matching entry in that order, not the final entry belonging to a different account and not a sum of cumulative amounts. This shows the selected account's cumulative amount after its final entry in the transaction.
- User example: the same account may have two USD 500 entries at rates 40.00 and 40.05 within one USD 1,000 exchange transaction. The entries remain separate; the Ledger row uses the latter matching entry's cumulative amount.

### Ledger — date navigation controls

Confirmed by the requesting user in this chat, 2026-10-04; related capability BC-005 and requirement FR-005.

- Above the Ledger list, provide a date picker, Today button, Previous/Next buttons and a navigation-step combo with Week, Month, Quarter and Year options.
- Previous/Next move the selected date backward/forward by the chosen step. Today selects the current device-local date. Use the existing selected-date transaction-read rules and transaction-list limit; this control layout does not redefine the query boundaries.
- These are Ledger date-navigation controls, separate from the Next/Previous name-search controls in catalog trees and pickers.
- Changing the selected Ledger date or account filter immediately reloads the transaction list using the existing capped selected-date query rules. No Apply button is required. Clearing the account filter and the Today/Previous/Next date actions follow the same reload behavior when they change the selection; existing post-edit refresh rules remain separate.
- When the transaction query indicates that more than the existing 300-item limit match, show a notice above the Ledger list: Showing the newest 300 transactions. Choose an earlier date to view older transactions. Apply this in both all-transactions and account-filtered modes, using the existing exceeded-limit indication rather than assuming that a result of exactly 300 proves truncation. Existing navigation-metadata retention after editor refresh remains unchanged.

### Ledger — all-transactions view

Confirmed by the requesting user in this chat, 2026-10-04; related capability BC-005 and requirement FR-005.

- After opening the books, Ledger is the initial main-window screen. It shows the newest transactions across all accounts, newest first, up to the existing transaction-list limit of 300. No account filter is selected.
- All-transactions Ledger columns appear in this order: Date/time, Description, Amount, Currency. Description is shown when present; Amount is nonnegative and expressed in base currency. The final Currency column shows the base currency in every row. This view has no account cumulative-amount column. In account-filtered mode, every row instead shows the selected account currency under the account-selector rules above.
- Calculate the displayed amount from the entries' rounded base-currency amounts (BaseAmount), using the following rule. Select a whole sign-side total; do not remove balancing-account entries from that total.

| Balancing-account entries in the transaction | Displayed amount |
| --- | --- |
| Positive side only | Absolute value of the sum of all negative entry BaseAmounts |
| Negative side only | Sum of all positive entry BaseAmounts |
| Both sides, or no balancing-account entries | Sum of all positive entry BaseAmounts |

- Confirmed example, expressed in base currency: Ac1 +50, Ac2 +45, balancing account +5, Ac3 -70, Ac4 -30. Display 100, from abs(-70 -30), not 95.
- This is a display calculation only; it does not change entries, transaction validity or accounting balances.

### Ledger — Draft visibility

Confirmed by the requesting user in this chat, 2026-10-04; related capability BC-005 and requirement FR-005.

- Show a Draft badge beside date/time for Draft transactions in the Ledger.
- In the transaction dialog, show a persistent warning explaining why the transaction is Draft, for example: Entries are not balanced, or At least two entries are required.
- Do not provide a manual status selector. Existing transaction validity and Draft/Confirmed rules determine the status; this presentation does not relax individual entry validation.

### Ledger — delete a transaction

Confirmed by the requesting user in this chat, 2026-10-04; related capability BC-005 and requirement FR-005.

- In the first version, the Ledger Delete action applies to one selected transaction and asks for confirmation before deletion.
- The Ledger does not offer selection and deletion of multiple transactions in the first version. This UI decision does not remove existing service operations for filtered deletion.

### Ledger — transaction dialog

- The transaction dialog places date/time and Description at the top, followed by an entries table with columns in this order: Account, Amount, Rate, Base amount. Base amount is calculated and read-only, using the existing base-amount calculation and precision rules (BR-017/018).
- In the transaction editor, use separate date and time fields beside each other: a calendar date picker and a 24-hour time picker with editable hours and minutes (HH:mm). Transaction time presentation omits seconds. New transactions still default to now; existing device-local display and UTC storage rules remain applicable. Ledger date navigation remains date-only under its existing query rules.
- Transaction Description uses a single-line textbox in the edit dialog and is displayed on a single line in the Ledger. It remains optional. No maximum character count is established by this presentation decision.
- When Description exceeds the Ledger column width, truncate its display with an ellipsis and show the full Description on hover. This does not truncate the stored Description.
- Amount and Rate are edited directly in the entries table, subject to existing currency, rate and precision rules. Each Account cell has a button on its right. Clicking the button opens an account-tree selection dialog for that entry.
- Place a Restore rate button beside each Rate field. On explicit click, reload the applicable catalog rate for the entry currency and transaction date under BR-015. Existing base-currency and rate-validation rules remain unchanged.
- When an entry account changes, retain Amount. If the old and new accounts use the same currency, retain the existing Rate, including a manually overridden rate. If their currencies differ, load the default rate for the new currency at the transaction date under BR-015 (base currency rate is 1). Recalculate Base amount using the retained Amount and resulting Rate; update Difference under the existing display rule.
- The Amount cell contains a numeric editing field and a separate read-only currency-code label beside it, for example [50.00] USD. The currency code stays visible during editing, is not part of the editable value, and introduces no additional tab stop. The label updates when the entry account changes.
- In the account-tree dialog, double-clicking an account or selecting it and clicking OK assigns that account to the entry and closes the picker. Groups can be expanded/collapsed but cannot be chosen as an entry account.
- Add entry appends a blank row to the entries table. Remove entry removes the selected row. These changes remain unsaved until the transaction is saved; the existing entry-validity rules still apply at Save.
- Below the entries table, show Difference (the sum of entry BaseAmounts) and the existing Add balancing entry button. Difference updates as entries change. The balancing action retains the existing explicit-click behavior and validation rules (BR-018).

Confirmed by the requesting user in this chat, 2026-10-04; related capability BC-005 and requirement FR-005.

- Double-clicking a transaction row or clicking Edit for the selected transaction opens that transaction in a separate editing dialog.
- Add opens the same dialog for a new unsaved transaction with date/time set to now. From the all-transactions Ledger, it starts with no entries. When the Ledger is filtered by account, it starts with one entry whose account is the selected filter account.
- The Ledger has a Duplicate button. It opens the selected transaction as a new unsaved copy in the transaction dialog, with date/time set to now, under the existing duplication rules (BR-020).
- The Ledger has a Create template action for the selected transaction. It opens a new unsaved template editor with the transaction accounts, amounts and Description copied under BR-022, preserving separate entries. The user chooses the template Name and Group before saving. Opening this editor does not save the template or modify the source transaction; shared template validation and Save/Cancel rules apply.
- The Ledger also has a From template button for creating a transaction from a template under the existing template rules (BR-022; BC-006; FR-006). It opens a template-selection tree dialog. Selecting a template and confirming the selection opens the populated transaction dialog as a new unsaved transaction for review and saving; selecting a template does not itself save the transaction.
- Save closes the dialog after a successful save. A failed save leaves the dialog open with its entered data.
- Cancel discards unsaved changes and closes the dialog. If unsaved changes exist, Cancel or closing the dialog window asks for confirmation before discarding them. Confirming discards the changes and closes the dialog; declining keeps the dialog open with the changes. No discard confirmation is needed when there are no unsaved changes.
- Existing transaction validation and Draft rules remain unchanged.

## Business Capabilities

`BC-###` identifies a high-level Business Capability in this BRD. Detailed Use Cases belong in the TRD as `UC-###-##`, linked to their parent capability; for example, `BC-003` maps to `UC-003-01` for merging category groups. Each detailed use case describes the specific action, alternative flows and expected results, and links to API/BFF operations. Traceability is **Business Capability → Detailed Use Case → API/BFF operations → test scenarios**. The detailed use cases themselves are not created by this BRD revision.

All business capabilities are first-release scope; relative priority not separately ranked. All use **ROLE-001**. Given/When/Then statements are **proposed acceptance formulations of cited decisions**, pending draft review; no new business policy is implied.

### BC-001 — Start bookkeeping for the first time

- Source: F Startup/Create/Initial content; C base currency.
- Trigger: owner starts without local copy.
- Preconditions: creating mode, any available internet connection for manual setup, Master absence confirmed before creating it.
- Main flow: (1) Show credentials-first setup. (2) Owner enters login/password; determine Master existence. (3) If absent, obtain the selected base currency and amount/rate precisions. (4) Create owner/Master and register the device. (5) Initialize six roots/base currency at 1 and the base-currency rebalancing account in Account root; select it in System configuration. (6) Obtain local data and enter ordinary use. If Master exists, follow the attachment branch under BC-002.
- Alternate/error flows: existing Master routes to attachment; unknown existence or creation outcome prevents another creation attempt until resolved under BR-003. No internet/failed download leaves no bookkeeping access. On failure, show the error and let the user retry manually; no automatic setup retries. If Master already exists after initial download failure, preserve it and retry through attachment. Missing required login/password prevents setup submission; no extra length/character rules.
- Outcome: initialized local bookkeeping with immutable base currency.
- Linked requirements: BR-001–BR-004, BR-017; FR-001; NFR-001; SC-001.
- Proposed scenarios: Given no master/local copy, when Create succeeds with PLN, then six roots, PLN rate 1, one PLN rebalancing account in Account root selected in System configuration, and no transactions. Given mobile data without Wi-Fi, when the user starts initial setup, then that connection is allowed because setup is manual. Given existing Master, when setup checks after credentials are supplied, then select the attachment branch instead of creation. Given Master creation succeeded but initial download failed, then show the error, keep normal access unavailable, preserve Master and retry downloading through the attachment branch only on user action. Given either required login or password is missing, when submitting setup, then submission is rejected; version one imposes no additional length/character/complexity rule.

### BC-002 — Open existing bookkeeping or add a device

- Source: F Startup/Open; S Ownership/Registration.
- Trigger: open the app with a local copy, or submit setup credentials without one when Master exists.
- Preconditions: local copy for ordinary use; any available internet connection and valid credentials for manual device setup.
- Main flow: (1) Check local existence only. (2) If present enter open/editing mode. (3) Otherwise show credentials-first setup. (4) Enter credentials; if Master exists, the application selects attachment. (5) Authenticate, register the device and download full business data. (6) Open local bookkeeping.
- Alternate/error flows: ordinary use offline without login; recovery/expiry use BC-010/BC-011. Invalid credentials do not authorize cloud access; show the error and allow manual retry through Open; no automatic retries. Required-fields-only credential validation is settled. No local copy means no normal access.
- Outcome: ordinary offline use or registered new local copy.
- Linked requirements: BR-001–BR-003, BR-005; FR-002; NFR-001, NFR-002; SC-001, SC-004.
- Proposed scenarios: Given existing copy/no pending recovery, when opening offline, then no sign-in or internet needed. Given successful setup attachment, then registration precedes download and local use becomes available. Given the app was reinstalled, when setup completes, then register a new local copy rather than reuse the old registration; the old registration follows the 90-day expiry rule.

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
- Proposed scenarios: Given repeated regional entries for one ISO code, when building currency choices, then show one entry with the first returned Name and Symbol. Given prior draft/template use, when references removed and account currency changed, then still reject. Given all classifications absent in slash format and Add currency unchecked, then valid name // generated. Given the same empty classifications, Add currency checked and account currency UAH, then valid name //(UAH) generated. Given a new Local configuration, then Add currency is unchecked. Given Add currency checked, then append the account currency identifier for both base-currency and other-currency accounts. Given `DefaultAccountName` is `{Correspondent}/{Category}/{Project}` and an account was generated as `Alice/Food/Home`, when Correspondent Alice is renamed to Bob, then that account name remains `Alice/Food/Home`. Different LocalDbs may use different default formats. Given new local configuration, then default order is Correspondent, Category, Project and separator is `/`. Given format settings, when choosing an order, then all six permutations are available. Given an empty or whitespace-only separator textbox, when focus leaves it or Save is clicked, then replace the input with /. The textbox accepts at most one character; the saved separator is exactly one non-whitespace character. A single non-whitespace character is valid. Given a rate that rounds to zero at configured RPr, reject. Given dataset creation, persist selected APr/RPr and base currency; later changes are rejected.

### BC-005 — Record, revise or duplicate transactions

- Source: C Transactions/Precision/Currency/Dates.
- Trigger: create/edit/duplicate/delete transaction.
- Preconditions: local access; completeness is required for Confirmed state, not for saving Draft work.
- Main flow: (1) Set time/accounts/amounts/Description. (2) Default rates by transaction date in the current device/UI timezone, permit non-base override/restore. (3) Calculate rounded base amounts. (4) Save a valid balanced complete transaction as Confirmed; otherwise save incomplete or unbalanced work as Draft, whether new or previously saved. (5) Calculate balances on demand.
- Alternate/error flows: fewer than two entries or imbalance permit Draft saving with a persistent warning, provided every present entry is valid. Missing accounts or invalid rates reject the save in either state. A previously Confirmed transaction may return to Draft and loses its accounting contribution. Empty amount becomes zero. Duplicate resets time to now; deletion removes contribution. Numeric-limit failures reject the operation with an error and unchanged existing data. Date and numeric validation remain mandatory in either state; Q-09 records the clarified Draft boundary.
- Outcome: active balanced record or excluded draft; account currency remains immutable from its first save.
- Linked requirements: BR-009, BR-011, BR-015–BR-021; FR-005; SC-002.
- Proposed scenarios: Given complete +100/-100, when saved, then active automatically. Given new +100/-99, when saved, then draft excluded from totals. Given 55555.5555 × 0.2222, then 12344.4444; opposite -12344.4444 at 1 balances. Given a Confirmed record, when saving an unbalanced edit, then save as Draft, show a warning and exclude the entire transaction from accounting results. Given zero entries or one valid entry, when saved, then retain a Draft for later repair. Given any larger number of individually valid entries with an unbalanced total, when saved, then retain a Draft; entry count alone imposes no Draft limit. Given an entry without an account or valid rate, when saving in either state, then reject without changing stored data. Given an amount or calculation exceeds supported numeric limits, when the operation is attempted, then reject it, show an error and leave existing data unchanged.

### BC-006 — Reuse transaction templates

- Source: C Templates.
- Trigger: define/apply template or create from transaction.
- Preconditions: local access; group-unique Name and account per entry.
- Main flow: (1) Define accounts/amounts/Description or copy active/draft transaction. (2) Save template. (3) Apply into transaction for review with applicable rates/copied Description. (4) Save under transaction rules.
- Alternate/error flows: empty/unbalanced templates allowed; applying does not save. For empty/single-entry templates, the editor opens with those entries and allows Save as Draft with a warning. User may repair immediately, save for later repair, or cancel without saving. Each present transaction entry must be valid under BR-016 before either Draft or Confirmed saving.
- Outcome: reusable template and optionally reviewed saved transaction.
- Linked requirements: BR-008, BR-009, BR-011, BR-015, BR-022; FR-006; SC-003.
- Proposed scenarios: Given unbalanced draft, when making template, then copy accounts/amounts/Description. Given applied template, then transaction persists only on Save and uses date-applicable rates. Given a single-entry template, when applied and saved, then retain a Draft excluded from accounting results. Given an empty template, then the editor opens empty and permits saving an empty Draft with a visible warning. Given Cancel before saving, then no new transaction is saved.

### BC-007 — Calculate balances and reports

- Source: C Filtering/Grouping/accounting/dates.
- Trigger: request balance or configure report.
- Preconditions: local access; report choices.
- Main flow: (1) Start unchecked. (2) Select classifications/unassigned/overrides. (3) Set dates/grouping. (4) Match accounts using OR within each type, AND across types. (5) Sum active entries, show signed totals with correct currency columns.
- Alternate/error flows: empty type selection gives empty report/warning. Drafts excluded; current classifications apply historically. Partially selected groups show a partial-selection mark (−). Saved explicit choices survive hierarchy moves. Missing identities are ignored; their saved choices apply again if they return.
- Outcome: on-demand selected totals with device-timezone boundaries displayed in a modal popup; users can save generated results to a CSV file.
- Linked requirements: BR-010, BR-019, BR-021, BR-023, BR-024, BR-026, BR-027, BR-028; FR-007; SC-002, SC-003.
- Proposed scenarios: Given no Category selection, then empty with warning. Given A selected, child C excluded, X in C explicitly included, then A outside C plus X. Given mixed currencies/no Currency grouping, then base totals only. Given a group with only some descendants selected, then its checkbox shows a partial-selection mark (−).

### BC-008 — Save and reopen report settings

- Source: C Saved selection/definitions.
- Trigger: organize report groups, or save/reopen, rename, edit, move, favorite or delete a saved report.
- Preconditions: local access; selections/grouping/optional fixed dates.
- Main flow: (1) Offer editable default name in `yyyy-MM-dd HH:mm:ss` format. (2) Save choices with the selected ReportGroup and favorite status. (3) Reopen against current hierarchy. (4) Ignore missing references and apply inheritance/overrides; before calculation, choose a date range in the modal pre-run dialog initialized from the saved range. Clicking Run or pressing Enter updates only the saved date range, closes the date popup, calculates results on demand from current bookkeeping data and opens the fully read-only results popup. (5) Allow renaming and editing saved instructions, or deleting the saved definition without deleting bookkeeping data.
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
- Proposed scenarios: Given Food renamed Groceries locally and Meals on master, when Master priority, then Meals. Given master-deleted account used in offline transaction, then restore dependency despite Master priority. Given neither side changed, then no business transfer but expiry renewed. Given AmountPrecision and RatePrecision were selected at creation, then all synchronized copies use those immutable System values for calculation and display; their Local account-name preferences remain unchanged. Given mobile-only Android, automatic sync does not start, manual may. Given authorization remembered after successful Create/Open, when normal synchronization runs, then no repeated login/password entry is required while cloud access remains authenticated.

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
- Trigger: owner opens Latest result, or the summary opens automatically after a synchronization attempt under BR-043.
- Preconditions: report available in current app session.
- Main flow: (1) Open the latest attempt in a read-only modal popup. (2) Show conflicts/corrections/errors and received created/updated/deleted counts by type. (3) Omit zero values/uploads. (4) Save the displayed summary to a TXT file when the owner chooses Save to file.
- Alternate/error flows: newer attempt replaces report; closure discards it. Critical failures have separate diagnostic logs in a log folder, automatically cleaned up after 7 days. Exact platform-specific paths are technical design details. Full replacement must count business changes rather than every downloaded row as new; calculation is later design.
- Outcome: owner can inspect meaningful latest-attempt results and optionally save their summary to TXT.
- Linked requirements: BR-030, BR-043; FR-012; NFR-005; SC-004.
- Proposed scenarios: Given two categories received/three transactions uploaded, then category received count shown, uploads not counted. Given app closed/reopened, then previous session report not retained. Given a diagnostic log has reached the end of its 7-day retention period, then it is automatically cleaned up.

## Functional Requirements

All capabilities below are first-release decisions from the cited sources; their consolidated wording is Draft, not approved.

| ID | Capability and verifiable outcome | Priority/status | Source | Linked business capabilities/rules |
| --- | --- | --- | --- | --- |
| FR-001 | Select startup mode and initialize bookkeeping with fixed base currency/agreed content. | First release; In Review | F | BC-001; BR-001–BR-004 |
| FR-002 | Open locally offline or authenticate/register/download on new device. | First release; In Review | F/S | BC-002; BR-001–BR-003, BR-005 |
| FR-003 | Maintain valid groups/classifications, including ReportGroup, roots, moves/merges/deletions, naming and favorites filtering. | First release; In Review | C/U | BC-003, BC-008; BR-006–BR-009, BR-011, BR-044 |
| FR-004 | Maintain currencies/rates/accounts with defaults, currency immutability after first save and protected deletion. | First release; In Review | C | BC-004; BR-008–BR-015, BR-017, BR-021 |
| FR-005 | Record/duplicate/edit/delete transactions, enforce balancing/draft rules and calculate balances. | First release; In Review | C | BC-005; BR-009, BR-011, BR-015–BR-021 |
| FR-006 | Maintain/apply templates, create from active/draft transactions without bypassing save rules. | First release; In Review | C | BC-006; BR-008, BR-009, BR-011, BR-015, BR-022 |
| FR-007 | Calculate reports using agreed filters/dates/grouping/currency and active-only totals; show pre-run date selection, display results in a modal popup and allow saving them to CSV. | First release; In Review | C/U | BC-007; BR-010, BR-019, BR-021, BR-023, BR-024, BR-026, BR-027, BR-028 |
| FR-008 | Organize report definitions in ReportGroup trees with shared group actions and favorites; save/reopen, rename, edit, move, favorite and delete report definitions. Calculate on demand against current data/hierarchy with inherited selections and overrides. Deleting a definition does not delete bookkeeping data. | First release; In Review | C/U | BC-003, BC-008; BR-006, BR-007, BR-008, BR-024, BR-025, BR-028, BR-044 |
| FR-009 | Configure/perform synchronization preserving validity and resolving/reporting conflicts. | First release; In Review | S | BC-009; BR-005, BR-011, BR-029–BR-038 |
| FR-010 | Recover uncertain/partly completed sync without duplicate application/premature loss. | First release; In Review | S | BC-010; BR-005, BR-035, BR-036, BR-038–BR-040 |
| FR-011 | Enforce expiry and owner-triggered fresh download, retaining Local configuration. | First release; In Review | S | BC-011; BR-005, BR-032, BR-041, BR-042 |
| FR-012 | Present the latest sync report in a read-only modal popup with TXT summary export, and provide diagnostics for unresolved critical failures. | First release; In Review | S/U | BC-012; BR-030, BR-043 |

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
| Q-03 | Resolved: per-LocalDb `DefaultAccountName`; six field orders; one non-whitespace separator, with empty or whitespace-only textbox input defaulting to / on focus loss or Save. Initial order: Correspondent, Category, Project. Initial separator: `/`. Existing account names do not change automatically after classification renames. | Requesting user, 2026-09-27 | Closed; original order/separator decisions and the currency-checkbox default are settled | BR-012; FR-004 |
| Q-04 | Resolved: use supplied CultureInfo/RegionInfo approach; keep one entry per ISO currency code using the first returned Name and Symbol. Name/Symbol remain editable and restorable from source defaults. | Requesting user, 2026-09-27 | Closed | BR-013; FR-004 |
| Q-05 | Resolved: APr/RPr are creation-time immutable System settings; calculations follow BR-017/018. APr/RPr ranges 0–4 and defaults 2/4 are confirmed. Display uses APr for amounts/BaseAmount and RPr for rates, without an independent display setting. Numeric overflow handling remains rejection without mutation; exact numeric bounds belong in TRD. | Requesting user, 2026-09-28 | Closed at business level; exact numeric bounds remain technical | BR-005/017/018; FR-001/004/005/009 |
| Q-06 | Resolved: partial checkbox for mixed selection; explicit choices survive moves, including redundant ones. Apply saved JSON as-is to the actual tree; ignore missing IDs, apply their saved choices if they return. Only user save recalculates stored included/excluded IDs. | Requesting user, 2026-09-27 | Closed | BR-024, BR-025; FR-007, FR-008 |
| Q-07 | Resolved: allow report rename/edit/delete; delete only calculation instructions. Default name is `yyyy-MM-dd HH:mm:ss`; reject empty/whitespace-only names; duplicates allowed. | Requesting user, 2026-09-27 | Closed | BR-028; FR-008 |
| Q-08 | Resolved: a non-root group may merge into its parent, another ancestor or the root. Root as source and descendant as destination remain forbidden. | Requesting user, 2026-09-27 | Closed | BR-007; FR-003 |
| Q-09 | Resolved 2026-10-01: Drafts relax only the minimum entry count and exact balance requirement. Zero or one entry is allowed, but every present entry requires an existing account, a valid rate and numeric amount (zero allowed). Date and numeric validation still apply. Previously Confirmed transactions may return to Draft. At least two valid entries and exact zero are required for automatic Confirmed state. Drafts can be copied into templates under the existing template rules. | Requesting user | Closed; invalid entries cannot be persisted by selecting Draft | BR-015–BR-022; FR-005, FR-006 |
| Q-10 | Resolved for version one: backup/restore deferred to version two. Azure-side-only backup/restore is the user-proposed direction; detailed policy remains future work. | Requesting user, 2026-09-27 | Closed for version one; version-two design pending | BC-010, BC-011; SC-004 |
| Q-11 | Business policy already settled: whole-version priority is subordinate to validity; restore dependencies, resolve name/currency/rate conflicts under BR-029–BR-033, and explain/log cases with no valid resolution. Combined-case algorithms and initial-rate remapping belong to TRD analysis. Return only genuinely new business-policy conflicts for decision; never silently relax rules. | S Priority/Dependencies/Uniqueness | Closed as generic business blocker; technical analysis remains required | BR-029–BR-033; FR-009 |
| Q-12 | Resolved: write diagnostic logs to a log folder; retain for 7 days, then automatically clean up. Exact platform-specific folder paths belong to technical design. Share/Export logs was proposed but not selected. | Requesting user, 2026-09-27 | Closed at business level | BR-043; FR-012 |
| Q-13 | Resolved: initial setup failure shows an error; no local copy means no bookkeeping access. Retry only manually. If Master creation succeeded but initial download failed, do not create it again; user-initiated retry uses the existing-Master attachment branch under the credentials-first route. Existing expiry replacement uses the previously agreed Download action. Exact error wording is a UI detail. | Requesting user, 2026-09-27; reaffirmed after repeated question | Closed; do not re-ask | BC-001, BC-002, BC-011 |
| Q-14 | Resolved: login and password are required fields only, with no additional length/character/complexity rules in version one. Remember authorization after successful Create/Open for normal sync. Login/password change and recovery deferred to version two. | Requesting user, 2026-09-27 | Closed | BR-003, BR-005; FR-001, FR-002, FR-009 |
| Q-15 | Resolved: reinstalling the app registers a new local copy; the old registration remains subject to the existing 90-day expiry rule. Retry duplicate-prevention mechanics remain later technical design. | Requesting user, 2026-09-27 | Closed for reinstallation behavior | FR-002, FR-010, FR-011 |
| Q-16 | Resolved: manual network actions may use any internet connection; automatic actions require Wi-Fi to start. Initial setup is manual and allows any connection. Existing ordinary offline use and previously agreed continuation after Wi-Fi loss remain unchanged. | Requesting user, 2026-09-27 | Closed | BC-001, BC-002, BC-009–BC-011 |
| Q-17 | Resolved: add ReportGroup as the sixth hierarchy, with shared group actions/rules, report Group picker, favorites for groups/reports and report double-click Run. Search/Next/Previous is also accepted for the existing Category/Project/Correspondent checkbox trees and does not change their checkbox selections. | Requesting user, 2026-10-05 | Closed | BC-003, BC-007, BC-008; BR-006, BR-007, BR-008, BR-024, BR-028, BR-044; FR-003, FR-007, FR-008 |

### Current release preparation

The application is not in production. Current schema changes are prepared by updating initial scripts and initial data and creating fresh development databases. Migrating existing development reports is not an acceptance requirement for this change. This clarification does not authorize deleting databases during requirements discussion.

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
| Reusable reports | SC-003 | BC-008 | BR-006, BR-007, BR-008, BR-024, BR-025, BR-028, BR-044 | FR-008 | None separately specified | C/U |
| Reconcile work | SC-004 | BC-009 | BR-005, BR-011, BR-029–BR-038 | FR-009 | NFR-001–NFR-004 | S |
| Recover sync | SC-004 | BC-010 | BR-005, BR-035, BR-036, BR-038–BR-040 | FR-010 | NFR-002–NFR-004 | S |
| Replace expired copy | SC-004 | BC-011 | BR-005, BR-032, BR-041, BR-042 | FR-011 | NFR-004 | S |
| Inspect sync results | SC-004 | BC-012 | BR-030, BR-043 | FR-012 | NFR-005 | S |

### Legacy identifier continuity

Legacy IDs remain permanent aliases or explicitly superseded references; never reuse for unrelated meanings. New IDs normalize the consolidated template. Discovery preserves historical rules and corrections.

| Legacy ID | Current equivalent/disposition |
| --- | --- |
| BRD-GRP-001 | BR-006; six single-root hierarchies, with Reports added in the current controlled revision |
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
| BRD-TXN-003 | BR-016 minimum two entries for Confirmed transactions; Drafts may have fewer, but every entry must be valid |
| BRD-TXN-004 | BR-015–BR-017 account/currency/amount/rate |
| BRD-TXN-005 | BR-018 rounded base amount |
| BRD-TXN-006 | BR-018 exact zero for active transactions; drafts permitted |
| BRD-TXN-007 | Superseded BR-019: drafts excluded from calculations but may synchronize/create templates |

## Clarification Log

### 2026-10-08 — Business-only increment and account-reference guards

User accepted tracking current account references in open existing transaction/template editors, checking every UI account-delete path before the service call, and removing protection when the editor closes. Persisted-reference checks remain in the service. User then requested a complete Business UI flow using an existing Local database and postponement of all other subdomains, including Reports, Synchronization and Help. Existing decisions are preserved; only actual Business integration gaps require further discussion.


### 2026-10-08 — Report failures

User agreed to all three report failure proposals: invalid saved JSON produces an error without replacing saved settings; calculation failure after successful date saving keeps those dates and returns to the report list with an error; failed CSV export retains the results popup and allows retry.



### 2026-10-08 — Shared report catalog and fresh data

User confirmed ReportGroup and report group/favorite/order conventions match the other five catalogs. Existing-report migration was not accepted: the application is pre-production and will use updated initial scripts/data with databases created from scratch. Technical initialization details are recorded in TRD 0.227.



### 2026-10-08 — Account-name properties and currency validation

Source: requesting user explicitly agreed to conditional CurrencyId validation and selected AccountNameOrder, AccountNameSeparator and AccountNameAddCurrency. The order name is unchanged; the other two names replace DefaultAccountNameSeparator and AddCurrencyToAccountName in current contracts. The saved LocalConfig bool controls the suffix. Empty currency is allowed for name generation with the flag off; flag on requires a valid currency, and Restore default name is disabled until it is chosen. Historical names below remain provenance.



### 2026-10-08 — Configuration flag source correction

The user rejected nested Local/System read objects because an existing function returns one flat POCO. Save includes only editable fields. Account-name generation reads the bool AddCurrencyToAccountName from LocalConfig; this supersedes the earlier bool parameter requirement.



### 2026-10-08 — Four-rule consistency cleanup

Source: requesting user, explicit four answers and Make cleanup. Updated BR-005/009/013/014/016/017/029 and affected acceptance scenarios. Local is the default; only CurrencyRate has Description among currency/rate entities; configured amount/rate precisions apply to calculation and display; Drafts have no separate entry-count limit and every present entry must remain valid. Historical superseded decisions below are retained as history. Review checklists for BRD 0.212/TRD 0.222 are historical and do not constitute a re-review of this revision.



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

### 2026-09-29 — Initial rebalancing account name

- Question: use "Rebalancing" as its initial name?
- User answer: "yes".
- Decision: the base-currency account created in the Account root during initialization has initial Name = Rebalancing.
- BRD 0.48 becomes Draft 0.49. Full-version approval remains outstanding.

### 2026-10-01 — Incomplete Drafts and return to Draft

- User direction: "User can save any transaction (old or new) but in the Draft state. App must show warning ... until he fixes transaction. But he can save transaction anytime".
- Exact clarification: "Drafts may contain zero or one entry, and entries without an account. They remain excluded from balances/reports. Is that what you mean?"
- Accepted answer: "Yes. Of course. Draft states are excluded everywhere. It's only for future fix".
- Source: requesting user, this chat, 2026-10-01.
- Updated BR-016/019/022, BC-005/006, Q-09 and legacy mapping. Drafts represent unfinished work and have no accounting effect; confirmed-to-draft saves are allowed. Historical disabled-Save decisions no longer apply to incomplete transaction drafts.
- Q-09 retains unresolved rate/date validity and incomplete Draft-to-template behavior. Existing draft synchronization is unchanged; no removal from synchronization was requested in this accounting-exclusion discussion.
- BRD 0.49 becomes Draft 0.50. Prior 0.32 approval remains historical and does not cover this revision.
- Downstream: TRD transaction/entry schemas, DTO nullability, state transitions and template-apply rules need alignment before implementation; this turn changes BRD only.

### 2026-10-01 — Every Draft entry must be valid

- Exact question: "may a Draft also save with a missing or nonpositive exchange rate, keeping it for later correction?"
- Accepted correction: "I think no. We can give possibility to save transaction with 0 or 1 entries, unbalanced. Put each entry mist be valid. It must have account, rate, amount (even 0)".
- Source: requesting user, this chat, 2026-10-01.
- Updated BR-016/019/022, BC-005/006 and Q-09. This supersedes the earlier permission to save entries without accounts: Draft relaxes entry count and balance only, not individual entry validity. Existing date, rate and numeric safety rules remain.
- BRD 0.50 becomes Draft 0.51; prior approval remains historical. TRD must align its minimum-entry, state-transition and Draft rules before implementation; entry AccountId and amount/rate need not become nullable for persisted Drafts.

### 2026-10-04 — Main navigation groups and currencies

- Source: the user's proposed left navigation panel, correction placing Accounts and Templates in Catalogs, and explicit "agree" accepting the revised group table and currency-rate placement.
- Recorded the accepted navigation under Main Navigation. Anza screenshots in docs/sources/Anza are visual references, not approval of their other features or accounting behavior.
- BRD 0.51 becomes Draft 0.52. This decision does not imply full-document approval.

### 2026-10-04 — Initial Ledger view and displayed transaction amount

- Source: the user's instruction to start with the newest 300 transactions without account filtering; requested date/time, optional Description and positive displayed amount; balancing-account example; positive-side fallback confirmation; final "correct" accepting the three-case rule.
- Recorded the agreed all-transactions Ledger behavior under Main Navigation. Account-specific Ledger details remain separate discussion.
- BRD 0.52 becomes Draft 0.53. No full-document approval is inferred.

### 2026-10-04 — Open and add transactions in a dialog

- User decision: "dialog. By double click or edit button".
- Accepted follow-up: Add opens the same dialog with date/time set to now and no entries; user answered "correct".
- Recorded these interactions under Ledger — transaction dialog. BRD 0.53 becomes Draft 0.54; no full-document approval is inferred.

### 2026-10-04 — Save, cancel and close the transaction dialog

- Accepted proposal: Save closes the dialog; Cancel discards changes; Cancel or closing the window asks for confirmation when unsaved changes exist. User answered "agree".
- Recorded under Ledger — transaction dialog. Existing validation remains binding; closing after Save requires a successful save.
- BRD 0.54 becomes Draft 0.55; no full-document approval is inferred.

### 2026-10-04 — Single-transaction deletion in the first version

- Source: proposal to delete the selected transaction after confirmation and question about single versus multiple selection; user selected "single selected transaction in the first version".
- Recorded under Ledger — delete a transaction. BRD 0.55 becomes Draft 0.56; no full-document approval is inferred.

### 2026-10-04 — Duplicate and From template buttons

- Accepted proposal: Duplicate opens the selected transaction as a new unsaved copy with date/time set to now. User confirmed the Duplicate button and additionally requested a From template button.
- Recorded both buttons under Ledger — transaction dialog without prescribing the template-selection interaction.
- BRD 0.56 becomes Draft 0.57; no full-document approval is inferred.

### 2026-10-04 — From template selection flow

- Accepted proposal: select a template in a tree dialog, then open the populated transaction dialog for review and saving. User answered "agree".
- Updated the From template rule in place. BRD 0.57 becomes Draft 0.58; no full-document approval is inferred.

### 2026-10-04 — Transaction dialog layout

- Accepted proposal: date/time and Description at the top, followed by Account, Amount, Rate and Base amount columns; Base amount is calculated and read-only. User answered "agree".
- Recorded under Ledger — transaction dialog. BRD 0.58 becomes Draft 0.59; no full-document approval is inferred.

### 2026-10-04 — Inline entry editing and account picker

- User confirmed editing Amount and Rate directly in the entries table and specified a button on the right of the Account field that opens an account-tree dialog.
- Recorded under Ledger — transaction dialog. BRD 0.59 becomes Draft 0.60; no full-document approval is inferred.

### 2026-10-04 — Account-tree selection interaction

- Accepted proposal: choose an account by double-clicking it or selecting it and clicking OK; groups expand/collapse but cannot be chosen as entry accounts. User answered "correct".
- Recorded under Ledger — transaction dialog. BRD 0.60 becomes Draft 0.61; no full-document approval is inferred.

### 2026-10-04 — Add and remove transaction entries

- Accepted proposal: Add entry appends a blank row; Remove entry removes the selected row; these changes remain unsaved until Save. User answered "agree".
- Recorded under Ledger — transaction dialog. BRD 0.61 becomes Draft 0.62; no full-document approval is inferred.

### 2026-10-04 — Difference display and balancing-button placement

- Accepted proposal: show Difference (sum of Base amounts) and the existing Add balancing entry button below the entries table; update Difference as entries change. User answered "agree".
- Recorded under Ledger — transaction dialog. BRD 0.62 becomes Draft 0.63; no full-document approval is inferred.

### 2026-10-04 — Amount field and currency label

- Accepted proposal: editable numeric Amount with a separate read-only currency label beside it, rather than inserting the code into the field on focus loss. The label stays visible during typing, adds no tab stop and updates when the account changes. User answered "agree".
- Recorded under Ledger — transaction dialog. BRD 0.63 becomes Draft 0.64; no full-document approval is inferred.

### 2026-10-04 — Restore rate button placement

- Accepted proposal: place Restore rate beside each Rate field to reload the applicable catalog rate for the transaction date. User answered "Ok".
- Recorded under Ledger — transaction dialog. BRD 0.64 becomes Draft 0.65; no full-document approval is inferred.

### 2026-10-04 — Entry account change and rate preservation

- User confirmed retaining Amount when changing an entry account and specified that Rate is retained when the old and new account currencies match; otherwise load the default rate for the new currency.
- Recorded under Ledger — transaction dialog, preserving the existing transaction-date rate lookup and calculated Base amount rules.
- BRD 0.65 becomes Draft 0.66; no full-document approval is inferred.

### 2026-10-04 — Draft badge and dialog warning

- Accepted proposal: a Draft badge beside date/time in the Ledger, a persistent explanatory Draft warning in the transaction dialog, and no manual status selector. User answered "agree".
- Recorded under Ledger — Draft visibility. BRD 0.66 becomes Draft 0.67; no full-document approval is inferred.

### 2026-10-04 — Single-line transaction Description

- User rejected the proposed multiline textbox and selected a single-line Description textbox in the edit dialog, matching its single-line presentation in the Ledger.
- Recorded under Ledger — transaction dialog. No character limit was specified. BRD 0.67 becomes Draft 0.68; no full-document approval is inferred.

### 2026-10-04 — Ledger Description overflow

- Accepted proposal: show an ellipsis when Description exceeds the Ledger column width, with full text available on hover. User answered "correct".
- Recorded alongside the single-line Description rule. BRD 0.68 becomes Draft 0.69; no full-document approval is inferred.

### 2026-10-04 — Ledger account selector and selection-control convention

- User accepted the account selector above the Ledger, initially All accounts, with an account-tree picker button and Clear to return to all accounts.
- User specified modal popups for complex selection structures such as account trees; combo boxes are reserved for simple, small, flat lists. No numeric size threshold was supplied.
- Recorded under Main Navigation. BRD 0.69 becomes Draft 0.70; no full-document approval is inferred.

### 2026-10-04 — Account-filtered Ledger cumulative amount

- User confirmed that repeated uses of an account remain separate transaction entries, potentially with different rates. The account-filtered transaction list displays the cumulative amount from the last entry for the selected account.
- User specified accounting order: transaction date/time, transaction GUID, entry order. Recorded this under Ledger — account selector; no entry combination is introduced.
- BRD 0.70 becomes Draft 0.71; no full-document approval is inferred.

### 2026-10-04 — Account-filtered Ledger amount and columns

- User accepted the signed sum of the selected account's entry amounts in its own currency and the Date/time, Description, Amount, Cumulative amount columns, specifying Currency as a separate column at the end.
- Recorded under Ledger — account selector. Separate transaction entries and the last-matching-entry cumulative amount remain unchanged.
- BRD 0.71 becomes Draft 0.72; no full-document approval is inferred.

### 2026-10-04 — Currency visible in every Ledger row

- User confirmed that the unfiltered transaction list shows amounts in base currency, the account-filtered list shows amounts in account currency, and currency is visible in every row in both modes.
- Updated the all-transactions columns to include Currency last, consistent with the already accepted final Currency column in account-filtered mode.
- BRD 0.72 becomes Draft 0.73; no full-document approval is inferred.

### 2026-10-04 — Add from the account-filtered Ledger

- User confirmed that Add from an account-filtered Ledger opens a new transaction with its first entry using the filtered account.
- Updated the Add rule in place: no initial entries in the all-transactions view; one preselected-account entry in account-filtered mode. Date/time still defaults to now.
- BRD 0.73 becomes Draft 0.74; no full-document approval is inferred.

### 2026-10-04 — Accounts catalog double-click behavior

- User rejected double-click navigation from an account to its Ledger. Double-click a group to edit only its name in a modal popup; double-click an account to edit it in a modal popup. A single click performs no opening/navigation action.
- Recorded under Accounts catalog — tree interaction, separate from account-picker behavior and subject to existing root protection.
- BRD 0.74 becomes Draft 0.75; no full-document approval is inferred.

### 2026-10-04 — Accounts tree selection and action availability

- User clarified that single click changes selection: the tree permits one selected group or account. Buttons and menus must enable/disable according to the selected type, with account-specific actions disabled for groups and group-specific actions disabled for accounts.
- User example: Merge 2 groups is enabled for a group selection and disabled for an account selection, subject to the existing root-group restrictions.
- Updated Accounts catalog — tree interaction in place. BRD 0.75 becomes Draft 0.76; no full-document approval is inferred.

### 2026-10-04 — Group merge flow and destination selection

- User accepted selecting the source in the catalog, choosing the destination in a modal tree popup and showing a warning before merging.
- User specified that after closing, the destination must be selected and the tree scrolled so the destination group is visible. Recorded this as the post-success merge state.
- Updated Accounts catalog — tree interaction. BRD 0.76 becomes Draft 0.77; no full-document approval is inferred.

### 2026-10-04 — Move and drag-and-drop catalog actions

- User accepted Move through a destination-group modal picker, followed by selecting and revealing the moved item.
- User specified group-to-group drag as move, element-to-group drag as move, and Shift-drag of a group onto another group as merge. The existing merge confirmation and destination-selection behavior apply.
- Recorded under Catalog trees — move and drag-and-drop. Other possible gestures remain unspecified. BRD 0.77 becomes Draft 0.78; no full-document approval is inferred.

### 2026-10-04 — Expand groups during drag hover

- Accepted proposal: expand a collapsed group when the user holds a dragged item over it, making nested destinations reachable. User answered "agree".
- Recorded under Catalog trees — move and drag-and-drop. BRD 0.78 becomes Draft 0.79; no full-document approval is inferred.

### 2026-10-04 — Add groups and accounts under the selected group

- Accepted proposal: Add group / Add account place the new item inside the selected group, then select and reveal it after Save. User answered "Agree".
- Recorded under Accounts catalog — tree interaction. BRD 0.79 becomes Draft 0.80; no full-document approval is inferred.

### 2026-10-04 — Selection after deleting a group or account

- Accepted proposal: select the parent group after deleting a group or account. User answered "yes".
- Recorded under Accounts catalog — tree interaction. BRD 0.80 becomes Draft 0.81; no full-document approval is inferred.

### 2026-10-04 — Shared catalog interactions and five dialog types

- User confirmed shared tree behavior across Correspondents, Categories, Projects and Templates and defined Edit-select, Select only groups, Select only all, Edit element and Edit group.
- Recorded uniqueness per catalog/window type, inclusion of the docked catalog, mode-dependent double-click behavior, conditional selection routing and both supplied flows under Catalog windows and selection modes.
- The proposed already-open select-only scenario was rejected as unreachable with blocked modal parents and non-editing leaf pickers. Earlier broad select-only nesting proposals are superseded.
- User explicitly requested saving the discussion in BRD/TRD. BRD 0.81 becomes Draft 0.82; full-document approval is not inferred.

### 2026-10-04 — Independent catalog activity inside pickers

- User fully accepted that catalog changes saved from a selection popup remain saved when the calling transaction is canceled, explaining that editing there is for convenience but is a separate activity.
- Recorded independent catalog persistence under Catalog windows and selection modes; TRD captures the corresponding operation boundary.
- BRD 0.82 becomes Draft 0.83; no full-document approval is inferred.

### 2026-10-04 — Account deletion and open transaction/template editors

- User confirmed that used entities cannot be deleted, specified the same account-deletion protection for editing transactions/templates, and chose entry removal for new transactions.
- The preceding user instruction also specifies clearing deleted optional selections without warning. Recorded both decisions under Catalog changes affecting an open editor.
- New templates and the remaining mandatory-reference/merge cases remain open. BRD 0.83 becomes Draft 0.84; no full-document approval is inferred.

### 2026-10-04 — Account deletion in a new template

- Question: For a new template, use the same entry-removal rule as a new transaction? User answered "yes".
- Updated Catalog changes affecting an open editor: a new unsaved template removes entries referencing an account deleted through catalog activity, subject to existing deletion restrictions. Related capability BC-006 and requirement FR-006.
- BRD 0.84 becomes Draft 0.85; no full-document approval is inferred.

### 2026-10-04 — Deleted currency in a new account editor

- Question: if the selected currency of a new unsaved account is deleted, clear Currency and require another selection before Save? User answered yes and specified choosing another currency or canceling the dialog.
- Updated Catalog changes affecting an open editor. Existing account currency immutability and saved-reference deletion protection remain unchanged. Related capability BC-004 and requirement FR-004.
- BRD 0.85 becomes Draft 0.86; no full-document approval is inferred.

### 2026-10-04 — Topmost modal interaction and parent-group protection

- User confirmed that only the top popup in the modal stack can be used or closed. Lower windows remain blocked.
- Accepted conclusion: the parent-group chooser is select-only in the confirmed catalog editor flows, so the selected group cannot be deleted through that interaction and no automatic root fallback is needed.
- Updated Catalog changes affecting an open editor; related capabilities BC-003/004/006 and requirements FR-003/004/006. BRD 0.86 becomes Draft 0.87; no full-document approval is inferred.

### 2026-10-04 — Element combination restricted to the main window

- User rejected automatic replacement of combined accounts in open editors and chose to make combining accounts, categories and similar elements available only from the main window, unavailable in popups.
- Recorded under Catalog changes affecting an open editor. Existing topmost-only modal interaction prevents initiating these operations while another editor is open. Group Merge scope remains a separate clarification.
- Related capabilities BC-003/004 and requirements FR-003/004. BRD 0.87 becomes Draft 0.88; no full-document approval is inferred.

### 2026-10-04 — Group merging remains available in Edit-select popups

- Question: does main-window-only combination also apply to group Merge, including Shift-drag in popups? User answered no: element combination substitutes entities across the dataset, whereas group merging moves contents to another group and is allowed from popups.
- Updated Catalog changes affecting an open editor to retain group Merge in Edit-select popups, with existing confirmation and merge rules. Select-only dialogs remain non-modifying.
- Related capability BC-003 and requirement FR-003. BRD 0.88 becomes Draft 0.89; no full-document approval is inferred.

### 2026-10-04 — Refresh renamed or moved references after picker cancellation

- Accepted proposal: after renaming or moving the currently selected item and canceling the picker, preserve its identity in the calling field and refresh the displayed name/path; saved changes are not undone. User answered "agree".
- Updated Catalog changes affecting an open editor; related capabilities BC-003–BC-006 and requirements FR-003–FR-006.
- BRD 0.89 becomes Draft 0.90; no full-document approval is inferred.

### 2026-10-04 — Reveal the current selection when opening a picker

- Accepted proposal: when opening a picker for a populated field, select its current item and scroll it into view, expanding parent groups as needed. User answered "correct".
- Recorded under Selection controls; related capabilities BC-003–BC-006 and requirements FR-003–FR-006.
- BRD 0.90 becomes Draft 0.91; no full-document approval is inferred.

### 2026-10-04 — Clear optional reference fields

- Accepted proposal: optional reference fields have a Clear (×) button beside the picker button; it clears the field without opening the picker. Mandatory fields have no Clear button. User answered "agree".
- Recorded under Selection controls; related capabilities BC-003–BC-006 and requirements FR-003–FR-006.
- BRD 0.91 becomes Draft 0.92; no full-document approval is inferred.

### 2026-10-04 — Favorites only toggle in tree pickers

- Question: should each tree picker have a Favorites only toggle, retaining ancestor groups as navigation paths under the existing rule? User answered "yes".
- Recorded under Selection controls, linked to BR-044 and capabilities BC-003/004/006. Picker-specific selection and modification restrictions remain unchanged.
- BRD 0.92 becomes Draft 0.93; no full-document approval is inferred.

### 2026-10-04 — Picker favorites toggle and selection restoration

- Question: if Favorites only would hide the field's current item, should opening the picker turn the filter off so the item is selected and visible?
- User decision: open the whole set for selection each time; when enabling favorites hides the selection, use the first possible selection; when disabling favorites, restore the whole set with the previous selection.
- Updated Selection controls, linked to BR-044 and capabilities BC-003/004/006. Whole-tree selection is remembered across each filter-on/filter-off transition; caller eligibility remains binding.
- BRD 0.93 becomes Draft 0.94; no full-document approval is inferred.

### 2026-10-04 — Name search in catalog trees and pickers

- Question: provide a search textbox matching names case-insensitively, retaining ancestor paths, and searching within the favorites-filtered set when Favorites only is on?
- User answered "argee", accepting the proposal.
- Recorded under Selection controls, linked to BR-044 and capabilities BC-003/004/006.
- BRD 0.94 becomes Draft 0.95; no full-document approval is inferred.

### 2026-10-04 — Search navigates the tree using Next and Previous

- User replaced search filtering with a search textbox and Next/Previous buttons, searching groups and elements across the whole set and navigating within the tree. Matching-only tree filtering was explicitly rejected.
- Updated Selection controls in place; the earlier favorites-limited search proposal is superseded. Handling a match hidden by Favorites only remains open.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006. BRD 0.95 becomes Draft 0.96; no full-document approval is inferred.

### 2026-10-04 — Disable search while Favorites only is on

- Question: if a search match is hidden by Favorites only, should search turn Favorites off to reveal it?
- User decision: disable the search feature when Favorites is switched on.
- Updated Selection controls: disable the search textbox and Next/Previous navigation while Favorites only is active; re-enable them when it is off. This resolves the hidden-search-match question without changing favorite flags or the existing selection-restoration rule.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006; BR-044. BRD 0.96 becomes Draft 0.97; no full-document approval is inferred.

### 2026-10-04 — Wrap search navigation

- Question: when search reaches the last match, should Next wrap to the first match, and Previous wrap from first to last?
- User answered "yes".
- Updated Selection controls, linked to capabilities BC-003/004/006 and requirements FR-003/004/006.
- BRD 0.97 becomes Draft 0.98; no full-document approval is inferred.

### 2026-10-04 — Search matches any part of a name

- Question: should search match any part of a name, for example bank finding both Bank account and My bank savings?
- User answered "correct".
- Updated Selection controls in place; existing case-insensitive matching remains applicable. Related capabilities BC-003/004/006 and requirements FR-003/004/006.
- BRD 0.98 becomes Draft 0.99; no full-document approval is inferred.

### 2026-10-04 — Search with no matches

- Question: if search finds no matches, keep the current selection and show No matches beside the search box?
- User answered "argee", accepting the proposal.
- Updated Selection controls; related capabilities BC-003/004/006 and requirements FR-003/004/006.
- BRD 0.99 becomes Draft 0.100; no full-document approval is inferred.

### 2026-10-04 — Shared Save and Cancel behavior for catalog editors

- Question: apply the transaction dialog Save/Cancel rules to all group and element editors, including closing after successful Save and confirmation before discarding unsaved changes?
- User answered "correct".
- Updated Catalog windows and selection modes; related capabilities BC-003/004/006 and requirements FR-003/004/006. Independently saved nested catalog activity remains separate.
- BRD 0.100 becomes Draft 0.101; no full-document approval is inferred.

### 2026-10-04 — Field and general save error presentation

- Question: show field validation errors beside the affected field and general save errors in a banner inside the dialog, keeping entered data intact?
- User answered "agree".
- Recorded under Editor error presentation; related capabilities BC-003–BC-006 and requirements FR-003–FR-006.
- BRD 0.101 becomes Draft 0.102; no full-document approval is inferred.

### 2026-10-04 — Confirm catalog deletion by name

- Question: should deleting a catalog group or element ask for confirmation showing its name, in both the main window and Edit-select popups?
- User answered "agree".
- Recorded in the shared catalog tree behavior; related capabilities BC-003/004/006 and requirements FR-003/004/006. Existing deletion restrictions remain applicable.
- BRD 0.102 becomes Draft 0.103; no full-document approval is inferred.

### 2026-10-04 — Tree context menu and right-click selection

- Question: should right-clicking a tree item select it and open a context menu with the same actions and enabled/disabled rules as the toolbar?
- User answered "correct".
- Recorded in the shared catalog tree interaction rules; related capabilities BC-003/004/006 and requirements FR-003/004/006.
- BRD 0.103 becomes Draft 0.104; no full-document approval is inferred.

### 2026-10-04 — Preserve main-window screen state during the session

- Question: when switching between main-window screens, preserve each screen selection, scroll position, search text and filters during the session, with modal pickers retaining their separate opening rules?
- User answered "correct".
- Recorded under Main-window screen state. This decision establishes session retention only, not persistence after application restart.
- BRD 0.104 becomes Draft 0.105; no full-document approval is inferred.

### 2026-10-04 — Ledger is the initial screen

- Question: after opening the books, should Ledger be the initial screen, showing the newest 300 transactions across all accounts?
- User answered "correct".
- Updated Ledger — all-transactions view in place; related capability BC-005 and requirement FR-005.
- BRD 0.105 becomes Draft 0.106; no full-document approval is inferred.

### 2026-10-04 — Ledger date navigation control layout

- Accepted proposal: a date picker, Today button, Previous/Next buttons and a step combo with Week / Month / Quarter / Year, above the Ledger list. User answered "Agree".
- Recorded under Ledger — date navigation controls. The user separately asked about WinUI date and time components; transaction time-entry precision is not established by this layout decision.
- BRD 0.106 becomes Draft 0.107; no full-document approval is inferred.

### 2026-10-04 — Transaction date and time with seconds

- User accepted the date/time component discussion and specified 24-hour transaction time with seconds, explaining that multiple transactions can occur within the same minute.
- Recorded separate calendar date and time fields, with HH:mm:ss transaction time, under Ledger — transaction dialog. The standard WinUI TimePicker does not provide seconds editing; the implementation must provide seconds-capable time input.
- Related capability BC-005 and requirement FR-005. BRD 0.107 becomes Draft 0.108; no full-document approval is inferred.

### 2026-10-04 — Reload Ledger when date or account filter changes

- Question: should changing the Ledger date or account filter reload the list immediately, without an Apply button?
- User answered "yes".
- Recorded under Ledger — date navigation controls, retaining existing query limits and post-edit refresh behavior. Related capability BC-005 and requirement FR-005.
- BRD 0.108 becomes Draft 0.109; no full-document approval is inferred.

### 2026-10-04 — Ledger transaction-limit notice

- Question: when more than 300 transactions match, show a notice above Ledger: Showing the newest 300 transactions. Choose an earlier date to view older transactions?
- User answered "agree".
- Recorded under Ledger — date navigation controls, tied to the existing query limit indication. Related capability BC-005 and requirement FR-005.
- BRD 0.109 becomes Draft 0.110; no full-document approval is inferred.

### 2026-10-04 — Currency list and exchange-rate table layout

- Question: place the currency list on the left and the selected currency exchange-rate table on the right within the main workspace?
- User answered "agree".
- Recorded under Currencies — layout; related capability BC-004 and requirement FR-004.
- BRD 0.110 becomes Draft 0.111; no full-document approval is inferred.

### 2026-10-04 — Exchange-rate columns and initial-rate label

- Question: use Date, Rate and Description columns and display the special initial rate as Initial in the Date column, hiding its internal date?
- User answered "agree".
- Updated Currencies — layout; related capability BC-004 and requirement FR-004, under BR-014.
- BRD 0.111 becomes Draft 0.112; no full-document approval is inferred.

### 2026-10-04 — Modal exchange-rate Add and Edit

- Question: should double-clicking a rate or clicking Edit open a modal rate editor, with Add opening the same editor for a new rate, under existing date, precision and base-currency restrictions?
- User answered "agree".
- Updated Currencies — layout; related capability BC-004 and requirement FR-004, under BR-014/017.
- BRD 0.112 becomes Draft 0.113; no full-document approval is inferred.

### 2026-10-04 — Exchange-rate display order

- Question: show ordinary exchange rates newest first, with the Initial rate fixed at the bottom?
- User answered "agree".
- Updated Currencies — layout; related capability BC-004 and requirement FR-004, under BR-014.
- BRD 0.113 becomes Draft 0.114; no full-document approval is inferred.

### 2026-10-04 — Currency-list columns

- Question: use Code, Name, Symbol and Favorite (star) columns in the currency list?
- User answered "yes".
- Updated Currencies — layout; related capability BC-004 and requirement FR-004, under BR-013/044.
- BRD 0.114 becomes Draft 0.115; no full-document approval is inferred.

### 2026-10-04 — Delete rates date-range dialog without initial-rate warning

- Proposal: a modal Delete rates dialog with From/To dates, the selected currency and a warning that Initial remains untouched.
- User accepted the range dialog but rejected the warning, specifying that the Initial rate is simply ignored by deletion.
- Recorded under Currencies — layout; related capability BC-004 and requirement FR-004, under BR-014. Initial-rate deletion remains prohibited.
- BRD 0.115 becomes Draft 0.116; no full-document approval is inferred.

### 2026-10-04 — Currency editor fields and Restore defaults

- Question: should currency editing open a modal dialog with Code read-only, editable Name/Symbol, a Favorite toggle and Restore defaults for Name/Symbol?
- User answered "yes".
- Recorded under Currencies — layout; related capability BC-004 and requirement FR-004, under BR-013/044.
- BRD 0.116 becomes Draft 0.117; no full-document approval is inferred.

### 2026-10-04 — Add currency editor and initial rate

- Question: should Add currency open a modal editor with a system-currency picker, fill editable Name/Symbol defaults after selection, and require the initial rate before Save?
- User answered "agree".
- Recorded under Currencies — layout; related capability BC-004 and requirement FR-004, under BR-013/014/017.
- BRD 0.117 becomes Draft 0.118; no full-document approval is inferred.

### 2026-10-04 — Hide existing codes in the Add currency picker

- Question: should the Add currency picker hide currency codes already present in the books, showing only currencies available to add?
- User answered "yes".
- Updated Currencies — layout; related capability BC-004 and requirement FR-004, under BR-013.
- BRD 0.118 becomes Draft 0.119; no full-document approval is inferred.

### 2026-10-04 — No search in currency picker dialogs

- Proposal: use Next/Previous search matching Code or Name in the system-currency picker.
- User rejected search in the currency picker dialog, explaining that a home user will not have many currencies.
- Recorded the no-search rule under Currencies — layout; related capability BC-004 and requirement FR-004. The rejected Code-or-Name search proposal is not an active requirement.
- BRD 0.119 becomes Draft 0.120; no full-document approval is inferred.

### 2026-10-04 — New ordinary exchange-rate date defaults to today

- Question: when adding an ordinary exchange rate, should Date default to today and remain editable?
- User answered yes and explicitly confirmed today as the default value.
- Updated Currencies — layout; related capability BC-004 and requirement FR-004, under BR-014.
- BRD 0.120 becomes Draft 0.121; no full-document approval is inferred.

### 2026-10-04 — Windows regional date and numeric formats

- User reaffirmed 24-hour HH:mm:ss and configured amount/rate precision.
- Follow-up question: should date format and decimal/thousands separators follow Windows regional settings? User answered "yes".
- Recorded under Windows display and input formats, retaining the already confirmed clock and precision rules. Related capabilities BC-004/005/006 and requirements FR-004/005/006; BR-017.
- BRD 0.121 becomes Draft 0.122; no full-document approval is inferred.

### 2026-10-04 — Accounts screen is management only

- Proposal: show account-currency and base-currency balances on the Accounts screen with a selected-date control.
- User rejected it: Accounts is only for management; users view an account balance in the transaction screen filtered by account.
- Recorded under Accounts catalog — tree interaction, linked to BC-004/005 and FR-004/005. Earlier technical notes assigning balance display to Accounts need downstream UI-purpose alignment; existing balance calculation behavior is unaffected.
- BRD 0.122 becomes Draft 0.123; no full-document approval is inferred.

### 2026-10-04 — Restore default account name button placement

- Question: place Restore default name beside the account Name field, using the chosen classifications and existing naming settings?
- User answered "yes".
- Recorded under Accounts catalog — tree interaction; related capability BC-004 and requirement FR-004, under BR-012.
- BRD 0.123 becomes Draft 0.124; no full-document approval is inferred.

### 2026-10-04 — New-account default name only on explicit Restore or empty-name Save

- Proposal: automatically refresh a new account's generated name on classification changes until manually edited.
- User replaced that proposal: use a manually entered name; fill a blank new-account name with the default when closing; Restore fills from the current category/correspondent/project, and later classification changes do not change Name.
- Recorded under Accounts catalog — tree interaction. Closing with Save uses the default if Name is empty; previously confirmed Cancel semantics still discard unsaved account changes. Automatic name refresh on classification change was rejected.
- Related capability BC-004 and requirement FR-004, under BR-008/012. BRD 0.124 becomes Draft 0.125; no full-document approval is inferred.

### 2026-10-04 — Template entries and account selection

- After clarification, user accepted Account, Amount and read-only account-derived Currency columns in the template editor.
- User accepted an account-selection button opening the account-tree popup with double-click/OK selection, plus Add entry and Remove entry for unsaved template rows.
- Recorded under Templates — entry editing; related capability BC-006 and requirement FR-006, under BR-022.
- BRD 0.125 becomes Draft 0.126; no full-document approval is inferred.

### 2026-10-04 — Create template from the selected Ledger transaction

- Question: add Create template for the selected Ledger transaction, opening a new template editor with copied accounts, amounts and Description, with Name and Group chosen before Save?
- User answered "correct".
- Recorded under Ledger — transaction dialog, linked to BC-005/006 and FR-005/006, under BR-022. This action opens a template editor directly from Ledger; its group picker must still obey catalog-window uniqueness.
- BRD 0.126 becomes Draft 0.127; no full-document approval is inferred.

### 2026-10-04 — Select-only fallback while a catalog editor is active

- Question: at Ledger → Create template → Choose group, open Select only groups because the template editor is already open, even without a Templates Edit-select window?
- Accepted answer: "Got it. Agree. And we have this issue for all elements. Category, Correspondent, Project, Account, Template I agree with proposed fix".
- Source: requesting user, this conversation. Updated the active catalog routing and parent-group rules and added the direct-editor flow. The same-catalog editor guard prevents a picker from requesting a duplicate editor; different catalogs retain their independent routing.
- Related capabilities BC-003–BC-006 and requirements FR-003–FR-006. TRD routing requires later alignment; no second artifact or implementation is changed by this clarification.
- BRD 0.127 becomes Draft 0.128; no full-document approval is inferred.

### 2026-10-05 — Template editor header

- Question: above the template entries table, show Name, Group with picker button, optional single-line Description and a Favorite checkbox?
- Accepted answer: "Agree." The user then returned to catalog-picker routing for further brainstorming; that new proposal is unresolved and is not applied by this revision.
- Source: requesting user, this conversation. Recorded the header under Templates — entry editing; related capability BC-006 and requirement FR-006, under BR-006/008/009/022/044.
- BRD 0.128 becomes Draft 0.129; no full-document approval is inferred.

### 2026-10-05 — Main-window-only group merging

- Question: restricting combinations to the main tree must also disable popup Shift-drag merging and context-menu commands?
- Accepted answer: "correct. We allow this only in the main windows."
- Source: requesting user, this conversation. Updated the active Merge availability, Shift-drag and Edit-select modification rules. This supersedes the earlier permission to merge groups inside Edit-select popups; ordinary permitted moves remain available there.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-007. TRD popup-merge scope requires later alignment.

### 2026-10-05 — Favorites in groups-only trees

- Question: a groups-only tree needs a clear rule: group favorites alone, or also paths to favorite elements that are hidden?
- Accepted answer: "Only favorite groups".
- Source: requesting user, this conversation. Added the groups-only filter rule under Selection controls. Existing necessary ancestor navigation paths remain under BR-044; hidden element favorites do not affect this filter.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-044. The revised picker-routing proposal remains open for clarification.
- BRD 0.129 becomes Draft 0.130; no full-document approval is inferred.

### 2026-10-05 — Docked catalog counts for nested group selection

- Question: Main Accounts tree → Add account → Choose group: open Edit-select groups with group management, or Select only groups because the main Accounts tree already exists?
- Accepted answer: "Select only groups".
- Source: requesting user, this conversation. Reaffirmed Confirmed flow 2 and clarified the active routing rule: the docked catalog counts even while blocked by a modal editor. Choosing the account group in this flow opens the non-modifying groups-only picker.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006. This resolves the docked-tree condition in the revised picker discussion; other proposed changes and remaining count/editor details are not approved by this answer.
- BRD 0.130 becomes Draft 0.131; no full-document approval is inferred.

### 2026-10-05 — Direct hidden-element counts in groups-only trees

- Question: for Store (4), count only elements directly inside Store, with each subgroup showing its own count, rather than including all subgroup elements?
- Accepted answer: "agree". This completes the earlier proposal to display hidden-element counts such as Store (4).
- Source: requesting user, this conversation. Added the count display and scope under Selection controls. Actual group-content deletion restrictions remain under BR-007.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-006/007. Revised direct-editor picker routing remains a separate clarification.
- BRD 0.131 becomes Draft 0.132; no full-document approval is inferred.

### 2026-10-05 — Groups-only Edit-select from a direct element editor

- Question: Ledger → Create template → Choose group, with no Templates tree already open: open Edit-select groups, allowing Add/Edit/Delete/Move groups, with element commands and Merge unavailable?
- Accepted answer: "Correct".
- Source: requesting user, this conversation, following the revised account/group picker scenarios and the confirmed docked-tree rule. Updated the active Edit-select presentations, group-selection routing and direct-template flow. A same-catalog element editor alone permits the groups-only editing picker; an existing same-catalog tree or group editor requires Select only groups.
- Related capabilities BC-003–BC-006 and requirements FR-003–FR-006. This supersedes the earlier element-editor guard for group selection. Select only all removal and group-editor fields remain separate open decisions; TRD routing requires later alignment.

### 2026-10-05 — Deleted group in a new unsaved account

- Question: new unsaved account selects Group G, its groups-only Edit-select picker deletes the eligible empty G, and the picker is canceled: clear Group without warning and require another group before Save?
- Accepted answer: "user cannot close new, unsaved account without choosing another group. Agree with you proposition".
- Source: requesting user, this conversation. Recorded the accepted clear-and-require-valid-group-before-Save rule under Catalog changes affecting an open editor, replacing the superseded claim that group deletion is impossible in every element-editor picker. The earlier shared Cancel/discard behavior is retained; no automatic root fallback is introduced.
- Related capabilities BC-003/004 and requirements FR-003/004, under BR-006/007. Applying this clear-field behavior to other new catalog element editors remains a separate clarification.
- BRD 0.132 becomes Draft 0.133; no full-document approval is inferred.

### 2026-10-05 — Deleted group in any new unsaved catalog element

- Question: apply the deleted-group rule to all new unsaved elements — Accounts, Categories, Correspondents, Projects and Templates: clear Group without warning, require a valid group before Save, and allow Cancel to discard the unsaved element?
- Accepted answer: "correct".
- Source: requesting user, this conversation. Expanded the existing mandatory-group rule under Catalog changes affecting an open editor to all five grouped catalogs and explicitly retained Cancel/discard when Group is missing. Existing group-content deletion restrictions remain unchanged.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-006/007. TRD editor-reference behavior requires later alignment.
- BRD 0.133 becomes Draft 0.134; no full-document approval is inferred.

### 2026-10-05 — Group editor parent selection and defaults

- Question: group editor has Name and Parent group with a picker; Add defaults to the selected group, Edit to its existing parent, the picker uses Select only groups, and changing parent moves the group only on Save while Cancel leaves it unchanged?
- Accepted answer: "Agree. Default group is group where user make selection otherwise root group".
- Source: requesting user, this conversation. Updated the active group editor and creation defaults, replacing the earlier Name-only editor. New group/account creation uses the originating selection's group context, falling back to the catalog root when none is available. Existing group edits start with the actual parent; parent changes remain unsaved until successful Save.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-006/007/008. Root protection, cycle rejection, naming validation and shared Save/Cancel rules remain applicable. TRD editor/routing requirements require later alignment.
- BRD 0.134 becomes Draft 0.135; no full-document approval is inferred.

### 2026-10-05 — Remove the groups-and-elements select-only mode

- Question: remove Select only all because no current flow needs it; element choices use Edit-select groups+elements and nested group choices use Select only groups?
- Accepted answer: "agree".
- Source: requesting user, this conversation. Removed the legacy Select only all type and fallback from active requirements. The five current catalog types are Edit-select groups+elements, Edit-select groups, Select only groups, Edit element and Edit group. Historical clarification entries remain as provenance.
- Related capabilities BC-003–BC-006 and requirements FR-003–FR-006. Existing same-catalog tree routing, dialog uniqueness, topmost-only modal behavior and caller eligibility remain applicable. TRD dialog types/routing require later alignment.
- BRD 0.135 becomes Draft 0.136; no full-document approval is inferred.

### 2026-10-05 — Reveal a newly added item hidden by Favorites only

- Question: Favorites only is on and the user saves a new non-favorite item, which would be hidden despite the post-Add select-and-reveal rule; turn Favorites only off after Save, then select and reveal the new item?
- Accepted answer: "agree".
- Source: requesting user, this conversation. Added the post-Add filter rule under Selection controls for catalog groups/elements. In this automatic filter-off case, select the new item rather than restoring the earlier whole-tree selection; ordinary manual toggle restoration remains unchanged.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-044 and shared post-Add behavior. Opening/saving a nested editor still does not automatically accept the picker choice. TRD tree-refresh behavior requires later alignment.
- BRD 0.136 becomes Draft 0.137; no full-document approval is inferred.

### 2026-10-05 — Keep a saved edit visible after clearing Favorite

- Question: Favorites only is on → edit a favorite item → clear Favorite → Save: turn the filter off and keep the edited item selected and visible?
- Accepted answer: "agree".
- Source: requesting user, this conversation. Added the saved-edit filter rule under Selection controls for catalog groups/elements in docked trees and Edit-select pickers. Retaining the edited item takes precedence over earlier remembered-selection restoration in this automatic filter-off case; ordinary manual filter toggling remains unchanged.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-044 and shared Save/Cancel behavior. Saving a nested edit still does not automatically accept a picker choice. TRD tree-refresh behavior requires later alignment.
- BRD 0.137 becomes Draft 0.138; no full-document approval is inferred.

### 2026-10-05 — Favorite checkbox in every group and element editor

- Question: add a Favorite checkbox to every catalog group and element editor, for both Add and Edit, with changes taking effect on Save?
- Accepted answer: "yes".
- Source: requesting user, this conversation. Added the shared control rule for all five grouped catalogs and updated the active group/element editor descriptions. Existing-value initialization, false new-item default, root protection and Cancel/discard follow the established BR-044 and editor rules.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-044. Saved favorite changes use the already agreed filter/selection behavior; the separate currency editor retains its existing favorite control. TRD editor requirements require later alignment.
- BRD 0.138 becomes Draft 0.139; no full-document approval is inferred.

### 2026-10-05 — Currency column in account trees

- Question: show a separate Currency column for account rows in the main tree and account pickers, for example Cash | USD and Cash | EUR, to help when account names repeat?
- Accepted answer: "agree."
- Source: requesting user, this conversation. Added the read-only account-currency column under Accounts catalog — tree interaction. Group rows have no currency value; the column applies to account pickers that display account elements.
- Related capabilities BC-004/005/006 and requirements FR-004/005/006, under BR-008/010/011. Existing account naming and currency immutability rules remain unchanged.

### 2026-10-05 — Toggle favorites directly using grid stars

- User request: "user should have possibility to switch favorite status on/off via grid, not only edit dialog"; use an empty/filled star, click to toggle, and when isFavorite filtering is active an element switched off disappears from the grid.
- Source: requesting user, this conversation. Added the inline favorite-control and filtered-row behavior under Selection controls and aligned the currency-list star description. Inline changes save directly as independent catalog activity. Explicitly scoped the earlier filter-off behavior to Add/Edit-dialog Save, so an inline star click keeps Favorites only active. Existing root and ancestor-navigation rules remain applicable.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-044. Select only groups permission and selection after a filtered selected row disappears remain separate clarifications; TRD grid/routing requirements require later alignment.
- BRD 0.139 becomes Draft 0.140; no full-document approval is inferred.

### 2026-10-05 — Read-only stars in Select only groups

- Question: keep favorite stars read-only in Select only groups, preserving its no-editing rule?
- Accepted answer: "correct. It's only for edit-selection popups".
- Source: requesting user, this conversation. Updated Select only groups and inline-star permissions. Within catalog tree popups, clicking a star is available in both Edit-select types only; Select only groups can display status and filter the view but cannot change favorite flags. Existing inline favorite controls in editable main catalogs remain under the preceding grid-star rule.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-044 and the catalog mode rules. Selection after a filtered selected row disappears remains a separate clarification; TRD mode permissions require later alignment.
- BRD 0.140 becomes Draft 0.141; no full-document approval is inferred.

### 2026-10-05 — Selection after an inline favorite change

- Question: if a star click removes the selected row or makes it unselectable, select and reveal the first selectable item in the remaining filtered tree; if none exists, clear selection and disable the picker OK button?
- Accepted answer: "agree".
- Source: requesting user, this conversation. Replaced the pending inline-selection clarification with the active fallback under Selection controls, applying the caller's existing selection rules. Selection changes remain separate from accepting a picker choice and keep Favorites only on.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-044 and catalog picker eligibility. TRD tree-refresh/selection behavior requires later alignment.
- BRD 0.141 becomes Draft 0.142; no full-document approval is inferred.

### 2026-10-05 — Failed inline favorite update

- Question: if saving a star change fails, keep the original favorite status, row and selection, and show an error banner in the current screen or picker?
- Accepted answer: "correct".
- Source: requesting user, this conversation. Added the failure behavior under Selection controls. Preserve the original filtered state and selection rather than applying the successful-change row-removal fallback; show the error in the current catalog screen or Edit-select picker.
- Related capabilities BC-003/004/006 and requirements FR-003/004/006, under BR-044 and inline-favorite behavior. TRD UI error handling requires later alignment.
- BRD 0.142 becomes Draft 0.143; no full-document approval is inferred.

### 2026-10-05 — Reports list and commands

- Question: Reports screen starts with a list of saved reports and New / Edit / Delete / Run buttons?
- Accepted answer: "Agree."
- Source: requesting user, this conversation. Added Reports — main screen under Main Navigation. Existing report-definition and on-demand calculation rules remain under BR-028; editor, filter-picker and results presentation are separate upcoming UI decisions.
- Related capabilities BC-007/008 and requirements FR-007/008, under BR-023–BR-028. TRD report UI requirements require later alignment.
- BRD 0.143 becomes Draft 0.144; no full-document approval is inferred.

### 2026-10-05 — Reports editor, filter trees and pre-run date selection

- Question 1: "Editor: New/Edit opens a modal report editor with Name, date range, grouping and filters. Save/Cancel follows our existing rules. Double-click a saved report opens Edit. Agree?"
- Accepted answer 1: "--1. Agree"
- Question 2: "Filters: Category, Project and Correspondent each have a modal tree picker with checkboxes and Unassigned. Users can change selections, but cannot add/edit/delete catalog items here. Agree?"
- Accepted answer 2: "--2. Agree. These trees are different. They contains checkboxes on the groups and elements + unassignment row."
- Question 3: "Results: Run opens a modal results window. From the list, it uses saved settings. From the editor, it previews current settings without saving them. Close returns to the list or editor. Agree?"
- User's replacement for question 3: "--3. Before run, user should see modal popup and can choose new range, (from-to, current week, current month, current year, +/- week , moths, year). After clicking run, the new range should be saved to the report. Then user can see report result and can save them"
- Source: requesting user, this conversation. Recorded accepted editor/filter requirements and the explicit replacement Run flow. Removed the superseded exclusion of relative report periods to allow the requested date shortcuts. Saving generated results is now required; its destination/format, exact shortcut behavior and handling of new/unsaved settings remain unresolved within this batch. The previously proposed unsaved-preview behavior and modal results placement are not accepted requirements.
- Related capabilities BC-007/008 and requirements FR-007/008, under BR-023–BR-028. TRD report UI/date and result-saving requirements require later alignment; no TRD change in this turn.
- Discussion workflow: the user requests three related questions per batch, recording accepted answers first, and resolving clarifications in the current batch before starting the next. This explicit instruction overrides the skill's single-question workflow.
- BRD 0.144 becomes Draft 0.145; no full-document approval is inferred.

### 2026-10-05 — Report date shifts, date-only update and CSV results

- Question 1: "Date buttons: Should Current week/month/year select the complete calendar period, and +/− move to the next/previous complete period? Example: February 1–28 → March 1–31. Or should +/− shift both entered dates?"
- Accepted answer 1: "--1. both entered date"
- Question 2: "Unsaved settings: When running from the editor, should Run save all current settings, including a new report? Or must the user Save first, with Run saving only the chosen date range?"
- Accepted answer 2: "--2. No. Ut should update current report only with date range information"
- Question 3: "Save results: Do you mean exporting a file, or keeping a calculated snapshot inside the app? If a file, which formats for version one—Excel, PDF, CSV?"
- Accepted answer 3: "--3. User can see result in the modal popup and can save it to the csv file"
- Source: requesting user, this conversation. Previous/next shifts both entered dates; Run changes only the current report date range, without saving other settings; results are modal and can be saved to CSV. No acceptance of full-current-period boundaries, month/year edge handling, Run availability for new/unsaved reports or detailed CSV contents is inferred.
- Affected IDs: BR-026/028; BC-007/008; FR-007/008 and Reports — main screen. TRD report UI/date and CSV requirements require later alignment; no TRD change in this turn.
- BRD 0.145 becomes Draft 0.146; no full-document approval is inferred.

### 2026-10-05 — Report calendar boundaries, saved-list Run and complete CSV export

- Question 1: "Date boundaries: Current buttons select the full calendar week/month/year. Month/year shifts use the last valid day when necessary: January 31 + one month → February 28. Agree?"
- Accepted answer 1: "--1 agree"
- Question 2: "Run flow: Run is available from the saved-report list. The editor has Save/Cancel; a new report must be saved before running. Canceling the date popup changes nothing. Agree?"
- Accepted answer 2: "--2. agree"
- Question 3: "CSV: A standard Save dialog lets users choose filename/location. Export the complete calculated table, including headers, grouping labels, currency codes and totals. Agree?"
- Accepted answer 3: "--3. agree"
- Source: requesting user, this conversation. Recorded the three accepted decisions and removed their active unresolved wording. Week boundaries remain Monday–Sunday under BR-026. This completes the pending date/Run/export questions; results layout, range changes while viewing results and report-list deletion interaction form the next Reports batch.
- Affected IDs: BR-026/028; BC-007/008; FR-007/008 and Reports — main screen. TRD report/date/CSV requirements require later alignment; no TRD change in this turn.
- BRD 0.146 becomes Draft 0.147; no full-document approval is inferred.

### 2026-10-05 — Read-only report results, double-click Run and confirmed list deletion

- Question 1: "Results layout: Report name and date range at the top, a read-only table using the chosen grouping, and Save CSV / Close buttons. Agree?"
- Accepted answer 1: "--1. agree"
- Question 2: "Change dates: Add a Change dates button to results. It opens the date popup; Run updates the saved range and refreshes the existing results. Cancel keeps the current results. Agree?"
- User's replacement for question 2: "-- 2. User double click (or run command) on the report. App showes popup with date range. user can change range and clicks run button (or Enter). app closes this popup and opens fully read-only popup with repost results"
- Question 3: "Delete report: Confirm deletion with the report name. Afterwards, select the next row, or the previous row if the deleted row was last. If none remain, clear selection. Agree?"
- User's replacement for question 3: "--3. User can click in the list of reports delete button (menu). After confirmation app deleted report"
- Source: requesting user, this conversation. Results layout is accepted. Double-click now invokes Run, superseding the earlier double-click Edit rule. The date popup closes before the read-only results popup opens; dates are changed by running again from the saved-report list. The proposed Change dates control is not accepted. Delete is available through the reports list button/menu and requires confirmation; the proposed automatic post-delete selection rule is not inferred from this answer.
- Affected IDs: BR-028; BC-007/008; FR-007/008 and Reports — main screen. TRD report UI requirements require later alignment; no TRD change in this turn.
- BRD 0.147 becomes Draft 0.148; no full-document approval is inferred.

### 2026-10-05 — Report saved-range initialization, invalid ranges and open-boundary shifts

- Question 1: "Initial range: The date popup starts with the report's saved range. For a new report, default to the current calendar month. Agree?"
- Accepted answer 1: "--1. agree"
- Question 2: "Invalid range: If From is later than To, disable Run—including Enter—and show an error beside the dates. Agree?"
- Accepted answer 2: "-- 2. agree"
- Question 3: "Open ranges: If only one date is entered, +/− shifts that date and leaves the other blank. If both are blank, disable +/−. Current week/month/year still fills both dates. Agree?"
- Accepted answer 3: "--3. agree"
- Source: requesting user, this conversation. Recorded all three accepted date rules under BR-026 and Reports — main screen. Existing inclusive and optional-boundary semantics remain applicable; no new mandatory-date constraint is introduced. The date-popup questions in this batch are settled.
- Affected IDs: BR-026; BC-007/008; FR-007/008. TRD report date/UI requirements require later alignment; no TRD change in this turn.
- BRD 0.148 becomes Draft 0.149; no full-document approval is inferred.

### 2026-10-05 — Report filter root checkbox; grouping and picker acceptance still pending

- Question 1: "Grouping: Use a Currency first checkbox and a Group by combo: Category, Project, Correspondent, Day, Week, Month, Year. New reports default to Category, with Currency first off. Agree?"
- Answer requiring explanation, not acceptance: "--1. recall me about currency checkbox. I forgot my idea."
- Question 2: "Filter picker: OK returns checkbox choices to the report editor. Cancel keeps the previous choices. Changes are persisted only when the report itself is saved. Agree?"
- Answer requiring explanation, not acceptance: "--2. I don't understand"
- Question 3: "Bulk selection: Each filter tree has Select all / Clear all buttons. They include groups, elements and the Unassigned row. Agree?"
- User's accepted replacement for the bulk-selection control: "--3. No. We have root node. User can make select all /clear all via it's check box"
- Source: requesting user, this conversation. Recorded root-checkbox bulk selection without separate buttons. Grouping controls/defaults and picker commit/cancel behavior are not accepted and require explanation. Whether the root action includes Unassigned remains a focused clarification. Stay in this batch until these items are resolved.
- Affected IDs: BR-024; BC-007/008; FR-007/008 and Reports — main screen. Existing currency calculation/display rules under BR-026/027 are unchanged. TRD filter UI requires later alignment; no TRD change in this turn.
- BRD 0.149 becomes Draft 0.150; no full-document approval is inferred.

### 2026-10-05 — Report grouping levels, picker confirmation and independent Unassigned node

- Question 1 after explaining the existing currency-total rules: "Keep this checkbox, default off, with Group by = Category initially?"
- Accepted answer 1: "--1. Agree. Currency is the first level grouping. The second combo is second-level (or single level) grouping"
- Question 2: "Filter popup: While editing a report, you open the Category checkbox tree. OK closes the tree and returns your choices to the report editor. Cancel closes it and keeps the previous choices. The report editor's Save persists them; canceling the report editor discards them. Agree with this behavior?"
- Accepted answer 2: "--2. correct"
- Question 3: "Root checkbox: Should selecting/clearing the root also select/clear Unassigned? I suggest yes."
- User's replacement for question 3: "--3. no. Let's make in this way. On the first level 2 nodes: unassigned and root. Root is tree. It's OK to select and deselect need to click both checkboxes"
- Source: requesting user, this conversation. Grouping controls/defaults and picker OK/Cancel are accepted. The picker first level contains Unassigned and Root as independent siblings; Root controls only its groups/elements tree. Selecting/clearing everything requires both checkboxes. Removed the superseded pending wording. This discussion batch is settled.
- Affected IDs: BR-024/026; BC-007/008; FR-007/008 and Reports — main screen. Currency totals under BR-027 remain unchanged. TRD report/filter UI requirements require later alignment; no TRD change in this turn.
- BRD 0.150 becomes Draft 0.151; no full-document approval is inferred.

### 2026-10-05 — Report list columns and empty-result display; new ReportGroup proposal

- Question 1: "Reports list: Columns Name | Grouping | From | To. Grouping can show, for example, Currency → Category. Agree?"
- Accepted answer 1: "--1. Agree"
- Question 2: "Filter-tree search: Use the same Search / Next / Previous navigation as other trees, revealing matches inside collapsed groups. Searching leaves checkbox selections unchanged. Agree?"
- Answer introducing a different idea: "--2. You think we should create ReportGroup entity. With same functionality? Good idea. Agree"
- Question 3: "Empty results: Show No matching data in the results popup. If a classification filter has no selections, show the existing warning and name that filter. Agree?"
- Accepted answer 3: "--3. Agree"
- Source: requesting user, this conversation. Recorded report-list columns and empty-result presentation. The second question referred to existing classification filter trees, not report organization; the user response proposes ReportGroup with catalog-like functionality. Opened Q-17 for the group proposal and original search question without assuming hierarchy, actions or favorites. Resolve these clarifications before proceeding to another topic.
- Affected IDs: BR-023/028; BC-007/008; FR-007/008 and Reports — main screen; new Q-17. TRD list/results UI requires later alignment, and any settled report-group scope needs later technical revision. No TRD change in this turn.
- BRD 0.151 becomes Draft 0.152; no full-document approval is inferred.

### 2026-10-05 — Sixth ReportGroup hierarchy and classification filter-tree search

- Question 1: "Filter search: Enable Search / Next / Previous in those checkbox trees, keeping checkbox selections unchanged. Agree?"
- Accepted answer 1: "--1. If you are talking about Category, Project, Correspondent tree, yes"
- Question 2: "Report groups: Show saved reports in a tree. Groups contain reports and nested groups; the report editor gets a Group picker. Agree?"
- Accepted answer 2: "--2. Agree. The behavior should be equals as for others 5 groups"
- Question 3: "Management: Reuse existing group Add/Edit/Delete/Move/Merge and drag/drop rules, with Favorites for groups and reports. Double-click a group edits it; double-click a report runs it. Agree?"
- Accepted answer 3: "--3. agree"
- Source: requesting user, this conversation. Added ReportGroup as the sixth hierarchy, applying shared group/root protection, movement, merging, selection, modal group-choice routing and favorite behavior. Report double-click continues to Run. The existing explicit report duplicate-name allowance is preserved as a naming exception. ReportGroup organizes saved definitions and does not add another classification filter. Search applies to the existing Category/Project/Correspondent checkbox trees without changing their checkbox selections. Q-17 is closed.
- Updated active hierarchy counts, initial content/capability scenarios, report/group/favorite rules, report UI, functional requirements and trace links. Historical clarification entries and approved 0.32 provenance are preserved.
- Affected IDs: BR-004/006/007/008/028/044; BC-001/003/007/008/009; FR-001/003/007/008/009; Q-17. TRD requires later alignment for ReportGroup, report group/favorite persistence, initial sixth root and report/filter UI. No TRD or implementation change in this turn.
- BRD 0.152 becomes Draft 0.153; no full-document approval is inferred.

### 2026-10-05 — Report header layout and full checkbox-filter trees; row layout pending

- Question 1: "Group rows: Show Name and favorite star. Leave Grouping, From and To blank. Report rows show their values and favorite star. Agree?"
- Answer requiring explanation, not acceptance: "--1. I dont' understand"
- Question 2: "Report editor: Header order: Name | Group picker | Favorite checkbox. Dates, grouping and classification filters appear below. Agree?"
- Accepted header-layout answer and terminology question: "--2. Agree. What does it mean classification filters"
- Question 3: "Checkbox filter trees: Always show the full Category/Project/Correspondent tree; omit the Favorites-only toggle in these filter popups. Agree?"
- Accepted answer 3: "--3.agree"
- Source: requesting user, this conversation. Recorded the editor header order and full report checkbox-filter trees without Favorites only. Named Category/Project/Correspondent filters explicitly in the editor layout; these are the already confirmed BR-023 filters. Scoped shared favorite-toggle rules to catalog tree pickers so they do not contradict the report checkbox-filter exception. ReportGroup row cells remain pending explanation in the same batch.
- Affected IDs: BR-023/028/044; BC-007/008; FR-007/008 and Reports — main screen/Selection controls. TRD report header/filter UI requires later alignment; no TRD or implementation change in this turn.
- BRD 0.153 becomes Draft 0.154; no full-document approval is inferred.

### 2026-10-05 — ReportGroup and report row layout confirmed

- Clarifying explanation: a ReportGroup organizes saved reports and has no calculation grouping or report date range. An example showed an Expenses group with blank Grouping/From/To cells and a nested Monthly expenses report with Category grouping and October dates, both with favorite stars.
- Exact question: "Agree with this row layout?"
- Accepted answer: "agree"
- Source: requesting user, this conversation. Confirmed the group/report row layout and removed its active unresolved wording. The example dates do not replace the existing regional date-format requirement. The Reports discussion batch is complete; the next UI discussion concerns Synchronization.
- Affected IDs: BR-006/028/044; BC-008; FR-008 and Reports — main screen. TRD report tree UI requires later alignment; no TRD or implementation change in this turn.
- BRD 0.154 becomes Draft 0.155; no full-document approval is inferred.

### 2026-10-05 — Synchronization progress and TXT summary; status meaning pending

- Question 1: "Screen: Show current status, last successful synchronization date/time, and Sync now / Latest result buttons. Agree?"
- Accepted date/buttons and request to clarify status: "--1. Current status. What does it mean? All others agree"
- Question 2: "Progress: Sync now opens a modal progress popup showing the current stage, including waiting for the server, with a Cancel button. Editing stays blocked during synchronization. Agree?"
- Accepted answer 2: "--2. agree."
- Question 3: "Latest result: Open a read-only modal summary showing received additions/updates/deletions by entity type, plus conflicts, corrections and errors. Omit zero counts, as already agreed. Agree?"
- Accepted answer 3 with additional export requirement: "--3. agree. With buttot save to file (txt)"
- Source: requesting user, this conversation. Recorded the date/buttons, modal progress and read-only latest summary with Save to file TXT export. Text export saves the displayed synchronization summary; detailed diagnostic logging remains under the existing separate BR-043/Q-12 rules. Current status meaning/labels remain unresolved in this batch; no acceptance of status labels or new access restrictions is inferred.
- Affected IDs: BR-034/043; BC-009/012; FR-009/012 and Synchronization — screen and dialogs. TRD synchronization UI and TXT export require later alignment; no TRD or implementation change in this turn.
- BRD 0.155 becomes Draft 0.156; no full-document approval is inferred.

### 2026-10-05 — Ready/recovery labels accepted; Master-controlled expiry questioned

- Exact question: "Show these three statuses on the Synchronization screen?", following the proposed Ready / Recovery required / Expired labels.
- Accepted labels and expiry question: "Status Expired. How can we identify it? Ready - OK Recovery required - OK. We have made sync, but doesn't get confirmation from Master. Master's status is undefined. Expired ???"
- Source: requesting user, this conversation. Recorded acceptance of Ready and Recovery required; Expired remains unresolved as a screen status. The user described an uncertain Master acceptance response as a recovery example. Existing BR-038–BR-040 also cover unfinished local recovery after a known publication; the example does not replace those rules.
- Rechecked BR-041/042 and their cited synchronization discovery, Expiration and replacement: Master clock controls the fixed 90-day registration period; Master-confirmed expiry rejects old synchronization and triggers disposal/replacement. Offline local elapsed time alone is not confirmation. Whether to present expiry as a lasting status or via the existing message/Download flow requires clarification.
- Affected IDs: BR-038–BR-042; BC-009/010/011; FR-009/010/011 and Synchronization — screen and dialogs. No underlying expiry/recovery policy, TRD or implementation change in this turn.
- BRD 0.156 becomes Draft 0.157; no full-document approval is inferred.

### 2026-10-05 — Two synchronization screen statuses and separate expiry/Download presentation

- Exact question: "Agree with this presentation?", following the proposal: Current status is Ready / Recovery required; expiry is shown as a message with Download after Master confirmation, not as a permanent screen status.
- Accepted answer: "Yes. I agree. And agree with this status"
- Source: requesting user, this conversation. Confirmed the two Current status labels and expiry presentation. Removed the pending expiry-display wording. The existing Master-clock 90-day expiration, disposal/replacement and recovery-access rules remain applicable; this answer selects their UI presentation.
- Affected IDs: BR-038–BR-042; BC-009/010/011; FR-009/010/011 and Synchronization — screen and dialogs. TRD status and expiry UI requires later alignment; no TRD or implementation change in this turn.
- BRD 0.157 becomes Draft 0.158; no full-document approval is inferred.

### 2026-10-05 — Recovery action caption, handled cancellation and automatic manual-result summary

- Question 1: "Button label: Show Sync now when Ready and Retry recovery when Recovery required. Both use the same progress popup. Agree?"
- Accepted answer 1: "--1 agree"
- Question 2: "Cancellation: Cancel, Esc or the popup's X requests cancellation. The popup stays open until the service finishes handling cancellation, then closes. Agree?"
- Accepted answer 2: "--2 agree"
- Question 3: "Manual completion: After a manual synchronization attempt ends, close progress and automatically open the latest-result summary, including success, failure or cancellation. Agree?"
- Accepted answer 3: "--3 agree"
- Source: requesting user, this conversation. Recorded all three accepted synchronization UI rules. Cancellation handling refers to completion by the local service; it does not imply Master rolled back or stopped, and existing pending-recovery/expiry rules still govern the resulting state. Automatic startup/exit presentation is the next discussion area.
- Affected IDs: BR-034/038–BR-040/043; BC-009/010/012; FR-009/010/012 and Synchronization — screen and dialogs. TRD progress/cancellation/completion UI requires later alignment; no TRD or implementation change in this turn.
- BRD 0.158 becomes Draft 0.159; no full-document approval is inferred.

### 2026-10-05 — Automatic startup and successful-exit synchronization presentation

- Question 1: "Startup: normal success opens Ledger. Failure, cancellation, conflicts or corrections show the result popup first. Ledger opens afterward only when business access is allowed. Agree?"
- Accepted answer 1: "--1 agree"
- Question 2: "Successful exit sync: normal success closes the app. If conflicts or corrections occurred, show the result first, with Save to file available. Closing this result completes exit. Agree?"
- Accepted answer 2: "--2 agree"
- Source: requesting user, this conversation. Recorded the two accepted flows. Startup presentation retains existing recovery/expiry access restrictions. Successful exit allows summary inspection and optional TXT saving before app closure. Failed/cancelled exit choices remain pending clarification; no acceptance of Retry / Exit / Stay is inferred.
- Affected IDs: BR-035/039/041/043; BC-009/010/012; FR-009/010/012 and Synchronization — screen and dialogs. TRD automatic synchronization presentation requires later alignment; no TRD or implementation change in this turn.
- BRD 0.159 becomes Draft 0.160; no full-document approval is inferred.

### 2026-10-05 — Failed/cancelled exit synchronization choices confirmed

- Exact clarification question: "Agree with these three buttons?", following the explanation: Retry tries synchronization/recovery again; Exit closes the app; Cancel exit keeps the app open instead of completing the exit request. Ordinary work is possible only when existing access rules allow it; pending recovery still blocks business access.
- Accepted answer: "agree"
- Source: requesting user, this conversation. Confirmed Retry / Exit / Cancel exit after failed or cancelled automatic exit synchronization. Replaced the unclear Stay caption with Cancel exit and removed the active unresolved presentation wording. The existing TXT summary export, retention, recovery and expiry rules continue to apply. This synchronization discussion batch is complete; the next UI area is Settings.
- Affected IDs: BR-035/039/041/043; BC-009/010/012; FR-009/010/012 and Synchronization — screen and dialogs. TRD exit/result presentation requires later alignment; no TRD or implementation change in this turn.
- BRD 0.160 becomes Draft 0.161; no full-document approval is inferred.

### 2026-10-05 — Settings sections, explicit Save/Cancel and immutable information

- Question 1: "Layout: two sections: This device (account naming, sync trigger, conflict priority) and Shared settings (balancing account). Agree?"
- Question 2: "Saving: changes apply only after clicking Save. Cancel restores saved values and keeps the Settings screen open. Agree?"
- Question 3: "Information: also show base currency, amount precision and rate precision in Shared settings as read-only fields, since they were fixed during bookkeeping creation. Agree?"
- Accepted answer to all three: "all agree"
- Source: requesting user, this conversation. Recorded the section layout, staged Settings edits with explicit Save/Cancel, and read-only creation-time information. Existing configuration scopes, immutability, precision and balancing rules remain applicable. No separate DisplayDecimalPlaces setting is introduced.
- Affected IDs: BR-005/012/017/018/029/034; BC-001/004/005/009; FR-001/004/005/009 and Settings — main screen. TRD Settings UI requires later alignment; no TRD or implementation change in this turn.
- BRD 0.161 becomes Draft 0.162; no full-document approval is inferred.

### 2026-10-05 — Device Settings controls and optional account-name currency suffix

- Question 1: "Account naming: combo with six classification orders, a single-character separator textbox, and a live example of the resulting name. Agree?"
- Accepted answer 1 and addition: "--1 agree. Let's add new checkbox Add currency. This check box added currency. E.g is not checked Store/Food/Life and checked Store/Food/Life(UAH) (if account in UAH) (this require add bool parameter to the function in IAccountService)"
- Question 2: "Synchronization: three radio buttons: Manual only / On startup / On exit. Only one selected. Agree?"
- Accepted answer 2: "--2 agree"
- Question 3: "Conflict priority: two radio buttons: Prefer Master / Prefer this device, with explanation: Used when the same item changed on both sides. Agree?"
- Accepted answer 3: "--3. agree"
- Source: requesting user, this conversation. Recorded all three control choices and the Local Add currency checkbox. The suffix uses the account currency code in parentheses with no preceding space. The existing default-name, manual override and no-automatic-renaming rules remain applicable. The initial checkbox value was not supplied and remains unresolved.
- User-specified technical input: add a bool parameter to the account default-name function in IAccountService. Recorded for later technical alignment, without selecting a signature or changing backend code.
- Affected IDs: BR-005/012/029/034; BC-004/009; FR-004/009 and Settings — main screen. TRD requires later alignment for Settings controls, the Local currency-suffix option and account-name service contract. No TRD or implementation change in this turn.
- BRD 0.162 becomes Draft 0.163; no full-document approval is inferred.

### 2026-10-05 — Account-name currency suffix defaults and final empty-classification rule

- Question 1: "Default: initially unchecked, preserving the current naming format. Agree?"
- Accepted answer 1: "--1 agree"
- Question 2: "Base currency: when checked, include the code for base-currency accounts too. Agree?"
- Accepted answer 2: "--2 yes. It is not differences. Base or not"
- Question 3: "Empty classifications: preserve existing separators. With all classifications empty and currency UAH, the generated name becomes //(UAH). Agree?"
- Initial response rejected that empty case and reiterated the checked format: "{Correspondent}/{Category}/{Project}({Account.CurrencyName})", with examples Store/Food/Life(UAH) and currency suffixes (UAH)/(USD)/(EUR).
- Final explicit clarification superseding the initial rejection and the assistant's currency-only interpretation: "Got it. If all empty the default name is //(UAH)"
- Source: requesting user, this conversation. Recorded Add currency initially unchecked, applicability to all currencies including the base currency, and retention of classification positions/separators when appending the account currency identifier. The all-empty checked slash format is //(UAH), not (UAH). Existing stored/manual account names and the default-name/Restore behavior remain unchanged.
- Updated the owning account-name rule, Settings control, account-naming acceptance scenario and original naming decision register. The user-requested IAccountService boolean parameter remains recorded technical input for later implementation; no backend change in this turn.
- Affected IDs: BR-005/012; BC-004; FR-004; Q-03 and Settings — main screen. TRD requires later alignment for Local naming configuration and the default-name service contract, including the user's Account.CurrencyName notation and its UAH/USD/EUR examples. No TRD or implementation change in this turn.
- BRD 0.163 becomes Draft 0.164; no full-document approval is inferred.

### 2026-10-05 — Balancing-account picker eligibility and Settings navigation/save failures

- Question 1: "Balancing account: use the usual account-tree picker. Only base-currency accounts can be chosen; other accounts remain visible. Agree?"
- Accepted answer 1 and addition: "--1. agree. We should disable button Select when user selects group or non basic account"
- Question 2: "Unsaved changes: navigating away or exiting shows Save / Discard / Cancel. Save or Discard continues the requested action; Cancel keeps Settings open. Agree?"
- Accepted answer 2: "--2. agree"
- Question 3: "Save failure: keep Settings open with entered values and an error message. Previously saved settings remain unchanged. Agree?"
- Accepted answer 3: "--3 agree"
- Source: requesting user, this conversation. Recorded the full balancing-account tree with only base-currency account selection, including the disabled Select button for groups/non-base-currency accounts. All selection gestures enforce the same eligibility rule. Recorded the unsaved Settings navigation/exit choice and preservation of entered/prior saved values after a save failure; failed Save does not continue a pending navigation or exit request.
- Affected IDs: BR-005/012/017/018/029/034; BC-001/004/005/009; FR-001/004/005/009 and Settings — main screen. TRD requires later alignment for picker eligibility, pending Settings edits and configuration-save failure behavior. No TRD or implementation change in this turn.
- BRD 0.164 becomes Draft 0.165; no full-document approval is inferred.

### 2026-10-05 — Missing balancing account, empty separator default and sync-trigger activation

- Question 1: "Missing balancing account: show an empty field and allow saving other settings. Adding a balancing entry requires choosing a replacement account. Agree?"
- Accepted answer 1: "--1 agree"
- Question 2: "Invalid separator: show an error beside the textbox and disable Save until it contains exactly one non-whitespace character. Agree?"
- User's replacement for the empty-input case: "--2 If empty, just put default /"
- Question 3: "Changing sync trigger: saving does not immediately start synchronization. On startup applies at the next launch; On exit applies at the next actual exit. Agree?"
- Accepted answer 3: "--3 agree"
- Source: requesting user, this conversation. Recorded the missing-balancing-account field/save behavior, default / for an empty separator textbox, and deferred trigger activation. Empty separator input is no longer an error. The separator normalization timing, handling of whitespace-only input and presentation for other invalid input still require clarification in this batch; no acceptance of the proposed error/Save-disable behavior is inferred.
- Updated owning rules and Settings UI; replaced the contradictory empty-separator acceptance scenario. Existing currency-suffix, precision, manual synchronization and recovery rules remain unchanged.
- Affected IDs: BR-005/012/018/034; BC-004/005/009; FR-004/005/009; Q-03 and Settings — main screen. TRD requires later alignment for Settings validation, missing balancing-account display and sync-trigger activation. No TRD or implementation change in this turn.
- BRD 0.165 becomes Draft 0.166; no full-document approval is inferred.

### 2026-10-05 — Separator input limit and normalization confirmed

- Exact clarification question: "One clarification: limit the separator textbox to one character. When focus leaves it or Save is clicked, replace empty or whitespace-only input with /. Agree?"
- Accepted answer: "agree"
- Source: requesting user, this conversation. Confirmed the one-character input limit, whitespace handling and normalization timing. Empty/whitespace-only input defaults to / on focus loss or Save, without a validation error. Updated the owning rule, Settings UI, account-naming acceptance scenario and naming decision register; removed the active pending validation wording. This Settings discussion batch is complete; the next area is Help.
- Affected IDs: BR-012; BC-004; FR-004; Q-03 and Settings — main screen. TRD requires later alignment for separator input/normalization alongside the earlier Settings changes. No TRD or implementation change in this turn.
- BRD 0.166 becomes Draft 0.167; no full-document approval is inferred.

### 2026-10-05 — Illustrated PDF guide deferred; About popup confirmed

- Question 1: "Guide: a read-only main-window screen with a short built-in guide, available offline. Agree?"
- User's replacement of the guide format: "--1. I think it's should be pdf with pictures"
- Question 2: "Navigation: topic list on the left, selected text on the right. Topics: Getting started, Transactions, Catalogs, Reports, Synchronization, Settings. Agree?"
- User's deferral: "--2. Pdf we will make latter."
- Question 3: "About: a button opens a modal popup showing the application name and version, with Close. Agree?"
- Accepted answer 3: "--3. agree"
- Source: requesting user, this conversation. Recorded a PDF with pictures and deferred its authoring to a later task, without assuming version-two scope. The proposed built-in text guide, two-pane topic navigation and topic list are not accepted requirements. Recorded the accepted modal About popup. Guide access/distribution/availability UI requires clarification; the PDF itself is not created in this turn.
- Affected items: Main Navigation and Help and About. TRD requires later alignment for Help access and the About popup once remaining UI decisions are settled. No TRD or implementation change in this turn.
- BRD 0.167 becomes Draft 0.168; no full-document approval is inferred.

### 2026-10-05 — Help dialog and offline PDF access confirmed

- Question 1: "Help opens a small modal dialog with User guide / About / Close. User guide stays disabled until the PDF is available. Agree?"
- Accepted answer 1: "--1 Agree"
- Question 2: "User guide opens the PDF in the default Windows PDF viewer. Agree?"
- Accepted answer 2: "--2 agree"
- Question 3: "Once ready, bundle the PDF with the application so Help works offline. Agree?"
- Accepted answer 3: "--3 agree"
- Source: requesting user, this conversation. Recorded all three Help UI decisions and removed the active pending guide-access wording. The illustrated guide's authoring/content remains deferred to a later task; no PDF is created in this turn. The previously accepted modal About popup remains unchanged. This Help discussion batch is complete; the next area is first-start setup UI.
- Affected items: Main Navigation and Help and About. TRD requires later alignment for Help/About modal presentation, PDF availability, offline bundling and launching the default Windows viewer. No TRD or implementation change in this turn.
- BRD 0.168 becomes Draft 0.169; no full-document approval is inferred.

### 2026-10-06 — First-start setup fields, creation controls and application exit

- Question 1: "Setup dialog: one modal dialog with Login, Password and Create / Open / Exit buttons. Agree?"
- Question 2: "Creation settings: base-currency picker and amount/rate precision combos (0–4, defaults 2 and 4). This section is disabled whenever Create is unavailable. Agree?"
- Question 3: "Closing: when no operation is running, Exit or X closes the app. Successful setup closes the dialog and opens Ledger. Agree?"
- Accepted answer to all three: "all agree"
- Source: requesting user, this conversation. Recorded the modal first-start UI, creation controls and exit/success navigation. Existing local-copy gating, Master-existence restrictions and immutable creation-time configuration remain applicable. Updated the Create rule/capability flow to explicitly include the already required amount/rate precision selections and their BR-017 trace.
- Affected IDs: BR-002/003/017; BC-001/002; FR-001/002 and First-start setup — dialog. TRD requires later alignment for setup presentation, creation controls and exit/navigation. No TRD or implementation change in this turn.
- BRD 0.169 becomes Draft 0.170, dated 2026-10-06; no full-document approval is inferred.

### 2026-10-06 — Master-check status, setup progress and return after failure/cancellation

- Question 1: "Master check: show Checking Master / Master exists / No Master / Check failed. After failure, offer Retry check. Agree?"
- Question 2: "Create/Open progress: use a modal popup showing the current stage and Cancel. Setup remains blocked; cancellation keeps progress open until the service handles it. Agree?"
- Question 3: "Failure or cancellation: return to Setup with entered values preserved. Show an error for failure; retries remain manual. Agree?"
- Accepted answer to all three: "agree all"
- Source: requesting user, this conversation. Recorded the three setup UI rules in the owning Create/Open rule and First-start setup section. No Master is only confirmed absence; failure/unknown state keeps Create unavailable. Cancellation handling refers to the local service and does not imply Master rollback. The existing created-Master/failed-download rule remains applicable.
- Affected IDs: BR-002/003; BC-001/002; FR-001/002 and First-start setup — dialog. TRD requires later alignment for status presentation, manual check retries, progress/cancellation and preservation of setup input. No TRD or implementation change in this turn.
- BRD 0.170 becomes Draft 0.171; no full-document approval is inferred.

### 2026-10-06 — Explicit base-currency selection and unknown Create outcome; field availability pending

- Question 1: "Field availability: enable creation fields once Master absence is confirmed. Keep them editable while required inputs are incomplete; Create stays disabled until inputs are valid. Agree?"
- Answer requiring explanation: "--1. I don't understand"
- Question 2: "Base currency: initially unselected; the user must choose it before creating bookkeeping. Agree?"
- Accepted answer 2: "--2. Agree"
- Question 3: "Unknown creation outcome: if Create loses its response, keep Create disabled and offer Retry check. Master exists → use Open; confirmed absence → allow Create again. Agree?"
- Accepted answer 3: "--3 Agree"
- Source: requesting user, this conversation. Recorded explicit base-currency selection and manual resolution of an unknown Master-creation outcome. Earlier absence confirmation must not enable another Create while that outcome is unknown. Confirmed existence routes to Open; confirmed absence allows Create subject to valid required input. Field availability while the Create button is disabled by incomplete input remains unresolved in this batch; the proposed field/button distinction is not accepted.
- Affected IDs: BR-002/003/017; BC-001/002; FR-001/002 and First-start setup — dialog. TRD requires later alignment for the empty initial base-currency field and uncertain creation-outcome handling. No TRD or implementation change in this turn.
- BRD 0.171 becomes Draft 0.172; no full-document approval is inferred.

### 2026-10-06 — Credentials-first automatic attachment/creation route

- Previous clarification asked whether creation fields remain usable while required input is incomplete.
- User's replacement flow: "User enter login/password. If master exist, new Local attaches to this master. If doesn't exist, new Master creates"
- Source: requesting user, this conversation. Recorded credentials-first Master determination and application selection of the existing-Master attachment or absent-Master creation branch. This supersedes the prior manual Create/Open choice and its field/button availability question. Required explicit base-currency selection and amount/rate precision settings still apply to new-Master creation. Activation controls, creation-settings presentation and navigation remain unresolved UI decisions.
- Updated BR-003, the owning setup section, BC-001/002 flows/scenarios and Q-13 wording. Existing manual retries, created-Master preservation, unknown-outcome checks, authentication and no-local-copy access restrictions remain applicable. Earlier clarification entries and approved 0.32 provenance are preserved.
- Affected IDs: BR-002/003/017; BC-001/002; FR-001/002; Q-13 and First-start setup — dialog. TRD first-use flow/contracts and setup UI require later alignment with the credentials-first route. No TRD or implementation change in this turn.
- BRD 0.172 becomes Draft 0.173; no full-document approval is inferred.

### 2026-10-06 — Continue, same-dialog creation step and Back confirmed

- Question 1: "Credentials step: Login, Password, Continue / Exit. Continue checks Master; if it exists, attachment starts automatically. Agree?"
- Question 2: "Master absent: the same dialog shows currency/precision fields and a Create button. Agree?"
- Question 3: "Back: before creation, return to the credentials step while preserving entered values. Agree?"
- Accepted answer to all three: "all agree"
- Source: requesting user, this conversation. Recorded the credential-step controls, automatic existing-Master attachment after Continue, same-dialog creation step and Back before creation. Removed the active unresolved setup-controls wording. Existing authentication, required creation settings, manual retry/unknown-outcome handling, progress/cancellation and setup access restrictions remain applicable.
- Affected IDs: BR-002/003/017; BC-001/002; FR-001/002 and First-start setup — dialog. TRD first-use UI requires later alignment for the Continue/Create/Back controls and two steps within one modal dialog. No TRD or implementation change in this turn.
- BRD 0.173 becomes Draft 0.174; no full-document approval is inferred.

### 2026-10-06 — Single Windows instance, restored window state and no theme feature

- Question 1: "Single instance: launching the app again brings the existing window and its active modal popup to the front. Agree?"
- Question 2: "Window state: remember the main window's size, position and maximized state between launches. Agree?"
- Accepted answer to questions 1 and 2: "1,2 agree."
- Question 3: "Appearance: follow Windows light/dark theme automatically. Agree?"
- User's rejection: "3. Don't need theme"
- Source: requesting user, this conversation. Recorded single-instance Windows activation and main-window state persistence. Recorded that no application theme feature is required; the automatic switching proposal is not accepted. No fixed light/dark mode or custom palette is inferred. Existing modal restrictions and session-only screen-state retention remain unchanged.
- Affected items: Main Navigation, Windows application and window state, and Main-window screen state. TRD Windows-host requirements require later alignment for activation, window-state restoration and theme-feature scope. No TRD or implementation change in this turn.
- BRD 0.174 becomes Draft 0.175; no full-document approval is inferred.

### 2026-10-06 — Minute-only transaction time replaces seconds input

- User decision: "CalendarDatePicker - OK. TimePicker. Let's change requirements. Will use without seconds TimePicker is OK".
- Source: requesting user, this conversation. Transaction time display/input uses 24-hour HH:mm without seconds, replacing the prior HH:mm:ss requirement. Calendar date selection remains accepted; specific built-in Windows controls are recorded in TRD.
- Device-local display and UTC timestamp storage remain binding. This UI decision does not establish timestamp truncation, treatment of pre-existing seconds or seconds for new/edited times; those details remain TQ-15. Report default-name timestamp formats and synchronization timeout seconds are unrelated and unchanged.
- Affected items: Windows display and input formats; Ledger transaction dialog; BC-005/FR-005; TRD Windows UI integration and TQ-15.
- BRD 0.175 becomes Draft 0.176. No full-document approval, database migration or application-code change is inferred.

### 2026-10-06 — Seconds on transaction save

- Exact question: "Next: I propose seconds = 00 for new transactions or when time changes. If an existing transaction's time stays unchanged, preserve its stored seconds. Agree?"
- Accepted answer: "agree".
- Source: requesting user, this conversation. New transactions and changed transaction times save seconds as 00. An unchanged existing time preserves its stored seconds despite the minute-only display/input. This resolves the previous seconds policy gap without requiring a database-wide truncation or migration.
- Affected items: Windows display and input formats; Ledger transaction dialog; BC-005/FR-005; TRD date/time controls and TQ-15.
- BRD 0.176 becomes Draft 0.177. No full-document approval or application-code change is inferred.

### 2026-10-06 — Flattened tree expansion and row interaction

- Source: requesting user, this conversation, following the supplied TreeGrid.png design and ListView-based flattened-tree proposal.
- Accepted answers: "--1. agree" for a separate bound visible-row collection; "--2. agree" for visibility through every ancestor and preserved nested expansion state; "--4. Agree" for nullable report check state; "One interaction detail Agree" for arrow-only expansion with ordinary row selection.
- Recorded arrow versus row-click behavior and expansion restoration in the shared catalog interaction rules. Technical collection, row-model and checkbox representation choices belong to TRD.
- Item 3 remains a clarification: the user accepts indentation if it can use a monospace font. No layout-spacer or monospace-font requirement is inferred as settled.
- Affected items: catalog windows/selection modes; BR-024; BC-003/004/006/007/008; TRD Windows UI integration and TQ-15.
- BRD 0.177 becomes Draft 0.178. No full-document approval or application implementation is inferred.

### 2026-10-06 — Selection after collapse confirmed

- Requesting user agreed: when collapsing a group hides the selected row, select that group; selection elsewhere stays unchanged.
- Resume discussion in batches of three related questions at the user's request.
- BRD 0.178 becomes Draft 0.179. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Tree/table headers and overflow confirmed

- User accepted stationary column headers without sorting and single-line text with ellipsis/full-text tooltips. User rejected horizontal scrolling: users should make columns narrower.
- Width-configuration details remain open; no automatic resizing policy is inferred.
- BRD 0.179 becomes Draft 0.180. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Column setup and local preferences confirmed

- User accepted all three proposals: Columns popup with editable widths and Apply/Cancel, local per-view width persistence separate for main screens and selection popups, and Restore defaults.
- BRD 0.180 becomes Draft 0.181. No full-document approval or application implementation is inferred.

### 2026-10-06 — Percentage column widths and limits confirmed

- User accepted widths expressed as percentages totaling 100%, proportional resizing with the window, minimum widths protecting essential controls, and fixed column order with all columns visible in version one.
- The minimum-viewport fallback is still open; no horizontal scrolling is introduced.
- BRD 0.181 becomes Draft 0.182. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Minimum window width and cell alignment confirmed

- User accepted the minimum usable window/popup width and the proposed text/control alignment, adding that the currency-name string in the Account tree must be centered.
- User requested an explanation of the proposed deep-nesting indentation cap. It is not approved; finish this clarification before the next question batch.
- BRD 0.182 becomes Draft 0.183. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Fixed eight-level visual indentation cap confirmed

- User specified a constant limit of 8 indentation steps. Every deeper row is displayed at level-8 indentation; the underlying hierarchy is unchanged. This replaces the pending proposal for a width-dependent cap.
- BRD 0.183 becomes Draft 0.184. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Tree arrow-key navigation confirmed

- User accepted all three keyboard proposals: Up/Down moves through visible rows; Right expands or enters an expanded group; Left collapses or navigates to its parent, with no parent navigation at the root.
- BRD 0.184 becomes Draft 0.185. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Tree activation and checkbox shortcuts confirmed

- User accepted Enter matching double-click, Home/End selecting the first/last visible row, and Space toggling a report-tree checkbox with partial becoming fully checked.
- BRD 0.185 becomes Draft 0.186. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Tree refresh and failure behavior confirmed

- User accepted refresh on reactivation after saved catalog changes, retaining tree state subject to existing explicit reveal rules, and keeping old rows with Retry after refresh failure while disabling editing and selection confirmation. Cancel/Close remains available.
- BRD 0.186 becomes Draft 0.187. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Synchronous ordinary local loading and saving confirmed

- User rejected additional local loading UI and the proposed save-time busy controls, requesting synchronous execution because SQLite operations are expected to be fast. Do not infer removal of existing validation/error or refresh-failure rules; remote synchronization/setup keeps its distinct flows.
- BRD 0.187 becomes Draft 0.188. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Decimal editing and validation boundaries confirmed

- User accepted unfinished text during typing, validation/precision/rounding on focus loss or Save, and preserving invalid text with a field-level error while preventing Save until corrected.
- BRD 0.188 becomes Draft 0.189. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Plain numeric editing and focus behavior confirmed

- User accepted select-all on entering an amount/rate field and focusing the first invalid field on Save while preserving other entered values. User corrected editing to plain unformatted text, without regional formatting; formatting occurs after focus loss.
- Editing decimal separator remains to be clarified before closing this question batch.
- BRD 0.189 becomes Draft 0.190. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Dot decimal separator for editing confirmed

- User chose a dot as the decimal separator in plain amount/rate editing text, independent of Windows regional settings. Regional display formatting still applies after leaving a valid field.
- BRD 0.190 becomes Draft 0.191. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Numeric paste, keypad and syntax confirmed

- User accepted plain dot-decimal paste with surrounding whitespace ignored and currency/grouping symbols rejected, numeric keypad decimal normalized to dot, and simple decimal numbers without expressions or scientific notation.
- BRD 0.191 becomes Draft 0.192. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Popup placement, resizing and saved dimensions confirmed

- User accepted all three proposals: center over parent within the monitor work area; resize tree pickers, transaction/template editors and report results while keeping simple forms fixed; remember dimensions locally by popup type and catalog/mode for tree pickers, reopening centered.
- BRD 0.192 becomes Draft 0.193. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Main-window-only minimize and manual monitor adjustment

- User rejected stack minimization/restoration and automatic accommodation for a smaller monitor. Only the main window has a minimize button; users close all popups before adjusting the main window. This supersedes the prior automatic work-area position adjustment.
- User questioned the need for a taskbar. Clarify Windows taskbar terminology before selecting its application representation.
- BRD 0.193 becomes Draft 0.194. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Main-window Windows taskbar icon confirmed

- After clarification of Windows taskbar terminology, user accepted a normal taskbar icon for the main window and no separate popup icons. This does not introduce popup minimization or a stack-minimize command.
- BRD 0.194 becomes Draft 0.195. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Local JSON UI preferences confirmed

- User accepted local JSON storage under the Windows user profile without synchronization; save column widths on Apply, popup dimensions on closing, main-window geometry on exit; fall back to defaults for missing/invalid UI settings without affecting bookkeeping data.
- BRD 0.195 becomes Draft 0.196. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Drag reorder, invalid-drop feedback and edge scrolling confirmed

- User accepted sibling reordering with an insertion line, keeping separate group/element sequences; invalid destinations show a not-allowed cursor with no mutation; dragging near viewport edges scrolls the tree. Existing move/merge gestures and mode restrictions remain binding.
- BRD 0.196 becomes Draft 0.197. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Favorites reordering allowed; placement specified

- User rejected disabling reorder under Favorites only. Insert immediately after the preceding visible sibling in the full sequence, or first when dropped at the start. The stated initial Order of 1 needs reconciliation with the existing zero-based Order contract.
- The other two questions in this batch, Escape canceling only a drag and retaining hover-expanded groups, remain unanswered; no approval is inferred.
- BRD 0.197 becomes Draft 0.198. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Drag cancellation and start-position clarification

- User answered yes to the first-position clarification, interpreted as retaining stored Order = 0 rather than changing numbering, and agreed to Escape canceling only the drag and preserving hover-expanded groups.
- BRD 0.198 becomes Draft 0.199. No full-document approval or implementation completion is inferred.

### 2026-10-07 — English technical text and original business language

- User clarified that all technical information in reports, logs and similar outputs is English; business information is in the user language. Preserve stored business text without translation. This resolves the language question, not CSV byte encoding.
- BRD 0.199 becomes Draft 0.200. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Empty exports, zero totals and report row ordering

- User accepted all three proposals: empty results remain exportable as headers only; include groups with matching transactions even when totals cancel to zero, excluding groups with no matches; date groups chronological and classification groups in catalog tree order, with CSV retaining displayed order.
- Affects BR-023/026/027/028 and UC-007-01. BRD 0.200 becomes Draft 0.201. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Report currency order, Unassigned and full paths

- User accepted currency catalog order and Unassigned first when matching data exists, then accepted full classification paths after clarification that these appear in report result grouping labels and CSV, not catalog trees or editors.
- Affects BR-026/027/028 and UC-007-01. BRD 0.201 becomes Draft 0.202. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Report sections, totals and negative amount presentation

- User accepted currency headings followed by rows with no expand/collapse, bold currency subtotals after sections and grand total at the bottom under existing calculation rules, and minus signs for negative amounts rather than parentheses.
- Affects BR-026/027/028 and UC-007-01. BRD 0.202 becomes Draft 0.203. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Sync-only remembered token authorization

- User clarified that authorization is for synchronization, not local work, then accepted a server-issued remembered token without saving the password, current-user Windows DPAPI protection, and login again on expired/rejected authorization while preserving local data and pending changes.
- Affects BR-005 and TQ-03. BRD 0.203 becomes Draft 0.204. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Per-Local tokens and same-account reauthorization

- User accepted all three proposals: independent authorization token per registered Local copy; read-only existing login and empty password for reauthorization to the same Master account; canceling login cancels sync but preserves local work and offline access subject to existing expiry/recovery rules.
- Affects BR-005 and TQ-03. BRD 0.204 becomes Draft 0.205. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Token file, missing-token recovery and network failures

- User accepted a separate DPAPI-protected token file under the Windows user profile outside database/UI JSON, reauthentication for missing/unreadable tokens retaining the current Local registration/database, and keeping tokens on connection failures rather than treating those failures as rejected authorization.
- Affects BR-005 and TQ-03. BRD 0.205 becomes Draft 0.206. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Token lifetime, authoritative clock and ownership scope

- User accepted all three proposals: fixed 90-day token lifetime from issue without per-sync extension, Master UTC clock determines expiration, and every synchronization request checks token ownership of the owner/Master dataset/Local registration. Token lifetime is distinct from Local-copy expiry.
- Affects BR-005 and TQ-03. BRD 0.206 becomes Draft 0.207. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Login case/space rules and exact passwords

- User accepted all three proposals: case-insensitive login matching; trim leading/trailing login spaces before account lookup/creation; case-sensitive password with no trimming or other normalization.
- Affects BR-003/005 and TQ-03. BRD 0.207 becomes Draft 0.208. No full-document approval or implementation completion is inferred.

### 2026-10-08 — PasswordHasher, rehash after login and temporary throttling

- User accepted ASP.NET Core PasswordHasher with salted hash storage, upgrading old hashes after successful verification when requested by the hasher without changing the password, and temporary throttling of repeated failed login attempts without permanent account lockout.
- Affects BR-005 and TQ-03. Exact work factors and throttle policy remain open. BRD 0.208 becomes Draft 0.209. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Login throttling removed from version one

- User rejected all three throttle-detail questions, specifying no limit in the first version. This supersedes the earlier acceptance of temporary throttling; remove attempt limits, cooldowns and failed-attempt lockout from active version-one requirements. Accepted password hashing and rehash behavior remain unchanged.
- Affects BR-005 and TQ-03. BRD 0.209 becomes Draft 0.210. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Master endpoint configuration and token address boundary

- User accepted all three proposals: local-file Master URL configuration without a UI editor, HTTPS for remote connections with HTTP only for localhost development, and reauthorization on address changes without sending the saved token to a different address or losing local data/pending changes.
- Affects BR-005, cloud connection/authentication contracts and TQ-03. BRD 0.210 becomes Draft 0.211. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Endpoint reload, configuration errors and dataset identity

- User accepted all three proposals: read Master URL at startup with restart required for file changes; missing/invalid URL reports a synchronization configuration error without blocking otherwise permitted local work; reauthorization at a different server must match the existing MasterDatasetKey, not merely the login.
- Affects BR-005 and Master connection/authorization contracts. BRD 0.211 becomes Draft 0.212. No full-document approval or implementation completion is inferred.

## Approval

- Current version: **Draft 0.218**, not submitted for full-version approval. The date-rule and account-currency corrections are explicitly accepted; no full-document approval is inferred.

### Prior approved baseline — provenance

- **Approved 0.32**.
- Approver: requesting user in this conversation; no personal name supplied or inferred.
- Decision date: 2026-09-27.
- Source: explicit current user message, “I approve BRD. Lets start with TRD”.
- Scope: entire BRD 0.32, covering core bookkeeping, synchronization and first use, including proposed acceptance formulations. Q-01 approval is satisfied by this decision; preceding drafting/history text is retained as provenance.
- Substance and identifiers unchanged by this metadata-only approval record. Discovery documents remain Draft.
- Historical next step for 0.32: draft TRD. This prior approval does not cover BRD 0.33, approve TRD, or authorize implementation.

