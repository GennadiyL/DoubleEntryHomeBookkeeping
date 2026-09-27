# Requirements Discovery: Personal Bookkeeping

## Status and Sources

- Feature folder: `docs/requirements/001-personal-bookkeeping/`.
- Version: 0.1.
- Status: Draft. Non-authoritative working record, including confirmed brainstorming decisions.
- Recorded: 2026-09-25; updated: 2026-09-26. Sources for subsequent clarifications and feature separation: requesting user in this task.
- Approval decision: None recorded for this artifact version.
- Approval provenance: Not supplied. Conversational confirmations below are not document approval.
- Separate discovery-to-business promotion decision: None recorded here. The user requested discovery first, followed later by BRD/TRD template rework.
- Sources: This task's brainstorming conversation, recorded 2026-09-25 and 2026-09-26; legacy [BRD Draft 0.1](../BRD.md) and [TRD Draft 0.1](../TRD.md), dated 2026-09-22; repository AGENTS.md; GL Analysis discovery template and artifact contract.
- Source distinction: Decisions below were explicitly confirmed by the user in this conversation unless identified as legacy, an assumption, or an open question. The original earlier conversation behind the legacy documents was not available separately.
- Authority: Discovery does not approve a BRD or authorize implementation. BRD drafting requires a qualifying promotion decision; TRD rework requires an approved BRD. Existing documents are drafts, not approved baselines.

## Original Request

> Let's make brainstorming.
> I added this plugin, but TRD and BRD were created before this installation.
> So We need to create discovery document and after that rework BRD and TRD according template in the [@GL Analysis](plugin://gl-analysis@git-plugins) plugin.
> Let's start with brainstroing.
> Do we have open questions now?

Save/pause instruction:

> Yes. Put all into appropriate documents. Let's continue tomorrow


Session-close instruction, 2026-09-26:

> Ok. In this case. Put all info in docs. And I am going to start separate chat for each of 3 features

The three features named by the user are First using, Administration, and Synchronisation. This separates future discovery work; it does not approve document versions.

## Problem or Opportunity

Interpretation: consolidate the existing personal double-entry bookkeeping requirements and today's decisions into discovery before reorganizing the BRD and TRD using GL Analysis. Existing documents predate that plugin and contain unresolved questions and outdated wording.

The product is personal home accounting software. Users organize accounts and classifications, enter balanced transactions in multiple currencies, reuse templates, and report movements. Quantified success criteria and the exact approval identity remain unknown.

## Intended Outcomes

- First version: manual bookkeeping, basic configurable reports with saved settings, transaction templates, transaction duplication, and group merging.
- Personal SQLite database in all versions; no separate multi-user roles/access-rights model requested.
- Strict double-entry accounting for active transactions; unbalanced drafts excluded from calculations.
- Preserve future synchronization needs, particularly permanent account-currency locking.
- Core-functionality brainstorming concluded on 2026-09-26 as sufficiently clarified for subsequent document review. User will open separate chats for First using, Administration, and Synchronisation; BRD/TRD template rework remains subsequent work.

## Stakeholders

- Personal database user: sole product user described in this conversation.
- Requesting user: supplied decisions and scope; formal approver identity not supplied.
- No additional stakeholders identified. Do not invent administrators or collaborative roles.

## Raw Ideas

- User proposed reports built by selecting Category, Project, and Correspondent trees, then filtering by date and grouping results. Decisions below capture the refined behavior.
- User proposed configurable account names built from classification short names, with a list of format options.
- User proposed currency choices populated from system region data, for example Windows RegionInfo values `ISOCurrencySymbol`, `CurrencySymbol`, and `CurrencyEnglishName`.
- User proposed saved report JSON containing IDs and ignoring missing entities.
- User proposed future bulk account creation by copying an account for each correspondent in a group, preserving all other attributes.
- User proposed future bulk restoration of account names with a user-selected account list.
- User suggested an old initial-rate date such as 1753-01-01 only as an example, not a fixed requirement.

## Alternatives Considered

- Explicit posting versus automatic activation: automatic activation on saving a balanced valid transaction chosen.
- Reverting an active transaction to draft versus rejecting invalid edits: reject invalid saves chosen.
- Balance tolerance versus rounding each BaseAmount: per-entry rounding to four decimal places, followed by an exact zero-sum check chosen.
- Automatic rounding adjustment versus manual correction: manual correction only chosen.
- Incrementing suffixes `_1`, `_2`, `_3` versus repeatedly appending `_1`: repeated `_1` chosen.
- Relative report periods versus fixed date bounds: fixed optional From/To dates chosen; day/week remain grouping options.
- Snapshot report group membership versus current membership: save group IDs and recalculate current membership chosen.
- Simple global exclusion precedence versus explicit selection overrides: user checkbox choices with descendant overrides chosen.

## Assumptions

- The session is recorded under 2026-09-25 from the supplied environment date. Individual decision timestamps are not independently available.
- Broad confirmation that users can rearrange groups/elements is interpreted within their respective types; cross-type conversion was not requested.
- Account-name duplicate permission is interpreted to cover both generated and manually edited names, based on the user's correction allowing duplications; clarify if narrower intent was meant.
- Currency choices should be deduplicated by ISO code to satisfy confirmed code uniqueness. Source precedence where regions give different names/symbols is not decided.
- Mandatory trimmed descriptions are interpreted as nonblank; explicit whitespace-only Description behavior should be confirmed if needed.
- Display of 0-4 decimal places was agreed, but ownership of that display setting was not specified.
- No assumption is an approved implementation requirement.

## Open Questions

No remaining gap currently prevents a clearly labeled draft describing the agreed first-version behavior. The following can remain explicit questions in a draft, but material details must be settled before approval:

1. Synchronisation is assigned to a separate feature chat. Carry forward permanent account-currency locking and the unresolved legacy synchronization trigger; no new synchronization behavior or release scope was decided here.
2. Success criteria and approval: what constitutes a usable first release, and who will approve exact document versions? No formal document approval or promotion decision has been recorded.
3. Account and Account Group report grouping are deferred to the next version; their aggregation details are not first-version open questions (user correction, 2026-09-26).
4. Saved report selection: nested group inheritance and minimal included/excluded ID storage are resolved (2026-09-26). Remaining: partial checkbox display; conflicting inherited states after hierarchy moves; missing IDs that later reappear. Preserve saved choices until resave and explicit-element overrides.
5. Saved reports: exact timestamp-based default-name format, empty-name behavior, and edit/delete workflow remain open. Names may duplicate. New reports start with every selection unchecked, including unassigned (user confirmation, 2026-09-26).
6. Account naming: final predefined format list remains open. All classifications absent is resolved for the slash format: default Name is // and is valid without manual replacement (user confirmation, 2026-09-26). Strict positional formatting preserves separators.
7. Numeric boundaries: maximum magnitudes, overflow, and display precision settings remain unspecified. Excess decimal input is resolved: UI prevents more than four fractional digits; API validates and rounds to four using midpoint-to-even (user answer, 2026-09-26). Four-place nearest-even BaseAmount rounding is settled.
8. Dates: exact hidden initial-rate sentinel remains open. The minimum transaction timestamp is 2001-01-01 00:00:00 UTC, checked after conversion to UTC. Current timezone means the device timezone, with no separate app setting (user confirmations, 2026-09-26). UTC rate lookup and local report boundaries are settled.
9. Currency catalog: supported platforms, source refresh behavior, unsupported ISO currencies, and which region's default Name/Symbol to restore. The Windows example is input, not a mandate for a Windows-only implementation.
10. Merge edge cases: root as source is forbidden by root immutability; merging into a descendant is rejected by cycle prevention (confirmed 2026-09-26). Root as destination and merging into an ancestor remain unspecified. Recursive coalescing of same-name subgroups was not requested; collision renaming was chosen.
11. Transaction/template details: creating a template from an unbalanced draft is allowed (user confirmation, 2026-09-26). Remaining details: whether zero-entry template application opens an unsavable transaction editor until two accounts are added; exact definition of required fields beyond agreed account/count/rate rules. These must not relax the confirmed save constraints.
12. Description defaults resolved (2026-09-26): Name and Description are edited independently; renaming does not change Description.
13. Backup/recovery belongs to Administration and will be discussed in its separate chat. The question about first-version backup/restore was not answered; no inclusion or deferral decision was made. Measurable performance remains a later document-review detail; SQLite speed was an expectation, not a measured target.

## Decisions and Rationale

Session decisions in this section are attributed to the requesting user, recorded 2026-09-25 with subsequent clarifications dated 2026-09-26. Rationale is included only where stated or explicitly distinguished as explanation.

### Scope and storage

- Manual transactions, reports, transaction templates, group merging, transaction duplication, and creating templates from transactions are first-version scope.
- All versions use a personal SQLite database.
- Balances and reports are calculated on demand from transactions; do not assume stored running balances.
- Bank imports and synchronization are deferred. Archive flags, account presets, bulk account creation, and bulk account-name restoration are future work.

### Legacy requirements retained as source input

- Five group/element pairs: AccountGroup/Account, CategoryGroup/Category, ProjectGroup/Project, CorrespondentGroup/Correspondent, TemplateGroup/Template.
- Each hierarchy has exactly one root. Groups have Parent and parent FK, Children, and corresponding Elements. Every element has a mandatory group and group FK.
- Account has mandatory Currency and independently optional Category, Project, and Correspondent.
- Category means activity; Project means something receiving investment and/or producing income; Correspondent is a physical or legal person. These remain separate types.
- User chooses base currency on database creation; it cannot subsequently change.
- Legacy TRD explicitly retains parent navigation and parent FK on TemplateEntry and TransactionEntry, identifying the same parent, for direct child-to-parent navigation. This is existing technical input, not a newly approved design.

### Transactions, drafts, and balances

- Every saved transaction, including drafts, has at least two entries; each entry has a mandatory Account.
- An entry has no independent currency: its currency is its Account's Currency.
- Empty Amount becomes zero. Zero amounts and repeated accounts are allowed, including two entries with zero amounts.
- Positive Amount increases an account balance; negative decreases it. User also expected this convention in the UI.
- A new unbalanced transaction can be saved as a draft. Drafts do not affect balances or reports.
- Saving a complete balanced transaction activates it automatically; no separate posting action.
- Active transactions can be edited but cannot be saved invalid or returned to draft.
- Active transactions may be deleted. Subsequent on-demand calculations omit them.
- New transaction date/time defaults to now and can be edited.
- Transaction duplication is first-version scope: copy accounts, amounts, rates, and comment; date/time defaults to now; user reviews/edits and saves the copy.
- No entry-level description/comment was requested; descriptive text is at transaction level, finally named optional Comment.
- Starting balances are ordinary balanced transactions, e.g. Bank +1000 and Opening Balance -1000. Account presets are deferred; no automatic preset creation is agreed.

### Calculation and precision

- Amount, Rate, and BaseAmount use decimal values with four decimal places; visible precision can be 0-4 places.
- For Amount and Rate input, UI prevents entering more than four fractional digits. API validates input and rounds excess fractional digits to four using midpoint-to-even (user answer to the nearest-even input question, 2026-09-26).
- BaseAmount = Amount multiplied by Rate, rounded per entry to four decimal places using midpoint-to-even rounding.
- Sum rounded BaseAmounts; require exactly zero for an active transaction. No tolerance-based acceptance.
- Balances and reports use those rounded BaseAmounts for base-currency totals.
- Example: 55555.5555 * 0.2222 = 12344.4444321, rounded to 12344.4444. An opposite entry Amount -12344.4444 at Rate 1 produces -12344.4444; sum is zero.
- User fixes imbalance manually. App may show the remaining difference; it does not create balancing adjustments or silently alter entries.
- All rates must be greater than zero after rounding to four decimal places. API raises an exception if rounding produces zero, e.g. 0.00001 becomes 0.0000 (user clarification, 2026-09-26; supersedes the initial negative answer). Base-currency rate is always 1 and cannot be overridden.

### Currency and rates

- ISO currency code is unique database-wide and read-only after creation; user cited a three-letter ISO code from system region data.
- Currency Name and Symbol are editable and can be restored from source defaults. Currency has optional Comment.
- Currency can be deleted only if no account uses it and it is not the database base currency.
- Creating a currency requires an initial rate (base currency remains 1). The initial rate has a hidden, system-controlled old date; user can edit its value but cannot view/change that date or delete the initial rate.
- 1753-01-01 was only an example of the old date. Exact sentinel remains undecided.
- First version supports one ordinary CurrencyRate per currency per UTC date. Ordinary dated rates can be edited/deleted.
- Default entry Rate comes from the latest CurrencyRate on or before the transaction's UTC date, with the initial rate providing the fallback.
- Entry stores its own Rate without a CurrencyRate reference. Changes to CurrencyRate do not propagate to entries.
- User may manually override a non-base entry Rate. Restore Rate reloads the applicable current CurrencyRate value for the transaction's UTC date.
- Changing transaction date leaves stored entry Rates unchanged until manual edit or Restore Rate.

### Dates and timezones

- Store transaction date/time in UTC; display in the current timezone. Current timezone means the device timezone, with no separate app timezone setting (user confirmation, 2026-09-26).
- Report date boundaries and day/week/month grouping use the current timezone.
- Rate lookup uses UTC date, not displayed local date.
- User-entered transaction timestamps must be on or after 2001-01-01 00:00:00 UTC; validate the UTC timestamp after converting local input. For example, 2001-01-01 00:30 in Warsaw corresponds to 2000-12-31 23:30 UTC and is rejected (UTC boundary confirmed by user, 2026-09-26). Ordinary rate dates cannot be earlier than 2001-01-01 UTC. Hidden initial-rate date is the system exception.

### Account lifecycle and classifications

- First use in any saved transaction, including a draft, permanently locks Account Currency.
- Use in a template also locks Account Currency.
- Deleting all references later does not unlock it. User cited avoiding synchronization inconsistency as rationale.
- Any transaction or template reference prevents account deletion, even with a zero balance.
- Category, Project, or Correspondent cannot be deleted while used by an account.
- Account classification changes apply to historical reports; reports use current assignments.
- Archive behavior is deferred.

### Groups and merging

- Root cannot be edited, renamed, moved, or deleted; UI uses fixed labels such as Accounts and Correspondents.
- Root Parent points to itself; root does not occur in its own Children collection.
- Root can directly contain elements.
- Users can rearrange non-root groups and elements within their types, including already-used elements.
- Reject cycles when moving groups. Merging a group into its own descendant is also rejected under this rule (user clarification, 2026-09-26).
- A group can be deleted only if it has no child groups and no elements.
- Merge A into B moves A's elements and child groups into B, resolves naming conflicts, then deletes the empty A.
- Individual operations with a forbidden name collision are rejected (user explicitly described throwing an exception).
- Bulk operations rename incoming conflicting items by appending `_1` repeatedly until unique: `myname`, `myname_1`, `myname_1_1`. Existing destination names stay unchanged.

### Names, descriptions, and comments

- Names are trimmed before saving, compared case-insensitively, and rejected if empty after trimming.
- Group names are unique among siblings. Element names are unique in their own group except Accounts, for which duplicates are allowed.
- Child-group names and element names have separate uniqueness scopes: a subgroup Travel and an element Travel may coexist.
- Template names are unique within their group.
- Accounts, groups, categories, projects, correspondents have mandatory Description, trimmed but not unique, separate from Name.
- All Descriptions initially default to a copy of Name, not a concatenation of long descriptions or an English currency label. Name and Description are edited independently: later changes to either do not update the other (user confirmation, 2026-09-26).
- Transactions, templates, currencies have optional Comment. Earlier transaction 'description' wording is replaced by Comment.
- Default Account Name combines classification short Names in a strict predefined format.
- Format is selected for the whole database from options such as `{Category}/{Project}/{Correspondent}` or `{Category}-{Correspondent}-{Project}`. Exact complete list remains open.
- Missing classifications keep their positions/separators; smarter formatting is deferred. With the {Category}/{Project}/{Correspondent} format and all three classifications absent, default Account Name is // (two slashes), valid without requiring manual replacement (user confirmation, 2026-09-26).
- Users can override Account Name and Restore Default Name.
- Changes to classifications leave Account Name unchanged until explicitly restored.
- Account Names may duplicate; the earlier proposal to reject generated duplicates was explicitly reversed.

### Transaction templates

- Template contains accounts and amounts, plus optional Comment and a group-unique Name.
- Every template entry has mandatory Account.
- Templates need not be balanced or meet the transaction minimum-entry rule; empty templates are allowed.
- Applying template opens a transaction for immediate review; persistence occurs only on Save.
- New entry Rates default from CurrencyRate for the transaction date; base-currency rate remains 1.
- Copy template Comment into transaction Comment.
- Users may create a template from an active transaction or an unbalanced draft, copying accounts, amounts, and comment (draft eligibility confirmed by user, 2026-09-26).

### Report filtering

- First version has exactly three entity selection trees: Category, Project, Correspondent. No Account or Account Group filter.
- Within one entity type, selected elements/groups/null combine using OR. Combine the three entity filters using AND to determine matching accounts.
- Null/unassigned is an explicit selectable option in each classification.
- New reports start with all Category, Project, and Correspondent selections unchecked, including groups, elements, and null/unassigned (user confirmation, 2026-09-26).
- No selection for an entity type yields an empty report with a warning, not an unrestricted filter.
- Group checkboxes select descendants recursively for convenience. User can uncheck subgroups/elements and recheck individual descendants.
- Report considers active transactions only, using matching accounts' entries.
- Optional From/To date range includes both endpoint dates. No relative periods; saved dates are exact fixed dates.

### Saved report selection and current hierarchy

- Saved settings preserve group IDs, individual element IDs, checked/unchecked choices, and null choices rather than only a snapshot of group members.
- Recalculate matching elements using current group membership each time. An element moving out stops matching that group unless individually selected.
- Missing element/group IDs are ignored, including deleted groups or merge sources. Saved report references do not prevent deletion of otherwise-unused entities.
- Both individual elements and groups can be explicitly excluded. Individual element IDs follow the same minimal storage rule as groups: save an inclusion or exclusion only when the element selection differs from the inherited state of its parent group; otherwise omit its ID (user confirmation, 2026-09-26).
- Explicit inclusion of an element overrides its parent group's exclusion. It must not be modeled as unconditional global subtraction of all excluded groups.
- Confirmed example: select GroupA (which contains GroupB, GroupC, GroupD); deselect GroupC; explicitly select ElementX in GroupC. Include A's current descendants outside C, and X inside C.
- Checking GroupC again selects all descendants and clears earlier individual overrides within it. The user confirmed the same propagation/reset principle for checking/unchecking a group.
- New members follow the selected group's state unless an applicable saved override changes it. Earlier saved choices persist until settings are resaved; do not rewrite them merely because membership changed.
- Nested group selections inherit the nearest saved ancestor state; the default with no applicable inclusion is unchecked. Save a checked group ID in included only where its parent state is unchecked, and an unchecked group ID in excluded only where its parent state is checked. Descendants sharing the inherited state are not individually stored (user rule, 2026-09-26).
- Confirmed example: check Group1Level; uncheck its child Group2LevelA; leave Group3LevelAA under A unchecked; check Group4LevelAAA under Group3LevelAA. Group2LevelB remains checked under Group1Level, and Group4LevelAAB remains unchecked under Group3LevelAA. JSON stores included = [Group1Level, Group4LevelAAA], excluded = [Group2LevelA], using their IDs.
- Checking Group2LevelA again selects its whole subtree and clears descendant overrides. Remove both its exclusion and the now-redundant Group4LevelAAA inclusion: JSON retains only Group1Level in included and no exclusions in this example (user confirmation, 2026-09-26).

### Report grouping and values

- Optional Currency is the first grouping level. The next level is one selected grouping: Category, Project, Correspondent, day, week, month, or year. Account and Account Group grouping are deferred to the next version (user correction, 2026-09-26).
- Time grouping supports day, week, month, year. Weeks run Monday-Sunday; months/years use calendar boundaries in current timezone. Quarter is outside the first-version grouping list (user correction, 2026-09-26).
- Show a single signed net total, not separate positive/negative subtotals.
- Without Currency grouping: if all included accounts share one currency, show two amount columns, account currency and base currency; otherwise show base currency only.
- With Currency grouping: currency groups show own-currency and base-currency totals. Mixed-currency grand totals use base currency only.
- Reports are totals only; no click-through to constituent transactions.
- First-version grouping is limited to the choices above; no Account or Account Group rollup display is needed.

### Saved report definitions

- Users can save/reopen report settings because configuring them is complex.
- Save entity selections/exclusions, grouping, and exact optional From/To dates.
- Default report name is timestamp-based; user can replace it; duplicates allowed.
- User proposed storing definitions as JSON with IDs; missing referenced entities are ignored at execution.

### Legacy question disposition

| Legacy question | Session outcome |
| --- | --- |
| Q-01 first use / synchronization | Any saved transaction or template locks currency permanently; synchronization trigger deferred. |
| Q-02 formula / override | Multiply, round each BaseAmount to 4 places; manual non-base overrides allowed. |
| Q-03 rate dates / selection | Latest rate on/before UTC date; mandatory initial fallback; exact sentinel open. |
| Q-04 precision / rounding | Four places, nearest-even, exact rounded sum, manual correction only. |
| Q-05 invalid activities | Drafts excluded from balances/reports; active invalid saves forbidden; synchronization deferred. |
| Q-06 incomplete drafts | At least 2 accounts required; blank Amount = 0; imbalance can be draft. Earlier unrestricted incomplete-draft answer narrowed. |
| Q-07 root | Self-parent, excludes itself from Children, permits direct elements, fixed/immutable. |
| Q-08 lifecycle | Rearrangement/cycle checks, usage-protected deletion, empty group deletion, merge; archive deferred. |
| Q-09 properties / uniqueness | Name/Description/Comment rules above; accounts may duplicate names; other uniqueness per group/parent. |
| Q-10 historical classifications | Current classifications apply to history. |
| Q-11 signs / zero / duplicates | Positive increases, negative decreases; zero amounts/repeated accounts allowed. |
| Q-12 reporting | Three classification filters; minimal included/excluded IDs with inherited states; net totals; Category/Project/Correspondent or day/week/month/year grouping. Account/Account Group grouping deferred. |
| Q-13 templates | Accounts/amounts/comment; mandatory account; empty/unbalanced allowed; save reviewed transaction. |
| Q-14 timezones | UTC persistence/rate date; current timezone display/report grouping and bounds. |

## Rejected Ideas

- Saving an invalid edit to an active transaction or returning it to draft.
- Saving a transaction with fewer than two entries or missing entry accounts, despite the earlier broad draft answer.
- Entry-specific currency and entry-level descriptive fields.
- Automatic balancing/rounding adjustments and tolerance-based zero checks.
- Automatically updating stored Rates after rate-table/date changes.
- Automatically renaming accounts after classification changes.
- Rejecting duplicate Account Names (earlier proposal reversed).
- Root included as its own child, or user-editable root labels.
- Incrementing bulk conflict suffix numbers instead of repeatedly appending `_1`.
- Account/Account Group filters in first-version reports. Grouping by them is also deferred to the next version.
- Empty entity selection meaning unrestricted selection.
- Relative report date periods, separate positive/negative report totals, and report drill-through.
- Fixed snapshot-only group membership and unconditional exclusion precedence over explicitly selected elements.
- Generated long concatenated descriptions: user corrected this to default Description = Name.

## Deferred Topics

| Topic | Owner / revisit trigger |
| --- | --- |
| Bank imports | User; later version. |
| First using | User will open a separate feature chat; carry forward base-currency choice, immutable base currency, and existing opening-balance rules. Detailed first-use workflow remains undecided. |
| Administration | User will open a separate feature chat; backup/restore belongs here and its release scope is undecided. |
| Synchronisation | User will open a separate feature chat; carry forward permanent account-currency locking and personal SQLite storage. Earlier implementation deferral remains unchanged pending that discussion. |
| Account and Account Group report grouping | User; next version, confirmed 2026-09-26. |
| Archive flags | User; future version. |
| Account presets, including Opening Balance | User; future version. |
| Bulk account copying for correspondents in a group | User; future version. |
| Bulk Restore Default Name with selected account list | User; future version. |
| Smarter name formatting for missing classifications | User; future consideration. |
| BRD/TRD conversion into GL Analysis templates | Continue after discovery/promotion decisions; TRD requires approved BRD. No exact date committed. |

## Promotion Readiness

- Ready to seek a discovery-to-business promotion decision: Core brainstorming is sufficiently clarified for a draft with the remaining review details explicitly marked. User requested saving and separate feature chats, not document approval or template rework in this turn.
- Questions blocking a safe BRD draft: No presently identified core gap prevents a draft with explicit open questions; formal promotion authorization still needs to be established before that phase.
- Questions that may remain in a draft BRD: Open Questions above, with material first-version behaviors resolved before approval. Deferred synchronization must not silently become first-version scope.
- Promotion decision reference: None recorded. Saving this session does not approve discovery, BRD, or TRD.
- Next recommended phase: User-led separate chats for First using, Administration, and Synchronisation. Read this discovery and the BRD continuation notes in each chat, retain confirmed core rules, and record any proposed changes explicitly. Later rework BRD from discovery and TRD after BRD approval.
- Handoff, 2026-09-26: Core brainstorming is closed for now. Remaining core details are retained for BRD/TRD review, not silently resolved. User will create the three feature chats. No additional chats were created by the assistant. Avoid re-asking settled questions; use the decisions above.

## Synchronization handoff update — 2026-09-27

Subsequent user decisions are consolidated in [Synchronization discovery v0.21](../002-synchronization/discovery.md). They supersede this record's earlier synchronization deferral: synchronization is included in the first release for Android and Windows desktop, with one user and one MasterDb. See that feature record for all confirmed rules, current copy-and-publish recovery direction, corrections, and remaining questions. Other core decisions remain applicable, including permanent account-currency locking and business-rule-protected deletion. Historical deferral wording above is retained as provenance only. No document approval or BRD/TRD promotion is implied.

