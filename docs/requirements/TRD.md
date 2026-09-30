# Technical Requirements Document: DoubleEntryHomeBookkeeping

## Document Control and BRD Reference

- Version: **0.58**. Status: **Draft**. Date: **2026-09-30**.
- Business source: [BRD 0.49](BRD.md), controlled Draft revision of approved BRD 0.32, incorporating the explicitly accepted 2026-09-28 date-rule, account-currency and template-description and optional-rate-comment corrections. Prior 0.32 approval: requesting user, 2026-09-27, “I approve BRD. Lets start with TRD”; no personal name inferred. That approval does not cover 0.49; full-version approval remains outstanding.
- Scope: core bookkeeping, synchronization and first use. Owner/intended approver: requesting user.
- Revision basis: replaces legacy TRD 0.1 while retaining its parent-reference constraint. Discovery sources: [core](001-personal-bookkeeping/discovery.md), [synchronization](002-synchronization/discovery.md), [first use](003-first-using/discovery.md). Later BRD decisions supersede their historical statements.
- Naming: BRD uses Business Capabilities `BC-###`; this TRD uses Detailed Use Cases `UC-###-##`, linked to `OP-###` under `SVC-###`. This is the user-selected convention, replacing the skill's default BRD UC convention.
- Authority: approved business behavior is binding. This TRD's proposed schemas, operation grouping, DTO shapes, error identifiers and concurrency policies require review. Approval of BRD does not approve TRD or authorize implementation.
- No previous PM/DTO/SVC/OP IDs existed in legacy TRD; identifiers introduced here are stable for subsequent revisions.

## Technical Scope and Constraints

### Confirmed inputs retained

- Android and Windows desktop, single-user prototype, one master and multiple registered complete local copies (BR-001–BR-005, NFR-001).
- SQLite business data locally and on Azure; first-version SQLite Admin.db. Azure Functions perform initial creation/connection. Prototype deployment address hardcoded. Version-two administration planned for Microsoft SQL Server; business datasets remain SQLite. These are explicit user technical inputs retained by BRD, not newly selected architecture.
- Two configuration tables: System synchronizes; Local does not. Base currency and APr/RPr: selected at creation, immutable, stored in System. Display amounts/BaseAmount with APr and rates with RPr; no independent display precision. Local: default account-name order/separator, sync trigger, conflict priority (BR-005, BR-012, BR-017).
- Versioned master files such as `{Guid}.db`; prepare changes in a copy and publish a complete candidate; download a complete local replacement. Recovery selects latest published version, not original failed snapshot. SyncKey/outcome/receipt tracking is necessary to resolve lost responses without duplicate business application (BR-038–BR-040; synchronization source).
- TransactionEntry and TemplateEntry retain BOTH parent navigation/reference and parent foreign-key identity; both identify the same parent. This explicit legacy TRD requirement survives; language mapping is deferred. Group root refers to itself as parent but is excluded from its own Children. Groups have child groups and corresponding elements; every element has a group identity/reference (core source).
- UTC transaction time; APr/RPr decimal calculation with midpoint-to-even; on-demand balances/reports. Stored entry rate is independent of rate catalog. JSON saved-report instructions preserve identities and explicit overrides; do not rewrite except user save (BR-015–BR-028).
- Currency catalog follows the user-supplied CultureInfo/RegionInfo enumeration recorded in BRD: exclude neutral/invalid regions, map ISO code/symbol/English name, keep first entry per code. No new catalog source selected.

### Boundary and proposal status

The service catalog below is a logical API/BFF contract, not a deployment diagram. Local operations must work offline; listing them does not require HTTP or a cloud round trip. Paths, verbs, transport statuses, class names, layers and projects are not selected. No C# source, implementation tasks or tests are created by this document.

All schema types, nullability, DTO directions, exposed fields and assignments are **proposed** unless explicitly stated as confirmed in this document. Business requiredness comes from BRD; it does not approve a particular serialization shape. Entity revisions and snapshot versions use confirmed signed 64-bit integers (`int64`). GUID identities for persistent business entities are confirmed; registration/operation identities and authentication-session representation remain proposed. Amounts and rounded BaseAmount use APr; rates use RPr. APr/RPr ranges and fixed SQLite scaling are confirmed below; calculation magnitudes/overflow details remain TQ-01.

## Role and Use-Case Traceability

| BRD role/capability | Retained meaning | Detailed use cases / operations | Authorization source | Status |
| --- | --- | --- | --- | --- |
| ROLE-001 | Sole owner; offline user/cloud owner | All UC rows below | BR-001–BR-005 | Business role confirmed; enforcement TQ-03 |
| ROLE-002 | Requirements approval | Non-system: approve documents, not an application endpoint | BRD Roles/Approval | Non-system |
| BC-001 | Initial creation | UC-001-01–UC-001-03; OP-001–OP-003 | BR-002–BR-004 | Detailed formulations proposed |
| BC-002 | Open/add/reinstall | UC-002-01–UC-002-03; OP-001, OP-004 | BR-001–BR-005 | Proposed |
| BC-003 | Organization | UC-003-01–UC-003-06; OP-005–OP-013 | BR-006–BR-009, BR-011 | Proposed |
| BC-004 | Currencies/accounts | UC-004-01–UC-004-09; OP-014–OP-025 | BR-008–BR-017, BR-021 | Proposed |
| BC-005 | Transactions/balances | UC-005-01–UC-005-06; OP-026–OP-031 | BR-015–BR-021 | Proposed |
| BC-006 | Templates | UC-006-01–UC-006-04; OP-032–OP-036 | BR-022 | Proposed |
| BC-007 | Report calculation | UC-007-01–UC-007-02; OP-005, OP-009, OP-037 | BR-023–BR-027 | Proposed |
| BC-008 | Saved reports | UC-008-01–UC-008-04; OP-038–OP-040 | BR-025, BR-028 | Proposed |
| BC-009 | Synchronize/configure | UC-009-01–UC-009-05; OP-041–OP-047 | BR-005, BR-029–BR-037 | Proposed |
| BC-010 | Recover | UC-010-01–UC-010-03; OP-043–OP-048 | BR-038–BR-040 | Proposed |
| BC-011 | Expiry replacement | UC-011-01–UC-011-02; OP-044, OP-047, OP-049 | BR-041–BR-042 | Proposed |
| BC-012 | Outcome/logging | UC-012-01–UC-012-02; OP-050–OP-052 | BR-043 | Proposed |

## Detailed Use Cases

All cases inherit ROLE-001 and their numbered parent BC. Normal local actions require an installed usable local copy; pending recovery/confirmed expiration blocks business access. Editing is blocked during sync/wait. Setup requires internet. Manual network actions allow any connection; automatic start requires Wi-Fi. These are shared preconditions, not extra login requirements.

Each row is a compact manual-test scenario specification: main flow states the action and expected result; alternatives state rejection/exception outcomes. Concrete test data will be chosen during verification. Reading these cases does not authorize implementation. Each listed operation appears in API/BFF Service Contracts. Operation names there resolve the IDs here.

| UC | Name / main flow and expected result | Alternative flows / expected result | API/BFF operations |
| --- | --- | --- | --- |
| UC-001-01 | Select startup mode: existing LocalDb opens; absent LocalDb shows creating mode. | Recovery pending blocks business access; existence check is not a full validation scan. | OP-001 |
| UC-001-02 | Check master: enable Create only after confirmed absence. | Existing master or unknown result keeps Create disabled; connection error displayed. | OP-002 |
| UC-001-03 | Create books: credentials/base currency, register device, five roots/base rate 1, create one base-currency rebalancing account in Account root and select it in System configuration, download local copy. | Missing credentials rejected; failed download means no normal access, Create disabled if master exists; manual Open retry. | OP-003, OP-004 |
| UC-002-01 | Open existing local books without internet/sign-in. | Pending recovery/confirmed expiry follow their dedicated flows. | OP-001 |
| UC-002-02 | Setup Open: authenticate existing owner, register and obtain complete LocalDb; remember authorization. | Invalid credentials or download failure shows error; retry manually, no bookkeeping without LocalDb. | OP-004 |
| UC-002-03 | After reinstall, register a new local copy. | Old registration remains under normal 90-day expiry, not reused. | OP-004 |
| UC-003-01 | Merge category groups: choose source/destination; move source elements/children, delete source, show updated tree. Same behavior applies to other group types. | Descendant destination/root source rejected; parent/ancestor/root destination permitted; incoming unique-name collisions repeatedly append `_1`. Post-merge highlighted selection is not prescribed by BRD. | OP-005, OP-007 |
| UC-003-02 | View a same-type tree including root and direct root elements. | Root not duplicated as its own child. | OP-005 |
| UC-003-03 | Create/edit non-root group; trim and validate Name/Description; save. | Blank/duplicate sibling name or root edit rejected; Description optional. | OP-005, OP-006 |
| UC-003-04 | Move non-root group and display new hierarchy. | Cross-type/cycle move rejected; individual forbidden name collision rejected. | OP-005, OP-006 |
| UC-003-05 | Delete empty non-root group; refresh tree. | Root/nonempty group deletion rejected. | OP-008 |
| UC-003-06 | Create/edit/move/delete Category, Project or Correspondent. | Duplicate name in group, blank Name or deletion while account references it rejected; renaming leaves Account Name unchanged. | OP-009–OP-013 |
| UC-004-01 | Load regional currency choices, one entry per ISO, retaining first Name/Symbol. | Skip neutral cultures/invalid regions. | OP-014 |
| UC-004-02 | Create currency with initial rate; base unchanged. | Duplicate ISO/nonpositive rounded rate rejected. | OP-015, OP-016 |
| UC-004-03 | Edit currency Name/Symbol/Description or restore Name/Symbol defaults. | ISO immutable; region default source is the same deduplicated catalog. | OP-014–OP-016 |
| UC-004-04 | Delete unused non-base currency. | Base/used currency deletion rejected. | OP-018 |
| UC-004-05 | View/add/edit/delete ordinary dated rates; edit initial rate value. | Duplicate calendar date, pre-minimum ordinary date, nonpositive rate or deleting/changing hidden initial date rejected; base rate fixed 1. | OP-017, OP-019, OP-020 |
| UC-004-06 | Create account with currency/group, optional classifications and generated or edited name. | Missing required values rejected; duplicate Account Name allowed; missing classification slots keep separators. | OP-021, OP-022, OP-025 |
| UC-004-07 | Edit/move account or reclassify it; future reports use current classification. | Currency immutable after the first successful account save; classification changes do not rename account. | OP-021, OP-022 |
| UC-004-08 | Delete unreferenced account. | Any transaction/template use still referenced blocks deletion even at zero balance. | OP-023 |
| UC-004-09 | Restore Account Name using current Local format/classification names. | No bulk rename; all absent produces separator twice. Existing names untouched unless explicitly saved. | OP-024, OP-022 |
| UC-005-01 | Enter balanced transaction with at least two accounts; save activates automatically. | Blank Amount is zero; repeated accounts/zero entries allowed; invalid time/rate/missing account/too few entries rejected; overflow shows error without changing existing data. | OP-026, OP-027 |
| UC-005-02 | Save new unbalanced transaction as draft; exclude from calculations. | Same minimum entries/accounts/rate rules apply to drafts. | OP-027 |
| UC-005-03 | Edit active transaction and save valid result. | Invalid active save or return to draft rejected. | OP-026, OP-027 |
| UC-005-04 | Delete active transaction; recalculated totals omit it. | Dependency/concurrency failures do not partially save; mechanisms proposed under TQ-02. | OP-028 |
| UC-005-05 | Duplicate transaction values with time now; review/save. | Cancel saves nothing; date change does not replace copied rates. | OP-029, OP-027 |
| UC-005-06 | Restore applicable rate or calculate account balances on demand. | Base rate stays 1; changing date alone never rewrites stored rates. | OP-030, OP-031 |
| UC-006-01 | Create/edit empty, single-entry or unbalanced template with unique Name and optional Description; Description is visible and independently editable. | Every existing entry needs account; account currency was already fixed at account creation. | OP-032, OP-033 |
| UC-006-02 | Apply template into transaction editor with applicable rates/Description. | Save disabled until at least two account-bearing entries; user adds entries or cancels. | OP-034, OP-027 |
| UC-006-03 | Create template from active transaction or draft, copying accounts/amounts/Description. | Target group/name must satisfy template rules. | OP-036, OP-033 |
| UC-006-04 | Delete template through its normal lifecycle. | Delete capability is a proposed interpretation of template maintenance; exact unsaved-editor behavior remains TQ-10. | OP-035 |
| UC-007-01 | Select report classifications and run on-demand signed totals with dates/grouping. | Empty selection in any dimension produces empty result/warning; drafts excluded; mixed-currency columns obey BR-027. | OP-005, OP-009, OP-037 |
| UC-007-02 | Check/uncheck groups and override descendants; partial groups display −. | Group toggle clears descendant editor overrides; no saved JSON rewrite until Save. | OP-005, OP-009, OP-037 |
| UC-008-01 | Save report instructions/name; default `yyyy-MM-dd HH:mm:ss`; duplicate names allowed. | Empty/whitespace-only name rejected. | OP-039 |
| UC-008-02 | Reopen saved report on actual hierarchy; apply explicit choices, even redundant after moves. | Ignore missing identities; reapply if they return; never rewrite saved JSON just by reading/running. | OP-038, OP-037 |
| UC-008-03 | Rename/edit/resave report; recalculate minimal include/exclude IDs from current choices only on user save. | Changing hierarchy alone must not trigger this recalculation. | OP-038, OP-039 |
| UC-008-04 | Delete saved instructions only. | Accounts/transactions untouched; calculated results were not persistent business data. | OP-040 |
| UC-009-01 | Read/edit System or Local settings; System synchronizes, Local stays per copy. | Base currency/APr/RPr immutable after creation; display follows APr/RPr; local naming order six permutations/default Correspondent-Category-Project, separator `/`, one non-whitespace character; one sync trigger only. | OP-041, OP-042 |
| UC-009-02 | Manual sync with remembered authorization on any connection. | Queue/wait message and editing block; Cancel allowed; authentication mechanism TQ-03. | OP-043–OP-047 |
| UC-009-03 | Automatic on-start/on-exit sync starts only on Wi-Fi. | Wi-Fi-started transfer may continue over mobile; startup failure permits ordinary offline work except recovery; exit failure permits closure. | OP-043–OP-047 |
| UC-009-04 | Resolve whole-item conflicts by priority, validity overriding; restore dependencies, deduplicate currencies and rename conflicts. | Unresolvable valid-result failure explains/logs; no weakened accounting/currency-immutability rules. No content-field merge; catalog ordering follows its separate contract. | OP-043, OP-045, OP-052 |
| UC-009-05 | No-change sync renews registration without business transfer; any Content or Order bit means local changes exist. | Master changed means download even when local unchanged. | OP-043, OP-044, OP-047 |
| UC-010-01 | Resolve uncertain operation outcome before replay/discard. | Unreachable retains pending state; business access blocked; never duplicate published batch. | OP-044, OP-048 |
| UC-010-02 | Recover latest published master; install safely and confirm actual snapshot version. | Interrupted download starts over; late newer master does not make one in-progress snapshot inconsistent. | OP-046–OP-048 |
| UC-010-03 | Cancel recovery or lose communication. | Pending state/access block persists; published master never rolled back; expiry overrides. | OP-044, OP-045, OP-048 |
| UC-011-01 | On confirmed 90-day expiry delete old copy/unsynced work and offer Download. | No viewing/export/preservation; preserve Local configuration; no automatic fresh download. | OP-044, OP-049 |
| UC-011-02 | User selects Download for fresh registered local data. | Any connection allowed; download failure leaves no usable copy. | OP-049, OP-047 |
| UC-012-01 | View latest session sync report with received change counts/conflicts/errors. | Omit zeros/uploads; new attempt replaces report; app close discards report. | OP-050 |
| UC-012-02 | Write detailed failure logs in log folder; clean after 7 days. | No Share/Export capability selected; cleanup execution mechanism TQ-08. | OP-051, OP-052 |

## Persistent Model Schemas

### Schema decision status and shared contracts

Except for explicitly confirmed contracts below, type/nullability/identity choices in this section remain **proposed**. Sources authorize the business concepts, not these exact storage fields. `Required` means non-null; `Nullable` allows null. Arrays can be empty unless a stated rule says otherwise. `enum` values and `object` fields are explicitly described below. Creation-time APr/RPr define decimal scales; SQLite scaled-integer representation is confirmed below; calculation bounds and overflow details remain TQ-01.

Confirmed identity for persistent business entities (PM-001–PM-010): `Id: uuid / Required` (GUID). Except for the five fixed root identities specified in PM-001, generate identity at creation, including offline Local creation, and preserve it through edits, moves, synchronization, deletion and restoration. Parent/foreign-key references use the same GUID identities. Independent creations on different Locals have distinct identities; no auto-increment identity allocation or routine synchronization-time ID renumbering is used. This also covers TransactionEntry and TemplateEntry identities. The confirmed sync contract applies to independently synchronized entities (PM-001–PM-006, PM-008, PM-010 and System configuration PM-011). PM-007 and PM-009 retain identity but have no independent tracking fields: entry changes mark the parent Transaction or Template as modified.

#### Favorite flags — confirmed 2026-09-28

PM-001, PM-002, PM-003, PM-005 and PM-008 include required persisted IsFavorite: bool for each group, element or currency, defaulting to false on creation. Users can mark/unmark favorites on non-root groups, elements and currencies and filter by that flag in the first release. All five root groups have IsFavorite fixed to false; UI must not offer a favorite toggle for them, and save/sync validation must preserve this invariant regardless of conflict priority. Changing IsFavorite adds ModificationType.Content, preserves any Order bit, and uses the shared EditRevision content-conflict rules, including the same-Local priority exception. IsFavorite does not use the special Local-wins Order policy. Template favorites participate in whole-template content synchronization. No inheritance/cascade to descendants is selected. Favorites filtering retains the ancestor path to matching favorite groups/elements, including non-favorite parents and the root as needed. These ancestors are navigation context only, not favorite matches; no flags or sync metadata change merely because the path is displayed. Opening a favorite group does not bypass the active favorites filter: show only favorite descendants, plus non-favorite ancestor groups needed to reach them. Other non-favorite children remain hidden; favorite status is not inherited.

Catalog DTO projections/mutations must support IsFavorite and favorite filtering; exact read/write shapes and operation selection remain TQ-05. Favorites filtering does not change persisted collection Order or the separate subgroup/element sequence rules.

#### Catalog ordering — separate synchronization contract

Confirmed 2026-09-27, extended 2026-09-28: ordering of child groups (PM-001) and grouped elements (PM-002, PM-005, PM-008) is shared data, synchronized separately from entity content. Each ordered catalog entity has a required persisted Order field of signed 32-bit integer type (int32). Each parent has two independent sequences: Children (direct subgroups) and Elements (direct elements). The user does not see a mixed subgroup/element list. Normalize Children to 0..G-1 and Elements to 0..E-1, where G and E are their respective accepted member counts. No shared index space or cross-collection tie exists; both sequences can start at 0. Empty collections have no Order values to assign. The self-parent root is excluded from Children and its numbering. Apply the merge, priority, append and GUID tie-break rules below independently to each collection. TransactionEntry/TemplateEntry Position remains part of its parent aggregate under PM-007/009, not this contract. Currency (PM-003) also supports manual ordering under this same separate synchronization contract. The currency catalog is one independent sequence per business dataset: required persisted Order: int32, normalized to 0..C-1 for C accepted currencies. Apply the same priority-relative order, GUID ascending tie-breaks, append-missing rule and post-merge normalization. Currency ordering changes do not modify currency content or its content revision or Content flag. This decision does not apply manual ordering to CurrencyRate records.

For each collection, first resolve content, accepted membership and deletions. Content uses EditRevision and the Content bit with the shared conflict-priority rules, including the same-Local exception; ordering is handled independently. A Local member with the Order bit set supplies its local Order regardless of the configured content priority. Without that bit, retain the accepted Master order. No OrderRevision or separate IsOrderModified boolean is selected; ModificationType carries the per-member Order flag. Ordering alone must not overwrite content or increment EditRevision.

After membership/content reconciliation, select each accepted member Order independently: use Local Order when its Order bit is set; otherwise retain accepted Master Order. Sort the merged values by Order ascending, then Id (GUID) ascending, and assign consecutive 0..N-1 positions. Concurrent changes may therefore shift the intended local or master placement; this outcome is explicitly accepted, and no exact drag-and-drop intent replay is required. Accepted members absent from the selected ordering sequence are appended in GUID ascending order before final renumbering. Never restore deleted/rejected members merely because they appear in an order list. Apply this procedure independently to each subgroup, element and currency collection. The previous unconditional configured-priority collection selection is superseded. Confirmed GUID comparison: represent each Id as a canonical lowercase GUID string in 8-4-4-4-12 hexadecimal format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx, without braces), then compare strings ordinally in ascending order. Master and every Local use this same rule for equal-Order tie-breaks and ordering appended members; comparison is culture-independent. This defines comparison only, not physical GUID storage or a change of identity.

Local flag rules (confirmed 2026-09-28):

- New ordered catalog member: Content | Order.
- Content edit: add Content without clearing an existing Order bit.
- Reorder: add Order on every member whose numeric Order changes, without adding Content for ordering alone.
- Move to another group: add Content | Order on the moved member; normalize source/destination collections and add Order to siblings whose positions change.
- Delete: apply the existing deletion/content rules to the deleted member; immediately normalize the surviving local collection and add Order to every shifted member. Only a never-submitted creation with EditRevision null is eligible for immediate local hard deletion under the existing rule.
- An existing Content bit survives all order-only operations. Captured bits clear atomically with durable saving of their outgoing delta batch. Later local edits set the appropriate bits again. Uncertain/failed synchronization preserves the saved batches and any newer uncaptured flags.

These flags are per entity, not per collection. TransactionEntry/TemplateEntry Position remains part of parent aggregate content: changing entry positions marks the parent Content, not independent entry Order flags. Unordered tracked entities use Content only. IsModified is no longer stored; if a convenience indicator is needed, ModificationType != None indicates changes not yet captured in a batch, not all pending synchronization work.

Confirmed change-detection rule (updated 2026-09-30): synchronization is pending when at least one durable outgoing batch exists OR any tracked entity has ModificationType != None. Flags represent local changes not yet captured in a batch; queued batches remain pending even when all entity flags are None. Content alone, Order alone, and Content | Order all qualify. Include pending ordering changes in synchronization even when no Content bits are set; an order-only edit cannot take the no-local-change path. Transfer only the applicable change semantics: an Order bit does not authorize overwriting content. Wire shape and detection implementation remain TQ-04/TQ-12. Normalization during accepted snapshot preparation is not a new local edit. Master prepares changes on a copy before publication. Local batch capture updates tracking metadata atomically before sending, as specified below; it does not imply Master acceptance. Separate order synchronization remains an explicit exception to previous whole-item wording; BRD alignment must be reviewed before full TRD approval, and BRD is unchanged here.

#### Confirmed concrete-entity table layout — 2026-09-27

Each concrete persistent business entity has its own table containing its applicable persistent fields, including inherited fields. Shared base models provide model reuse only; they have no tables or separate base rows. An entity is not split across base and derived tables, and no Kind discriminator combines distinct entity types into one table.

PM-001 expands to five separate tables for AccountGroup, CategoryGroup, CorrespondentGroup, ProjectGroup and TemplateGroup. PM-002 expands to separate Category, Correspondent and Project tables. Account, Currency, CurrencyRate, Transaction, TransactionEntry, Template, TemplateEntry and SavedReport likewise each have their own table. Names here identify entity types; exact physical table naming is not selected. TransactionEntry and TemplateEntry remain owned by their parent for synchronization even though they have separate tables; they do not gain independent sync fields.

This decision describes one table per concrete entity with its own complete field set, not joined base/derived table inheritance. Scalar storage details and ORM configuration remain outside this decision.

#### Confirmed synchronization tracking contract — 2026-09-27

`ITrackedEntity` represents synchronization metadata, not general audit history. This contract replaces the former shared `Revision`, `CreatedAt`, `EditedAt`, `DeletedAt` proposal and legacy `Original`, `Current`, `IsDeleted` approach. Separate audit fields are not selected by this decision.

MasterDb and LocalDb have identical business database schemas, including redundant modification flags on Master. Row contents differ. Admin.db remains separate and is not downloaded.

| Property | Canonical type | Nullability | Confirmed meaning and authority |
| --- | --- | --- | --- |
| EditRevision | int64 | Nullable | Revision of the last accepted delta batch that changed this entity content; null for a new, never-submitted local creation; 0 for a creation prepared for submission whose Master revision is unknown; positive for a known Master-assigned revision. Master allocates one global revision per newly accepted SyncKey within the dataset and assigns it to all entities whose accepted content changes in that batch, including creation/deletion/restoration. Unchanged, rejected and Order-only entities retain their previous EditRevision. Local may change null to the sentinel 0 when durably preparing the creation batch before sending; Local never generates or increments positive revisions. Installation copies the Master value. Master never stores 0 as an accepted entity revision. |
| DeleteRevision | int64 | Nullable | Null = alive, settable on both Local and Master; 0 = pending soft deletion, settable only by Local; >= 1 = Revision of the snapshot accepting deletion, assigned only by Master. Local may copy Master-assigned positive values during synchronization but never generates them. No negative values, separate deletion flag or datetime. |
| ModificationType | flags enum | Required | None = 0, Content = 1, Order = 2; Content and Order combine as 3. Replaces IsModified. Local accumulates uncaptured change bits per tracked entity. Capturing those changes in a durable outgoing batch clears the captured bits in the same local transaction; later edits set bits again. Saved batches remain pending until successful installation of their accepted result. Present but redundant as local-change metadata on Master; downloaded bits are not authoritative. |

Signed 64-bit integer (`int64`) width is confirmed for EditRevision, DeleteRevision and all snapshot-version references, including AcknowledgedVersion. Scalar storage mapping remains TQ-02/TQ-04. **Revision** means the positive int64 identity of a Master database snapshot within MasterDatasetKey. Each newly accepted delta batch produces one snapshot and one Revision; merge revision and snapshot version are the same counter, not separate counters or timestamps. EditRevision and DeleteRevision both reference this counter. Existing fields named Version, PublishedVersion, InstalledVersion and AcknowledgedVersion refer to this same snapshot Revision; their names are retained here. An entity stores the Revision of its last accepted content change, not an entity-specific counter.

Assignment authority: Local initializes EditRevision to null for a new entity and sets it to 0 when durably preparing its first creation batch. Zero means that entity may exist on Master but its accepted Revision is unknown; it does not mean Master itself is unavailable. An ordinary edit to an entity with a known positive EditRevision preserves that value. Only Master assigns positive EditRevision values; Local copies them during installation. Accepted Master entities always have EditRevision >= 1. EditRevision must never be negative. DeleteRevision null is valid on both sides; only Local originates 0 and only Master originates a positive deletion Revision. Accepting deletion at Revision R sets both EditRevision and DeleteRevision to R. Accepting restoration at Revision S sets EditRevision to S and DeleteRevision to null.

| Activity | EditRevision | DeleteRevision | ModificationType / outcome |
| --- | --- | --- | --- |
| Create locally | null | null | Content; also Order for a new ordered catalog member |
| Edit content locally | Keep received revision | null for live entity | Add Content; preserve existing Order bit |
| Delete locally after prior Master acceptance | Keep received revision | 0 | Add Content; normalize survivors under ordering rules |
| Delete locally before creation is prepared for submission | null | No marker retained | Immediately hard-delete locally subject to reference rules; no outgoing creation/deletion |
| Prepare new creation batch before sending | Set null to 0 | null | Save the immutable creation batch and local sentinel atomically before network transmission |
| Edit submitted creation with unknown Master outcome | Keep 0 | null | Add Content; queue the edit in a later batch, not as a new creation |
| Delete submitted creation with unknown Master outcome | Keep 0 | 0 | Soft-delete and queue deletion in a later batch; never immediately hard-delete |
| Install accepted live state | Copy Master revision | null | None |
| Install accepted deletion | Revision retained on Master marker | Positive on Master marker | Hard-delete locally after accepted outcome and successful installation |

Immediate deletion of a never-submitted local creation remains subject to existing business deletion/reference rules. Preparing its first immutable outgoing batch and changing its local EditRevision from null to 0 must be one durable local transaction completed before sending. The saved first batch retains creation semantics; changing the live local row to 0 must not turn that batch into an edit. Keep 0 after timeout, cancellation or failed transmission, including when Master might never have received the request. Later batches contain edits or soft deletion of the same identity and are processed after the creation batch; Master skips that earlier batch if already accepted. A revision of 0 means possibly present on Master, not safe to hard-delete. Successful installation replaces 0 with the accepted positive Master revision, or removes the row after accepted deletion. No return from 0 to null is authorized by an uncertain outcome.

For an existing entity, when the Content bit is set and the revision differs from Master, this indicates a content conflict. Confirmed same-Local priority exception (2026-09-30): resolve the entity current Master EditRevision through the accepted-batch record, scoped to MasterDatasetKey. If that revision was produced by the incoming batch LocalDatasetKey, incoming content wins regardless of configured priority; otherwise use configured conflict priority. Compare the source of the current entity revision, not the latest dataset-wide revision or merely any earlier batch from that Local. Apply the exception to content edits, deletion and restoration, subject to business validity. Already accepted SyncKeys are skipped before conflict resolution. An accepted incoming content change receives the new batch revision. For example, K1 from Local1 produces B revision 16; K2 from Local1 still based on revision 15 wins over revision 16. If Local2 has since produced B revision 17, K2 uses configured priority instead. Matching revision allows the local change subject to validation. An absent Content bit means receive Master content without a content conflict, even if Order is set and revisions differ. Null revision in the saved creation batch identifies creation; 0 in a subsequent batch identifies an edit/deletion whose accepted Master revision is not yet known. Apply preceding batches first and then the shared content-conflict rules, including the same-Local exception. Unrelated creation identity-collision handling remains TQ-04. Priority selects whole entity content, excluding catalog Order governed by the separate ordering contract; there is no general content-field merge; business validity overrides priority (BR-029–BR-033). Keeping unchanged Master state does not increase revision. A winning local change that changes accepted state receives the newly allocated batch revision, not the previous entity revision plus one.

A winning restoration clears DeleteRevision and receives the accepted batch revision. A Local that previously hard-deleted the entity inserts the restored row with the same Id, accepted revision and ModificationType None; it is not a new local creation. Failed/uncertain sync preserves pending non-expired changes until its outcome is resolved. SyncKey/outcome recovery remains mandatory.

#### Durable batch capture and modification flags — confirmed 2026-09-30

Save an immutable outgoing delta batch and clear exactly the ModificationType flags captured by that batch in one local transaction. Capture both the values and applicable Content/Order semantics. If capture fails, neither the batch nor its flag clearing is committed. For first submission of a creation, the same transaction changes the live row EditRevision from null to 0 while preserving creation semantics in the batch. Flags set by subsequent edits belong to a later batch; do not clear them as acknowledgement of an earlier batch.

A failed or uncertain network attempt retains the saved batch; do not rebuild it from current entity values or restore its flags merely to retry. Pending synchronization means queued batches exist OR uncaptured flags exist. The no-local-change path is permitted only when neither exists. Saving a batch is not sync success: retain it until successful installation of the accepted result. Exact queue storage and atomic installation/queue removal remain TQ-04.

#### Global merge revision allocation — confirmed 2026-09-30

For each newly accepted delta batch identified by SyncKey, Master allocates the next monotonically increasing int64 merge revision within that MasterDatasetKey. Several new batches in one HTTP request produce separate snapshots with consecutive Revisions in processing order; Local downloads only the final resulting snapshot. Replaying an already accepted SyncKey reuses its recorded outcome and revision; it neither allocates a new Revision, produces another snapshot nor reapplies changes. Every entity whose content changes in a batch receives that same revision. For example, with current merge revision 15, a batch creating A, editing B and deleting C assigns EditRevision 16 to all three; unchanged entities retain earlier values. A newly accepted batch with no content changes still produces its snapshot Revision, but changes no entity EditRevision.

The accepted batch outcome records the association between its merge revision, LocalDatasetKey and SyncKey, scoped to MasterDatasetKey. Entity revision equality remains the basis for detecting stale content. These records must remain available while needed to resolve the source of a current entity EditRevision and to recognize retried SyncKeys. Exact storage, safe retention/compaction and allocation/publication atomicity remain TQ-04. The confirmed same-Local exception above uses this association; BRD priority wording requires alignment before full approval. No LastContentLocalDatasetKey property on every entity is required by this revision decision.

#### Confirmed snapshot versions and deletion acknowledgement

Published Master snapshots have positive int64 Version, starting at 1. A new snapshot gets current published Master Version + 1, never Max(AcknowledgedVersion) + 1. GUID filenames can remain; Version is the ordered snapshot identity. Serialized allocation/publication remains TQ-04.

Each LocalDb stores AcknowledgedVersion; Master administration stores it per registered LocalDb (PM-014). It means the snapshot successfully installed, not merely sent/published. Local durably records successful installation; Master advances the registration only after receipt confirming that actual Version. Lost receipts can be retried without moving acknowledgements backwards. AcknowledgedVersion is non-null, initially 0 on Local and in its Master registration before the first successful installation/receipt respectively. Zero means no snapshot acknowledged; published snapshot versions start at 1. A failed initial download leaves the value at 0.

Master accepts deletion by assigning the accepted batch revision and setting DeleteRevision to the snapshot Version publishing that deletion. Local hard-deletes the accepted deleted row during installation. Master retains its deletion marker until every remaining non-expired registration has AcknowledgedVersion >= DeleteRevision; then a subsequent snapshot may omit the marker. Download failure/missing receipt does not satisfy this condition. Winning restoration clears the marker and cancels deletion eligibility.

Example: X with EditRevision 1 is deleted in snapshot Revision 10, receiving EditRevision 10 and DeleteRevision 10. Local1/Local2 acknowledge 10; Local3 acknowledges 8, so Master retains X. After Local3 acknowledges 10 or later, Master may remove the marker. If Local3's valid stale edit wins first, X instead becomes live at the Revision of the snapshot accepting restoration (for example 11), with DeleteRevision null, and is restored to other Locals with its original identity.

These are tracking semantics, not a complete wire protocol or local optimistic-edit token. EditRevision does not change between local edits and cannot alone detect concurrent local edits; that mechanism remains TQ-02.


Shared text-field semantics (confirmed 2026-09-28): Name is mandatory for named entities under their existing rules. Description is optional, visible and independently editable; absent or empty Description does not block saving. Remove Comment fields throughout. Rates and transactions use Description without acquiring a Name field. Template.Description is visible, is not overwritten from Name, and is copied to Transaction.Description when applied. Creation of a template from a transaction and duplication of a transaction copy Description. No entry-level description is introduced.

#### Precision and user-requested balancing — confirmed 2026-09-28

Base currency, APr and RPr are selected at dataset creation and stored in SystemConfiguration. They cannot be changed later through settings or synchronization. Amounts (including template amounts) use APr; stored entry and currency-catalog rates use RPr. Derived BaseAmount rounds Amount × Rate to APr using midpoint-to-even, then transaction validation sums the rounded BaseAmounts. Display amounts and BaseAmount with APr and rates with RPr; there is no separate DisplayDecimalPlaces setting. SQLite stores persisted Amount and Rate values as signed 64-bit INTEGER scaled by 10,000, independent of the configured APr/RPr. Normalize input to its configured precision first, then multiply by 10,000 exactly and validate the integer range before storage. Read by dividing by 10,000 using decimal arithmetic. Example: Amount 12.34 with APr=2 stores 123400; Rate 1.2345 with RPr=4 stores 12345. Domain values and calculations remain decimal, not floating-point; BaseAmount remains derived and is not stored. The representable storage interval is -922337203685477.5808 through 922337203685477.5807, further restricted to the configured precision and positive-rate rule. This storage range does not guarantee products or totals fit; overflow handling remains required.

For any nonzero current total, the editor offers Add balancing entry. On an explicit click, append an entry using the configured account whose currency is the base currency, Amount = -total, Rate = 1 and derived BaseAmount = -total. Recalculate the whole transaction and require exactly zero for confirmation; all other save rules still apply. No tolerance or maximum-difference threshold is imposed. The user can alternatively add this entry manually. Appending edits the unsaved transaction; the normal save action persists it. Subsequent edits can create a new imbalance and must be revalidated. No entry is added silently.

The balancing-account selection is stored in synchronized SystemConfiguration (PM-011), shared across Master and all Local copies. It references an Account whose currency is the dataset base currency. Initialization creates one Account initially named Rebalancing in the Account root group with the dataset base currency and sets BalancingAccountId to its Id. It is an ordinary account: transaction/template references still prevent deletion, but this settings selection alone does not. The user may change the selection in Settings to another base-currency account. If the configured account is absent or soft-deleted, Add balancing entry must leave the transaction unchanged and show: "Cannot add a balancing entry: the rebalancing account is missing. Please choose a rebalancing account in Settings." Manual balancing remains available. Do not recreate the account automatically. BalancingAccountId is a nullable GUID property of SystemConfiguration. When the selected account is deleted, clear BalancingAccountId to null. A null selection uses the same missing-account message and leaves the transaction unchanged. Base currency/APr/RPr immutability does not apply to this selection. APr and RPr each accept integers 0–4 inclusive, with defaults APr = 2 and RPr = 4. Creation rejects values outside this range. Display precision follows APr/RPr as defined above. DTO-016 creation/configuration DTOs and balancing-action contracts require alignment under TQ-05; existing DTO omissions cannot override these confirmed requirements.

### PM-001 — Group

Purpose: five same-type hierarchies, BR-006–BR-009. Confirmed model approach: AccountGroup, CategoryGroup, CorrespondentGroup, ProjectGroup and TemplateGroup are separate derived models sharing a common group base. Equal current field sets do not collapse them into one model with a Kind enum. PM-001 describes their shared contract, not an instantiable generic group record. Each model has parent/children of its own group type and elements of its corresponding element type. Shared identifier above. The confirmed concrete-entity table layout applies.

| Property | Canonical type | Nullability | Constraints / relationship | Source/status |
| --- | --- | --- | --- | --- |
| ParentId | uuid | Required | One same-type parent; root self-parent | C retained; type proposed |
| Name | string | Required | Trim/nonblank; sibling uniqueness; fixed root label | BR-006/008; proposal |
| Description | string | Nullable | Optional; root immutability still applies | BR-009; confirmed |

Confirmed root identities (2026-09-27): initialize exactly these five root groups when creating the business database. The IDs are hardcoded, never generated per device or per database, and remain identical across Master and Local copies. Each root has ParentId equal to its own Id and is excluded from its own Children. Existing root immutability rules remain binding. These persistent identity constants must not change for existing databases without migration.

| Root group | Fixed Id (GUID) |
| --- | --- |
| AccountGroup | 22D0BBCC-37EC-4AF4-B5C7-9CAF34FEC1E9 |
| CategoryGroup | CDB033F6-8686-4222-B33F-66B5A3BF2948 |
| CorrespondentGroup | 9C19A1FE-9C57-4703-9105-79075987EE45 |
| ProjectGroup | 7C9385F6-28F2-4947-8B44-E81DD8D01949 |
| TemplateGroup | B232A84F-47D8-426E-AA54-BA8771B8B6DE |

Source values: [RootsIds.cs](../../backend/Business.Models/Constants/RootsIds.cs), confirmed by the requesting user. IsRoot is a derived boolean: true exactly when a group Id equals the fixed root Id for that group type. It is not a persistent field and is not independently editable or synchronized. DTO projections may expose this read-only derived value. An extension method is an implementation option, not a required technical contract.

Relationships: parent reference, child groups excluding self, type-matching elements. Lifecycle: non-root create/edit/move/merge/delete; protected root; source removed after merge. Concurrency/sync tracking: shared contract; merge transaction scope TQ-02.

### PM-002 — Classification

Purpose: distinct Category, Project, Correspondent concepts (BR-010). Confirmed model approach: Category, Correspondent and Project are separate derived element models sharing a common element base, even though their current field sets are equal. Each belongs to its corresponding group type. PM-002 describes their shared contract, not a single classification model distinguished by a Kind enum. Account and Template retain their specialized contracts in PM-005/PM-008. Shared Id/sync tracking. New model types can be introduced by derivation rather than expanding a Kind enum; no additional business type is authorized here. The confirmed concrete-entity table layout applies.

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| GroupId | uuid | Required | Corresponding derived group in PM-001; many elements to one group | BR-006; proposed |
| Name | string | Required | Nonblank trimmed; unique within group, case-insensitive | BR-008; proposed |
| Description | string | Nullable | Optional and independent of Name | BR-009; confirmed |

Lifecycle: create/edit/move; delete only unused by accounts; no automatic account rename. Concurrency/sync tracking shared.

### PM-003 — Currency

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| Code | string | Required | ISO code unique, immutable | BR-013; proposed |
| Name | string | Required | Catalog English default; editable | BR-013; proposed |
| Symbol | string | Required | First catalog symbol; editable | BR-013; proposed |
| Description | string | Nullable | Optional | BR-013; proposed |

Shared Id/sync tracking. Many PM-004 rates per currency; business dataset's base currency identifies one PM-003. Delete only unused/non-base. Base selection immutable. Name/Symbol blank policy not independently specified; proposed required type does not invent nonblank validation. Concurrency/sync tracking shared.

### PM-004 — CurrencyRate

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| CurrencyId | uuid | Required | One PM-003 | BR-014; proposed |
| EffectiveDate | date | Required | DateOnly calendar date with no timezone conversion; unique ordinary date per currency; initial date fixed at 1970-01-01 by shared constant | BR-014/021; proposed |
| Rate | decimal | Required | Scale RPr, positive after rounding; base = 1 | BR-014/017; proposed type, confirmed calculation |
| Description | string | Nullable | Optional notes on this rate, including initial rate; independent of Currency.Description and transaction/template descriptions | BR-009/014; confirmed 2026-09-28 |

Confirmed initial-rate representation (2026-09-28): one shared application constant defines the DateOnly value 1970-01-01 for the initial rate. Use the same value on Master and all Locals; do not duplicate date literals or expose it as an editable setting. IsInitial is derived from EffectiveDate equaling this constant, not persisted as a boolean. Exactly one initial fallback rate exists per currency; its date is hidden and immutable, its row cannot be deleted, and its value remains editable subject to the base-currency rate of 1. Ordinary rates remain dated 2001-01-01 or later. The constant declaration name and source location are implementation details.

Shared Id/sync tracking; edit initial value but not its date; ordinary date rate lifecycle under BR-014. No stored link from transaction entry to this row. Concurrency/sync tracking shared.

Confirmed date semantics (2026-09-28): store transaction/backend timestamps as UTC instants. CurrencyRate.EffectiveDate is a date-only value (DateOnly), with no timezone or UTC conversion. For default rate and Restore Rate, convert the transaction timestamp to the current UI/device timezone, take its calendar date, and choose the latest rate on or before that date, with the initial fallback. Applying templates uses the same lookup. Stored entry rates remain independently editable; date or timezone changes never automatically rewrite them.

Report day/week/month/year grouping and From/To calendar dates use the current UI/device timezone. Convert local period boundaries to UTC instants for filtering; use inclusive start and exclusive next-period start. Determine boundaries in that timezone rather than assuming every local day is 24 hours. DateOnly values are interpreted directly as calendar dates, not UTC instants. Existing minimum transaction instant remains 2001-01-01 00:00 UTC; minimum ordinary rate date remains DateOnly 2001-01-01.

Example: transaction January 1 04:00 in UTC+8 is stored December 31 20:00 UTC and uses the rate applicable on January 1. That local January 1 report covers December 31 16:00 UTC inclusive through January 1 16:00 UTC exclusive. UI should make the local transaction date used for the default understandable. The exact DTO mechanism for supplying the device timezone/calendar lookup date to backend operations remains TQ-05; do not use the backend host timezone as a substitute.

### PM-005 — Account

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| GroupId | uuid | Required | Account group PM-001 | BR-006; proposed |
| CurrencyId | uuid | Required | PM-003 | BR-010; proposed |
| CategoryId | uuid | Nullable | Category PM-002 | BR-010; proposed |
| ProjectId | uuid | Nullable | Project PM-002 | BR-010; proposed |
| CorrespondentId | uuid | Nullable | Correspondent PM-002 | BR-010; proposed |
| Name | string | Required | Trim/nonblank; duplicates permitted | BR-008/012; proposed |
| Description | string | Nullable | Optional and independent of Name | BR-009; confirmed |

Confirmed currency lifecycle (2026-09-28): currency may be selected/changed only during unsaved account creation. First successful save fixes it permanently, even without any transaction/template use. Canceling creation persists nothing. No CurrencyLocked property or separate lock flag is stored or exposed; existing-account edits and synchronization must preserve its chosen currency. This does not prohibit identity remapping to the same logical currency during the existing currency-deduplication process.

Shared Id/sync tracking. Delete blocked by any transaction/template reference. Classifications affect history but not stored name. Concurrency/sync tracking shared.

### PM-006 — Transaction

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| OccurredAt | datetime | Required | UTC, minimum 2001-01-01; display device timezone | BR-020/021; proposed type |
| Description | string | Nullable | Optional, aggregate-level | BR-016; proposed |
| State | enum | Required | Undefined = 0 (unset, never persisted); Draft = 1; Planned = 2 (reserved for future); Confirmed = 3. Current persisted states are Draft and Confirmed; rules below. | BR-019; representation confirmed 2026-09-27 |

Confirmed state representation (2026-09-27):

- `Undefined = 0`: unset/uninitialized state, replacing the legacy name NoValid. It is not a valid persisted transaction state; a successful save must determine Draft or Confirmed under the rules below.

- `Draft = 1`: unbalanced transaction, excluded from all balances and reports. Persisted drafts must still have at least two entries, an account on each entry, and valid dates/rates and all other required values. Draft does not permit saving an incomplete or otherwise invalid transaction. An editor with zero or one entry cannot be saved.
- `Planned = 2`: reserved for future valid transactions excluded from accumulated balances and reports. No first-release creation, selection or transition workflow is enabled for this state. This reservation does not add planned-transaction functionality to BRD scope.
- `Confirmed = 3`: valid transaction included in balances and reports. This is the stored representation of BRD's active transaction. Saving a complete balanced transaction automatically produces Confirmed; Confirmed cannot return to Draft and invalid edits are rejected.

Balance means the exact sum of per-entry rounded BaseAmount values is zero. Entry count is the number of entries, not an Amount value or a count of distinct accounts; repeated accounts remain permitted. Values 0, 1, 2 and 3 are explicit; no additional valid persisted state is selected by this decision.

Shared Id/sync tracking. Owns 2+ PM-007 on any persisted save; entry mutation only through whole transaction. Lifecycle create/draft/activate/edit/delete with valid rules; no stored running balances. Concurrency whole aggregate; sync tracking shared.

### PM-007 — TransactionEntry

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| TransactionId | uuid | Required | Exactly one PM-006; retained parent reference must match | Legacy TRD explicit parent relation; type proposed |
| AccountId | uuid | Required | Exactly one PM-005 | BR-016; proposed |
| Amount | decimal | Required | Scale APr; blank input maps to zero | BR-016/017; proposed |
| Rate | decimal | Required | Independent stored value; no CurrencyRate foreign key | BR-015; proposed |
| Position | int32 | Required | Persisted order within the parent Transaction; survives save/load and whole-aggregate sync; repeated accounts remain separate entries | Confirmed 2026-09-27; shared entry-order rule below |

Shared entry-order rule for PM-007 and PM-009 (confirmed 2026-09-27): Position is int32, numbered consecutively 0 through N-1 within its parent. Saving an edited entry collection assigns positions from its intended order after insertion, removal or reordering. Empty templates have no positions to assign. Synchronization preserves the accepted whole aggregate and its positions; it does not merge entry lists or renumber collections merely because synchronization occurred. This rule applies only to TransactionEntry and TemplateEntry, not catalog elements or child groups.

Derived value (confirmed 2026-09-27): BaseAmount = Amount × Rate, rounded per entry to APr decimal places using midpoint-to-even. BaseAmount is calculated when needed, not stored as a persistent column or synchronized independently. Balances, reports and the exact-zero transaction check use these rounded per-entry values. It is read-only and cannot be supplied as an authoritative input.

Shared Id; tracking belongs to parent aggregate; repeat accounts allowed. No independent entry service or sync. Lifecycle follows parent; deletion propagation managed at aggregate boundary.

### PM-008 — Template

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| GroupId | uuid | Required | Template group PM-001 | BR-022; proposed |
| Name | string | Required | Trim/nonblank; unique within group | BR-008/022; proposed |
| Description | string | Nullable | User-visible optional text; copied to transaction Description when applying template | BR-022; confirmed role |

Template has one visible optional Description. No hidden description, separate Comment or automatic Name-to-Description assignment remains.

Shared Id/sync tracking. Owns zero or more PM-009; aggregate concurrency/sync. Applying does not save transaction. Lifecycle maintenance from BR-022; deletion operation remains explicit proposal TQ-10.

### PM-009 — TemplateEntry

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| TemplateId | uuid | Required | Exactly one PM-008; parent reference and identity match | Legacy TRD; type proposed |
| AccountId | uuid | Required | Exactly one PM-005; account currency is already immutable | BR-011/022; proposed |
| Amount | decimal | Required | Scale APr; template need not balance | BR-022; proposed |
| Position | int32 | Required | Persisted order within the parent Template; shared entry-order rule in PM-007 applies; survives save/load and whole-aggregate sync | Confirmed 2026-09-27; shared entry-order rule below |

Shared Id; tracking belongs to template aggregate. Rates are resolved on applying template, not copied as template rate fields. Parent lifecycle/concurrency.

### PM-010 — SavedReport

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| Name | string | Required | Nonblank; default yyyy-MM-dd HH:mm:ss; duplicates allowed | BR-028; proposed |
| DefinitionJson | string | Required | JSON shape defined by DTO-012; instructions only, no result cache | BR-025/028 and user JSON direction; shape proposed |

Shared Id/sync tracking. Referenced group/element identities are soft references, do not protect deletion. Read/run never normalizes persisted instructions; save recalculates minimal override lists from edited actual state. Missing identities retained until explicit save may remove obsolete references. Rename-only handling must preserve definition unless owner saves changed selection; exact mutation shape TQ-05. Concurrency/sync tracking shared.

### PM-011 — SystemConfiguration

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| MasterDatasetKey | string | Required | Generate once during dataset initialization using Guid.ToString(); preserve unchanged in Master and all Local copies. Reject synchronization between different dataset keys. Not the SystemConfiguration primary key. | User naming/type decision and lifecycle confirmed 2026-09-29 |
| BaseCurrencyId | uuid | Required | References the dataset base Currency (PM-003); stored in SystemConfiguration, selected during initialization and immutable afterward | BR-004; type, requiredness and System placement confirmed 2026-09-29 |
| BalancingAccountId | uuid | Nullable | Initially references the generated rebalancing PM-005 in Account root; base-currency account selection shared through System sync; clear to null when the selected account is deleted; selection alone does not prevent deletion | BR-018; System ownership confirmed 2026-09-28; nullable GUID and clearing accepted 2026-09-29 |
| APr | int32 | Required | Amount and rounded BaseAmount fractional places; range 0–4 inclusive, default 2; selected at creation, immutable | BR-017; confirmed |
| RPr | int32 | Required | Rate fractional places; range 0–4 inclusive, default 4; selected at creation, immutable | BR-017; confirmed |
| EditRevision / DeleteRevision / ModificationType | Shared tracking contract | As defined above | Synchronized configuration tracking | Confirmed 2026-09-27 |

Identifier naming convention (2026-09-29): use Id for entity primary keys, {Entity}Id for foreign keys/references between entities, and Key for other identifiers. MasterDatasetKey is a GUID-formatted string using Guid.ToString(), separate from the table primary key. Corresponding dataset identifiers elsewhere in this TRD use the same name and representation; their proposed contracts remain proposed. Other identifier roles and their final names require review before finalizing schemas.

Purpose: synchronized System table; base currency, precision and balancing-account setting placement confirmed; singleton key remains proposed. One per dataset. Lifecycle initialize, edit mutable settings, sync. Shared synchronization contract applies; local concurrency/publication mechanisms remain TQ-02/TQ-04.

### PM-012 — LocalConfiguration

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| LocalDatasetKey | string | Required | Local copy identity generated with Guid.NewGuid().ToString() when registering a new Local copy; stored in LocalConfiguration and registered on Master. Preserve through synchronization database replacement; a fresh installation gets a new key. Not the configuration primary key. | BR-001/005; name, type, placement and lifecycle confirmed 2026-09-29 |
| AcknowledgedVersion | int64 | Required; default 0 | Successfully installed snapshot Version; placement here proposed | Confirmed 2026-09-27 |
| DefaultAccountNameOrder | enum | Required | Stored component order; exactly six values: CorrespondentCategoryProject (default), CorrespondentProjectCategory, CategoryCorrespondentProject, CategoryProjectCorrespondent, ProjectCorrespondentCategory, ProjectCategoryCorrespondent. Each value specifies the corresponding component sequence; numeric assignments remain unresolved. | BR-012; stored setting/name and six-value enum confirmed 2026-09-29 |
| DefaultAccountNameSeparator | string | Required | Stored separator; exactly one non-whitespace character; default /; Unicode counting TQ-01 | BR-012; stored setting/name confirmed 2026-09-29; type proposed |
| SyncTrigger | enum | Required | ManualOnly, OnStart, OnExit; default ManualOnly | BR-034; proposed names |
| ConflictPriority | enum | Required | Master, Local; default Master | BR-029; proposed names |

Confirmed account-name format (2026-09-29): derive the default format from stored DefaultAccountNameOrder and DefaultAccountNameSeparator; do not persist a separate DefaultAccountName format string.

One per local copy; survives replacement, never synchronized. No login/password assumed here. Lifecycle initialize/update locally. Concurrency local settings TQ-02; auditing unresolved. Defaults/format constraints are confirmed business rules; physical columns are proposals.

### PM-013 — OwnerAdministration

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| OwnerId | uuid | Required | Proposed identity; single owner in prototype | BR-001; proposed |
| Login | string | Required | Required only; case/normalization TQ-03 | BR-003; proposed |
| PasswordHash | string | Required | User specified hash, never plaintext; algorithm/encoding/salt TQ-03 | F technical input; representation proposed |
| MasterDatasetKey | string | Required | Owned master | BR-001; proposed |
| ActiveVersion | int64 | Required | Current published PM-016 | S publication; proposed |

Lives in Admin.db, not exported as business snapshot. No first-release change/recovery workflow. Lifecycle first creation; concurrency serialized publication/creation unresolved TQ-04. Audit fields unresolved. No multi-owner roles added.

### PM-014 — LocalRegistration

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| LocalDatasetKey | string | Required | Registered Local copy identity; same value as LocalConfiguration.LocalDatasetKey; lifecycle defined in PM-012 | BR-001; confirmed 2026-09-29 |
| MasterDatasetKey | string | Required | Exactly one owner master | BR-001; proposed |
| LastSuccessfulSyncAt | datetime | Nullable | UTC master clock; absent before completed initial download | BR-041; proposed |
| AcknowledgedVersion | int64 | Required; default 0 | Per-registration installed snapshot Version; advance only after receipt | BR-040; confirmed 2026-09-27 |

Purpose: expiry/deletion acknowledgements. No display name in v1. Lifecycle register before download, acknowledge success, expire/remove. Incomplete registration cleanup TQ-04; concurrency/audit fields unresolved. Placement in Admin.db confirmed by F for device registration.

### PM-015 — SynchronizationState

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| SyncKey | string | Required | Generate with Guid.NewGuid().ToString() for each new synchronization operation. Persist and reuse the same key for retries and recovery of that operation; a new operation gets a new key. Master uses the key to recognize an already applied operation and avoid applying its changes twice. | Name, type and lifecycle confirmed 2026-09-29 |
| LocalDatasetKey | string | Required | Initiating registration | BR-038; proposed |
| PublishedVersion | int64 | Nullable | Outcome if publication completed | BR-038; proposed |
| InstalledVersion | int64 | Nullable | Actual local installed result | BR-040; proposed |
| Stage | enum | Required | Prepared, OutcomeUnknown, Published, Installed, Acknowledged, FailedBeforePublication | BR-038–BR-040; proposed state names |

Logical record covers durable cloud outcome and local pending-install evidence; it is not an instruction to duplicate all columns at both locations. Storage split, retention, atomic transitions and outcome authority TQ-04. Never use equal timestamps alone as proof. Concurrency/sync tracking unresolved. Lifecycle persists sufficiently to resolve retries even after old master file cleanup.

### PM-016 — MasterVersion

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| Version | int64 | Required | Ordered snapshot identity; shared version contract | Confirmed 2026-09-27 |
| MasterDatasetKey | string | Required | Owner dataset | BR-001; proposed |
| FileName | string | Required | Unique version filename, e.g. {Guid}.db | S explicit example; shape proposed |
| PublishedAt | datetime | Required | UTC; separate from conflict priority | S direction; proposed |

Retain active/download-needed versions. Delete superseded versions only when no active transfer/dependency needs them; pending latest-version recovery does not itself retain its original snapshot. No backup/history promise. Publication concurrency, reader leases, audit and cleanup safety TQ-04.

Logs and session reports are not extra synchronized business tables. Logs are files in log folder, seven-day retention; latest sync report is session memory only (BR-043).

## DTO Schemas

Except for references to confirmed int64 snapshot-version/entity-revision semantics, DTO directions, operation assignments, fields, types and nullability below are **proposed**; business validation sources remain binding. DTOs are separate projections, not persistent records or permission to expose administrative/audit fields. No OP uses a PM as input/output. `None` means no payload, not no authorization.

Each property cell uses `name: canonical type`; the following column lists nullability in the same order. This compact schema is exhaustive for this draft; no undeclared fields are implied. `array<object>` items refer to an explicitly named DTO projection or nested object below. DTO fields never expose PasswordHash, cloud file locations or other owners' registration data.

| ID / name / direction | Properties and canonical types | Nullability (same order) | Validation / domain mapping / source | Consuming operations |
| --- | --- | --- | --- | --- |
| DTO-001 Query / request | Id: uuid; ParentId: uuid; From: date; To: date | Nullable; Nullable; Nullable; Nullable | Type-specific query selection remains TQ-05; operation-specific identity/filter; date filters inclusive where applicable. IDs map to selected logical records; BR-006/021/026. | OP-005,009,015,019,021,026,030–032,038 |
| DTO-002 ListResult / response | Items: array<object> | Required | Closed item projection selected by OP row: DTO-004,005,006,007,008,009,011 or013; no mixed/untyped PM exposure. Array empty allowed. | OP-005,009,014,015,019,021,026,032,038 |
| DTO-003 IdentityCommand / request | Id: uuid; ExpectedRevision: int64 | Required; Nullable | Target identity; expected revision proposal TQ-02. Delete/apply behavior supplied by OP. | OP-008,013,018,020,023,024,028,029,034,035,036,040 |
| DTO-004 GroupData / shared | Id: uuid; ParentId: uuid; Name: string; Description: string; IsRoot: bool; EditRevision: int64 | Nullable; Required; Required; Nullable; Required; Nullable | PM-001 projection; Id absent for create. Projection family for the five derived PM-001 types; exact DTO specialization remains TQ-05. IsRoot derived from the type-specific fixed root Id; IsRoot and revision read-only; group reference rules BR-006–009. | OP-005 via DTO-002, OP-006 |
| DTO-005 ClassificationData / shared | Id: uuid; GroupId: uuid; Name: string; Description: string; EditRevision: int64 | Nullable; Required; Required; Nullable; Nullable | Projection family for Category, Project and Correspondent; exact DTO specialization remains TQ-05. Id absent for create; group/name/description BR-008–010. | OP-009 via DTO-002, OP-010–OP-012 |
| DTO-006 CurrencyData / shared | Id: uuid; Code: string; Name: string; Symbol: string; Description: string; InitialRate: decimal; EditRevision: int64 | Nullable; Required; Required; Required; Nullable; Nullable; Nullable | PM-003 and initial PM-004. InitialRate required for create except fixed base 1; omitted for metadata edit; Code immutable. Catalog has absent Id. BR-013/014/017. | OP-014/015 via DTO-002, OP-016 |
| DTO-007 RateData / shared | Id: uuid; CurrencyId: uuid; EffectiveDate: date; Rate: decimal; Description: string; IsInitial: bool; EditRevision: int64 | Nullable; Required; Nullable; Required; Nullable; Required; Nullable | PM-004; optional Description describes this rate only; hide initial sentinel by absent EffectiveDate on initial-rate view; ordinary date mandatory. IsInitial is a read-only projection derived from the fixed 1970-01-01 date, not a stored field or user-controlled creation of extra fallback. BR-014/017/021. | OP-017, OP-019 via DTO-002 |
| DTO-008 AccountData / shared | Id: uuid; GroupId: uuid; CurrencyId: uuid; CategoryId: uuid; ProjectId: uuid; CorrespondentId: uuid; Name: string; Description: string; EditRevision: int64 | Nullable; Required; Required; Nullable; Nullable; Nullable; Required; Nullable; Nullable | PM-005; CurrencyId selectable on creation only, immutable on existing-account saves; account rules BR-010–012. Default-name preview uses same classification fields. | OP-021 via DTO-002, OP-022,025 |
| DTO-009 TransactionData / shared | Id: uuid; OccurredAt: datetime; Description: string; Entries: array<object>; State: enum; EditRevision: int64 | Nullable; Required; Nullable; Required; Nullable; Nullable | PM-006/007 projection; Entries shape E below; entry order must preserve PM-007 Position. State follows PM-006: Undefined=0 is unset and never persisted; Draft=1 or Confirmed=3 derived on save; Planned=2 reserved for future, not a current selectable state. No downgrade. Save min two account-bearing entries; editor projection may have fewer before save. BR-015–021. | OP-026 via DTO-002, OP-027,029,034 |
| DTO-010 MergeCommand / request | SourceId: uuid; DestinationId: uuid; ExpectedSourceRevision: int64; ExpectedDestinationRevision: int64 | Required; Required; Nullable; Nullable | Same derived group type in PM-001; type-specific operation selection remains TQ-05. Source/destination explicit; root source/descendant destination invalid. Self-merge handling proposed reject under TQ-10. BR-007/008. | OP-007 |
| DTO-011 TemplateData / shared | Id: uuid; GroupId: uuid; Name: string; Description: string; Entries: array<object>; EditRevision: int64 | Nullable; Required; Required; Nullable; Required; Nullable | PM-008/009 projection; Description is visible optional user input; Entries shape T below, empty allowed; entry order must preserve PM-009 Position; BR-022. | OP-032 via DTO-002, OP-033,036 |
| DTO-012 ReportDefinition / shared | Category: object; Project: object; Correspondent: object; From: date; To: date; CurrencyFirst: bool; GroupBy: enum | Required; Required; Required; Nullable; Nullable; Required; Required | Each dimension shape S below. GroupBy Category,Project,Correspondent,Day,Week,Month,Year. PM-010 JSON definition; BR-023–027. | OP-037; nested DTO-013 in OP-038/039 |
| DTO-013 SavedReportData / shared | Id: uuid; Name: string; Definition: object; EditRevision: int64 | Nullable; Required; Required; Nullable | Definition exactly DTO-012. PM-010; name validation BR-028. Read preserves raw intent; save minimization TQ-05. | OP-038 via DTO-002, OP-039 |
| DTO-014 CalculationResult / response | Rows: array<object>; Warnings: array<string> | Required; Required | Each row shape R below; signed amounts; no transaction drill-through. Balances/report preview only, not stored business data; BR-019/026/027. | OP-031,037 |
| DTO-015 ConfigurationData / shared | Scope: enum; APr: int32; RPr: int32; DefaultAccountNameOrder: enum; DefaultAccountNameSeparator: string; SyncTrigger: enum; ConflictPriority: enum | Required; Nullable; Nullable; Nullable; Nullable; Nullable; Nullable | Scope System,Local. System exposes APr/RPr read-only after creation; balancing-account selection belongs to System, with exact DTO shape pending TQ-05; Local requires its four settings. Enums per PM-012. No scope-crossing writes. PM-011/012; BR-005/012/017/034. | OP-041,042 |
| DTO-016 SetupRequest / request | Login: string; Password: string; BaseCurrencyCode: string; LocalDatasetKey: string; RequestId: uuid | Required; Required; Nullable; Required; Required | BR-003 required credentials; base code required only Create, not Open. Registration/request IDs proposed for retry; password transport/local retention TQ-03. | OP-003,004 |
| DTO-017 SetupResult / response | MasterDatasetKey: string; LocalDatasetKey: string; AuthorizationHandle: string; Transfer: object | Required; Required; Required; Required | Owner dataset/registration, proposed opaque authorization handle, Transfer DTO-021; do not expose hash/Admin.db. BR-003/005. | OP-003,004 |
| DTO-018 StatusResult / response | State: enum; MasterExists: bool; SyncKey: string; PublishedVersion: int64; InstalledVersion: int64; LastSuccessAt: datetime; Message: string | Required; Nullable; Nullable; Nullable; Nullable; Nullable; Nullable | State CreatingMode,OpenMode,MasterAbsent,MasterPresent,Waiting,Running,OutcomeUnknown,Published,Installed,Complete,Expired,Failed. Per-OP allowed subset TQ-04; ownership-scoped, no global directory listing. | OP-001,002,044,045,047,051,052 |
| DTO-019 SyncRequest / request | SyncKey: string; LocalDatasetKey: string; KnownVersion: int64; Priority: enum; Trigger: enum; Changes: object | Required; Required; Nullable; Required; Required; Required | Priority Master,Local; Trigger Manual,OnStart,OnExit,Recovery. Changes closed wire shape unresolved TQ-04 (not PM exposure); do not activate this input shape until specified. BR-029/034/038. | OP-043 |
| DTO-020 SyncIdentity / request | SyncKey: string; LocalDatasetKey: string | Required; Required | Owned registration/operation identity; auth separate from payload. BR-038–040. | OP-044,045,048 |
| DTO-021 TransferDescriptor / response | Version: int64; TransferToken: string; IntegrityProof: string | Required; Required; Nullable | Logical authorized snapshot transfer, never Admin.db. Bytes travel outside this descriptor; encoding/checksum/expiry TQ-04. BR-039/040. | OP-043,046,048,049; nested OP-003/004 |
| DTO-022 Receipt / request | SyncKey: string; LocalDatasetKey: string; InstalledVersion: int64 | Required; Required; Required | Confirm actual installed snapshot version; repeat safely after lost reply; BR-040. | OP-047 |
| DTO-023 TransferRequest / request | LocalDatasetKey: string; Version: int64; TransferToken: string | Required; Nullable; Nullable | Current owner copy; version/token required when downloading selected snapshot, absent in initial expiry replacement request. Precise handshake TQ-04. | OP-046,049 |
| DTO-024 SyncReport / response | SyncKey: string; Items: array<object>; Messages: array<string> | Nullable; Required; Required | Items shape C below; latest attempt/session only. BR-043. | OP-050 |
| DTO-025 DiagnosticEvent / request | OccurredAt: datetime; OperationId: string; Message: string; Detail: string | Required; Required; Required; Nullable | Local log; secret redaction/content policy TQ-08. No password/token logging intended. BR-043. | OP-052 |
| DTO-026 ValueResult / response | Name: string; Rate: decimal | Nullable; Nullable | Name only for account-name operations; Rate only for Restore Rate. No other field implied. BR-012/015. | OP-024,025,030 |

### Closed nested DTO objects (all proposed)

| Shape | Property | Canonical type | Nullability | Validation / mapping |
| --- | --- | --- | --- | --- |
| E transaction entry | Id | uuid | Nullable | PM-007 identity; absent for unsaved entry |
| E | AccountId | uuid | Nullable | Required on save; nullable only for unfinished editor |
| E | Amount | decimal | Nullable | Blank becomes zero on save |
| E | Rate | decimal | Required | Positive after rounding; base fixed 1 |
| E | BaseAmount | decimal | Nullable | Derived/read-only: Amount × Rate rounded per entry to APr places, midpoint-to-even; not persisted or trusted from input |
| T template entry | Id | uuid | Nullable | PM-009 identity |
| T | AccountId | uuid | Required | PM-005 |
| T | Amount | decimal | Required | APr fractional places |
| S selection dimension | IncludedGroupIds | array<uuid> | Required | Saved explicit group inclusions |
| S | ExcludedGroupIds | array<uuid> | Required | Saved explicit exclusions |
| S | IncludedElementIds | array<uuid> | Required | Explicit element inclusions |
| S | ExcludedElementIds | array<uuid> | Required | Explicit element exclusions |
| S | IncludeUnassigned | bool | Required | Explicit null-classification choice |
| R calculation row | Key | string | Required | Proposed stable display/group identity; encoding TQ-05 |
| R | Label | string | Required | Display caption |
| R | CurrencyCode | string | Nullable | Own-currency column if applicable |
| R | Amount | decimal | Nullable | Null where mixed-currency own amount invalid |
| R | BaseAmount | decimal | Required | Signed net rounded-entry total |
| R | Level | int32 | Required | Group hierarchy presentation; exact layout TQ-05 |
| C sync report count | EntityType | enum | Required | Group,Classification,Currency,Rate,Account,Transaction,Template,SavedReport,SystemConfiguration |
| C | Created | int32 | Required | Received local business changes; zero omitted in display |
| C | Updated | int32 | Required | Same |
| C | Deleted | int32 | Required | Same |

EditRevision projections are read-only sync metadata. Proposed ExpectedRevision fields require a separate local concurrency contract (TQ-02); they are not assumed equivalent to EditRevision.

Shared DTOs are proposed read/write views, not an instruction to accept read-only fields. Operations must reject/ignore client-owned audit/derived values per a final TQ-02 contract. Response field exposure must be approved before implementation. DTO-019 Changes is deliberately unresolved rather than an arbitrary `object` schema claimed complete.

## API/BFF Service Contracts

### Shared operation contract profiles

Every OP below names DTO input/output and source behavior. The operation list and all DTO selections are proposed. Transport metadata for every OP is **unresolved** (local invocation vs transport adapter, route, status mapping); local operations must remain offline-capable. No persistent model is an API argument/result.

- **L/R — local read:** ROLE-001; no login; only active usable local business state, except setup/status/configuration/report/log administration exceptions explicitly listed. No business mutation; consistent read snapshot proposed. Repeatable read with current-data result, not identical cached response. BR-002/005/039. Snapshot consistency/concurrency TQ-02.
- **L/W — local mutation:** ROLE-001; usable local state, no editing during sync/wait. Validate before commit; proposed atomic mutation of affected aggregate/dependencies. Existing data unchanged on numeric failure (BR-017). Retry deduplication/expected revisions unresolved TQ-02; do not assume all writes safely repeat. Effects supplied in row.
- **P — preview:** same local authorization as L/R, no persistence; recalculated from current inputs; repeated call may reflect changed rates/catalog. Transaction save remains separate.
- **S — setup:** ROLE-001 as prospective/existing owner; no local login gate. Create permitted only absent master; Open requires credential authentication; registration before download. Proposed request identity prevents duplicate partial setup; serialization/transaction boundary/idempotency TQ-03/TQ-04. Initial retry manual only.
- **C — cloud sync:** ROLE-001 authenticated owner and owned LocalDatasetKey/SyncKey/version; remembered authorization. Durable operation outcome; before-publication and after-publication boundaries are BR-038. Same SyncKey must not reapply published batch; receipts repeat safely. Exact durable protocol TQ-04. No implicit rollback after cloud publication.
- **D — local diagnostics/status:** ROLE-001 or application acting for that owner; no cloud auth. Does not grant blocked business access. Session report read/log writes/cleanup effects specified below; timing and filesystem transaction policy TQ-08.

For each operation the profile is its explicit authorization, transaction and idempotency policy reference. Where marked unresolved, approval is blocked until specified. No capability acquires an automatic new cloud endpoint just because listed here.

| SVC | Logical service | Purpose / source |
| --- | --- | --- |
| SVC-001 | StartupService | BC-001/002; BR-001–005 |
| SVC-002 | GroupService | BC-003; BR-006–009 |
| SVC-003 | ClassificationService | BC-003; BR-010/011 |
| SVC-004 | CurrencyService | BC-004; BR-013–017/021 |
| SVC-005 | AccountService | BC-004; BR-010–012 |
| SVC-006 | TransactionService | BC-005; BR-015–021 |
| SVC-007 | TemplateService | BC-006; BR-022 |
| SVC-008 | ReportService | BC-007/008; BR-023–028 |
| SVC-009 | ConfigurationService | BC-009; BR-005/012/017/034 |
| SVC-010 | SynchronizationService | BC-009–011; BR-029–042 |
| SVC-011 | DiagnosticsService | BC-012; BR-043 |

| OP | Service.operation | Profile | Input → output (proposed) | Validation/domain errors and observable effects / BRD source |
| --- | --- | --- | --- | --- |
| OP-001 | SVC-001.GetStartupState | D | None → DTO-018 | BR-002/039/041: mode from existence and pending/expiry state; no business data mutation. |
| OP-002 | SVC-001.CheckMasterExists | S read | None → DTO-018 | BR-003: availability only; failed call means unknown, never absence; bootstrap information exposure TQ-03. |
| OP-003 | SVC-001.CreateBooks | S | DTO-016 → DTO-017 | BR-003/004: require credentials/base currency; create owner/master/roots/base/config defaults, one base-currency rebalancing account in Account root and its System selection, and register; partial cloud success not undone on local download failure. |
| OP-004 | SVC-001.OpenBooks | S | DTO-016 → DTO-017 | BR-001/003/005: authenticate/register/download; new registration after reinstall; repeated attempt identity TQ-04. |
| OP-005 | SVC-002.GetTree | L/R | DTO-001 → DTO-002(GroupData) | BR-006: same-kind tree, root once; response contains group projections; elements via classification/account/template reads. |
| OP-006 | SVC-002.SaveGroup | L/W | DTO-004 → DTO-004 | BR-006–009: create/edit/move non-root; reject cycle/type/name violation; no root edit. |
| OP-007 | SVC-002.MergeGroups | L/W | DTO-010 → DTO-004 | BR-007/008: move children/elements, suffix incoming conflicts, delete source; destination projection returned; atomicity proposal TQ-02. |
| OP-008 | SVC-002.DeleteGroup | L/W | DTO-003 → None | BR-007: only empty non-root, deletion tracking for sync. |
| OP-009 | SVC-003.GetElements | L/R | DTO-001 → DTO-002(ClassificationData) | BR-010: select Category/Project/Correspondent; no cross-type confusion. |
| OP-010 | SVC-003.CreateElement | L/W | DTO-005 → DTO-005 | BR-008–010: correct group, nonblank unique Name; Description optional. |
| OP-011 | SVC-003.UpdateElement | L/W | DTO-005 → DTO-005 | BR-009/010/012: edit metadata, preserve stored account names. |
| OP-012 | SVC-003.MoveElement | L/W | DTO-005 → DTO-005 | BR-006–008: same-type destination and individual uniqueness. |
| OP-013 | SVC-003.DeleteElement | L/W | DTO-003 → None | BR-011: reject while account uses it; saved report refs do not block. |
| OP-014 | SVC-004.GetAvailableCurrencies | P | None → DTO-002(CurrencyData) | BR-013: regional enumeration, skip invalid, first per ISO. Catalog access independent of network. |
| OP-015 | SVC-004.GetCurrencies | L/R | DTO-001 → DTO-002(CurrencyData) | BR-013: local currency projections. |
| OP-016 | SVC-004.SaveCurrency | L/W | DTO-006 → DTO-006 | BR-013/014: create with fallback rate or edit metadata; code immutable, reject duplicate. Currency plus initial rate atomicity proposed. |
| OP-017 | SVC-004.SaveRate | L/W | DTO-007 → DTO-007 | BR-014/017/021: positive rounded rate, base fixed 1, unique date, initial date immutable. |
| OP-018 | SVC-004.DeleteCurrency | L/W | DTO-003 → None | BR-013: unused and non-base only; dependent rates lifecycle TQ-02. |
| OP-019 | SVC-004.GetRates | L/R | DTO-001 → DTO-002(RateData) | BR-014: initial value shown without hidden sentinel. |
| OP-020 | SVC-004.DeleteRate | L/W | DTO-003 → None | BR-014: ordinary only, initial cannot delete; existing entry rates unchanged. |
| OP-021 | SVC-005.GetAccounts | L/R | DTO-001 → DTO-002(AccountData) | BR-010: current classification/currency; no lock projection. |
| OP-022 | SVC-005.SaveAccount | L/W | DTO-008 → DTO-008 | BR-008–012: required group/currency/name; Description optional; currency immutable after first successful account save; duplicate names allowed. |
| OP-023 | SVC-005.DeleteAccount | L/W | DTO-003 → None | BR-011: reject any transaction/template reference. |
| OP-024 | SVC-005.PreviewRestoredName | P | DTO-003 → DTO-026 | BR-012: current Local format/current classifications; persisted only by OP-022. |
| OP-025 | SVC-005.PreviewDefaultName | P | DTO-008 → DTO-026 | BR-012: new account naming; non-name required-field rules apply at save, not this preview. |
| OP-026 | SVC-006.GetTransactions | L/R | DTO-001 → DTO-002(TransactionData) | BR-016–021: complete entry projections for viewing/editing; pagination transport TQ-09. |
| OP-027 | SVC-006.SaveTransaction | L/W | DTO-009 → DTO-009 | BR-015–021: aggregate validation, rounding, Confirmed/Draft policy from PM-006, account currency immutability; no partial aggregate save proposed. |
| OP-028 | SVC-006.DeleteTransaction | L/W | DTO-003 → None | BR-019/032: remove calculation contribution, retain sync deletion evidence until eligible. |
| OP-029 | SVC-006.DuplicateTransaction | P | DTO-003 → DTO-009 | BR-020: copy rates/accounts/amounts/description, now timestamp, new unsaved identity. |
| OP-030 | SVC-006.RestoreRate | P | DTO-001 → DTO-026 | BR-015: Query.Id = account; rate lookup DateOnly from Query.From, representing the transaction date in current UI/device timezone; latest on/before date; no stored entry mutation. |
| OP-031 | SVC-006.GetBalances | L/R | DTO-001 → DTO-014 | BR-019: on-demand active-entry totals; query shape/date boundary details TQ-05. |
| OP-032 | SVC-007.GetTemplates | L/R | DTO-001 → DTO-002(TemplateData) | BR-022: include full entries. |
| OP-033 | SVC-007.SaveTemplate | L/W | DTO-011 → DTO-011 | BR-022: unique name/accounts, empty allowed; save visible optional Description independently of Name; preserve existing account currency immutability. |
| OP-034 | SVC-007.ApplyTemplate | P | DTO-003 → DTO-009 | BR-022: unsaved transaction with current-time proposal, date-applicable rates; Cancel no mutation. |
| OP-035 | SVC-007.DeleteTemplate | L/W | DTO-003 → None | Proposed template maintenance interpretation TQ-10; if accepted preserve account currency immutability, propagate deletion. |
| OP-036 | SVC-007.FromTransaction | P | DTO-003 → DTO-011 | BR-022: active/draft source; copy accounts/amounts/description; target name/group supplied before OP-033. |
| OP-037 | SVC-008.Calculate | L/R | DTO-012 → DTO-014 | BR-023–027: current tree/classifications, read-only selection evaluation, empty-warning and currency grouping. |
| OP-038 | SVC-008.GetDefinitions | L/R | DTO-001 → DTO-002(SavedReportData) | BR-025/028: preserve JSON intent exactly on read. |
| OP-039 | SVC-008.SaveDefinition | L/W | DTO-013 → DTO-013 | BR-025/028: user save, name validation, recompute minimal override IDs for selection save; rename-only handling TQ-05. |
| OP-040 | SVC-008.DeleteDefinition | L/W | DTO-003 → None | BR-028: only definition deleted; bookkeeping unchanged. |
| OP-041 | SVC-009.GetConfiguration | D read | None → DTO-015 | BR-005: scope selection/return both scopes requires DTO finalization TQ-05; no business data access bypass. |
| OP-042 | SVC-009.SaveConfiguration | L/W | DTO-015 → DTO-015 | BR-005/012/017/034: validate scope fields; System tracked for sync, Local not; admission during recovery TQ-05. |
| OP-043 | SVC-010.Synchronize | C | DTO-019 → DTO-021 | BR-029–038: queue, change detection, priority/validity, publication; no-change result may need status rather than transfer, unresolved TQ-04. |
| OP-044 | SVC-010.GetOutcome | C read | DTO-020 → DTO-018 | BR-038–041: durable known/unknown outcome and expiry; never infer from equal timestamps. |
| OP-045 | SVC-010.Cancel | C | DTO-020 → DTO-018 | BR-036/038: stop current work where possible; no rollback after publication; post-publication pending recovery. |
| OP-046 | SVC-010.Download | C | DTO-023 → DTO-021 + snapshot stream | BR-039/040: consistent selected version; byte transport unresolved, do not expose Admin.db; restart incomplete transfer. |
| OP-047 | SVC-010.Acknowledge | C | DTO-022 → DTO-018 | BR-040/041: idempotent receipt of actual installed version, master-clock success/expiry, deletion eligibility. |
| OP-048 | SVC-010.Recover | C | DTO-020 → DTO-021 | BR-038–040: resolve prior outcome then latest snapshot; editing/viewing/report block remains until completion. |
| OP-049 | SVC-010.ReplaceExpired | C | DTO-023 → DTO-021 | BR-041/042: only user Download; expired copy disposal, new registration handling and preserved Local settings; handshake TQ-04. |
| OP-050 | SVC-011.GetLatestSyncReport | D read | None → DTO-024 | BR-043: latest in-session attempt only; absent-report representation TQ-08. |
| OP-051 | SVC-011.CleanupLogs | D | None → DTO-018 | BR-043: delete logs beyond 7-day retention; local operation, no network restriction implied. |
| OP-052 | SVC-011.WriteDiagnostic | D | DTO-025 → DTO-018 | BR-043: detailed local log in folder; no business mutation; sanitized fields TQ-08. |

## Validation and Error Contracts

Names below describe domain outcomes, not approved machine codes or HTTP statuses. Payload shape/transport mappings remain TQ-07. Business rules follow the prior approved BRD baseline plus explicit accepted corrections recorded in current BRD; remaining mechanics are proposed. No validation failure may be presented as successful saving/synchronization.

| Contract/error | Applies to | Trigger/rule | Observable result | Source/status |
| --- | --- | --- | --- | --- |
| Required value absent | OP-003/004/006/010–012/022/027/033/039 | Required credentials, nonblank required names, required account/group | Reject operation; user corrects input | BR-003/008/009/016/022/028 |
| Hierarchy invalid | OP-006/007/008/012 | Root mutation/source, descendant merge, move cycle, wrong type, nonempty deletion | Reject; do not alter hierarchy | BR-006/007 |
| Name conflict | OP-006/007/010–012/033 | Individual forbidden collision or bulk collision | Reject individual; repeatedly append `_1` to incoming bulk names | BR-008 |
| Protected reference | OP-013/018/023 | Classification/account/currency still used; base currency | Reject deletion | BR-011/013 |
| Account currency immutable | OP-022/043 | Attempt to change currency after first successful account save | Reject local change; sync must preserve chosen currency | BR-011/030 |
| Invalid numeric value | OP-016/017/022/027/033/037 | Nonpositive rounded rate or supported range exceeded | Error and existing data unchanged; no silent clipping | BR-017/018 |
| Invalid transaction | OP-027 | <2 entries, missing accounts, invalid date/rate, invalid active edit | Reject; new unbalanced otherwise-valid record can be draft | BR-016–021 |
| Empty report selection | OP-037 | Any dimension has no selected elements/unassigned | Empty result with warning, not unrestricted | BR-023 |
| Missing report reference | OP-037/038 | Saved identity absent from actual tree | Ignore without rewriting definition; returning identity reactivates choice | BR-025 |
| Cloud authorization failure | OP-003/004/043–049 | Invalid credentials/owner scope | Reject access; no other owner data returned; normal local opening still no login | BR-003/005 |
| Setup partial success | OP-003/004 | Master created, local download failed | Create disabled; user retries Open manually; no normal use without LocalDb | BR-003 |
| Waiting for master | OP-043/048 | Another sync active | Waiting message, no editing, cancellable unlimited healthy wait | BR-034/036 |
| Communication inactivity | OP-043–049 | No response/data for 30 seconds, healthy queue waiting excluded | Stop attempt; pre/post-publication state decides recovery | BR-036/038 |
| Conflict cannot be validly repaired | OP-043 | Existing policies cannot yield valid data | Explain issue/remedy; diagnostic log; preserve publication/recovery boundaries | BR-030/038 |
| Recovery pending | OP-001/local business operations | Uncertain or partly installed publication | Block all business access; preserve state; do not imply rollback | BR-039 |
| Expired registration | OP-043/044/048/049 | Master confirms 90 days elapsed | Delete expired local data; no viewing/export; Download action required | BR-041/042 |
| Stale local revision | All L/W operations | Proposed expected revision mismatch | Proposed rejection rather than overwriting newer edit; policy not yet approved | TQ-02 |

## Security and Authorization

All operations use ROLE-001 as business principal; application-triggered operations act for that owner, not an invented administrator role. ROLE-002 has no API rights.

| Protected operations | Roles | Ownership/scope | Source | Enforcement decisions |
| --- | --- | --- | --- | --- |
| OP-001 | ROLE-001 | Local existence/recovery state only | BR-002 | Local startup flags TQ-04 |
| OP-002/003 | ROLE-001 prospective owner | Hardcoded single-user deployment; Create only when master absent | BR-001/003 | Safe first-owner bootstrap and races TQ-03/TQ-04 |
| OP-004 | ROLE-001 | Authenticate existing owner before registration/download | BR-003/005 | Credential verification/authorization retention TQ-03 |
| OP-005–040 | ROLE-001 | Current local dataset; no app-opening authentication; business access gates apply | BR-002/034/039 | Gate enforcement/concurrency TQ-02/TQ-04 |
| OP-041/042 | ROLE-001 | Own Local/System settings; Local never synchronizes | BR-005 | Configuration access while recovery pending TQ-05 |
| OP-043–049 | ROLE-001 | Owner dataset plus owned registration, operation and version; never trust IDs alone | BR-005/038–041 | Auth handle/expiration, upload/download authorization TQ-03/TQ-04 |
| OP-050–052 | ROLE-001/application for owner | Session diagnostics/log folder; no remote log-sharing API | BR-043 | Secret redaction/file permissions TQ-08 |

Only login/password requiredness is user-approved input policy. Do not impose invented length/complexity constraints. Password hashing algorithm, salt/work parameters, secure credential storage, session authorization and revocation are technical decisions TQ-03, not settled by “remember authorization”. Initial provisioning of a single-user endpoint must be protected; the exact bootstrap control is unresolved. Version-two password change/recovery is not introduced here.

## Data and Integration Requirements

1. **Ownership:** one dataset owner; complete local business copy. Administration/credential registry is not a business snapshot. System configuration travels with business state; preserve Local settings across replacement.
2. **Identifiers:** use the confirmed shared GUID business-entity identity contract, including stable report references. Registration/operation identities and configuration singleton-key choices remain proposals; this decision does not replace integer snapshot Versions with GUIDs.
3. **Lifecycle/deletion:** apply the confirmed shared tracking and snapshot acknowledgement contracts. Full snapshots must not leak admin metadata or resurrect accepted deletion markers as live local rows. TQ-04 specifies safe preparation/installation.
4. **Master publication:** obtain active snapshot, serialize one writer per master, prepare valid candidate, publish active version and durable operation outcome atomically. Local commit is separate; later failure never rolls master back. Primitive, hosting, lock ownership and atomicity unresolved.
5. **Transfer:** pin selected immutable version for a consistent download; safe local activation before receipt. Restart partial transfer. Keep prior non-expired outgoing data while outcome unresolved. Confirm installed snapshot version, not merely latest master known. State machine and file switching TQ-04.
6. **Recovery:** resolve prior SyncKey before duplicate upload or disposal; recover latest master; repeat lost receipt safely. 90-day confirmed expiry supersedes preservation/recovery. Expiry is evaluated by master, not guessed by local wall clock.
7. **Cleanup:** obsolete published files removable only after download/dependency safety; publication outcomes outlive files as needed. No backup retention; backup/restore version two. Registration that never completed initial download needs explicit lifecycle policy TQ-04.
8. **Catalog:** local system culture/region data from user's example. First value per code as enumerated; platform differences do not rewrite already saved Currency Name/Symbol unless user restores/edits or normal sync conflict rules apply.
9. **Migration:** schema version representation, upgrade compatibility, supported client versions and rollback behavior TQ-06. Do not activate a snapshot the local client cannot safely use; this is a proposed design constraint, not a replacement for existence-only mode selection.
10. **Precision:** SQLite mapping must preserve approved decimal semantics, including exact rounded zero test. Use confirmed int64 scale 10,000 for persisted Amount/Rate; finalize calculation ranges under TQ-01; binary floating approximation is not permitted to relax BR-018. No fixed performance limits inferred from SQLite choice.
11. **Report JSON:** evaluate persisted selection as-is against current tree. Explicit overrides survive moves, redundant state and missing IDs. Minimize only on explicit selection save. Report deletion deletes definition only; sync treats saved definition as business item.
12. **Integration scope:** Azure first, Functions for setup, hardcoded prototype address. No bank import, AWS, browser/Apple work, multi-user administration or cloud backup implementation included.

## Performance, Reliability, and Observability

| Requirement | Technical contract | Measurement context | Status/blocker |
| --- | --- | --- | --- |
| NFR-001 | Local OP-005–040 operate offline; setup/manual network any connection, automatic start Wi-Fi | Android/Windows; usable installed copy, not pending recovery | Behavior confirmed; local boundary/adapters proposed |
| NFR-002 | Exact-zero active totals, protected identities/references/locks, owner checks | Save, merge and sync paths with aggregate validation | Rule confirmed; precision/auth/atomicity TQ-01–04 |
| NFR-003 | 30-second inactivity timer reset by response/data/progress; healthy queue no timeout | Start/end event and progress transport must be defined | TQ-04; no arbitrary overall deadline |
| NFR-004 | Preserve uncertain operation state; block business access; 90-day expiry by master clock | Initial acknowledgement, ordinary/no-change sync and recovery | TQ-04; no false success/duplicate batch |
| NFR-005 | Latest report only until closure; logs in folder 7 days | New attempt overwrites report; log cleanup age boundary | Behavior confirmed; cleanup schedule and clock TQ-08 |
| SC-001 | Exercise UC-001/002 with creation/retry/open/reinstall/initial content | Detailed UC rows map approved BC acceptance criteria | Verification pending; contracts proposed |
| SC-002 | Exercise hierarchy/locking/rates/draft/active arithmetic cases | Include rounding, zero rate, duplicate account and overflow cases | Exact numeric bounds TQ-01 |
| SC-003 | Exercise templates and report inheritance/current-data behavior | Empty/single-entry template; moved/missing/reappearing IDs; deletion of definition only | Serialization/editor mapping TQ-05 |
| SC-004 | Exercise concurrent-device conflicts, cancellation, lost responses and expiry | Distinguish before publication, after publication, after installation/before receipt | Fault verification later; protocol TQ-04 |

No arbitrary latency/throughput/storage-capacity target is added. Diagnostic reports count received changes, not uploads or all rows in a replacement file. Confirmed report behavior (2026-09-28): show "Currency reordering" once per synchronization report if any currency receives a changed Order during synchronization. Do not count or emit one message per renumbered currency; omit the message when no received currency Order changed. Upload-only activity is not reported as received changes. Future only: a message such as "Reordering elements in group {name of group}", once per affected group when any of its elements receives a changed Order. Group-reordering messages are deferred and are not required in the current release; this deferral does not defer group/element ordering synchronization itself. Count derivation TQ-08 must retain that distinction.

## Assumptions and Open Technical Questions

These questions identify concrete draft contract gaps. They do not reopen settled BRD decisions. All proposals remain inactive until accepted in an exact TRD version. Owner: requesting user with technical design input.

| ID | Item / type | Affected contracts | Approval/handoff impact |
| --- | --- | --- | --- |
| TQ-01 | Partly resolved: display follows APr/RPr and SQLite Amount/Rate storage is int64 scaled by 10,000. Open: calculation magnitude limits and overflow detection for products/totals; Unicode definition of one separator. Account-name format is derived from the two stored settings in PM-012. BaseAmount derivation is confirmed in PM-007; initial-rate date and derived IsInitial are confirmed in PM-004. | PM-003–009/012; DTO-006–011/014/015; OP-016–037 | Blocks exact storage/validation contract. BRD scale/rounding/overflow result remain binding. |
| TQ-02 | Partly resolved: shared sync fields and entity revisions confirmed above. Open: configuration singleton-key choices, remaining identifier role/name alignment, separate local expected-revision policy; local transaction boundaries for merge/save/deletion; idempotency of local mutation retries. | PM-001–012; L/W operations | Blocks schemas and mutation safety; never substitutes timestamps for sync outcomes. |
| TQ-03 | Open: first-owner bootstrap, login case/normalization, password-hash parameters, remembered authorization/expiry, local secret storage, per-operation ownership enforcement. | PM-013; DTO-016–020/023; OP-002–004/043–049 | Blocks secure setup/cloud contract; no new password complexity rule. |
| TQ-04 | Open: Admin.db hosting/serialization, durable sync state machine, typed Changes DTO, no-change response, snapshot byte protocol, atomic publication/outcome and receipt persistence, writer wait/liveness, reader leases/cleanup, incomplete registration retry/expiry, null-revision identity collisions. Int64 snapshot versions and deletion acknowledgement thresholds confirmed above. | PM-014–016; DTO-018–023; OP-003/004/043–049 | Blocks complete sync contracts; combined conflicts must implement BR-029–033 or fail/report without invalid publication. |
| TQ-05 | Proposal/open: creation/configuration DTOs for immutable APr/RPr and balancing-account setting/action contracts; device-timezone/calendar-date context for rate lookup and reporting, entry-order serialization (array order versus explicit Position), type-specific DTO/service selection for derived group/element models (no Kind enum), final DTO read/write split, list paging/filter fields, report result grouping/selection-save versus rename, one/both configuration scopes in response, settings admission during recovery, ID remapping in reports. | DTO-001–015/026; OP-005–042 | Blocks exposed shapes/editor contracts; saved JSON preservation rules unchanged. |
| TQ-06 | Open: schema version and upgrade/download compatibility, migration failure behavior. | All PMs; snapshot operations | Blocks safe compatible local installation; no implementation schema inferred. |
| TQ-07 | Proposal/open: domain error DTO/code taxonomy and local/transport mappings; canceled/absent result representation. | All OPs | Observable BRD errors fixed; exact response contracts incomplete. |
| TQ-08 | Open: log folder per platform, seven-day clock/cleanup trigger, diagnostic secret redaction and latest report counts after full replacement. | DTO-024/025; OP-050–052 | Blocks diagnostic details; no new Share/Export feature. |
| TQ-09 | Open: minimum supported client/runtime versions and verification workload; only propose capacity/latency limits if needed. | NFR-001–005/SC-001–004 | Runtime/deployment constraints unresolved; no new business target. |
| TQ-10 | Proposal to review: explicit DeleteTemplate operation, self-merge rejection, exact preview input/output requiredness. | UC-006-04; OP-007/025/035/036 | These details are labeled proposals, not silent additions to BRD. Keep unsupported operations inactive until resolved. |
| TQ-11 | Resolved 2026-09-27: int32 Position, consecutive 0..N-1 on saving edited collections; preserve accepted positions during whole-aggregate sync. Scope excludes catalog element/group ordering. | PM-007/009; DTO-009/011; OP-027/033 | Entry ordering resolved; DTO serialization remains TQ-05. |
| TQ-12 | Partly resolved: per-entity ModificationType flags, Local-wins flagged Order independent of content priority, int32 separate zero-based catalog sequences, normalization and GUID tie-breaks. Merged per-member Order values sort ascending with GUID tie-break, then normalize; changed intended placement is accepted. Canonical lowercase GUID strings with ordinal ascending comparison are confirmed. Order-only changes require synchronization, whether represented by uncaptured flags or a queued batch. Open: order transport and change-detection implementation. | PM-001/002/003/005/008; catalog DTOs and mutation/sync operations | Complete collection ordering protocol; review BRD alignment before approval. |
| TQ-13 | Resolved 2026-09-28: favorites default false, roots permanently non-favorite, favorite changes synchronize as Content; filtering retains necessary ancestor paths and remains favorites-only inside favorite groups. | PM-001/002/003/005/008; catalog DTOs/mutations | Favorite behavior resolved; exact DTO shapes/operation selection remain TQ-05. |

## Complete Traceability Matrix

Each approved BRD ROLE, BC, BR, FR, NFR and SC receives one row. `Partial` means the business behavior is represented, but proposed types/DTOs/enforcement or open technical mechanisms prevent complete contractual coverage. It does not mean the business decision is unknown. `Non-system` applies only to the requirements approval role. No claim of complete implementation coverage is made.

| BRD ID | TRD ID(s) / section | Coverage | Notes |
| --- | --- | --- | --- |
| SC-001 | Performance, Reliability, and Observability; row SC-001 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| SC-002 | Performance, Reliability, and Observability; row SC-002 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| SC-003 | Performance, Reliability, and Observability; row SC-003 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| SC-004 | Performance, Reliability, and Observability; row SC-004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| ROLE-001 | Security and Authorization; all OP-001–OP-052 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| ROLE-002 | Role and Use-Case Traceability: approval is non-system | Non-system | No application endpoint; business approval only. |
| BR-001 | PM-011–PM-016; OP-001–OP-004/041–OP-049 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-002 | PM-011–PM-016; OP-001–OP-004/041–OP-049 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-003 | PM-011–PM-016; OP-001–OP-004/041–OP-049 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-004 | PM-011–PM-016; OP-001–OP-004/041–OP-049 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-005 | PM-011–PM-016; OP-001–OP-004/041–OP-049 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-006 | PM-001/002; OP-005–OP-013 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-007 | PM-001/002; OP-005–OP-013 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-008 | PM-001/002; OP-005–OP-013 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-009 | PM-001/002/005/008; OP-005–OP-013/022/033 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-010 | PM-003–PM-005/012; OP-014–OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-011 | PM-003–PM-005/012; OP-014–OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-012 | PM-003–PM-005/012; OP-014–OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-013 | PM-003–PM-005/012; OP-014–OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-014 | PM-003–PM-005/012; OP-014–OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-015 | PM-003–PM-005/012; OP-014–OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-016 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-017 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-018 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-019 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-020 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-021 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-022 | PM-008/009; OP-032–OP-036 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-023 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-024 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-025 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-026 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-027 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-028 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-029 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-030 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-031 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-032 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-033 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-034 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-035 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-036 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-037 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-038 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-039 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-040 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-041 | PM-014/015; OP-044/049; UC-011-01/02 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-042 | PM-014/015; OP-044/049; UC-011-01/02 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-043 | DTO-024/025; OP-050–OP-052 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-044 | Shared favorite contract; PM-001/002/003/005/008; TQ-13 | Partial | Favorite behavior and content sync confirmed; DTO/operation contracts remain TQ-05. |
| BC-001 | UC-001-01–UC-001-03; OP-001–OP-004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-002 | UC-002-01–UC-002-03; OP-001/004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-003 | UC-003-01–UC-003-06; OP-005–OP-013 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-004 | UC-004-01–UC-004-09; OP-014–OP-025 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-005 | UC-005-01–UC-005-06; OP-026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-006 | UC-006-01–UC-006-04; OP-032–OP-036 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-007 | UC-007-01–UC-007-02; OP-037 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-008 | UC-008-01–UC-008-04; OP-038–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-009 | UC-009-01–UC-009-05; OP-041–OP-047 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-010 | UC-010-01–UC-010-03; OP-044–OP-048 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-011 | UC-011-01–UC-011-02; OP-044/047/049 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-012 | UC-012-01–UC-012-02; OP-050–OP-052 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-001 | UC-001-01–UC-001-03; OP-001–OP-004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-002 | UC-002-01–UC-002-03; OP-001/004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-003 | UC-003-01–UC-003-06; OP-005–OP-013 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-004 | UC-004-01–UC-004-09; OP-014–OP-025 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-005 | UC-005-01–UC-005-06; OP-026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-006 | UC-006-01–UC-006-04; OP-032–OP-036 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-007 | UC-007-01–UC-007-02; OP-037 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-008 | UC-008-01–UC-008-04; OP-038–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-009 | UC-009-01–UC-009-05; OP-041–OP-047 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-010 | UC-010-01–UC-010-03; OP-044–OP-048 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-011 | UC-011-01–UC-011-02; OP-044/047/049 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-012 | UC-012-01–UC-012-02; OP-050–OP-052 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| NFR-001 | Performance, Reliability, and Observability; row NFR-001 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| NFR-002 | Performance, Reliability, and Observability; row NFR-002 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| NFR-003 | Performance, Reliability, and Observability; row NFR-003 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| NFR-004 | Performance, Reliability, and Observability; row NFR-004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| NFR-005 | Performance, Reliability, and Observability; row NFR-005 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |

## Development Handoff Readiness

- **Not ready**. BRD 0.49 and TRD 0.58 are Draft; BRD 0.32 approval remains prior-baseline provenance only.
- Blockers: TQ-01–TQ-10 and TQ-12. TQ-11 and TQ-13 are resolved. Proposed schemas/directions/exposed fields, concurrency, authentication, sync wire protocol and transport/error definitions remain incomplete.
- DTO/PM boundary: persistent models are not operation inputs/outputs. DTO-019 Changes and snapshot streaming are explicitly unresolved and cannot be treated as implemented contracts.
- Traceability: all 79 applicable BRD IDs enumerated (2 roles, 12 capabilities, 44 rules, 12 functional, 5 nonfunctional, 4 success criteria). One non-system approval role; 78 Partial; zero Covered. This is drafting coverage, not a passing contract review or development handoff.
- Detailed use cases are concise scenario specifications linked to operations. Proposed additions and unresolved technical behavior remain visible instead of masquerading as complete manual test procedures.
- Handoff requires approved BRD, approved TRD with material gaps closed, and passing review results. No implementation/code/test generation is authorized here.

## Clarification Log

### 2026-09-27 — Entity revisions, snapshot versions and shared sync schema

- Exact closing question: “should `IsModified` exist only in local storage, with Master merely reading the incoming flag?”
- Accepted answer: “We should have equal Dbs. Local and Master. It's OK for existence redundant fields, e.g. IsModified in MasterDb. So. I agree with this approach. Put it into TRD”.
- Source: requesting user in this chat, following explicit agreement on revisions, snapshot versions, pending DeleteVersion 0 and per-registration acknowledgements.
- Decision: identical business schemas, redundant Master IsModified, and the confirmed tracking/acknowledgement contracts above. Supersedes tentative timestamp/boolean deletion proposals and Draft 0.2 shared audit/revision fields.
- Affected IDs: PM-001–PM-016; DTO-003–DTO-011/013 and DTO-018–DTO-023; OP-043–OP-049; UC-010-02; TQ-02/TQ-04; Data and Integration Requirements.
- Draft 0.2 becomes Draft 0.3. This records acceptance of the stated decision only; full TRD approval remains outstanding.

### 2026-09-27 — Delete a local creation before first synchronization

- Exact question: “if an entity is created and deleted locally before its first sync, should Local hard-delete it immediately?”
- Accepted answer: “Yes. Obviously”.
- Source: requesting user in this chat.
- Decision: apply the immediate local hard-deletion lifecycle row above; no Master deletion marker is needed for an entity Master never received. Existing reference validation and uncertain-outcome recovery rules remain binding.
- Affected IDs: PM-001–PM-010 shared lifecycle; OP-008/013/018/020/023/028/035/040 and OP-043; TQ-04.
- Draft 0.3 becomes Draft 0.4; full TRD approval remains outstanding.

### 2026-09-27 — Revision and snapshot integer width

- Exact question: “use **64-bit integers** for `EditRevision`, `DeleteVersion`, and snapshot versions?”
- Accepted answer: “correct”.
- Source: requesting user in this chat.
- Decision: signed 64-bit integers (`int64`) for entity revisions, DeleteVersion and all snapshot-version references, including AcknowledgedVersion; existing nullability and sentinel rules remain unchanged.
- Affected IDs: PM-001–PM-006/008/010–PM-016; DTO-004–DTO-009/011/013/018/019/021–DTO-023; TQ-02/TQ-04.
- Draft 0.4 becomes Draft 0.5; full TRD approval remains outstanding.

### 2026-09-27 — Initial acknowledged snapshot version

- Exact question: “use **`AcknowledgedVersion = 0` before the first successful snapshot installation**, since published versions start at 1?”
- Accepted answer: “0 is OK”.
- Source: requesting user in this chat.
- Decision: required int64 AcknowledgedVersion defaults to 0 locally and per Master registration; existing installation and receipt rules govern advancement.
- Affected IDs: PM-012/014; OP-003/004/047; TQ-04 and shared snapshot acknowledgement contract.
- Draft 0.5 becomes Draft 0.6; full TRD approval remains outstanding.

### 2026-09-27 — Stable GUID business identities

- Exact question: “Shall I record that rule in TRD?” Rule proposed: “GUIDs generated locally and preserved through synchronization”.
- Accepted answer: “Let's use GUID. It's significantly simpler”.
- Source: requesting user in this chat, after discussing independent auto-increment collisions and reference remapping across Local databases.
- Decision: apply the shared GUID identity contract above for persistent business entities and their references. Independent local creations must not be treated as the same entity because local numeric sequences overlap. Configuration singleton keys and registration/operation identifiers remain separate decisions.
- Affected IDs: PM-001–PM-010 and their relationship fields; TQ-02; Data and Integration Requirements.
- Draft 0.6 becomes Draft 0.7; full TRD approval remains outstanding.

### 2026-09-27 — Five hardcoded root identities

- Proposal accepted: “keep your five fixed root GUIDs”.
- Accepted answer: “Agree. Will use hardcoded 5 root entities ids”.
- Source: requesting user in this chat; exact values read from backend/Business.Models/Constants/RootsIds.cs.
- Decision: PM-001 records the five fixed root GUIDs as an exception to generated business identities; all Master and Local copies use those constants.
- Affected IDs: PM-001 and shared business identity contract; UC-001-03; OP-003/005.
- Draft 0.7 becomes Draft 0.8; full TRD approval remains outstanding.

### 2026-09-27 — Derived root indicator

- Exact proposal: “IsRoot is derived from the fixed root identity”; no stored boolean. An IsRoot() extension method was explained as an implementation option.
- Accepted answer: “agree”.
- Source: requesting user in this chat.
- Decision: remove IsRoot from persistent PM-001 fields; compute it by comparing the group Id with its type-specific fixed root Id. DTO-004 can retain a read-only derived projection.
- Affected IDs: PM-001; DTO-004; OP-005/006.
- Draft 0.8 becomes Draft 0.9; full TRD approval remains outstanding.

### 2026-09-27 — Derived group and element models

- Exact question: “keep separate persistent models for each group and element type, as your C# code does, rather than generic models with a stored Kind field?”
- Accepted answer: “enum Kind is bad approach. We should use derived classes. They are equals (5 groups, 3 element Category, Correspondent and Project). But it's more clean and gives possibility to and new derived entity in the future istead of expand Kind enum”.
- Source: requesting user in this chat.
- Decision: PM-001 and PM-002 define shared contracts for separate derived models; remove Kind enum fields. Dependent DTO proposals no longer assume Kind; their exact type-specific selection remains TQ-05. Physical table mapping is not selected.
- Affected IDs: PM-001/002, relationships to PM-005/008; DTO-001/004/005/010; TQ-05.
- Draft 0.9 becomes Draft 0.10; full TRD approval remains outstanding.

### 2026-09-27 — One table per concrete entity

- Exact question: “Concrete tables only: AccountGroup, CategoryGroup, etc. Each table includes inherited fields such as Id, Name, and sync fields. No base-class tables.” versus “Base and derived tables”; “Is that what you mean?”
- Accepted answer: “Not base and derived. Each entity has own subset of fields”. Earlier direction: “Yes. We should use each entity in it's own table. Table per type”.
- Source: requesting user in this chat.
- Decision: use the concrete-entity table layout above; shared bases have no persistent tables/rows. This resolves the physical inheritance-layout question left open in Draft 0.10, without selecting ORM configuration or scalar mappings.
- Affected IDs: PM-001–PM-010; shared persistent-model contract.
- Draft 0.10 becomes Draft 0.11; full TRD approval remains outstanding.

### 2026-09-27 — Derived transaction-entry BaseAmount

- Exact question: “Keep it derived as `Amount × Rate`, rounded to four decimal places using midpoint-to-even?”
- Accepted answer: “yes”.
- Source: requesting user in this chat.
- Decision: PM-007 defines BaseAmount as a calculated, non-persistent value with the existing BR-018 per-entry rounding rule. Remove the persistence-choice ambiguity from TQ-01 and align DTO entry shape E.
- Affected IDs: PM-007; DTO-009 entry shape E and consumers DTO-014; OP-027/031/037; TQ-01.
- Draft 0.11 becomes Draft 0.12; full TRD approval remains outstanding.

### 2026-09-27 — Transaction state values and draft save boundary

- Exact clarification question: “Should users now be allowed to save drafts with zero or one entry?”
- Accepted answer: “You are right, User cannot save transaction where enties.amount < 2. Don't need to change BRD”. In context, this confirms entry count below two remains unsavable.
- Source: requesting user in this chat, following the proposed values Draft = 1, Planned = 2, Confirmed = 3 and the instruction to reserve Planned for the future.
- Decision: PM-006 defines the enum representation and current save boundary. Confirmed maps to BRD active; Planned is reserved without first-release workflow; drafts retain all mandatory save constraints except balance. BRD unchanged.
- Affected IDs: PM-006/007; DTO-009; OP-027/031/037; UC-005-01–UC-005-03.
- Draft 0.12 becomes Draft 0.13; full TRD approval remains outstanding.

### 2026-09-27 — Undefined transaction state

- Exact question: “keep `NoValid = 0` as an unset value that cannot be saved, or rename it `Undefined = 0`?”
- Accepted answer: “`Undefined = 0`”.
- Source: requesting user in this chat.
- Decision: PM-006 names the unset value Undefined = 0; it cannot be a persisted transaction state. Existing Draft/Planned/Confirmed rules remain unchanged.
- Affected IDs: PM-006; DTO-009; OP-027.
- Draft 0.13 becomes Draft 0.14; full TRD approval remains outstanding.

### 2026-09-27 — Persistent entry ordering

- Exact question: “Should entry order survive saving and synchronization? If yes, I recommend storing Position.”
- Accepted answer: “Yes. We heed Position property in entries.”
- Source: requesting user in this chat.
- Decision: both TransactionEntry and TemplateEntry persist Position within their parent; save/load and aggregate synchronization preserve order. Numeric convention remains TQ-11; exact DTO serialization remains TQ-05.
- Affected IDs: PM-007/009; DTO-009/011; OP-027/033; TQ-05/TQ-11.
- Draft 0.14 becomes Draft 0.15; full TRD approval remains outstanding.

### 2026-09-27 — Consecutive entry positions, separate from catalog ordering

- Exact proposal: “Recalculate only when saving an edited collection; preserve positions during sync. Use that rule?” Consecutive integer positions were proposed as 0..N-1.
- Accepted answer: “Oh. You are talking about entrires. I though you were tacking about order of element in the group. I totally agree with entries in Template and Transaction. But I an not sure about elements in group”.
- Source: requesting user in this chat.
- Decision: apply the shared entry-order rule in PM-007 to both entry models. No catalog ordering strategy is selected by this answer.
- Affected IDs: PM-007/009; OP-027/033; TQ-11.
- Draft 0.15 becomes Draft 0.16; full TRD approval remains outstanding.

### 2026-09-27 — Synchronized catalog element ordering

- Exact question: “should catalog ordering synchronize across devices, or remain a local display preference?”
- Accepted answer: “Yes. Ordering should be the same for all devices”.
- Source: requesting user in this chat, discussing elements within groups.
- Decision: record shared synchronization scope above; representation and conflict/renumbering details remain TQ-12. No numbering strategy is inferred.
- Affected IDs: PM-002/005/008; TQ-12; shared persistent-model contract.
- Draft 0.16 becomes Draft 0.17; full TRD approval remains outstanding.

### 2026-09-27 — Separate catalog ordering merge

- Exact proposal: “preserving priority order [A, C], appending missing elements deterministically, then assigning consecutive integer Order values: [A, C, B]. Equal numeric order values would not mean duplicate entities.”
- Accepted answer: “agree”.
- Source: requesting user in this chat, following the instruction to synchronize Order separately, use priority database order and normalize collections with missing/duplicate positions.
- Decision: replace the earlier open ordering section with the separate ordering contract above; normalize after accepted membership merge and prevent order maintenance from overwriting content. Exact tie-breakers and order tracking remain open, not inferred approvals.
- Affected IDs: PM-002/005/008; shared tracking contract; UC-009-04; OP-043; TQ-12/TQ-04.
- Draft 0.17 becomes Draft 0.18; BRD alignment and full TRD approval remain outstanding.

### 2026-09-27 — GUID tie-breaks for catalog ordering

- Exact proposal: “Equal Order values in the priority collection: sort those elements by GUID ascending” and “Multiple elements missing from the priority collection: append them in GUID ascending order”; all devices use the same comparison rule.
- Accepted answer: “agree”.
- Source: requesting user in this chat, after confirming the two uses of GUID ordering.
- Decision: apply GUID ascending only to equal priority Order values and the appended missing-member list, as defined in the separate ordering contract. Canonical comparison representation remains open in TQ-12.
- Affected IDs: PM-002/005/008; OP-043; TQ-12.
- Draft 0.18 becomes Draft 0.19; full TRD approval remains outstanding.

### 2026-09-27 — Catalog Order integer type and range

- Exact question: “use Order: int32, numbered 0..N-1 within each group after normalization?”
- Accepted answer: “yes”.
- Source: requesting user in this chat.
- Decision: the shared catalog ordering contract defines required Order as int32 with consecutive zero-based values per group after normalization; prior merge and tie-break rules remain unchanged.
- Affected IDs: PM-002/005/008; OP-043; TQ-12.
- Draft 0.19 becomes Draft 0.20; full TRD approval remains outstanding.

### 2026-09-28 — Independent subgroup and element sequences

- Exact question: “Do you mean child groups and elements have separate ordering sequences within a parent—each 0..N−1—but both use the same separate-from-content sync rules?”
- Accepted answer: “use cannot see mix with subgroups and elements. So we should ordering separately subgroups 0..N and elements 0...N”.
- Source: requesting user in this chat.
- Decision: apply the shared ordering contract independently to direct subgroups and direct elements; no mixed list or shared index space. Preserve the previously accepted zero-based consecutive convention, using each collection's own count.
- Affected IDs: PM-001/002/005/008; OP-043; TQ-12.
- Draft 0.20 becomes Draft 0.21; full TRD approval remains outstanding.

### 2026-09-28 — Currency catalog ordering

- Exact question: “should currencies also have manual ordering with the same sync rules? Your Currency model already has Order.”
- Accepted answer: “Yes”.
- Source: requesting user in this chat.
- Decision: extend the shared catalog ordering contract to Currency, with its own dataset-wide zero-based int32 sequence and ordering synchronization separate from content. CurrencyRate ordering is not changed.
- Affected IDs: PM-003; currency DTO/mutation and sync contracts; OP-043; TQ-12.
- Draft 0.21 becomes Draft 0.22; full TRD approval remains outstanding.

### 2026-09-28 — Per-entity modification flags and deletion normalization

- Exact closing question: “I recommend normalizing locally after deletion and flagging the shifted elements. Agree?”
- Accepted answer: “agree”.
- Source: requesting user in this chat, following agreement to track reordered members individually with ModificationType flags and to prefer modified Local order regardless of content priority.
- Decision: replace IsModified with None=0, Content=1, Order=2 flags; record creation, reorder, movement and deletion-normalization rules in the shared contract. Supersede unconditional configured-priority ordering. Partial-sequence reconciliation remains explicitly unresolved in TQ-12.
- Affected IDs: shared tracking contract; PM-001–PM-006/008/010/011; catalog order PM-001/002/003/005/008; OP-043; TQ-04/TQ-12.
- Draft 0.22 becomes Draft 0.23; full TRD approval remains outstanding.

### 2026-09-28 — Reconcile concurrent catalog ordering

- Exact question: “when Local and Master ordering changes combine, accept sorting by merged Order, then GUID, followed by renumbering—even if this slightly changes the intended placement?”
- Accepted answer: “yes”.
- Source: requesting user in this chat.
- Decision: the shared ordering contract now defines sorting and normalization after per-member Local/Master Order selection. Concurrent placement shifts are acceptable. Canonical GUID comparison and transport remain unresolved.
- Affected IDs: PM-001/002/003/005/008; OP-043; TQ-12.
- Draft 0.23 becomes Draft 0.24; full TRD approval remains outstanding.

### 2026-09-28 — Canonical GUID ordering comparison

- Exact question: “for identical GUID comparison everywhere, compare canonical lowercase GUID strings ordinally—no culture-dependent sorting?”
- Accepted answer: “correct”.
- Source: requesting user in this chat.
- Decision: apply the shared canonical lowercase GUID string/ordinal ascending comparison contract to ordering tie-breaks and appended members across Master and all Locals. Physical identifier storage remains unaffected.
- Affected IDs: PM-001/002/003/005/008; OP-043; TQ-12.
- Draft 0.24 becomes Draft 0.25; full TRD approval remains outstanding.

### 2026-09-28 — Order-only changes require synchronization

- Exact question: “Should Order alone mark the database as having changes to synchronize?”
- Accepted answer: “Yes”.
- Source: requesting user in this chat; preceding input: “We can add "Currency reordering" to the report”.
- Decision: ModificationType != None indicates pending local synchronization work, including Order alone. Record the requested report label while retaining received-change reporting scope; exact report representation remains TQ-08.
- Affected IDs: shared ordering contract; UC-009-05; OP-043/050; DTO-019/024; TQ-04/TQ-08/TQ-12.
- Draft 0.25 becomes Draft 0.26; full TRD approval remains outstanding.

### 2026-09-28 — Single currency-reordering report message

- Exact question: “show Currency reordering once per sync when received currency order changed, rather than counting every renumbered currency?”
- Accepted answer: “yes.” Additional direction: “Generally in the future we can add something like Reordering elements in group {name of group}. We can put this single time in the report if any element is gotten new Order. But this is in the future”.
- Source: requesting user in this chat.
- Decision: one Currency reordering message per report when received currency Order changes; group-specific reordering messages are future work, not current-release requirements. Detailed report count derivation remains TQ-08.
- Affected IDs: DTO-024; OP-050; UC-012-01; TQ-08.
- Draft 0.26 becomes Draft 0.27; full TRD approval remains outstanding.

### 2026-09-28 — Rate lookup by device-local calendar date

- Confirmed explanation: UTC transaction storage; date-only currency rates; default/Restore Rate lookup by transaction date in current UI/device timezone; report calendar boundaries converted to UTC instants.
- Accepted answer: “agree. Make corrections”.
- Source: requesting user in this chat, explicitly authorizing BRD and TRD alignment after the UTC+8 example.
- Decision: correct PM-004 and OP-030, retain stored entry rate independence and add the January 1 example. BRD is controlled Draft 0.33; prior 0.32 approval does not cover it.
- Affected IDs: PM-004/006/007; UC-004-05; DTO-007/009/012; OP-030/034/037; TQ-05.
- Draft 0.27 becomes Draft 0.28; full-version approval remains outstanding.

### 2026-09-28 — Initial-rate date constant and derived marker

- Proposal: fixed initial-rate date, derived IsInitial, ordinary dates from 2001-01-01, hidden immutable initial date and non-deletable initial rate.
- Accepted correction: “Initial day rate. Let's make it 01-01-1970 or something else. SqLite doesn't support 0001-01-01. Maybe we should put this value into constants. All others statements are OK”.
- Source: requesting user in this chat. Use the explicitly suggested 1970-01-01 date as the shared constant.
- Decision: PM-004 defines the constant and derived marker. The selected date is an application convention, not a SQLite minimum-date constraint: SQLite documents date-function support from 0000-01-01 through 9999-12-31 (https://www.sqlite.org/lang_datefunc.html).
- Affected IDs: PM-004; DTO-007; OP-017/019/020; TQ-01.
- Draft 0.28 becomes Draft 0.29; full TRD approval remains outstanding.

### 2026-09-28 — Remove CurrencyLocked; currency immutable after creation

- Exact question: “Does closes the creating dialog mean successfully saves the new account? Canceling would create nothing.”
- Accepted answer: “yes”.
- Source: requesting user in this chat, following the explicit request to remove CurrencyLocked.
- Decision: PM-005 and DTO-008 no longer contain CurrencyLocked. Currency is editable before first successful account save only; transaction/template use no longer establishes a lock. Related mutation/sync contracts now preserve this invariant. BRD aligned as Draft 0.34.
- Affected IDs: PM-005/009; DTO-008; UC-004-07/006-01; OP-021/022/027/033/035/043; TQ-02.
- Draft 0.29 becomes Draft 0.30; full-version approval remains outstanding.

### 2026-09-28 — Template Description mirrors Name on each save

- Exact question: “Should the app copy Name → Description on every template save, including renaming, or only when creating the template?”
- Accepted answer: “every”.
- Source: requesting user in this chat, after choosing both Description and Comment with Description hidden in version 1.
- Decision: PM-008 persists hidden Description and visible optional Comment. Every template save copies final Name to Description; applying a template continues to copy Comment to the transaction. BRD aligned as Draft 0.35.
- Affected IDs: PM-008; DTO-011; UC-006-01; OP-033/034/036.
- Draft 0.30 becomes Draft 0.31; full-version approval remains outstanding.

### 2026-09-28 — Description and Comment semantics

- Exact question: “Keep a comment on each rate, for notes such as bank exchange rate or manual estimate, separate from the currency's own comment?”
- Accepted answer: “yes”, with clarification that Description is a mandatory long name for groups/elements and Rate, Template and Transaction comments are optional.
- Source: requesting user in this chat.
- Decision: add optional CurrencyRate.Comment in PM-004 and DTO-007; document common Description/Comment semantics without changing hidden Template.Description behavior. BRD aligned as Draft 0.36.
- Affected IDs: PM-001/002/004/005/006/008; DTO-007; OP-017/019.
- Draft 0.31 becomes Draft 0.32; full-version approval remains outstanding.

### 2026-09-28 — Simplified visible text fields

- Accepted direction: mandatory Name, optional Description, remove Comment; replace rate Comment with Description. Transaction replacement and Template.Description-to-Transaction.Description copying confirmed by “yes”; visibility clarified as “no hidden”.
- Source: requesting user in this chat.
- Decision: align schemas, DTO nullability, save validation and copy operations with BRD-009/022. Template.Description is visible and independent; no automatic Name copy. Historic clarification entries are retained as superseded provenance.
- Affected IDs: PM-001–PM-006/008; DTO-004–DTO-009/011; UC-003/004/005/006; OP-006/010/022/029/033/034/036.
- Draft 0.32 becomes Draft 0.33; BRD aligned as Draft 0.37; full-version approval remains outstanding.

### 2026-09-28 — Favorite content flag

- Exact question: “Should favorites synchronize across devices as ordinary content changes, using ModificationType.Content and the configured conflict priority?”
- Accepted answer: “yes”, following the request to keep favorite flags and filtering for groups/elements in version 1.
- Source: requesting user in this chat.
- Decision: add the shared IsFavorite contract for PM-001/002/005/008. Track changes as Content, not Order. BRD aligned as Draft 0.38; remaining scope/default/filter details are explicit in TQ-13.
- Affected IDs: PM-001/002/005/008; BR-044 trace; TQ-05/13.
- Draft 0.33 becomes Draft 0.34; full-version approval remains outstanding.

### 2026-09-28 — Currency favorites

- Exact question: include currencies in favorites too? Your Currency model already has IsFavorite.
- Accepted answer: yes.
- Source: requesting user in this chat.
- Decision: currencies support favorites and filtering with the same ordinary Content synchronization and configured conflict-priority rules as groups/elements.
- Affected IDs: PM-003; currency DTOs/mutations; BR-044 trace; TQ-13.
- Draft 0.34 becomes Draft 0.35; full-version approval remains outstanding.

### 2026-09-28 — Favorite default

- Exact question: should IsFavorite default to false for newly created items?
- Accepted answer: yes.
- Source: requesting user in this chat.
- Decision: new favorite-capable items default to IsFavorite = false; existing favorite values and synchronization rules are unchanged.
- Affected IDs: PM-001/002/003/005/008; TQ-13.
- Draft 0.35 becomes Draft 0.36; full-version approval remains outstanding.

### 2026-09-28 — Root groups remain non-favorite

- Exact question: may users mark root groups as favorites, or should roots always remain non-favorites?
- Accepted answer: non favorite.
- Source: requesting user in this chat.
- Decision: all five root groups have IsFavorite fixed to false; the general favorites feature does not relax root protection.
- Affected IDs: PM-001; shared favorites contract; TQ-13.
- Draft 0.36 becomes Draft 0.37; full-version approval remains outstanding.

### 2026-09-28 — Ancestor paths in favorites filtering

- Exact question: when filtering favorites, show non-favorite parent groups only as navigation paths to favorite descendants? They would remain non-favorites.
- Accepted answer: yes.
- Source: requesting user in this chat.
- Decision: retain necessary ancestor paths without changing IsFavorite; do not treat navigation ancestors as favorite matches.
- Affected IDs: BR-044 trace; favorites contract; TQ-13.
- Draft 0.37 becomes Draft 0.38; full-version approval remains outstanding.

### 2026-09-28 — Favorites-only children

- Exact question: when opening a favorite group with the filter active, show only favorite descendants, or all its children?
- Accepted answer: only favorite.
- Source: requesting user in this chat.
- Decision: keep the filter active inside favorite groups; retain only favorite descendants and their necessary navigation ancestors, without inherited favorite status.
- Affected IDs: BR-044 trace; favorites contract; TQ-13.
- Draft 0.38 becomes Draft 0.39; full-version approval remains outstanding.

### 2026-09-28 — Immutable precision settings and balancing button

- Accepted sequence: separate APr/RPr; round each BaseAmount to APr; offer a base-currency balancing entry for any difference only when the user clicks Add balancing entry.
- Final answer: configured at the creating. Set in System config. User cannot change them. So user cannot change Base currency, APr, RPr.
- Source: requesting user in this chat.
- Decision: record creation-time immutable settings and the explicit balancing action; prior fixed-four-place/no-assisted-adjustment rules are superseded. Precision ranges/defaults, balancing-account configuration details and physical scaling remain unresolved.
- Affected IDs: PM-004/007/009/011; OP-003/027/042; TQ-01/05; BR-017/018.
- TRD Draft 0.39 becomes 0.40; full-version approval remains outstanding.

### 2026-09-28 — Precision bounds and defaults

- Exact question: what allowed ranges and defaults should APr and RPr have?
- Accepted answer: 0-4. ARr - 2 default, RPr - 4 default. ARr is interpreted as APr in context.
- Source: requesting user in this chat.
- Decision: both precisions accept integer values 0 through 4 inclusive; APr defaults to 2 and RPr defaults to 4. Creation-time immutability remains unchanged.
- Affected IDs: PM-011; TQ-01; creation/configuration contracts.
- TRD 0.40 becomes Draft 0.41; full-version approval remains outstanding.

### 2026-09-28 — Display precision and SQLite scaling

- Exact question: remove DisplayDecimalPlaces and display amounts using APr and rates using RPr?
- Accepted answer: Yes. And for SqLite we use 4 digits scale.
- Source: requesting user in this chat, following scaled-integer SQLite storage discussion.
- Decision: remove independent display precision; fixed SQLite INTEGER scaling is 10,000 for Amount/Rate regardless of APr/RPr. Calculation rounding remains governed by APr/RPr; BaseAmount stays derived.
- Affected IDs: PM-004/007/009/011; DTO-015; TQ-01.
- TRD 0.41 becomes Draft 0.42; full-version approval remains outstanding.

### 2026-09-28 — Shared balancing-account setting

- Exact question: store the balancing-account selection in System config, shared across devices?
- Accepted answer: yes.
- Source: requesting user in this chat.
- Decision: the base-currency balancing-account selection belongs to synchronized System configuration. Selection/default and unavailable-account behavior remain unresolved.
- Affected IDs: PM-011; DTO-015; TQ-05; balancing action contract.
- TRD 0.42 becomes Draft 0.43; full-version approval remains outstanding.

### 2026-09-28 — Initialize rebalancing account and handle its deletion

- User direction: create five roots plus one rebalancing account in the root group during initialization; if deleted, explain that automatic rebalancing cannot proceed and ask the user to choose a rebalancing account in Settings.
- Source: requesting user in this chat; replaces the suggestion to start with an empty selection.
- Decision: create/select an ordinary base-currency account in Account root; missing/deleted account blocks the assisted action without modifying the transaction and displays the Settings guidance. Ordinary reference-based deletion protection remains.
- Affected IDs: PM-005/011; UC-001-03; OP-003/023/042; balancing action; TQ-05.
- TRD 0.43 becomes Draft 0.44; full-version approval remains outstanding.

### 2026-09-29 — Initial rebalancing account name

- Question: use "Rebalancing" as its initial name?
- User answer: "yes".
- Decision: the base-currency account created in the Account root during initialization has initial Name = Rebalancing.
- TRD 0.44 becomes Draft 0.45. Full-version approval remains outstanding.

### 2026-09-29 — Nullable balancing-account selection

- Question: Next: I suggest nullable `BalancingAccountId`. When the selected account is deleted, clear it to `null`. The user selects a replacement in Settings. Agree?
- User answer: agree. Is `BalancingAccountId` property of SystemConfig?
- Source: requesting user in this clarification conversation.
- Decision: nullable GUID selection belongs to synchronized SystemConfiguration; clear it on deletion of the selected account.
- Affected IDs: PM-011; TQ-05.
- TRD 0.45 becomes Draft 0.46; full-version approval remains outstanding.

### 2026-09-29 — Base currency in SystemConfiguration

- Question: Next: `BaseCurrencyId` placement is still marked as proposed. I recommend storing it in `SystemConfig`, alongside `APr`, `RPr`, and `BalancingAccountId`. Required GUID, selected during initialization, then immutable. Agree?
- User answer: Obviously.
- Source: requesting user in this clarification conversation.
- Decision: confirm required GUID BaseCurrencyId in SystemConfiguration, selected during initialization and immutable afterward.
- Affected ID: PM-011.
- TRD 0.46 becomes Draft 0.47; full-version approval remains outstanding.

### 2026-09-29 — Derive default account-name format

- Question: Next: account-name format. We already store component order and separator. I suggest deriving `DefaultAccountName` from those settings instead of storing a duplicate format string. Agree?
- User answer: Yes. Of course. We should store some kind of `DefaultAccountNameOrder` and `DefaultAccountNameSepatator`.
- Source: requesting user in this clarification conversation.
- Decision: store order and separator; derive the format. Normalize the spelling to DefaultAccountNameSeparator.
- Affected IDs: PM-012; DTO-015 (proposed field names aligned); TQ-01.
- TRD 0.47 becomes Draft 0.48; full-version approval remains outstanding.

### 2026-09-29 — Account-name order enum

- Question: Next: represent the order as an enum with six values, one for each permutation of Correspondent, Category, and Project?
- User answer: agree
- Source: requesting user in this clarification conversation.
- Decision: DefaultAccountNameOrder is an enum with exactly six permutation values; retain the existing Correspondent,Category,Project default. Numeric assignments remain unresolved.
- Affected ID: PM-012.
- TRD 0.48 becomes Draft 0.49; full-version approval remains outstanding.

### 2026-09-29 — DatasetKey naming and representation

- Source: requesting user in this clarification conversation.
- User direction: `SystemConfig.DatasetKey` as Guid.ToString(). Use Id as primary key, {Entity}Id as foreign key, and another suffix, preferably Key, for other identifiers.
- Decision: record this explicit naming/type correction; DatasetKey is a string, not the SystemConfiguration primary key. Align corresponding dataset identifier names/types in proposed schemas. The earlier proposal to generate once, preserve across copies and reject mismatches is not treated as accepted by this correction.
- Affected IDs: PM-011/013/014/016; DTO-017; TQ-02.
- TRD 0.49 becomes Draft 0.50; full-version approval remains outstanding.

### 2026-09-29 — DatasetKey lifecycle

- Question: For its lifecycle: generate once during initialization, preserve across Master/Local copies, and reject sync between different dataset keys. Agree?
- User answer: Agree.
- Source: requesting user in this clarification conversation.
- Decision: confirm DatasetKey initialization, preservation and mismatched-dataset rejection.
- Affected IDs: PM-011; TQ-02.
- TRD 0.50 becomes Draft 0.51; full-version approval remains outstanding.

### 2026-09-29 — LocalCopyKey identity and lifecycle

- Question: Next: identity of a Local database copy. I suggest `LocalCopyKey`, stored in `LocalConfig` and registered on Master: string generated with `Guid.NewGuid().ToString()`; created when registering a new Local copy; preserved when sync replaces its database; a fresh installation gets a new key. Agree?
- User answer: agree
- Source: requesting user in this clarification conversation.
- Decision: confirm LocalCopyKey name, string representation, placement and lifecycle. Align corresponding proposed DTO fields without approving their complete shapes.
- Affected IDs: PM-012/014/015; DTO-016/017/019/020/022/023.
- TRD 0.51 becomes Draft 0.52; full-version approval remains outstanding.

### 2026-09-29 — Master, Local and synchronization key names

- Question: Next: use `SyncKey` for a synchronization operation, also generated with `Guid.NewGuid().ToString()`. Reuse it for retries of that operation; generate a new key for the next operation. This prevents a retry from applying changes twice. Agree?
- User answer: Agree. But let's slightly rename: MasterDatasetKey, LocalDatasetKey, SyncKey.
- Source: requesting user in this clarification conversation.
- Decision: rename DatasetKey to MasterDatasetKey and LocalCopyKey to LocalDatasetKey, preserving their accepted placement and lifecycle; confirm SyncKey as a generated GUID string reused for retries of the same operation. Align active schema and prose references; retain historical clarification logs. Proposed DTO shapes and detailed durable sync protocol remain unresolved.
- Affected IDs: PM-011–016; DTO-016–020/022–024; synchronization and recovery contracts.
- TRD 0.52 becomes Draft 0.53; full-version approval remains outstanding.

### 2026-09-30 — Global revision per accepted delta batch

- User direction: use one incrementing merge revision for all entities changed in that merge; new A, edited B and deleted C all receive revision 16 when the previous merge revision was 15.
- Question: I recommend one revision per newly accepted delta batch (`SyncKey`), rather than one per HTTP request containing several batches. Retrying an already accepted batch does not allocate another revision. Is that what you mean by each merge?
- User answer: >>I recommend one revision per newly accepted delta batch — Totally agree
- Source: requesting user in this clarification conversation.
- Decision: replace per-entity increments with global per-batch revision allocation, preserving entity revision comparison and independent snapshot Version.
- Affected IDs: shared tracking contract PM-001–006/008/010/011; PM-015; OP-043; TQ-04.
- TRD 0.53 becomes Draft 0.54; full-version approval remains outstanding.

### 2026-09-30 — Same-Local content-conflict priority

- Question: Next: confirm your priority rule—if the entity current revision came from the same `LocalDatasetKey`, incoming content wins despite a revision mismatch; otherwise, use configured priority?
- User answer: Confirm
- Source: requesting user in this clarification conversation.
- Decision: activate the same-Local exception using the source of the current entity revision in the accepted batch mapping; retain business validation and independent Order handling.
- Affected IDs: shared tracking contract PM-001–006/008/010/011; PM-015; OP-043; TQ-04. BRD priority wording requires downstream alignment.
- TRD 0.54 becomes Draft 0.55; full-version approval remains outstanding.

### 2026-09-30 — Zero revision for submitted creation

- Proposal: EditRevision null means new/never submitted; 0 means creation submitted with unknown Master revision; positive means a known Master revision. Atomically save the outgoing creation batch and set the local row to 0 before sending; preserve 0 after failure and use soft deletion until the outcome is known.
- Question: Shall we adopt this?
- User answer: Yes. Adopt
- Source: requesting user in this clarification conversation, following the requirement that later batches edit or soft-delete an entity possibly present on Master.
- Decision: adopt the three states and durable pre-send transition; preserve creation semantics in the original batch and process later edits/deletions in batch order.
- Affected IDs: shared tracking contract PM-001–006/008/010/011; PM-015; OP-043; TQ-04.
- TRD 0.55 becomes Draft 0.56; full-version approval remains outstanding.

### 2026-09-30 — Clear captured flags when saving the batch

- Question: Once changes are saved in a delta batch, clear their local ModificationType flags in the same local transaction. The saved batch remains pending until successful sync. Later edits set the flags again, so the next batch contains only newer changes. Sync is needed when queued batches exist or any modification flags are set. Agree?
- User answer: Agree
- Source: requesting user in this clarification conversation.
- Decision: adopt atomic batch capture and captured-flag clearing; use queued batches OR uncaptured flags for pending-sync detection. Supersede clearing only after installation.
- Affected IDs: shared tracking/order contracts; PM-015; OP-043; TQ-04/TQ-12.
- TRD 0.56 becomes Draft 0.57; full-version approval remains outstanding.

### 2026-09-30 — Unified snapshot Revision and field names

- User direction: use EditRevision / DeleteRevision; define Revision in BRD or TRD and check assignment rules. EditRevision null (new) or 0 (unknown Master revision) is set on Local; >=1 is assigned by Master. DeleteRevision null (alive) is set on both, 0 (soft deletion) on Local, >=1 on Master.
- Source: requesting user, following agreement that each accepted batch produces a snapshot.
- Decision: define one snapshot Revision counter; rename DeleteVersion to DeleteRevision throughout active contracts. Clarify that Local can copy positive values from Master, that ordinary edits retain known revisions, and that accepted deletion sets both fields to the same Revision. Preserve historical logs.
- Affected IDs: shared tracking and snapshot contracts; PM-001–016; OP-043–049.
- TRD 0.57 becomes Draft 0.58; full-version approval remains outstanding.

## Approval

- Current decision: **Not submitted**.
- Exact TRD version reviewed: none.
- Intended approver: requesting user.
- Decision date: not applicable.
- Scope: consolidated technical requirements, detailed use cases, proposed schemas and API/BFF contracts.
- Next step: review this Draft 0.58 and settle technical choices in place. Do not reopen settled BRD questions. No TRD approval is inferred from the instruction to begin drafting.

