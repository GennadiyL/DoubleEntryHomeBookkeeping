# Requirements Discovery: Personal Bookkeeping

## Status and Sources

- Feature folder: `docs/requirements/001-personal-bookkeeping/`.
- Version: 0.1.
- Status: Draft. Non-authoritative working record, including confirmed brainstorming decisions.
- Recorded: 2026-09-25.
- Approval decision: None recorded for this artifact version.
- Approval provenance: Not supplied. Conversational confirmations below are not document approval.
- Separate discovery-to-business promotion decision: None recorded here. The user requested discovery first, followed later by BRD/TRD template rework.
- Sources: This task's brainstorming conversation, recorded 2026-09-25; legacy [BRD Draft 0.1](../BRD.md) and [TRD Draft 0.1](../TRD.md), dated 2026-09-22; repository AGENTS.md; GL Analysis discovery template and artifact contract.
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

## Problem or Opportunity

Interpretation: consolidate the existing personal double-entry bookkeeping requirements and today's decisions into discovery before reorganizing the BRD and TRD using GL Analysis. Existing documents predate that plugin and contain unresolved questions and outdated wording.

The product is personal home accounting software. Users organize accounts and classifications, enter balanced transactions in multiple currencies, reuse templates, and report movements. Quantified success criteria and the exact approval identity remain unknown.

## Intended Outcomes

- First version: manual bookkeeping, basic configurable reports with saved settings, transaction templates, transaction duplication, and group merging.
- Personal SQLite database in all versions; no separate multi-user roles/access-rights model requested.
- Strict double-entry accounting for active transactions; unbalanced drafts excluded from calculations.
- Preserve future synchronization needs, particularly permanent account-currency locking.
- Continue brainstorming next session, starting with synchronization as the user indicated; migration of BRD/TRD is subsequent work.

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

1. Synchronization: user requested discussion next session. First-version synchronization remains deferred. What future behavior creates the currency-lock concern? The legacy synchronization trigger is unspecified.
2. Success criteria and approval: what constitutes a usable first release, and who will approve exact document versions? No formal document approval or promotion decision has been recorded.
3. Report grouping: define exact Account Group aggregation depth/rollups and overlapping ancestor display. Checkbox discussions settled filtering; they did not fully settle Account Group output structure. Currency is optional first level; one other grouping follows.
4. Saved report selection: define nested subgroup overrides beyond the explicit GroupA/GroupC/ElementX example; partial checkbox display; conflicts after hierarchy moves; missing IDs that later reappear; exact stored representation. Do not replace the confirmed explicit-element override with global exclusion precedence.
5. Saved reports: exact timestamp-based default-name format, empty-name behavior, edit/delete workflow, and default initial selections. Names may duplicate.
6. Account naming: final predefined format list, behavior when all three classifications are absent, and handling resulting empty/only-separator names. Strict positional formatting was chosen; missing slots are not automatically removed.
7. Numeric boundaries: maximum magnitudes, excess decimal input handling, overflow, and display precision settings remain unspecified. Four-place nearest-even BaseAmount rounding is settled.
8. Dates: exact hidden initial-rate sentinel, timezone source, and interpretation of the 2001-01-01 limit at timezone boundaries. UTC rate lookup and local report boundaries are settled.
9. Currency catalog: supported platforms, source refresh behavior, unsupported ISO currencies, and which region's default Name/Symbol to restore. The Windows example is input, not a mandate for a Windows-only implementation.
10. Merge edge cases: root as source is forbidden by root immutability; clarify root as destination and ancestor/descendant merges while preserving cycle prevention and fixed-root behavior. Recursive coalescing of same-name subgroups was not requested; collision renaming was chosen.
11. Transaction/template details: whether a draft may be used to create a template; whether zero-entry template application opens an unsavable transaction editor until two accounts are added; exact definition of required fields beyond agreed account/count/rate rules. These must not relax the confirmed save constraints.
12. Description defaults: whether renaming Name later changes Description; no automatic ongoing synchronization was agreed.
13. Other first-release non-functional needs such as backup/recovery and measurable performance are not yet discussed. SQLite speed was a user expectation, not a measured target.

## Decisions and Rationale

All session decisions in this section are attributed to the requesting user, recorded 2026-09-25. Rationale is included only where stated or explicitly distinguished as explanation.

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
- BaseAmount = Amount multiplied by Rate, rounded per entry to four decimal places using midpoint-to-even rounding.
- Sum rounded BaseAmounts; require exactly zero for an active transaction. No tolerance-based acceptance.
- Balances and reports use those rounded BaseAmounts for base-currency totals.
- Example: 55555.5555 * 0.2222 = 12344.4444321, rounded to 12344.4444. An opposite entry Amount -12344.4444 at Rate 1 produces -12344.4444; sum is zero.
- User fixes imbalance manually. App may show the remaining difference; it does not create balancing adjustments or silently alter entries.
- All rates must be greater than zero. Base-currency rate is always 1 and cannot be overridden.

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

- Store transaction date/time in UTC; display in the current timezone.
- Report date boundaries and day/week/month grouping use the current timezone.
- Rate lookup uses UTC date, not displayed local date.
- User-entered transaction dates and ordinary rate dates cannot be earlier than 2001-01-01. Hidden initial-rate date is the system exception.

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
- Reject cycles when moving groups.
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
- All Descriptions default to a copy of Name, not a concatenation of long descriptions or an English currency label. Users may edit Description separately.
- Transactions, templates, currencies have optional Comment. Earlier transaction 'description' wording is replaced by Comment.
- Default Account Name combines classification short Names in a strict predefined format.
- Format is selected for the whole database from options such as `{Category}/{Project}/{Correspondent}` or `{Category}-{Correspondent}-{Project}`. Exact complete list remains open.
- Missing classifications keep their positions/separators; smarter formatting is deferred.
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
- Users may create a template from a transaction, copying accounts, amounts, and comment.

### Report filtering

- First version has exactly three entity selection trees: Category, Project, Correspondent. No Account or Account Group filter.
- Within one entity type, selected elements/groups/null combine using OR. Combine the three entity filters using AND to determine matching accounts.
- Null/unassigned is an explicit selectable option in each classification.
- No selection for an entity type yields an empty report with a warning, not an unrestricted filter.
- Group checkboxes select descendants recursively for convenience. User can uncheck subgroups/elements and recheck individual descendants.
- Report considers active transactions only, using matching accounts' entries.
- Optional From/To date range includes both endpoint dates. No relative periods; saved dates are exact fixed dates.

### Saved report selection and current hierarchy

- Saved settings preserve group IDs, individual element IDs, checked/unchecked choices, and null choices rather than only a snapshot of group members.
- Recalculate matching elements using current group membership each time. An element moving out stops matching that group unless individually selected.
- Missing element/group IDs are ignored, including deleted groups or merge sources. Saved report references do not prevent deletion of otherwise-unused entities.
- Both individual elements and groups can be explicitly excluded.
- Explicit inclusion of an element overrides its parent group's exclusion. It must not be modeled as unconditional global subtraction of all excluded groups.
- Confirmed example: select GroupA (which contains GroupB, GroupC, GroupD); deselect GroupC; explicitly select ElementX in GroupC. Include A's current descendants outside C, and X inside C.
- Checking GroupC again selects all descendants and clears earlier individual overrides within it. The user confirmed the same propagation/reset principle for checking/unchecking a group.
- New members follow the selected group's state unless an applicable saved override changes it. Earlier saved choices persist until settings are resaved; do not rewrite them merely because membership changed.
- Exact handling of deeper nested group overrides remains a clarification item.

### Report grouping and values

- Optional Currency is the first grouping level. The second level is one selected grouping: time period, Category, Project, Correspondent, Account, or Account Group.
- Time grouping supports day, week, month, quarter, year. Weeks run Monday-Sunday; quarters/years use calendar boundaries in current timezone.
- Show a single signed net total, not separate positive/negative subtotals.
- Without Currency grouping: if all included accounts share one currency, show two amount columns, account currency and base currency; otherwise show base currency only.
- With Currency grouping: currency groups show own-currency and base-currency totals. Mixed-currency grand totals use base currency only.
- Reports are totals only; no click-through to constituent transactions.
- Account/Account Group grouping remains in scope despite removing their selection filters. Exact hierarchy rollup display remains open.

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
| Q-12 reporting | Three classification filters, saved dynamic tree choices, net totals, grouping; output hierarchy details open. |
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
- Account/Account Group filters in first-version reports; grouping by them remains allowed.
- Empty entity selection meaning unrestricted selection.
- Relative report date periods, separate positive/negative report totals, and report drill-through.
- Fixed snapshot-only group membership and unconditional exclusion precedence over explicitly selected elements.
- Generated long concatenated descriptions: user corrected this to default Description = Name.

## Deferred Topics

| Topic | Owner / revisit trigger |
| --- | --- |
| Bank imports and synchronization implementation | User; later version. Synchronization discussion requested for next session. |
| Archive flags | User; future version. |
| Account presets, including Opening Balance | User; future version. |
| Bulk account copying for correspondents in a group | User; future version. |
| Bulk Restore Default Name with selected account list | User; future version. |
| Smarter name formatting for missing classifications | User; future consideration. |
| BRD/TRD conversion into GL Analysis templates | Continue after discovery/promotion decisions; TRD requires approved BRD. No exact date committed. |

## Promotion Readiness

- Ready to seek a discovery-to-business promotion decision: Not yet requested in this pause/save turn; user wants to continue brainstorming next session.
- Questions blocking a safe BRD draft: No presently identified core gap prevents a draft with explicit open questions; formal promotion authorization still needs to be established before that phase.
- Questions that may remain in a draft BRD: Open Questions above, with material first-version behaviors resolved before approval. Deferred synchronization must not silently become first-version scope.
- Promotion decision reference: None recorded. Saving this session does not approve discovery, BRD, or TRD.
- Next recommended phase: Continue discovery, starting with the user's planned synchronization discussion and remaining material ambiguities. Then make the separate promotion decision and rework BRD; rework TRD after BRD approval.
- Resume note: User paused due to tiredness. Avoid re-asking settled questions; use the legacy-question disposition and decisions above.
