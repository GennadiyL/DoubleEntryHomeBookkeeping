# Technical Requirements Document: DoubleEntryHomeBookkeeping

## Document Control and BRD Reference

- Revision 0.232, 2026-10-08: define Business-only WinUI integration against an existing Local database, defer other domains, and record the accepted coordinator account-reference registry. No complete-document approval inferred.

- Revision 0.231, 2026-10-08: record accepted report JSON, post-date-save calculation and CSV-export failure outcomes, aligned with BRD 0.217. No full approval or application implementation inferred.

- Revision 0.230, 2026-10-08: user confirmed CurrencyHeader Amount/BaseAmount are null and appear as blank cells in UI/CSV. Report-row Kind, removal of Key/Level and separate warnings remain confirmed. Draft status retained.

- Revision 0.229, 2026-10-08: user accepted removal of Key and Level from report-result rows. Currency-header numeric fields still require clarification; no decision on their nullability is inferred. Draft status retained.

- Revision 0.228, 2026-10-08: user confirmed flat ordered report-result rows, explicit Data/CurrencyHeader/CurrencySubtotal/GrandTotal kinds, and warnings separate from rows. Header field nullability and obsolete Key/Level proposals remain for clarification. Draft status retained.

- Revision 0.227, 2026-10-08: user confirmed ReportGroup/shared report catalog fields match the other five catalogs. Application is pre-production; update initial schema/seed data for fresh databases instead of migrating old development reports. No scripts or databases are changed in this documentation revision.

- Revision 0.226, 2026-10-08: user confirmed AccountNameOrder, AccountNameSeparator and AccountNameAddCurrency as the LocalConfig naming properties. GetDefaultName reads the saved flag; CurrencyId may be absent with the flag off and must identify a valid currency with it on. DTO fields align; no application code changed or full-version approval inferred.

- Revision 0.225, 2026-10-08: user confirmed reuse of the existing flat ConfigurationInfo read shape and editable-only Settings save input, and replaced the GetDefaultName bool argument with saved LocalConfig.AddCurrencyToAccountName. CurrencyId remains an input; conditional validation is still open. Draft status retained.

- Revision 0.224, 2026-10-08: user confirmed combined Setup Settings read/save, atomic Local/System persistence, and CurrencyId/AddCurrency inputs for account-name generation. Remaining DTO layout and conditional currency validation stay open; no full-version approval inferred.

- Controlled revision 0.223, 2026-10-08: requesting user explicitly requested the four-rule cleanup, aligned to BRD 0.217. Local is the default conflict priority; Currency has no Description while CurrencyRate retains it; AmountPrecision and RatePrecision govern calculation and display; Drafts have no separate entry-count limit, but every present entry must be valid and have an Account. Confirmed state still requires at least two valid entries and exact balance. No complete-document approval is inferred.

- Version: **0.232**. Status: **Draft**. Date: **2026-10-08**.
- Business source: [BRD 0.218](BRD.md), Draft dated 2026-10-08, and the individually confirmed UI/business decisions in this conversation. Prior full-version approval covers BRD 0.32 only, requesting user, 2026-09-27. No approval of complete BRD 0.218 or TRD 0.232 is inferred.
- Scope: core bookkeeping, synchronization, first use and Windows UI integration. Owner/intended approver: requesting user.
- Revision basis: replaces legacy TRD 0.1 while retaining its parent-reference constraint. Discovery sources: [core](001-personal-bookkeeping/discovery.md), [synchronization](002-synchronization/discovery.md), [first use](003-first-using/discovery.md). Later BRD decisions supersede their historical statements.
- Naming: BRD uses Business Capabilities `BC-###`; this TRD uses Detailed Use Cases `UC-###-##`, linked to `OP-###` under `SVC-###`. This is the user-selected convention, replacing the skill's default BRD UC convention.
- Authority: approved business behavior is binding. This TRD's proposed schemas, operation grouping, DTO shapes, error identifiers and concurrency policies require review. Approval of BRD does not approve TRD or authorize implementation. On 2026-10-03 the requesting user explicitly authorized starting implementation of the cumulative-amount and transaction-list decisions; this does not imply full-document approval.
- No previous PM/DTO/SVC/OP IDs existed in legacy TRD; identifiers introduced here are stable for subsequent revisions.
- Draft alignment authorization: requesting user, 2026-10-06, Lets start TRD, following BRD 0.177 and the completed UI discussion. Repository instructions authorize expanding TRD as requirements stabilize. This authorizes draft alignment from individually confirmed decisions despite current BRD Draft status, not formal document approval or application changes.

## Technical Scope and Constraints

### Common C# conventions — confirmed 2026-10-08

- Ordinary method parameters must be explicit. Prefer method overloads when different supported call forms are needed, rather than optional parameters with default values.
- Optional parameters are reserved for exceptional conventions, such as `CancellationToken cancellationToken = default`.
- Nullability is independent of optionality. A required nullable parameter must still be passed explicitly, including `null` when allowed by the operation.
- `IAccountService.GetDefaultName` and its implementation require explicit `Guid? currencyId`, without `= null`. No three-argument convenience overload is introduced by this change. Currency validation still depends on `AccountNameAddCurrency`.

- Avoid type aliases in ordinary code. Use them only in exceptional cases, such as two types with the same name used in one file. For a name collision, alias only one of the types; use the other by its original name.
- When Business and DAL models share a name, alias the Business model as `{Name}Entity` and import the DAL model namespace normally. For example, use `using CurrencyRateEntity = Business.Models.Entities.CurrencyRate;` and refer to the DAL model as `CurrencyRate`, without a `DalEntity` alias.

### Confirmed inputs retained

- Android and Windows desktop, single-user prototype, one master and multiple registered complete local copies (BR-001–BR-005, NFR-001).
- SQLite business data locally and on Azure; first-version SQLite Admin.db. Azure Functions perform initial creation/connection. Prototype deployment address hardcoded. Version-two administration planned for Microsoft SQL Server; business datasets remain SQLite. These are explicit user technical inputs retained by BRD, not newly selected architecture.
- Two configuration tables: System synchronizes; Local does not. Base currency and AmountPrecision/RatePrecision: selected at creation, immutable, stored in System. Display amounts/BaseAmount with AmountPrecision and rates with RatePrecision; no independent display precision. Local: default account-name order/separator, Add currency flag, sync trigger and conflict priority. LocalConfig.AccountNameAddCurrency is a required bool, default false, confirmed 2026-10-08.
- Versioned master files such as `{Guid}.db`; prepare changes in a copy and publish a complete candidate; download a complete local replacement. Recovery selects latest published version, not original failed snapshot. SyncKey/outcome/receipt tracking is necessary to resolve lost responses without duplicate business application (BR-038–BR-040; synchronization source).
- TransactionEntry and TemplateEntry retain BOTH parent navigation/reference and parent foreign-key identity; both identify the same parent. This explicit legacy TRD requirement survives; language mapping is deferred. Group root refers to itself as parent but is excluded from its own Children. Groups have child groups and corresponding elements; every element has a group identity/reference (core source).
- UTC transaction time; AmountPrecision/RatePrecision decimal calculation with midpoint-to-even; on-demand balances/reports. Stored entry rate is independent of rate catalog. JSON saved-report instructions preserve identities and explicit overrides; do not rewrite except user save (BR-015–BR-028).
- Currency catalog follows the user-supplied CultureInfo/RegionInfo enumeration recorded in BRD: exclude neutral/invalid regions, map ISO code/symbol/English name, keep first entry per code. No new catalog source selected.

### Windows UI framework and local service calls — 2026-10-04

- The Windows desktop application will use WinUI 3 with C# and XAML. This is the user-confirmed Windows UI framework choice.
- The Windows application hosts the required dependencies, resolves Business service interfaces through dependency injection and calls them directly in process. Local UI operations do not call WebAPI or require a running WebAPI host.
- WebAPI remains a separate host for remote service access. Android and future Apple/Linux UI framework choices remain outside this Windows decision; a shared cross-platform UI is not required.

### Windows UI integration — 2026-10-06

Source: [BRD 0.218, Main Navigation and its UI sections](BRD.md#main-navigation). The exact labels, defaults, financial display formulas, selection rules and failure behavior in those confirmed sections are authoritative for this draft. This section maps them to technical responsibilities; it does not approve proposed models/DTOs or claim implementation completion.

#### Host and shell

WinUI 3/C#/XAML remains selected. The Windows host resolves service interfaces through DI and calls them in process. Local UI does not call WebAPI; remote Master access remains behind the service/integration boundary. One application instance brings its existing window/top modal to the front on repeated activation. Restore main-window size, position and maximized state. No application theme feature is required; no fixed palette is inferred.

Navigation is Daily work (Ledger, Reports), Catalogs (Accounts, Correspondents, Categories, Projects, Templates, Currencies), Application (Synchronization, Settings, Help, Exit). Main-screen selection/scroll/search/filter state is session-only; window geometry is restored across launches. Exact storage/lifetime is TQ-15/16.

Service read results and unsaved editor state must remain detached from persistence. Save/Cancel and independently saved nested catalog actions follow BRD. Dates and formatted numeric display use regional settings; amount/rate editing follows the separate plain-text rule below, transaction time HH:mm without seconds, amount/rate precision configured, storage UTC/date-only as already defined. Input must preserve exact decimal monetary values. The reusable TextBox-based amount/rate component and per-call data-service scope policy are confirmed below; remaining controls, numeric editing/commit behavior and UI dispatch stay TQ-15.

#### Five catalog window types and routing

The six catalogs include Reports/ReportGroup. Report checkbox filters are a separate UI surface.

| Window type | Capability |
| --- | --- |
| Edit-select groups+elements | Docked management or modal element picker; eligible elements only; group/element management and ordinary moves |
| Edit-select groups | Modal group choice with group Add/Edit/Delete/Move; no element commands or merges |
| Select only groups | Modal leaf groups-only picker; no mutation, favorite edit or further editor/picker |
| Edit element | Modal unsaved fields/reference pickers/Favorite, saved only on Save |
| Edit group | Modal unsaved Name/Parent/Favorite; parent picker always Select only groups |

- Active identity is (catalog, window type); docked catalog counts. Only top modal is usable/closable. Different catalogs may coexist in the chain.
- Element selection uses Edit-select groups+elements. A group choice uses Select only groups when either same-catalog Edit-select presentation or Edit group editor is active; otherwise Edit-select groups. Edit element alone does not force fallback.
- Docked double-click edits except report Run; picker double-click accepts eligible rows. Explicit Edit handles editing inside pickers. All selection paths enforce caller eligibility.
- Merge groups and supported element combination are docked-main-only, unavailable in every popup including Shift-drag. Ordinary permitted moves remain in Edit-select; leaf select-only is readonly.
- Editor failure preserves entered values; dirty Cancel/X uses discard confirmation. Successfully completed nested catalog activity remains saved even if parent is canceled.
- Groups-only rows show direct hidden-element counts, excluding descendants/child groups. Required projections/loading for ReportGroup remain TQ-16.
- Search navigates names across the complete applicable tree, including collapsed branches, with Next/Previous wraparound; it does not filter or persist. Disable it under Favorites only.
- Pickers open Favorites off and reveal current identity. Optional references have Clear; mandatory do not. Favorites retain matches/ancestor paths; groups-only filtering uses group flags. Inline stars are writable only where modification is permitted. Failure restores old row/state/selection/filter; successful removal and Add/Edit reveal follow the distinct BRD rules.
- Refresh calling references after independent edits: retain identity after rename/move; eligible deleted optional references clear silently. New transaction/template deleted accounts remove entries; existing editors protect current accounts including unsaved choices. Deleted new-account currency or unsaved element Group clears the mandatory field and blocks Save. Draft-reference enforcement uses the confirmed coordinator registry below.

#### Screen-to-service responsibilities

| Surface | Binding and required behavior | Technical gap |
| --- | --- | --- |
| Ledger/transaction | OP-026–030/057/061/063; initial latest 300 all-account rows, single selection and modal editor; duplicate/template drafts; account-filter last matching entry cumulative amount | Cap wrapper/refresh TQ-05; exact all-account amount formula remains BRD source; no new balance formula inferred |
| Accounts/classifications | OP-005–013/022/023/025/053/054/062/063; management, immutable saved currency; no balance display on Accounts screen | OP-025 receives CurrencyId and reads AccountNameAddCurrency from LocalConfig; currency is optional with the flag off and required/valid with it on |
| Currencies/rates | OP-014–020; selected rate panel, immutable code, no currency-picker search; range deletion skips Initial silently | Existing precision/date/base/initial-rate rules retained |
| Templates | OP-033–036/009/063; Name/Group/Description/Favorite, ordered Account/Amount/readonly currency entries | Unsaved preparation and refresh/reference rules retained |
| Reports | OP-037–040/070; ReportGroup tree; double-click Run; period popup then readonly result/CSV | ReportGroup model conventions confirmed; update fresh initialization and service projections; date-only mutation remains TQ-16 |
| Settings | OP-071/072; one Setup read and one atomic Setup save for Local/shared sections; staged Save/Cancel, readonly immutable values, eligible balancing-account picker | Combined operations and one-transaction Save confirmed 2026-10-08; existing flat ConfigurationInfo read shape retained; editable-only save payload confirmed |
| Sync | OP-043–050; Ready/Recovery required, Sync now/Retry recovery, modal stages/cancel, readonly summary/TXT | Runtime signals/absent results TQ-04/07/15 |
| First start | OP-001–004; Continue after credentials, automatic attachment or same-dialog Create/Back | Auth/check/bootstrap/request details TQ-03/04/16 |
| Help/shell | Modal Help User guide/About/Close; guide disabled until available; bundled PDF/default Windows viewer, About name/version | PDF authoring deferred; host/package/file/window state details TQ-15/16 |

#### Orchestration constraints

- Report filter trees are readonly full classification sets, independent Unassigned/Root siblings, inherited overrides and partial checks. Root excludes Unassigned; search leaves checks unchanged; no Favorites or catalog mutation.
- Run saves only From/To, preserving grouping, all saved classification IDs/overrides and other report settings, then closes period popup and calculates readonly results. Explicit full settings Save alone may normalize edited selection. CSV export includes the complete table; generated results are not persisted report data.
- Report CSV uses comma-separated columns. Enclose values containing commas, double quotes or line breaks in double quotes, escaping embedded double quotes by doubling them. Export numbers without thousands separators, using a decimal dot and configured precision; export dates as yyyy-MM-dd. These export formats are independent of Windows regional settings. Encode report CSV files as UTF-8 with BOM, preserving business text in its stored language.
- Report CSV starts with the column-header row, followed by the complete report table including grouping labels and totals. Do not prepend title or date-range metadata lines.
- For an empty report result, keep Save CSV available and export column headers only. No matching data remains a results-popup message rather than a CSV data row. Existing empty-filter warnings still apply.
- Include a report group when it contains matching transactions even if its signed total is zero; omit groups with no matching transactions. This does not change the existing matching-entry and accounting-inclusion rules.
- Order report date groups chronologically. Category/Project/Correspondent groups follow their catalog tree order. CSV preserves the displayed report row order.
- Currency groups in report results follow the currency catalog order. When Unassigned has matching data, show it before named Category/Project/Correspondent groups within the applicable report grouping. In the grouping-label column of report results and CSV, use the full classification path, such as Home/Food or Travel/Food, to distinguish identical names under different groups. This does not change stored names or catalog/editor name display.
- When Currency grouping is enabled, display each currency as a section heading followed by its result rows. Keep all sections visible without expand/collapse controls. Show each currency subtotal after its section and the grand total at the bottom, using bold text for totals. Preserve BR-027 rules for own/base currency totals and mixed-currency grand totals. Display negative report amounts with a minus sign rather than parentheses; existing regional display formatting and separate CSV numeric formatting remain applicable.
- Suggest ReportName_yyyyMMdd_HHmmss.csv in the Save dialog, using export time and replacing filename-forbidden characters in the report name. The user may change the filename. Exact replacement character and export-time timezone are not selected by this rule.
- Export the synchronization summary TXT as UTF-8 with BOM, containing the text displayed in the summary popup. Existing English technical-text and original-language business-text rules apply.
- Application-generated technical text in reports, logs and similar outputs is English, including column headings, status labels and diagnostic messages. Business text, such as account/category names and user-entered descriptions, remains in the user's language exactly as stored; do not translate it. This language rule does not change the separately confirmed numeric/date formats.
- Settings missing balancing account is empty and permits other changes; groups/non-base/no selection disable Select. Separator empty/whitespace normalizes to / on focus loss or Save. Trigger change does not start sync immediately. One Setup call saves Local/System together in one transaction under OP-072. Read uses the existing flat ConfigurationInfo shape. Save accepts editable settings only. LocalConfig.AccountNameAddCurrency is a required bool, default false.
- Manual sync auto-shows summary after progress closes for every outcome. Startup errors/cancellation/conflicts/corrections show summary and then permit Ledger only if access allows. Successful exit closes directly or after correction/conflict summary; failed/canceled exit offers Retry/Exit/Cancel exit. Cancellation completion does not claim Master rollback.
- Pending recovery/expiry gates remain service-owned authoritative state, not inferred from labels. Confirmed expiry follows disposal/message/Download, not permanent Expired screen status.

#### Report failure contract — confirmed 2026-10-08

Invalid persisted report JSON must surface an error from report loading/deserialization; do not replace the definition with empty/default settings or write a repaired value. This is a failure, distinct from a valid definition whose filter selects nothing. OP-070 date-range persistence and OP-037 calculation are separate operations: after successful date save, a calculation failure leaves the saved dates unchanged, shows an error and returns to the report list rather than opening results. Do not roll back the previously committed dates or claim successful calculation. CSV-export failure is handled in the results popup: retain the generated result rows, keep the popup open, show an error and allow an explicit retry of file saving without rerunning the report. No automatic retry or new report persistence is introduced. Existing diagnostic redaction and editor/error rules remain applicable; concrete exception classes remain TQ-07.

#### UI foundations — confirmed decisions and open choices

- MVVM is confirmed by the user, 2026-10-06, after the XAML/code-behind/ViewModel explanation. XAML defines layout/bindings; code-behind handles initialization and UI-specific events; a separate ViewModel holds screen state and commands. Authoritative business validation and persistence stay in existing services called in process.
- CommunityToolkit.Mvvm is selected 2026-10-06 after the manual/generated property-notification comparison. Use its observable-property and command helpers in UI ViewModels. WinUI observable properties use the documented partial-property form. The exact compatible package version remains an implementation/runtime choice; domain/DAL contracts and validation responsibilities are unchanged.
- Dialog hosting is confirmed 2026-10-06: owned WinUI windows for nested editors/pickers, ContentDialog for simple confirmations, and one shared coordinator managing opening/closing, parent blocking, focus restoration and existing catalog/window-type routing rules. The coordinator tracks the chain; individual dialogs retain their UI/ViewModels. This decision does not register popup instances as DI singletons or resolve data-service scope lifetime.
- Open each popup centered over its immediate parent. Do not add automatic popup resizing or repositioning to accommodate a smaller monitor; the user closes popups before maximizing or adjusting the main window.
- Only the main window has a minimize button; popups have none. The user must close all modal popups before operating the main window, including minimizing, maximizing or resizing it. Do not introduce a minimize/restore-whole-stack command.
- The main window has one normal Windows taskbar icon, allowing the user to return after minimizing it. Popups have no separate taskbar icons. The previously confirmed main-window-only minimize button and modal parent-blocking rules remain unchanged.
- Tree pickers, transaction/template editors and report-result popups are resizable. Simple forms, such as group-name editors, have a fixed size. Existing minimum usable dimensions remain applicable.
- Persist resizable popup dimensions locally per popup type, and separately per catalog/mode for tree pickers. Restore those dimensions when reopened, centering over the current parent rather than restoring the previous popup position.
- Store UI preferences for window geometry and column widths in a local JSON settings file under the Windows user profile. These UI preferences do not synchronize. Existing business Local/System configuration contracts remain unchanged.
- Persist column widths when Apply is clicked, resizable popup dimensions when the popup closes, and main-window geometry on application exit.
- If the UI settings file is missing or invalid, use default UI settings so the application can open. This fallback does not change bookkeeping data.
- Data-service lifetime is confirmed 2026-10-06. DbContext, repositories, Business services and components depending on that context retain Scoped registrations. All repositories/services in one call share its scope/context; existing compatible Shared singleton helpers are not changed by this rule.
- The boundary matches WebAPI: one request scope around a call, disposed after completion. WinUI explicitly creates a scope for each logical service call, resolves the service within it, waits for completion and then disposes the scope on success/failure. The next call receives a fresh scoped graph/context.
- DAL AsNoTracking reads do not make context reuse across calls safe: write operations attach entities. Tracked entities from one public service call must not remain in the context used by the next UI call. A popup lifetime is not the data-service scope lifetime.
- ViewModels retain detached/materialized DTO/editor values between calls, not DbContext/repository/scoped-service instances. An application-wide UI coordinator/runner uses a scope factory for data calls; it does not capture scoped Business/DAL dependencies.
- The synchronous local-UI decision does not itself change existing service signatures. How the UI integrates any asynchronous service implementation remains an implementation detail; per-call scope disposal must still follow actual operation completion.
- No application classes or lifetime registrations are changed/generated in this documentation turn. Scope-runner wiring remains an implementation detail under TQ-15; the boundary/Scoped policy is settled.

References: [Microsoft MVVM Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/), [Microsoft ContentDialog](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.contentdialog). The native dialog limitation must be respected by the chosen stack implementation. The reusable decimal input component and per-call scope boundary are confirmed. Remaining controls, numeric editing/commit behavior, dispatcher/progress/cancellation, scope-runner wiring and cache refresh remain TQ-15.

#### Amount/rate input control — confirmed

Confirmed by the requesting user, 2026-10-06: use one reusable TextBox-based input component that parses decimal directly, uses Windows regional formatting for display rather than editing, and applies AmountPrecision/RatePrecision with caller-specific amount/rate validation. TextBox is built in; the decimal parsing/precision/validation behavior is application-owned and reused across amount/rate fields. All existing amount/rate/empty-value/business validation rules remain binding. No application control or source file is created by this decision.

WinUI NumberBox.Value is double, so its numeric value is not the authoritative exact-decimal amount/rate input. [Microsoft NumberBox.Value documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.numberbox.value?view=windows-app-sdk-1.8). The component choice and focus-loss/Save validation and formatting boundaries are settled; remaining numeric-input details stay under TQ-15. [Microsoft TextBox documentation](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/text-box).

- While editing amount/rate text, allow unfinished values such as a minus sign or a number ending with the decimal separator without immediately reporting an error. Validate on focus loss and on Save. Apply configured precision and the existing rounding rule at those boundaries, not after every keystroke. If validation fails, preserve the entered text, show an error beside that field and prevent saving until corrected. Existing empty-value and caller-specific amount/rate rules remain binding.
- On entering an amount/rate field, select its entire value for replacement and use plain, unformatted editing text without thousands grouping or regional formatting. Apply display formatting after focus leaves a valid field. Use a dot as the decimal separator while editing, regardless of Windows regional settings; for example, edit 1234.56 and format it regionally only after focus loss. Pasted input uses the same plain dot-decimal format: ignore surrounding whitespace, but reject currency symbols and thousands separators under the existing field-validation rules. The numeric keypad decimal key inserts a dot regardless of Windows settings. Accept simple decimal numbers only; calculator expressions and scientific notation are unsupported. If Save encounters invalid input, focus the first invalid field and preserve the other entered values.

#### DecimalBox precision — confirmed

Confirmed by the requesting user, 2026-10-09: the reusable control is `WinUi.Controls.DecimalBoxControl.DecimalBox`, located in `backend/WinUi.Controls/DecimalBoxControl`.

- Set `Precision` in C# or XAML. It specifies fractional decimal places, accepts integers from 0 through 28, and defaults to 2. Out-of-range assignments are rejected.
- The always-visible inline spin buttons use an exact decimal step of `10^-Precision`: precision 0 gives 1, precision 2 gives 0.01, and precision 4 gives 0.0001.
- Round valid input with midpoint-to-even on focus loss or an explicit `TryCommit()`, rather than after each keystroke. Hosts call `TryCommit()` before saving.
- Assigning `Value` rounds it to the current precision. Changing `Precision` rounds the current committed value immediately and updates the spin step.
- `Value` is nullable `decimal`; caller-specific required-value and range validation remain separate. The control's 0–28 precision range does not change the permitted business AmountPrecision/RatePrecision configuration ranges.
- The Columns dialog sets `Precision = 2` for all width fields, including Currency.

C# configuration:

```csharp
new DecimalBox
{
    Precision = 4,
    Value = 10m
};
```

XAML configuration, with `controls` mapped to `using:WinUi.Controls.DecimalBoxControl`:

```xml
<controls:DecimalBox Precision="4" />
```

#### Date/time controls — confirmed

Confirmed by the requesting user, 2026-10-06: use built-in CalendarDatePicker for calendar dates and built-in TimePicker for transaction time. Transaction time display/input is 24-hour HH:mm without seconds; this replaces the earlier seconds-capable custom input proposal and HH:mm:ss UI requirement in BRD. Configure the TimePicker clock as 24HourClock.

The native TimePicker ignores seconds. Confirmed by the requesting user, 2026-10-06: save seconds as 00 for a new transaction or when the user changes an existing transaction time. If the existing time is unchanged, preserve its stored seconds. Keep the original time value distinct from the minute-only control value so initialization or editing other fields does not silently clear seconds. UTC transaction timestamps and date-only semantics remain as previously defined; no stored field-type change or database-wide timestamp truncation/migration is authorized. The separate report default-name format remains unchanged.

References: [Microsoft CalendarDatePicker](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/calendar-date-picker), [Microsoft TimePicker](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/time-picker).

#### Flattened ListView tree — confirmed foundation

- Star filtering retains starred rows and required ancestor paths without removing hidden nodes from the underlying tree. Search stays enabled; entering nonblank search text or invoking Next/Previous clears the star filter and searches the whole applicable tree. This supersedes the earlier search-disable rule.

Source: requesting user's row-ViewModel proposal and confirmations, 2026-10-06. Visual reference: [TreeGrid.png](../sources/TreeGrid/TreeGrid.png). The reference establishes a combined hierarchy with aligned data columns and no connector lines; its task-specific columns do not replace the bookkeeping catalogs' defined columns.

- Use a reusable ListView-based flattened tree. Each row uses a common column layout, with an indented name/expander area in the first column. Keep the other column boundaries aligned. Native TreeView and third-party TreeGrid components are not selected for this approach.
- Keep the complete set of detached row ViewModels separately from the observable VisibleRows collection bound to ListView.ItemsSource. An IsVisible property alone does not filter the collection. Preserve row identities and existing row instances when updating the visible collection in the established hierarchy/order.
- A row represents exactly one group or one element; GroupModel and ElementModel are mutually exclusive. Retain a stable identity and parent association. IndentLevel follows hierarchy depth; an element is one level below its parent group. These are UI state concepts, not new persistent models or service DTO contracts.
- IsOpened belongs to group UI state. A row can appear only when every ancestor is open and the existing mode/filter permits it. Collapsing an ancestor hides its entire descendant range without clearing descendants' own expansion flags. Reopening traverses only open ancestor paths; closed subgroups remain visible while their descendants remain hidden.
- Recalculate the visible projection after expansion/collapse and relevant hierarchy/filter changes, then update the existing bound collection. ListView supplies row selection, scrolling and virtualization; hierarchy visibility and permitted drag/drop actions are application responsibilities. Preserve the virtualizing list layout with a constrained viewport.
- Dropping between sibling rows reorders the dragged item within that sibling sequence: groups among groups, elements among elements. Show an insertion line for reorder placement. Dropping onto a group retains the existing move-into-group behavior and main-window Shift-merge exception. Existing catalog/mode permissions remain binding.
- Favorites only does not disable sibling reordering or permitted moves into another group. Dropping between two visible siblings places the dragged item immediately after the preceding visible sibling in the complete sibling sequence, including hidden items; the user example is insertion after Order 5 and before visible Order 9 at Order 6. Dropping at the start places the item first in the complete sibling sequence. First position retains the existing zero-based stored Order = 0; the numbering convention does not change. Keep the established separate group/element sequences and mode permissions.
- Show a not-allowed cursor for invalid drop destinations and make no change, including when a group is dragged into its own descendant. Existing domain validation remains authoritative.
- While dragging near the top or bottom edge of the tree viewport, automatically scroll so destinations outside the visible area can be reached.
- Escape during an active drag cancels only that drag, without closing the current popup or changing stored data. Groups expanded by hover during the drag remain open after a drop or cancellation.
- Report row check state uses a nullable Boolean: true checked, false unchecked, null partial. It represents BR-024's existing report selection rules across the underlying tree, including hidden descendants; ordinary management trees do not show these checkboxes.
- For mouse interaction, only the expander arrow toggles expansion. Ordinary row clicks select; existing double-click edit/select rules and independent star/checkbox interactions remain applicable.
- When the tree row area has keyboard focus, Up/Down selects the previous/next visible row. Right Arrow expands a closed group; on an expanded group it selects the first visible child, if any. Left Arrow collapses an expanded group; otherwise it selects the parent, if any. At the root, the parent-navigation action does nothing. Navigation uses the actual hierarchy even beyond the visual indentation cap and preserves existing picker eligibility rules.
- With a tree row focused, Enter invokes the same action as double-click: edit in management views, select an eligible item in selection pickers, or run a saved report. Existing mode-specific eligibility and disabled-action rules apply. Home/End selects the first/last visible row. In report checkbox trees, Space toggles the selected row checkbox; a partially checked group becomes fully checked, following the existing report checkbox propagation rules. These row shortcuts do not override text-entry or other focused controls.
- After saved catalog changes, refresh affected open screens and pickers when they become active again. This includes returning from a nested picker canceled after independently saving catalog changes.
- During tree refresh, preserve expanded groups, selection and scroll position where possible. Previously confirmed rules for revealing added/moved items and other explicit selection/visibility outcomes take priority.
- If refresh fails, keep the previous rows visible, show an error with Retry, and disable editing and selection confirmation until refresh succeeds. Cancel/Close remains available.
- Ordinary local SQLite loading and editor saving use synchronous execution from the UI action, as requested by the user. Do not add progress indicators, progress popups or temporary busy-state input/Save/close guards for these short operations. Existing validation, error reporting, transaction rollback and explicit refresh-failure rules remain applicable. This decision does not change synchronization or first-use creation/attachment progress and cancellation flows. Expected short duration is a design assumption, not a measured performance guarantee.
- Indentation uses an empty layout spacer in the first column, with width equal to min(IndentLevel, 8) multiplied by a shared indentation step. Only the hierarchy/name area shifts; the other columns stay aligned. A monospace font is not required. The exact step remains a layout choice; 16 units was an example, not an accepted fixed value. Confirmed by the requesting user, 2026-10-06.
- If collapse removes the selected descendant from VisibleRows, transfer selection to the collapsed group. Preserve selection outside that subtree. Apply existing picker eligibility rules to the resulting selection.
- Keep column headers visible during vertical scrolling, aligned with their row columns; headers do not invoke sorting. Use single-line row text with ellipsis on overflow and a tooltip containing the full text. Horizontal scrolling is not allowed. Configure column widths through a Columns popup with editable width values and Apply/Cancel buttons; do not resize columns by dragging their borders. Save applied widths locally per view, separately for main screens and selection popups, and restore them on reopening. Restore defaults resets the popup values to predefined column widths; Apply commits them and Cancel discards pending changes. Column widths are percentages of the available row width and must total 100%; preserve those proportions when the viewport changes. Prevent widths that hide essential row controls, including expanders, checkboxes and favorite stars. In version one all columns remain visible in their fixed order; Columns changes widths only. Enforce a minimum window/popup width so it cannot be resized below the width needed for usable columns. Exact minimum dimensions remain a layout detail. Names and descriptions are left-aligned; amounts and rates are right-aligned; stars and checkboxes are centered within their allotted space. The currency-name text in the Account tree is centered. Cap visual indentation at eight steps, using a fixed constant of 8. Rows deeper than level 8 use the same visual indentation as level 8. Preserve actual hierarchy depth, parent relationships and expand/collapse behavior; this is a display limit only.
- Tree interaction and column/header decisions recorded in this document are settled; TQ-15 tracks remaining implementation integration. This choice does not authorize application implementation or change service scope boundaries.

### Boundary and proposal status

The service catalog below is a logical API/BFF contract, not a deployment diagram. Local operations must work offline; listing them does not require HTTP or a cloud round trip. Unless explicitly confirmed elsewhere in this document, paths, verbs, transport statuses, class names, layers and projects remain unselected. No C# source, implementation tasks or tests are created by this document.

All schema types, nullability, DTO directions, exposed fields and assignments are **proposed** unless explicitly stated as confirmed in this document. Business requiredness comes from BRD; it does not approve a particular serialization shape. Entity revisions and snapshot versions use confirmed signed 64-bit integers (`int64`). GUID identities for persistent business entities are confirmed; registration/operation identities and authentication-session representation remain proposed. Amounts and rounded BaseAmount use AmountPrecision; rates use RatePrecision. AmountPrecision/RatePrecision ranges and fixed SQLite scaling are confirmed below; calculation bounds follow the confirmed decimal/storage-limit policy below.

### Confirmed naming and Currency scope — 2026-09-30

The requesting user confirmed that existing code names are the correct contract names: CurrencyRate.Date (formerly EffectiveDate), Transaction.DateTime (formerly OccurredAt), and System configuration AmountPrecision/RatePrecision (formerly APr/RPr). Use these names in persistent schemas, corresponding DTO fields and active explanations. Date remains a calendar date without timezone conversion; DateTime remains a UTC instant. The precision names still mean fractional decimal places, with the same ranges, defaults, rounding and immutability. This is a naming correction, not a change to those semantics. The unrelated DiagnosticEvent.OccurredAt field is unchanged.

PM-010 is named Report, replacing SavedReport; it still stores report instructions, not calculated results. DTO-013 is correspondingly named ReportInfo. Stable PM/DTO/OP IDs and report behavior remain unchanged.

Currency has no Description property or corresponding DTO field. CurrencyRate.Description remains optional. The user reaffirmed this rule on 2026-10-08; BRD 0.218 BR-009/BR-013/BR-014 is aligned. Earlier clarification entries retain their historical terminology and are superseded by this decision where inconsistent.

### Subdomain ownership and current work scope — 2026-10-01

The user selected five subdomains, each with its own solution folder and Contracts/Impl projects. These are project responsibility boundaries, not separate databases or deployments. Shared persistent business entities remain in Business.Models.

| Subdomain | Responsibility |
| --- | --- |
| Business | Catalogs, currencies/rates, accounts, transactions, templates, account balances and internal read-only configuration access through IConfigOperation. SVC-002–007. |
| Setup | Create/open books and user-facing Local/System configuration changes. SVC-001/009/012. |
| Reporting | Saved report definitions and report calculation. SVC-008. Account balances stay in Business. |
| Synchronization | Change capture/transfer, conflicts, recovery, expiry and synchronization diagnostics. SVC-010/011. |
| Administration | Multiple-dataset administration in the next version. Required version-one owner/registration persistence still belongs to setup/synchronization work. |

Current sequence, corrected by the user on 2026-10-08: first align the current persistent models, Business services, DAL and fresh-database SQL/seed scripts with confirmed BRD/TRD changes. ReportGroup and configuration naming are explicit prerequisites even though report workflows remain deferred. Then deliver the Business-only WinUI flow against an existing initialized Local database. Connect and complete the Business UI before resuming Administration, Synchronization, Reporting or Setup discussion/implementation. Preserve their recorded requirements as deferred. Reports, Synchronization, Settings and Help are unavailable in this increment. This documentation update does not start application implementation or approve the complete requirements package.

Business reads configuration internally through IConfigOperation. Setup owns external configuration services and user-facing configuration writes. OP-065 and SVC-013 are retired; Business exposes no public configuration service. Business never writes configuration, including when deleting or combining the selected balancing account. Setup owns clearing or changing persisted BalancingAccountId and may expose a remote API. Business resolves configuration locally and returns an absent selection when the referenced account is missing or deleted, without changing the stored value or calling Setup to repair it. Synchronization applies synchronized System configuration.

The frontend owns the unsaved balancing-entry action. Existing Business UI decisions are sufficient to proceed with the scoped flow; raise only concrete integration gaps or contradictions, not a new broad discovery round. Use stored configuration for Business behavior without implementing Setup editing. Package selection, scope-runner wiring, modal-window integration and refresh wiring are implementation tasks within confirmed behavior. Resolve actual contract gaps when encountered; do not treat deferred-domain questions as prerequisites for this increment.

#### Open-editor account deletion protection — confirmed 2026-10-08

The popup coordinator maintains a UI-only registry keyed by each open existing transaction/template editor. Its account-ID set follows the editor's current entries, including unsaved account selections, and is updated when entries or account selections change. Every UI account-delete action checks this registry before invoking the service; a referenced account cannot be deleted. Closing the editor, including Cancel, removes its registration. Existing service validation independently protects persisted references.

New unsaved transaction/template editors do not register this protection: if an account is independently deleted, their affected entries are removed under the established rule. The registry stores identities only; it does not retain a service scope, DbContext or tracked entity, and creates no persistent lock.

### Business increment integration and acceptance

- Startup assumes an existing Local database with compatible schema, seed data and configuration. Start at the unfiltered newest-300 Ledger. First-use provisioning, migration and online authorization/synchronization flows are deferred.
- WinUI calls existing Business service contracts in process. Create and dispose a fresh service/DbContext scope per completed logical call; editor state and returned data remain detached. No local WebAPI host is required.
- Connect the Ledger and transaction/template actions, the five Business catalog trees and their nested pickers/editors, and currencies/rates. Reuse the confirmed table, modal, numeric, date/time, validation and refresh behavior.
- Implement applicable accepted Business contract changes, including account-name generation reading AccountNameOrder, AccountNameSeparator and AccountNameAddCurrency, without pulling deferred Setup or Reporting functionality into this increment. Read configuration through the existing Business arrangement; do not restore retired OP-065/SVC-013.
- Verify an end-to-end run on a prepared Local database: startup and Ledger load; transaction add/edit/save/cancel and persisted reload; account-filtered cumulative amount; catalog/picker independent saves and deletion guards; template use; currency/rate editing; orderly exit. Validate changed service contracts and persistence through appropriate existing tests as implementation proceeds.
- Full-product handoff gaps remain recorded below. They are not automatically blockers for this increment; only gaps affecting its actual execution require resolution.

## Role and Use-Case Traceability

| BRD role/capability | Retained meaning | Detailed use cases / operations | Authorization source | Status |
| --- | --- | --- | --- | --- |
| ROLE-001 | Sole owner; offline user/cloud owner | All UC rows below | BR-001–BR-005 | Business role confirmed; enforcement TQ-03 |
| ROLE-002 | Requirements approval | Non-system: approve documents, not an application endpoint | BRD Roles/Approval | Non-system |
| BC-001 | Initial creation | UC-001-01–UC-001-03; OP-001–OP-003 | BR-002–BR-004 | Detailed formulations proposed |
| BC-002 | Open/add/reinstall | UC-002-01–UC-002-03; OP-001, OP-004 | BR-001–BR-005 | Proposed |
| BC-003 | Organization | UC-003-01–UC-003-06; OP-005–OP-013 | BR-006–BR-009, BR-011 | Proposed |
| BC-004 | Currencies/accounts | UC-004-01–UC-004-09; OP-009, OP-014–OP-020, OP-022–OP-023, OP-025 | BR-008–BR-017, BR-021 | Proposed |
| BC-005 | Transactions/balances | UC-005-01–UC-005-06; OP-026–OP-031, OP-057–OP-060, OP-061, OP-064/066–069 | BR-015–BR-021 | Proposed |
| BC-006 | Templates | UC-006-01–UC-006-04; OP-009, OP-033–OP-036 | BR-022 | Proposed |
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
| UC-001-02 | Submit credentials with Continue, check Master and select attachment or creation fields. | Failed/unknown check never means absence. Lost creation response requires manual Retry check before another creation. | OP-002/003/004; Windows UI integration |
| UC-001-03 | Absent Master: same-dialog explicit base currency and precisions 0–4/defaults 2/4, then Create; initialize six roots/balancing account and register/download. | Back before creation preserves values. Existing Master attaches; partial created-Master failure is preserved. Sixth root identity TQ-16. | OP-003/004; PM-001/011/012 |
| UC-002-01 | Open existing local books without internet/sign-in. | Pending recovery/confirmed expiry follow their dedicated flows. | OP-001 |
| UC-002-02 | Existing-Master route after Continue authenticates/registers/downloads and remembers authorization. | Invalid credentials or failed/cancelled download returns Setup with values; retries manual; no business access without Local copy. | OP-004; Windows UI integration |
| UC-002-03 | After reinstall, register a new local copy. | Old registration remains under normal 90-day expiry, not reused. | OP-004 |
| UC-003-01 | Merge category groups: choose source/destination; move source elements/children, delete source, show updated tree. Same behavior applies to other group types. | Descendant destination/root source rejected; parent/ancestor/root destination permitted; incoming unique-name collisions repeatedly append `_1`. After a successful merge, select the destination group and scroll it into view, expanding its ancestor path as needed (BRD Main Navigation). | OP-005, OP-007 |
| UC-003-02 | View a same-type tree including root and direct root elements. | Root not duplicated as its own child; three common columns, plus CurrencyName for the account tree. | OP-005, OP-009, OP-062 |
| UC-003-03 | Create/edit non-root group; trim and validate Name/Description; save. | Blank/duplicate sibling name or root edit rejected; Description optional. | OP-005, OP-006 |
| UC-003-04 | Move non-root group and display new hierarchy. | Cross-type/cycle move rejected; individual forbidden name collision rejected. | OP-005, OP-006 |
| UC-003-05 | Delete empty non-root group; refresh tree. | Root/nonempty group deletion rejected. | OP-008 |
| UC-003-06 | Create/edit/move/delete Category, Project or Correspondent. | Duplicate name in group, blank Name or deletion while account references it rejected; renaming leaves Account Name unchanged. | OP-009–OP-013 |
| UC-004-01 | Load regional currency choices, one entry per ISO, retaining first Name/Symbol. | Skip neutral cultures/invalid regions. | OP-014 |
| UC-004-02 | Create currency with initial rate; base unchanged. | Duplicate ISO/nonpositive rounded rate rejected. | OP-015, OP-016 |
| UC-004-03 | Edit currency Name/Symbol or restore Name/Symbol defaults. | ISO immutable; region default source is the same deduplicated catalog. | OP-014–OP-016 |
| UC-004-04 | Delete unused non-base currency. | Base/used currency deletion rejected. | OP-018 |
| UC-004-05 | View/add/edit ordinary dated rates; a separate Delete action removes ordinary rates in the selected currency/date range; edit initial rate value. | An existing currency/date is updated rather than rejected as a duplicate; pre-minimum ordinary date, nonpositive rate or changing hidden initial date rejected; range deletion skips the initial rate rather than rejecting the request; base rate fixed 1. | OP-017, OP-019, OP-020 |
| UC-004-06 | Create account with currency/group, optional classifications and generated or edited name. | Missing required values rejected; duplicate Account Name allowed; missing classification slots keep separators. | OP-009, OP-022, OP-025 |
| UC-004-07 | Edit/move account or reclassify it; future reports use current classification. | Currency immutable after the first successful account save; classification changes do not rename account. | OP-009, OP-022 |
| UC-004-08 | Delete unreferenced account. | Any transaction/template use still referenced blocks deletion even at zero balance. | OP-023 |
| UC-004-09 | Restore Name from Local naming order/separator/current classifications and optional account-currency suffix. | Blank new Name uses default on Save; explicit Restore is one-time; existing/manual names do not auto-change. All-empty checked slash name //(UAH). CurrencyId input retained; AccountNameAddCurrency is read from LocalConfig, not passed by the caller. CurrencyId may be null when AccountNameAddCurrency is false; a valid currency is required when true. | OP-025/022; PM-012 |
| UC-005-01 | Enter balanced transaction with at least two valid entries; save activates automatically. | Blank Amount is zero; repeated accounts/zero entries allowed; invalid time/rate/missing account rejected; fewer than two valid entries saves as Draft; overflow shows error without changing existing data. | OP-026, OP-027, OP-057–OP-060, OP-061 |
| UC-005-02 | Save a new or existing transaction with zero/one valid entry or an unbalanced total as Draft; exclude from calculations and show a persistent Draft warning. | Every present entry requires a valid account, positive rate (base rate 1) and numeric amount, including zero. Date and numeric validation still apply; a previously Confirmed transaction may return to Draft. | OP-027 |
| UC-005-03 | Edit and save under PM-006; after commit, read fresh complete data for displayed transaction IDs and refresh cumulative amounts. | Invalid data rejects without changes; Draft transitions follow PM-006. If the edited row no longer matches, remove it and warn without refilling the vacant place. Navigation performs a fresh capped query. The exact displayed-ID refresh contract remains TQ-05. | OP-026/027/057–060; refresh operation assignment unresolved |
| UC-005-04 | Delete one transaction or all transactions matching a validated filter; recalculated totals omit deleted Confirmed transactions. | Validate filter criteria before mutation. No matches is success; a database failure rolls back all affected aggregates and sync flags and raises a critical exception. The five filter variants are defined by OP-064/066–069. | OP-028, OP-064/066–069 |
| UC-005-05 | Duplicate transaction values with time now; review/save. | Cancel saves nothing; date change does not replace copied rates. | OP-029, OP-027 |
| UC-005-06 | Restore applicable rate or calculate account balances on demand. | Base rate 1; changing date does not replace rates. Accounts UI is management-only; filtered Ledger displays last matching entry cumulative amount. Existing balance API retained. | OP-030/031/061 |
| UC-006-01 | Create/edit empty, single-entry or unbalanced template with unique Name and optional Description; Description is visible and independently editable. | Every existing entry needs account; account currency was already fixed at account creation. | OP-009, OP-033 |
| UC-006-02 | Apply template into transaction editor with applicable rates/Description. | Zero/single-entry or unbalanced results may save as Draft with a warning; every present entry must be valid. User may repair now, save for later, or cancel. | OP-034, OP-027 |
| UC-006-03 | Create template from active transaction or draft, copying accounts/amounts/Description. | Target group/name must satisfy template rules. | OP-036, OP-033 |
| UC-006-04 | Delete template through its normal lifecycle. | Uses the shared entity deletion rules and aggregate lifecycle; failures leave the entire operation unchanged. | OP-035 |
| UC-007-01 | Run saved report through period popup, then readonly signed result and optional CSV. | Persist dates only before calculation; From > To blocks Run. Date/group/currency rules BR-026/027; empty classification selection names warning; no drill-through. | OP-070/037; DTO-036/014 |
| UC-007-02 | Choose classification checks in readonly full-tree popup with independent Unassigned/Root and partial/inherited descendants. | Root excludes Unassigned; search leaves checks; no favorites/catalog mutation. OK returns pending choices, Cancel keeps previous choices. | OP-005/009/037; Windows UI integration |
| UC-008-01 | Save report Name/ReportGroup/Favorite/instructions; default yyyy-MM-dd HH:mm:ss; duplicates allowed. | Blank Name rejected; common group/picker/favorite rules apply. Group/favorite/order fields follow shared catalog conventions; projection alignment remains TQ-16. | OP-039; PM-001/010 |
| UC-008-02 | Reopen saved report on actual hierarchy; apply explicit choices, even redundant after moves. | Ignore missing identities; reapply if they return; never rewrite saved JSON just by reading/running. | OP-038, OP-037 |
| UC-008-03 | Full settings Save normalizes edited selection; period Run updates From/To alone. | Read/run/hierarchy and period save never normalize saved identities/overrides or change other settings. | OP-038/039/070 |
| UC-008-04 | Delete saved instructions only. | Accounts/transactions untouched; calculated results were not persistent business data. | OP-040 |
| UC-009-01 | Staged Settings This device/Shared with Save/Cancel and unsaved navigation/exit prompt. | Immutable values readonly; balancing nullable/base-only; Add currency initially false; separator normalizes; failed Save retains entered/prior saved values. One Setup call commits both sections atomically under OP-072. | OP-071/072; Windows UI integration |
| UC-009-02 | Manual Sync now/Retry recovery, modal stages/Cancel; close progress then auto-show latest summary for all outcomes. | Editing blocked while waiting/running/handling cancel; cancellation awaits service; recovery/expiry gates retained. | OP-043–050; Windows UI integration |
| UC-009-03 | Automatic start/exit sync obeys connectivity/trigger rules and summary/navigation flow. | Startup access depends on recovery/expiry; successful exit may wait for correction/conflict summary; failed/canceled exit offers Retry/Exit/Cancel exit. | OP-043–050; Windows UI integration |
| UC-009-04 | Resolve whole-item conflicts by priority, validity overriding; restore dependencies, deduplicate currencies and rename conflicts. | Unresolvable valid-result failure explains/logs; no weakened accounting/currency-immutability rules. No content-field merge; catalog ordering follows its separate contract. | OP-043, OP-045, OP-052 |
| UC-009-05 | No-change sync renews registration without business transfer; any new entity with EditRevision null or any Content or Order bit means local changes exist. | Master changed means download even when local unchanged. | OP-043, OP-044, OP-047 |
| UC-010-01 | Resolve uncertain operation outcome before replay/discard. | Unreachable retains pending state; business access blocked; never duplicate published batch. | OP-044, OP-048 |
| UC-010-02 | Recover latest published master; install safely and confirm actual snapshot version. | Interrupted download starts over; late newer master does not make one in-progress snapshot inconsistent. | OP-046–OP-048 |
| UC-010-03 | Cancel recovery or lose communication. | Pending state/access block persists; published master never rolled back; expiry overrides. | OP-044, OP-045, OP-048 |
| UC-011-01 | On confirmed 90-day expiry delete old copy/unsynced work and offer Download. | No viewing/export/preservation; preserve Local configuration; no automatic fresh download. | OP-044, OP-049 |
| UC-011-02 | User selects Download for fresh registered local data. | Any connection allowed; download failure leaves no usable copy. | OP-049, OP-047 |
| UC-012-01 | Readonly latest-session sync summary of received counts/conflicts/corrections/errors and optional TXT Save to file. | Omit zeros/uploads; new attempt replaces result, closure discards it. Summary export differs from detailed diagnostic-log export. | OP-050; Windows UI integration |
| UC-012-02 | Detailed failure logs with seven-day cleanup. | Diagnostic Share/Export unselected; confirmed TXT summary export remains allowed. Cleanup TQ-08. | OP-051/052 |

## Persistent Model Schemas

### Schema decision status and shared contracts

Except for explicitly confirmed contracts below, type/nullability/identity choices in this section remain **proposed**. Sources authorize the business concepts, not these exact storage fields. `Required` means non-null; `Nullable` allows null. Arrays can be empty unless a stated rule says otherwise. `enum` values and `object` fields are explicitly described below. Creation-time AmountPrecision/RatePrecision define decimal scales; SQLite scaled-integer representation is confirmed below; calculation bounds and overflow rejection follow the confirmed decimal/storage-limit policy below.

Confirmed identity for persistent business entities (PM-001–PM-010): `Id: uuid / Required` (GUID). Except for the five fixed root identities specified in PM-001, generate identity at creation, including offline Local creation, and preserve it through edits, moves, synchronization, deletion and restoration. Parent/foreign-key references use the same GUID identities. Independent creations on different Locals have distinct identities; no auto-increment identity allocation or routine synchronization-time ID renumbering is used. TransactionEntry and TemplateEntry still have generated row identities, but their replacement-on-save contract below is an explicit exception to preserving identities through aggregate edits. The confirmed sync contract applies to independently synchronized entities (PM-001–PM-006, PM-008, PM-010 and System configuration PM-011). PM-007 and PM-009 have no independent tracking fields: entry changes mark the parent Transaction or Template as modified. On aggregate creation, create the complete entry set with generated row IDs. On aggregate edit, delete the old entry set and create the complete replacement set with new row IDs. Entry DTOs do not need IDs or row matching. Account references, amounts, rates where applicable, and entry order determine the new set. This does not authorize replacing the parent identity or regenerating IDs of unrelated entities.

#### Favorite flags — confirmed 2026-09-28

PM-001, PM-002, PM-003, PM-005 and PM-008 include required persisted IsFavorite: bool for each group, element or currency, defaulting to false on creation. Reports use the same required persisted IsFavorite bool, default false, under BR-028/044 and the confirmed PM-010 contract. Users can mark/unmark favorites on non-root groups, elements and currencies and filter by that flag in the first release. All six root groups have IsFavorite fixed to false; UI must not offer a favorite toggle for them, and save/sync validation must preserve this invariant regardless of conflict priority. Changing IsFavorite adds ModificationType.Content, preserves any Order bit, and uses the shared EditRevision content-conflict rules, including the same-Local priority exception. IsFavorite does not use the special Local-wins Order policy. Template favorites participate in whole-template content synchronization. No inheritance/cascade to descendants is selected. Favorites filtering retains the ancestor path to matching favorite groups/elements, including non-favorite parents and the root as needed. These ancestors are navigation context only, not favorite matches; no flags or sync metadata change merely because the path is displayed. Opening a favorite group does not bypass the active favorites filter: show only favorite descendants, plus non-favorite ancestor groups needed to reach them. Other non-favorite children remain hidden; favorite status is not inherited.

Catalog DTO projections/mutations expose IsFavorite and parent/group identities. Favorites filtering is the frontend responsibility: retain favorite rows and the ancestor paths needed to reach them, without changing stored flags. No Business filtering operation is required. Favorites filtering does not change persisted collection Order or the separate subgroup/element sequence rules.

#### Catalog ordering — separate synchronization contract

Confirmed 2026-09-27, extended 2026-09-28: ordering of child groups (PM-001) and grouped elements (PM-002, PM-005, PM-008) is shared data, synchronized separately from entity content. Each ordered catalog entity has a required persisted Order field of signed 32-bit integer type (int32). Each parent has two independent sequences: Children (direct subgroups) and Elements (direct elements). The user does not see a mixed subgroup/element list. Normalize Children to 0..G-1 and Elements to 0..E-1, where G and E are their respective accepted member counts. No shared index space or cross-collection tie exists; both sequences can start at 0. Empty collections have no Order values to assign. The self-parent root is excluded from Children and its numbering. Apply the merge, priority, append and GUID tie-break rules below independently to each collection. TransactionEntry/TemplateEntry Position remains part of its parent aggregate under PM-007/009, not this contract. Currency (PM-003) also supports manual ordering under this same separate synchronization contract. The currency catalog is one independent sequence per business dataset: required persisted Order: int32, normalized to 0..C-1 for C accepted currencies. Apply the same priority-relative order, GUID ascending tie-breaks, append-missing rule and post-merge normalization. Currency ordering changes do not modify currency content or its content revision or Content flag. This decision does not apply manual ordering to CurrencyRate records.

For each collection, first resolve content, accepted membership and deletions. Content uses EditRevision and the Content bit with the shared conflict-priority rules, including the same-Local exception; ordering is handled independently. A Local member with the Order bit set supplies its local Order regardless of the configured content priority. Without that bit, retain the accepted Master order. No OrderRevision or separate IsOrderModified boolean is selected; ModificationType carries the per-member Order flag. Ordering alone must not overwrite content or increment EditRevision.

After membership/content reconciliation, initialize each accepted new member with the Order supplied in its creation payload, without requiring an Order bit. For existing members, select Order independently: use Local Order when its Order bit is set; otherwise retain accepted Master Order. Sort the merged values by Order ascending, then Id (GUID) ascending, and assign consecutive 0..N-1 positions. Concurrent changes may therefore shift the intended local or master placement; this outcome is explicitly accepted, and no exact drag-and-drop intent replay is required. Accepted members absent from the selected ordering sequence are appended in GUID ascending order before final renumbering. Never restore deleted/rejected members merely because they appear in an order list. Apply this procedure independently to each subgroup, element and currency collection. The previous unconditional configured-priority collection selection is superseded. Confirmed GUID comparison: represent each Id as a canonical lowercase GUID string in 8-4-4-4-12 hexadecimal format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx, without braces), then compare strings ordinally in ascending order. Master and every Local use this same rule for equal-Order tie-breaks and ordering appended members; comparison is culture-independent. This defines comparison only, not physical GUID storage or a change of identity.

Local flag rules (confirmed 2026-09-28; creation rule updated 2026-10-01):

- New tracked entity, including an ordered catalog member: None. EditRevision null identifies a new, never-submitted creation. Its creation payload includes all content and the initial Order where applicable, without requiring modification flags. ModificationType describes subsequent edits, not creation; later content edits and reorders set their applicable bits under the existing edit rules.
- Content edit: add Content without clearing an existing Order bit.
- Reorder: add Order on every member whose numeric Order changes, without adding Content for ordering alone. Confirmed 2026-10-01: one reorder API operation saves all affected rows and their modification flags in one local SQLite transaction. Updates are prepared per row and committed once; failure rolls back the entire reorder so the previous persisted order and flags remain intact. Existing Content bits are preserved.
- Move to another group: add Content | Order on the moved member; normalize source/destination collections and add Order to siblings whose positions change.
- Delete: apply the existing deletion/content rules to the deleted member; immediately normalize the surviving local collection and add Order to every shifted member. Application Delete operations always soft-delete, including never-submitted creations: set DeleteRevision to 0, add Content and preserve EditRevision and existing flags. Hard deletion of a never-submitted deletion is deferred to delta preparation as defined below.
- An existing Content bit survives all order-only operations. Captured bits clear atomically with durable saving of their outgoing delta batch. Later local edits set the appropriate bits again. Uncertain/failed synchronization preserves the saved batches and any newer uncaptured flags.

These flags are per entity, not per collection. TransactionEntry/TemplateEntry Position remains part of parent aggregate content: changing entry positions marks the parent Content, not independent entry Order flags. Edits to unordered tracked entities use Content only; creation uses None. IsModified is no longer stored; if a convenience indicator is needed, ModificationType != None indicates edits not yet captured in a batch, not new creations or all pending synchronization work.

Confirmed change-detection rule (updated 2026-10-01): synchronization is pending when at least one durable outgoing batch exists OR any tracked entity has EditRevision null OR any tracked entity has ModificationType != None. EditRevision null identifies an entity awaiting batch capture even when ModificationType is None: a live row needs creation capture, while a row with DeleteRevision 0 needs local cleanup. Flags represent subsequent edits not yet captured in a batch; queued batches remain pending even when all entity flags are None. Content alone, Order alone, and Content | Order all qualify. Include pending ordering changes in synchronization even when no Content bits are set; an order-only edit cannot take the no-local-change path. Transfer only the applicable change semantics: an Order bit does not authorize overwriting content. Wire shape and detection implementation remain TQ-04/TQ-12. Normalization during accepted snapshot preparation is not a new local edit. Master prepares changes on a copy before publication. Local batch capture updates tracking metadata atomically before sending, as specified below; it does not imply Master acceptance. Separate order synchronization remains an explicit exception to whole-content conflict resolution; BRD 0.218 BR-029 now records this previously accepted exception.

#### Confirmed concrete-entity table layout — 2026-09-27

Each concrete persistent business entity has its own table containing its applicable persistent fields, including inherited fields. Shared base models provide model reuse only; they have no tables or separate base rows. An entity is not split across base and derived tables, and no Kind discriminator combines distinct entity types into one table.

PM-001 includes ReportGroup as the sixth concrete group table under the same shared structure and rules as the other five. Update the initial schema and initial data for fresh databases; no conversion/backfill of existing development reports is required. PM-002 expands to separate Category, Correspondent and Project tables. Account, Currency, CurrencyRate, Transaction, TransactionEntry, Template, TemplateEntry and Report likewise each have their own table. Names here identify entity types; exact physical table naming is not selected. TransactionEntry and TemplateEntry remain owned by their parent for synchronization even though they have separate tables; they do not gain independent sync fields.

This decision describes one table per concrete entity with its own complete field set, not joined base/derived table inheritance. Scalar storage details and ORM configuration remain outside this decision.

#### Confirmed synchronization tracking contract — 2026-09-27

`ITrackedEntity` represents synchronization metadata, not general audit history. This contract replaces the former shared `Revision`, `CreatedAt`, `EditedAt`, `DeletedAt` proposal and legacy `Original`, `Current`, `IsDeleted` approach. Separate audit fields are not selected by this decision.

MasterDb and LocalDb have identical business database schemas, including redundant modification flags on Master. Row contents differ. Admin.db remains separate and is not downloaded.

| Property | Canonical type | Nullability | Confirmed meaning and authority |
| --- | --- | --- | --- |
| EditRevision | int64 | Nullable | Revision of the last accepted delta batch that changed this entity content; null for a new, never-submitted local creation; 0 for a creation prepared for submission whose Master revision is unknown; positive for a known Master-assigned revision. Master allocates one global revision per newly accepted SyncKey within the dataset and assigns it to all entities whose accepted content changes in that batch, including creation/deletion/restoration. Unchanged, rejected and Order-only entities retain their previous EditRevision. Local may change null to the sentinel 0 when durably preparing the creation batch before sending; Local never generates or increments positive revisions. Installation copies the Master value. Master never stores 0 as an accepted entity revision. |
| DeleteRevision | int64 | Nullable | Null = alive, settable on both Local and Master; 0 = pending soft deletion, settable only by Local; >= 1 = Revision of the snapshot accepting deletion, assigned only by Master. Local may copy Master-assigned positive values during synchronization but never generates them. No negative values, separate deletion flag or datetime. |
| ModificationType | flags enum | Required | None = 0, Content = 1, Order = 2; Content and Order combine as 3. Replaces IsModified. Creation initializes None and is detected through EditRevision null. Local accumulates uncaptured subsequent-edit bits per tracked entity. Capturing those changes in a durable outgoing batch clears the captured bits in the same local transaction; later edits set bits again. Saved batches remain pending until successful installation of their accepted result. Present but redundant as local-change metadata on Master; downloaded bits are not authoritative. |

Signed 64-bit integer (`int64`) width is confirmed for EditRevision, DeleteRevision and all snapshot-version references, including SnapshotRevision. Scalar storage mapping remains TQ-02/TQ-04. **Revision** means the positive int64 identity of a Master database snapshot within MasterDatasetKey. Each newly accepted delta batch produces one snapshot and one Revision; merge revision and snapshot version are the same counter, not separate counters or timestamps. EditRevision and DeleteRevision both reference this counter. Existing fields named Version, PublishedVersion, InstalledVersion and SnapshotRevision refer to this same snapshot Revision; their names are retained here. An entity stores the Revision of its last accepted content change, not an entity-specific counter.

Assignment authority: Local initializes EditRevision to null for a new entity and sets it to 0 when durably preparing its first creation batch. Zero means that entity may exist on Master but its accepted Revision is unknown; it does not mean Master itself is unavailable. An ordinary edit to an entity with a known positive EditRevision preserves that value. Only Master assigns positive EditRevision values; Local copies them during installation. Accepted Master entities always have EditRevision >= 1. EditRevision must never be negative. DeleteRevision null is valid on both sides; only Local originates 0 and only Master originates a positive deletion Revision. Accepting deletion at Revision R sets both EditRevision and DeleteRevision to R. Accepting restoration at Revision S sets EditRevision to S and DeleteRevision to null.

| Activity | EditRevision | DeleteRevision | ModificationType / outcome |
| --- | --- | --- | --- |
| Create locally | null | null | None; EditRevision null identifies creation; creation payload includes all content and initial Order for an ordered catalog member |
| Edit content locally | Keep received revision | null for live entity | Add Content; preserve existing Order bit |
| Delete locally after prior Master acceptance | Keep received revision | 0 | Add Content; normalize survivors under ordering rules |
| Delete locally before creation is prepared for submission | Keep null | 0 | Soft-delete and add Content; normalize survivors; retain the row until delta preparation |
| Prepare delta for a never-submitted deleted entity | null | 0 | Hard-delete locally during atomic batch capture; emit neither creation nor deletion for this entity |
| Prepare new creation batch before sending | Set null to 0 | null | Save the immutable creation batch and local sentinel atomically before network transmission |
| Edit submitted creation with unknown Master outcome | Keep 0 | null | Add Content; queue the edit in a later batch, not as a new creation |
| Delete submitted creation with unknown Master outcome | Keep 0 | 0 | Soft-delete and queue deletion in a later batch; never immediately hard-delete |
| Install accepted live state | Copy Master revision | null | None |
| Install accepted deletion | Revision retained on Master marker | Positive on Master marker | Hard-delete locally after accepted outcome and successful installation |

Confirmed 2026-10-01: application Delete operations perform only soft deletion, regardless of EditRevision. Before merging, delta preparation accepts the captured local changes. At this boundary, and only when EditRevision is null and DeleteRevision is 0, the never-submitted deleted entity may be physically removed without emitting a creation or deletion in the delta. This cleanup, durable saving of any outgoing delta and acceptance of captured changes commit in one local transaction; failure preserves the rows and pending changes. A cleanup-only capture needs no empty outgoing batch. Respect reference integrity and remove eligible dependents before their parent; do not cascade-delete submitted rows or their deletion evidence. Retaining soft-deleted rows until capture leaves room for future restore functionality; no restore API or guarantee of restoration after physical cleanup is introduced. Preparing its first immutable outgoing batch and changing its local EditRevision from null to 0 must be one durable local transaction completed before sending. The saved first batch retains creation semantics; changing the live local row to 0 must not turn that batch into an edit. Keep 0 after timeout, cancellation or failed transmission, including when Master might never have received the request. Later batches contain edits or soft deletion of the same identity and are processed after the creation batch; Master skips that earlier batch if already accepted. A revision of 0 means possibly present on Master, not safe to hard-delete. Successful installation replaces 0 with the accepted positive Master revision, or removes the row after accepted deletion. No return from 0 to null is authorized by an uncertain outcome.

For an existing entity, when the Content bit is set and the revision differs from Master, this indicates a content conflict. Confirmed same-Local priority exception (2026-09-30): resolve the entity current Master EditRevision through the accepted-batch record, scoped to MasterDatasetKey. If that revision was produced by the incoming batch LocalDatasetKey, incoming content wins regardless of configured priority; otherwise use configured conflict priority. Compare the source of the current entity revision, not the latest dataset-wide revision or merely any earlier batch from that Local. Apply the exception to content edits, deletion and restoration, subject to business validity. Already accepted SyncKeys are skipped before conflict resolution. An accepted incoming content change receives the new batch revision. For example, K1 from Local1 produces B revision 16; K2 from Local1 still based on revision 15 wins over revision 16. If Local2 has since produced B revision 17, K2 uses configured priority instead. Matching revision allows the local change subject to validation. An absent Content bit means receive Master content without a content conflict, even if Order is set and revisions differ. Null revision in the saved creation batch identifies creation; 0 in a subsequent batch identifies an edit/deletion whose accepted Master revision is not yet known. Apply preceding batches first and then the shared content-conflict rules, including the same-Local exception. Unrelated creation identity-collision handling remains TQ-04. Priority selects whole entity content, excluding catalog Order governed by the separate ordering contract; there is no general content-field merge; business validity overrides priority (BR-029–BR-033). Keeping unchanged Master state does not increase revision. A winning local change that changes accepted state receives the newly allocated batch revision, not the previous entity revision plus one.

A winning restoration clears DeleteRevision and receives the accepted batch revision. A Local that previously hard-deleted the entity inserts the restored row with the same Id, accepted revision and ModificationType None; it is not a new local creation. Failed/uncertain sync preserves pending non-expired changes until its outcome is resolved. SyncKey/outcome recovery remains mandatory.

#### Durable batch capture and modification flags — confirmed 2026-09-30

Save an immutable outgoing delta batch and clear exactly the ModificationType flags captured by that batch in one local transaction. Capture live new entities with EditRevision null and DeleteRevision null regardless of their ModificationType, including their full creation content and initial Order where applicable. Entities with EditRevision null and DeleteRevision 0 are excluded from outgoing changes and physically removed at this capture boundary under the cleanup rules above. For subsequent edits, capture the values and applicable Content/Order semantics. If capture fails, neither the batch, its flag clearing nor the physical cleanup is committed. For first submission of a creation, the same transaction changes the live row EditRevision from null to 0 while preserving creation semantics in the batch. Flags set by subsequent edits belong to a later batch; do not clear them as acknowledgement of an earlier batch.

A failed or uncertain network attempt retains the saved batch; do not rebuild it from current entity values or restore its flags merely to retry. Pending synchronization means queued batches exist OR new entities with EditRevision null exist OR uncaptured flags exist. The no-local-change path is permitted only when all three are absent. Saving a batch is not sync success: retain it until successful installation of the accepted result. Exact queue storage and atomic installation/queue removal remain TQ-04.

#### Global merge revision allocation — confirmed 2026-09-30

For each newly accepted delta batch identified by SyncKey, Master allocates the next monotonically increasing int64 merge revision within that MasterDatasetKey. Several new batches in one HTTP request produce separate snapshots with consecutive Revisions in processing order; Local downloads only the final resulting snapshot. Replaying an already accepted SyncKey reuses its recorded outcome and revision; it neither allocates a new Revision, produces another snapshot nor reapplies changes. Every entity whose content changes in a batch receives that same revision. For example, with current merge revision 15, a batch creating A, editing B and deleting C assigns EditRevision 16 to all three; unchanged entities retain earlier values. A newly accepted batch with no content changes still produces its snapshot Revision, but changes no entity EditRevision.

The accepted batch outcome records the association between its merge revision, LocalDatasetKey and SyncKey, scoped to MasterDatasetKey. Entity revision equality remains the basis for detecting stale content. These records must remain available while needed to resolve the source of a current entity EditRevision and to recognize retried SyncKeys. Exact storage, safe retention/compaction and allocation/publication atomicity remain TQ-04. The confirmed same-Local exception above uses this association; BRD 0.218 BR-029 now records this previously accepted exception. No LastContentLocalDatasetKey property on every entity is required by this revision decision.

#### Confirmed snapshot versions and deletion acknowledgement

Published Master snapshots have positive int64 Version, starting at 1. A new snapshot gets current published Master Version + 1, never Max(SnapshotRevision) + 1. GUID filenames can remain; Version is the ordered snapshot identity. Serialized allocation/publication remains TQ-04.

SnapshotRevision (renamed from AcknowledgedVersion by the requesting user on 2026-09-30) is a required signed 64-bit integer, mapped to C# long. Each LocalDb stores SnapshotRevision; Master administration stores it per registered LocalDb (PM-014). It means the snapshot successfully installed, not merely sent/published. Local durably records successful installation; Master advances the registration only after receipt confirming that actual Version. Lost receipts can be retried without moving acknowledgements backwards. SnapshotRevision is non-null, initially 0 on Local and in its Master registration before the first successful installation/receipt respectively. Zero means no snapshot acknowledged; published snapshot versions start at 1. A failed initial download leaves the value at 0.

Master accepts deletion by assigning the accepted batch revision and setting DeleteRevision to the snapshot Version publishing that deletion. Local hard-deletes the accepted deleted row during installation. Master retains its deletion marker until every remaining non-expired registration has SnapshotRevision >= DeleteRevision; then a subsequent snapshot may omit the marker. Download failure/missing receipt does not satisfy this condition. Winning restoration clears the marker and cancels deletion eligibility.

Example: X with EditRevision 1 is deleted in snapshot Revision 10, receiving EditRevision 10 and DeleteRevision 10. Local1/Local2 acknowledge 10; Local3 acknowledges 8, so Master retains X. After Local3 acknowledges 10 or later, Master may remove the marker. If Local3's valid stale edit wins first, X instead becomes live at the Revision of the snapshot accepting restoration (for example 11), with DeleteRevision null, and is restored to other Locals with its original identity.

Confirmed local editing scope (2026-09-30): one user and one application instance edit each local database. Concurrent external edits through SQLite tools are unsupported and are the user's responsibility; the application need not detect or reconcile them. No separate local optimistic-concurrency revision or expected-revision token is required. EditRevision remains synchronization metadata and does not increment on ordinary local edits. Cross-device synchronization conflicts, editing restrictions during sync/wait, transaction integrity and crash recovery remain governed by their existing contracts.


Shared text-field semantics (confirmed 2026-09-28): Name is mandatory for named entities under their existing rules. Description is optional, visible and independently editable; absent or empty Description does not block saving. Remove Comment fields throughout. Currency has no Description; rates and transactions use Description without acquiring a Name field. Template.Description is visible, is not overwritten from Name, and is copied to Transaction.Description when applied. Creation of a template from a transaction and duplication of a transaction copy Description. No entry-level description is introduced.

#### Precision and user-requested balancing — confirmed 2026-09-28

Base currency, AmountPrecision and RatePrecision are selected at dataset creation and stored in SystemConfiguration. They cannot be changed later through settings or synchronization. AmountPrecision governs amount and BaseAmount calculation and display, including template amounts. RatePrecision governs rate precision for calculation and display, including stored entry and currency-catalog rates. Derived BaseAmount rounds Amount × Rate to AmountPrecision using midpoint-to-even, then transaction validation sums the rounded BaseAmounts. Display amounts and BaseAmount with AmountPrecision and rates with RatePrecision; there is no separate DisplayDecimalPlaces setting. SQLite stores persisted Amount and Rate values as signed 64-bit INTEGER scaled by 10,000, independent of the configured AmountPrecision/RatePrecision. Normalize input to its configured precision first, then multiply by 10,000 exactly and validate the integer range before storage. Read by dividing by 10,000 using decimal arithmetic. Example: Amount 12.34 with AmountPrecision=2 stores 123400; Rate 1.2345 with RatePrecision=4 stores 12345. Domain values and calculations remain decimal, not floating-point; BaseAmount remains derived and is not stored. The representable storage interval is -922337203685477.5808 through 922337203685477.5807, further restricted to the configured precision and positive-rate rule. This storage range does not guarantee products or totals fit. Confirmed calculation policy (2026-10-01): use C# decimal arithmetic for products, per-entry rounding and sums. Reject an operation if an input/conversion/intermediate product or total exceeds the supported decimal calculation range, or a normalized persisted Amount/Rate cannot fit the signed int64 scaled storage range. Do not wrap, clamp, switch to floating point or persist a partial result. Mutation failures leave existing data and synchronization flags unchanged; read calculations fail without returning misleading totals. Derived BaseAmount and aggregate totals are bounded by decimal calculations, not by the scaled storage interval unless they are subsequently used as a persisted Amount. No additional arbitrary business amount cap is imposed. Configured precision, midpoint-to-even rounding, positive-rate and base-rate rules still apply.

For any nonzero current total, the editor offers Add balancing entry. On an explicit click, append an entry using the configured account whose currency is the base currency, Amount = -total, Rate = 1 and derived BaseAmount = -total. Recalculate the whole transaction and require exactly zero for confirmation; all other save rules still apply. No tolerance or maximum-difference threshold is imposed. The user can alternatively add this entry manually. The frontend calculates and appends this entry to the unsaved transaction, using the configured precision, midpoint-to-even rounding and balancing-account selection. No separate Business operation is required. The normal save action persists it after Business validates all entries and derives Draft or Confirmed state. Subsequent edits can create a new imbalance and must be revalidated. No entry is added silently.

The balancing-account selection is stored in synchronized SystemConfiguration (PM-011), shared across Master and all Local copies. It references an Account whose currency is the dataset base currency. Initialization creates one Account initially named Rebalancing in the Account root group with the dataset base currency and sets BalancingAccountId to its Id. It is an ordinary account: transaction/template references still prevent deletion, but this settings selection alone does not. The user may change the selection in Settings to another base-currency account. If the configured account is absent or soft-deleted, Add balancing entry must leave the transaction unchanged and show: "Cannot add a balancing entry: the rebalancing account is missing. Please choose a rebalancing account in Settings." Manual balancing remains available. Do not recreate the account automatically. BalancingAccountId is a nullable GUID property of SystemConfiguration. When the selected account is deleted, Business preserves the stored BalancingAccountId and exposes null in its local read result. Only Setup may clear or change the stored selection. A null selection uses the same missing-account message and leaves the transaction unchanged. Base currency/AmountPrecision/RatePrecision immutability does not apply to this selection. AmountPrecision and RatePrecision each accept integers 0–4 inclusive, with defaults AmountPrecision = 2 and RatePrecision = 4. Creation rejects values outside this range. Display precision follows AmountPrecision/RatePrecision as defined above. DTO-016 creation/configuration DTOs and frontend balancing-action behavior follows the confirmed editor responsibility; existing DTO omissions cannot override these confirmed requirements.

### PM-001 — Group

Purpose: six same-type hierarchies, including Reports/ReportGroup, BR-006–BR-009/028. ReportGroup uses the same common group fields, relationships, ordering, favorites, validation and lifecycle as the other five group types, confirmed 2026-10-08. Confirmed model approach: AccountGroup, CategoryGroup, CorrespondentGroup, ProjectGroup, TemplateGroup and ReportGroup are separate derived models sharing a common group base. Equal current field sets do not collapse them into one model with a Kind enum. PM-001 describes their shared contract, not an instantiable generic group record. Each model has parent/children of its own group type and elements of its corresponding element type. Shared identifier above. The confirmed concrete-entity table layout applies.

| Property | Canonical type | Nullability | Constraints / relationship | Source/status |
| --- | --- | --- | --- | --- |
| ParentId | uuid | Required | One same-type parent; root self-parent | C retained; type proposed |
| Name | string | Required | Trim/nonblank; sibling uniqueness; fixed root label | BR-006/008; proposal |
| Description | string | Nullable | Optional; root immutability still applies | BR-009; confirmed |

Retain the five confirmed root identities below. BRD 0.177 requires six initialization roots; ReportGroup now has its own fixed root identity recorded below and used by initialization constants and seed data. Do not reuse one of the five existing constants. The IDs are hardcoded, never generated per device or per database, and remain identical across Master and Local copies. Each root has ParentId equal to its own Id and is excluded from its own Children. Existing root immutability rules remain binding. These persistent identity constants must not change for existing databases without migration.

| Root group | Fixed Id (GUID) |
| --- | --- |
| AccountGroup | 22D0BBCC-37EC-4AF4-B5C7-9CAF34FEC1E9 |
| CategoryGroup | CDB033F6-8686-4222-B33F-66B5A3BF2948 |
| CorrespondentGroup | 9C19A1FE-9C57-4703-9105-79075987EE45 |
| ProjectGroup | 7C9385F6-28F2-4947-8B44-E81DD8D01949 |
| TemplateGroup | B232A84F-47D8-426E-AA54-BA8771B8B6DE |
| ReportGroup | 8FD28CC7-69B2-441C-A27B-C85F56DF0D44; fixed in RootsIds and initial seed data, identical on Master and Locals |

Source values: [RootsIds.cs](../../backend/Business.Models/Constants/RootsIds.cs), confirmed by the requesting user. IsRoot is a derived boolean: true exactly when a group Id equals the fixed root Id for that group type. It is not a persistent field and is not independently editable or synchronized. DTO projections may expose this read-only derived value. An extension method is an implementation option, not a required technical contract.

Relationships: parent reference, child groups excluding self, type-matching elements. Lifecycle: non-root create/edit/move/merge/delete; protected root; source removed after merge. Concurrency/sync tracking: shared contract; merge transaction scope confirmed by the shared merge transaction rule.

### PM-002 — Classification

Purpose: distinct Category, Project, Correspondent concepts (BR-010). Confirmed model approach: Category, Correspondent and Project are separate derived element models sharing a common element base, even though their current field sets are equal. Each belongs to its corresponding group type. PM-002 describes their shared contract, not a single classification model distinguished by a Kind enum. Account and Template retain their specialized contracts in PM-005/PM-008. Shared Id/sync tracking. New model types can be introduced by derivation rather than expanding a Kind enum; no additional business type is authorized here. The confirmed concrete-entity table layout applies.

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| GroupId | uuid | Required | Corresponding derived group in PM-001; many elements to one group | BR-006; proposed |
| Name | string | Required | Nonblank trimmed; unique within group, case-insensitive | BR-008; proposed |
| Description | string | Nullable | Optional and independent of Name | BR-009; confirmed |

Lifecycle: create/edit/move; delete only unused by accounts; no automatic account rename. Concurrency/sync tracking shared.

Confirmed classification merge (2026-10-01): CombineElements applies to Category, Correspondent and Project. Source and destination belong to the same classification type. Find every Account referencing the source and replace only that type's foreign key and reference with the destination: CategoryId/Category, CorrespondentId/Correspondent or ProjectId/Project. Leave the other two classifications and Account names unchanged. Then delete the source element using the shared deletion lifecycle; retain the destination. Changed accounts and source deletion use the shared content-tracking rules. Account replacement has its own contract under PM-005; Template merging is unsupported as confirmed under PM-008. All merge changes commit together under the confirmed merge transaction rule; other invalid-input handling remains TQ-10.

### PM-003 — Currency

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| Code | string | Required | ISO code unique, immutable | BR-013; proposed |
| Name | string | Required | Catalog English default; editable | BR-013; proposed |
| Symbol | string | Required | First catalog symbol; editable | BR-013; proposed |

Shared Id/sync tracking. Many PM-004 rates per currency; business dataset's base currency identifies one PM-003. Delete only unused/non-base. Base selection immutable. Name/Symbol blank policy not independently specified; proposed required type does not invent nonblank validation. Concurrency/sync tracking shared.

### PM-004 — CurrencyRate

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| CurrencyId | uuid | Required | One PM-003 | BR-014; proposed |
| Date | date | Required | DateOnly calendar date with no timezone conversion; unique ordinary date per currency; initial date fixed at 1970-01-01 by shared constant | BR-014/021; proposed |
| Rate | decimal | Required | Scale RatePrecision, positive after rounding; base = 1 | BR-014/017; proposed type, confirmed calculation |
| Description | string | Nullable | Optional notes on this rate, including initial rate; independent of transaction/template descriptions | BR-009/014; confirmed 2026-09-28 |

Confirmed initial-rate representation (2026-09-28): one shared application constant defines the DateOnly value 1970-01-01 for the initial rate. Use the same value on Master and all Locals; do not duplicate date literals or expose it as an editable setting. IsInitial is derived from Date equaling this constant, not persisted as a boolean. Exactly one initial fallback rate exists per currency; its date is hidden and immutable, its row cannot be deleted, and its value remains editable subject to the base-currency rate of 1. Ordinary rates remain dated 2001-01-01 or later. The constant declaration name and source location are implementation details.

Confirmed rate deletion (2026-10-01): a separate UI Delete action calls the rate deletion operation with CurrencyId, FromDate and ToDate. Delete all matching ordinary rates for that currency using the shared deletion lifecycle, always excluding the initial rate. Including the initial date in the requested range does not reject the request and does not delete or modify the initial row. Existing transaction-entry rates are unchanged. All affected rate deletions and sync metadata commit together under the local API action rule. Confirmed 2026-10-01: both endpoints are inclusive; select FromDate <= Date <= ToDate, excluding the initial rate. Equal endpoints select that one calendar date. Confirmed 2026-10-01: if FromDate > ToDate, throw a validation exception before any deletion or sync-metadata change; do not commit. The exception type/code remains part of the error-contract design.

Confirmed 2026-10-01: rate add/update locates the row by CurrencyId and Date. Update Rate and Description when found; otherwise create a row with a generated Id. The caller does not supply the rate Id. A different Date targets a different currency/date pair; this upsert does not identify or move a previous-date row. Keep at most one row per pair. Shared Id/sync tracking; edit initial value but not its date; ordinary date rate lifecycle under BR-014. No stored link from transaction entry to this row. Concurrency/sync tracking shared.

Confirmed date semantics (2026-09-28): store transaction/backend timestamps as UTC instants. CurrencyRate.Date is a date-only value (DateOnly), with no timezone or UTC conversion. For default rate and Restore Rate, convert the transaction timestamp to the current UI/device timezone, take its calendar date, and choose the latest rate on or before that date, with the initial fallback. Applying templates uses the same lookup. Stored entry rates remain independently editable; date or timezone changes never automatically rewrite them.

Report day/week/month/year grouping and From/To calendar dates use the current UI/device timezone. Convert local period boundaries to UTC instants for filtering; use inclusive start and exclusive next-period start. Determine boundaries in that timezone rather than assuming every local day is 24 hours. DateOnly values are interpreted directly as calendar dates, not UTC instants. Existing minimum transaction instant remains 2001-01-01 00:00 UTC; minimum ordinary rate date remains DateOnly 2001-01-01.

Example: transaction January 1 04:00 in UTC+8 is stored December 31 20:00 UTC and uses the rate applicable on January 1. That local January 1 report covers December 31 16:00 UTC inclusive through January 1 16:00 UTC exclusive. UI should make the local transaction date used for the default understandable. Business operations and Reporting execute on the local device and use its current timezone directly. No timezone API parameter or additional timezone-context contract is required. Local input/display and calendar boundaries use device-local time; persisted transaction timestamps remain UTC. Convert each calendar boundary using the local timezone and its applicable daylight-saving rules. Remote synchronization uses UTC for timestamps and does not perform local bookkeeping or report calculations.

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

Confirmed account replacement (2026-10-01): IAccountService.CombineElements replaces the source Account with the destination Account in every TransactionEntry and TemplateEntry that references the source. Both accounts must reference the same currency; reject a currency mismatch before modifying any entries. Update AccountId and the matching account reference; amounts, stored transaction rates and entry order remain unchanged. Confirmed 2026-10-02: recalculate the destination account's affected CumulativeAmount values after replacement under the PM-007 ordering and contribution rules; Draft entries retain zero. Cache-only changes to other transactions do not set synchronization flags. Do not combine or remove entries when replacement produces repeated references to the destination account; repeated accounts are already permitted. Under the shared aggregate synchronization contract, add Content to every affected Transaction and Template, preserving existing flags; entries have no independent sync flags. This operation does not change either account's currency. After all references have been replaced, delete the source Account under the shared deletion/tracking lifecycle; retain the destination Account. If System configuration selected the source account, preserve its stored BalancingAccountId; Business reads expose null after source deletion and never silently select the destination. Setup owns any later persistent settings correction. All affected aggregates, destination cumulative-amount recalculation and source deletion commit together in the same database transaction or all roll back under the confirmed merge transaction rule; other invalid-input handling remains TQ-10.

### PM-006 — Transaction

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| DateTime | datetime | Required | UTC, minimum 2001-01-01; display device timezone | BR-020/021; proposed type |
| Description | string | Nullable | Optional, aggregate-level | BR-016; proposed |
| State | enum | Required | Undefined = 0 (unset, never persisted); Draft = 1; Planned = 2 (reserved for future); Confirmed = 3. Current persisted states are Draft and Confirmed; rules below. | BR-019; representation confirmed 2026-09-27 |

Confirmed state representation (2026-09-27):

- `Undefined = 0`: unset/uninitialized state, replacing the legacy name NoValid. It is not a valid persisted transaction state; a successful save must determine Draft or Confirmed under the rules below.

- `Draft = 1`: transaction with fewer than two entries or a nonzero rounded base total, excluded from all accounting balances, totals and reports. Any number of entries, including zero, is permitted; Drafts have no separate entry-count limit. Every present entry must have an existing account, valid positive rate (base rate 1) and numeric amount, including zero. Date and numeric validation remain mandatory. UI shows a persistent Draft warning. Both Add and Update may save Draft, including a previously Confirmed transaction; removing its accounting contribution is part of the saved state change. Draft relaxes entry count and balance only, not entry validity.
- `Planned = 2`: reserved for future valid transactions excluded from accumulated balances and reports. No first-release creation, selection or transition workflow is enabled for this state. This reservation does not add planned-transaction functionality to BRD scope.
- `Confirmed = 3`: valid transaction included in balances and reports. This is the stored representation of BRD's active transaction. Saving a complete balanced transaction automatically produces Confirmed; saving fewer than two valid entries or an unbalanced edit returns it to Draft. Invalid entries, dates or numeric values are rejected in either state.

Balance means the exact sum of per-entry rounded BaseAmount values is zero. Entry count is the number of entries, not an Amount value or a count of distinct accounts; repeated accounts remain permitted. Values 0, 1, 2 and 3 are explicit; no additional valid persisted state is selected by this decision.

Shared Id/sync tracking. Owns zero or more PM-007 entries. Drafts have no separate entry-count limit; every present entry must be valid, including a mandatory existing Account, valid rate and numeric amount. Confirmed transactions require at least two valid entries and an exactly balanced total. Entry mutation occurs only through the whole transaction. Lifecycle create/draft/activate/edit/delete with valid rules; cached account-currency cumulative amounts follow PM-007. Concurrency whole aggregate; sync tracking shared.

### PM-007 — TransactionEntry

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| TransactionId | uuid | Required | Exactly one PM-006; retained parent reference must match | Legacy TRD explicit parent relation; type proposed |
| AccountId | uuid | Required | Exactly one PM-005 | BR-016; proposed |
| Amount | decimal | Required | Scale AmountPrecision; blank input maps to zero | BR-016/017; proposed |
| Rate | decimal | Required | Independent stored value; no CurrencyRate foreign key | BR-015; proposed |
| Position | int32 | Required | Persisted order within the parent Transaction; survives save/load and whole-aggregate sync; repeated accounts remain separate entries | Confirmed 2026-09-27; shared entry-order rule below |

Confirmed cumulative-amount cache (2026-10-02): TransactionEntry has a calculated, persisted CumulativeAmount in its account currency. It is the account running sum of Amount including the current contributing entry. Only this cumulative value is cached; there is no CumulativeBaseAmount. Existing base-currency balance outputs and per-entry BaseAmount calculation remain unchanged. Existing accounting inclusion rules still exclude Draft and deleted transactions. Confirmed 2026-10-02: every entry belonging to a Draft transaction stores CumulativeAmount = 0 and is excluded from cumulative-cache lookups, including selection of the preceding balance. A Confirmed-to-Draft transition removes the old accounting contribution and recalculates affected subsequent account cumulative amounts; all entries in the saved Draft have CumulativeAmount = 0. A Draft-to-Confirmed transition adds the new accounting contribution and calculates cumulative amounts for the newly contributing entries and affected subsequent account entries. Both transitions are part of Update and use the same atomic save/recalculation boundary below.

Order entries separately for each account by Transaction.DateTime ascending, then Transaction.Id in binary ascending order, then entry Position ascending. Do not convert Transaction.Id to a string for comparison. This rule is specific to the cumulative calculation; it does not replace the separate catalog-order GUID comparison. Confirmed binary comparison (2026-10-02): use the 16 bytes returned by the parameterless Guid.ToByteArray(), compared lexicographically in ascending order from byte index 0 through 15 as unsigned byte values; the first differing byte determines the order. Equal arrays mean equal transaction identities. SQLite ordering must use that same byte sequence and comparison, including the inverse order when selecting the last preceding entry. Do not substitute GUID text ordering, Guid.CompareTo(), or a different byte layout. This settles comparison semantics; implementing the matching database representation or sorting expression remains an implementation task, not authorization to change unrelated catalog ordering.

For transaction Add/Update/Delete, transaction changes, applicable synchronization tracking and recalculation of affected CumulativeAmount values must commit in the same database transaction. If saving or recalculation fails, roll back both; no partially updated cache or transaction may become visible as a committed result. Deleting a Confirmed transaction removes its contribution and recalculates subsequent CumulativeAmount values for every account referenced by its entries. Deletion, applicable synchronization flags/deletion evidence and recalculation commit together or all roll back. This applies to single deletion and to Confirmed transactions removed by filtered bulk deletion; the existing whole-batch atomic boundary remains binding. This extends OP-027/028/064/066–069 and the shared L/W mutation boundary. Source: the requesting user's accepted cache scope, ordering correction and atomicity answer in this chat on 2026-10-02.

Confirmed cache-only synchronization rule (2026-10-02): recalculating CumulativeAmount alone does not mark the entry's parent Transaction as modified for synchronization and does not set synchronization flags on later transactions whose cached values change. Preserve any existing synchronization flags; cache maintenance must not clear evidence of actual edits. Actual transaction changes continue to use the established synchronization-tracking rules. This distinction applies within the atomic maintenance boundary above and to account combination under PM-005/OP-054, which recalculates the destination account's affected cumulative amounts in the same database transaction as the combination.

Confirmed post-merge local rebuild (2026-10-02): after downloading a merged database, rebuild CumulativeAmount locally for all accounts before making that database available for normal use. Apply the established account ordering, Confirmed-only contribution and zero Draft-entry rules. The rebuild does not set synchronization flags. An incomplete or failed rebuild must not expose the downloaded database for normal use; this prerequisite does not redefine the existing synchronization outcome or recovery protocol. The full-account command variant below supplies the recalculation scope; rebuild orchestration and restart/recovery mechanics remain unresolved.

Confirmed cumulative maintenance command (2026-10-02): use ICumulativeAmountCommand to update CumulativeAmount for entries belonging to one specified Account. The requesting user's phrase "particular amount" is interpreted as "particular account" from the preceding account-scoped discussion. CumulativeAmountCommand must be implemented in DataAccess.EntityFramework.SqLite; this is the user's explicit provider-specific placement decision. Use parameterized SQLite running-sum/update SQL for the account-scoped operation rather than loading and tracking every affected entry for individual EF updates. Exact method names and declaration syntax remain to be defined.

| Command variant | Logical inputs | Calculation scope and initial value |
| --- | --- | --- |
| Range recalculation | Account identity, inclusive UTC start timestamp, initial cumulative amount | Recalculate that account's entries from the start timestamp through its last entry. Seed the sum with the last live Confirmed entry's CumulativeAmount strictly before the timestamp; use zero if no preceding contributing entry exists. The initial value must correspond to the same account and boundary in the caller's database transaction. No end-date cutoff is introduced. |
| Full account recalculation | Account identity | Recalculate the account's complete history with zero as the initial cumulative amount; no date-range filter. |

Confirmed transaction ownership and call chain (2026-10-02): the application service starts the database transaction through the unit of work, calls ICumulativeOperation, and commits through that unit of work in the same service function after the complete operation succeeds. ICumulativeOperation calls ICumulativeAmountCommand. Failure propagates to the service, which owns rollback of the whole transaction. Operations and commands do not own or manage the transaction context: they do not begin, commit or roll back transactions, accept transaction-lifecycle responsibilities, or require callers to pass a transaction context. Transaction participation is supplied by the shared DAL/unit-of-work infrastructure. No command-level CurrentTransaction check is required by this contract; the earlier discussion example with such a check is superseded. The same service function owns the entire mutation and cumulative recalculation boundary; invoking an operation or command does not create a nested transaction.

Both variants apply the established binary transaction ordering, entry Position, Confirmed-only contribution, Draft-zero and deleted-entry exclusion rules. Update only the derived cache; cache-only changes do not set synchronization flags. Execute within the service-owned unit-of-work transaction under the call-chain rule above; operation and command contracts have no transaction-context responsibilities. The range opening value is a supplied calculation seed, not editable transaction data. Confirmed opening-balance retrieval (2026-10-02): provide a read-only DAL function taking the account identity and inclusive recalculation start timestamp. It finds the last live Confirmed TransactionEntry strictly before that timestamp and obtains its CumulativeAmount. Select the last entry by Transaction.DateTime descending, then Transaction.Id descending under the confirmed Guid.ToByteArray() byte comparison, then Position descending; exclude Draft and deleted transactions. ICumulativeOperation invokes this DAL function and passes the resulting opening balance to the range variant of ICumulativeAmountCommand. If no preceding contributing entry exists, use zero as already specified by the range contract. Confirmed responsibility (2026-10-02): when no predecessor exists, the DAL reports absence rather than substituting zero. ICumulativeOperation in Business interprets that absence and supplies zero to ICumulativeAmountCommand. A found predecessor whose cumulative amount is zero remains a found result and must be distinguishable from absence. The implemented absence representation is decimal? (null means absent); the business fallback must not be implemented in the DAL lookup. The lookup does not create or manage transactions; the service owns the transaction covering lookup and update. The command consumes the supplied opening value without repeating its lookup. The implemented contract is DataAccess.Contracts.Repositories.ITransactionEntryRepository.GetPrevious(accountId, beforeDateTime), returning decimal?; retrieval behavior and ownership are confirmed. The full-account variant supports the post-merge rebuild of every account. The SQLite implementation now maps TransactionEntry.Amount and CumulativeAmount to scaled int64 and Transaction.Id/TransactionEntry.TransactionId to the specified BLOB bytes. Parameterized window-SUM UPDATE SQL is validated against the application schema by local SQLite tests. Existing databases still require explicit migration before using these mappings.

CumulativeAmount schema (storage and overflow confirmed 2026-10-02): persist CumulativeAmount as a signed 64-bit integer (long/int64), scaled by 10,000, matching Amount storage. A stored value represents the account-currency amount multiplied by 10,000. Every persisted cumulative value must fit this range even when all individual entry amounts fit. Reject the operation on cumulative calculation or storage-conversion overflow and roll back the complete outer transaction, including transaction/account changes, synchronization tracking and cache updates. A failed rebuild must not make the candidate database usable. Never wrap, clamp, silently truncate or switch to floating point to obtain a result. The implemented logical property is non-nullable decimal in account-currency units; the physical integer representation and overflow outcome are confirmed. The user's initial maintenance proposal uses accounts from both old and new entries and recalculates from Min(old DateTime, new DateTime) to the last account entry; Add has only a new date. The user also proposed excluding this cache from synchronization, allowing zeros on Master and exposing cumulative maintenance to the merging subdomain. Post-merge local rebuilding before normal use is confirmed above. These remaining lifecycle and service details remain under discussion; the two account-scoped DAL command variants and SQLite implementation placement are confirmed above. Stored cache values on retained deleted entries also remain TQ-14; Draft entry values and both Draft/Confirmed transition effects are confirmed above.

Cumulative implementation discussion record — 2026-10-02 (technical guidance and proposals; not additional approved contracts):

- Initial user direction: for an edit, collect the union of accounts from the old and new transaction entry sets before replacing entries, and maintain each affected account from Min(old Transaction.DateTime, new Transaction.DateTime) through its last entry. Add uses the new date; removal uses the old contribution. Equal-boundary timestamps are included in full; the opening value comes strictly before the boundary. State transitions use only the applicable Confirmed contribution. Precise orchestration remains to be finalized.
- Initial user synchronization direction: CumulativeAmount is derived and need not be synchronized as authoritative business content; Master may contain zero cache values. This is distinct from excluding bytes from a complete database snapshot. Rebuilding the downloaded merged database before normal use is already confirmed. Exact Master normalization and transfer representation remain open.
- Initial user service proposal: ICumulativeService exposes cumulative maintenance to outer clients such as the merging subdomain; ICumulativeOperation serves internal Business work. These higher-level interfaces are separate from the accepted ICumulativeAmountCommand. The Service-to-Operation-to-Command call chain and service-owned transaction lifecycle are confirmed above. Exact service/operation methods and outer-client exposure remain unresolved; no additional API exposure is inferred.
- SQL calculation pattern discussed: obtain or supply the opening balance; create an artificial first input row containing it; append only the selected account's live entries at or after the inclusive UTC boundary; use SUM of Confirmed contributions with an explicit ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW window ordered by seed first, DateTime, binary Transaction.Id and Position; force Draft results to zero; UPDATE matching real entry IDs, optionally skipping unchanged cache values. The artificial row is not persisted. The full-account variant starts from zero. The selected range must not silently restart at zero when earlier contributions exist. No end timestamp is imposed. This is an illustrative execution pattern, not verified production SQL.
- Parameterization: pass account identity, boundary and opening value as database parameters. ExecuteSqlInterpolatedAsync accepts a FormattableString and binds its holes as parameters when the interpolated expression is passed directly; assigning it to string first loses that behavior. Parameterization is distinct from guaranteed prepared-command or execution-plan reuse. Use the actual mapped table/column names and database parameter representations; raw SQL does not automatically apply entity property value converters.
- Transaction mechanics discussed: the service begins, commits or rolls back through the unit of work in the same service function. DAL infrastructure supplies the shared connection and active database transaction to execute commands; operations and commands do not manage or inspect the transaction lifecycle. The command executes immediately and never commits independently. Database-side calculation must see saved replacement entries. An EF SaveChanges inside an explicit transaction flushes those entries without committing the outer transaction; the cumulative SQL then executes before the single final commit. Direct cache SQL needs no subsequent SaveChanges. Failure requires rollback of the entire outer operation, and tracked stale cache values must not later overwrite direct SQL results. The relationship of these internal flushes to the document's existing one-AcceptChanges contract must be made explicit before implementation; one atomic commit remains binding.
- SQLite isolation fact: Microsoft.Data.Sqlite defaults to Serializable and treats requested isolation as a minimum; a requested ReadCommitted is promoted to Serializable. Serializable was recommended during the discussion, not explicitly selected as a new provider-independent isolation requirement.
- Numeric implementation guidance: the signed-int64 scale of 10,000 and rollback on cumulative overflow are now confirmed in the schema rule above. Validate scale and range rather than silently truncating a decimal-to-integer conversion. SQLite integer SUM reports overflow; adding a separate integer opening value with ordinary SQL arithmetic can instead promote an overflowing result to floating point. The seed-row pattern keeps the opening balance inside SUM. Mapping and SQL must implement the confirmed numeric contract; example SQL still requires validation against the real schema.
- Performance limits: the range variant avoids recalculating and updating earlier entries but still needs an efficient predecessor lookup. AccountId is on entries and DateTime is on transactions; an ordinary single-table index cannot span that join. Examine actual query plans and measure recent edits, historical edits and full rebuilds. A single command still reads/orders/writes affected rows; no latency guarantee or benchmark result is claimed.
- Alternatives discussed, not selected as the primary command implementation: narrow EF projections plus decimal calculation and property-only tracked updates; parameterized per-entry SQL, optionally using one reusable prepared command; difference-based range updates for incremental edits. The accepted primary direction remains account-scoped parameterized SQLite running-sum/update SQL. Full rebuilding must not rely on a previously valid cache.
- Implementation status from this chat's inspection: Business TransactionEntry contains CumulativeAmount; the inspected DAL TransactionEntry lacks it, and the inspected EF configuration does not implement the specified scaled-integer conversion. The unit of work already exposes explicit database transactions. These are inspection findings, not implementation completion; no code or schema was changed in this discussion.

Technical references: [SQLite window functions](https://www.sqlite.org/windowfunctions.html), [SQLite UPDATE](https://www.sqlite.org/lang_update.html), [SQLite SUM](https://www.sqlite.org/lang_aggfunc.html), [SQLite expression arithmetic](https://www.sqlite.org/lang_expr.html), [EF SQL parameterization](https://learn.microsoft.com/en-us/ef/core/querying/sql-queries), [EF transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions), and [Microsoft.Data.Sqlite isolation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions). Reviewed 2026-10-02.

Shared entry-order rule for PM-007 and PM-009 (confirmed 2026-09-27): Position is int32, numbered consecutively 0 through N-1 within its parent. Saving an edited entry collection assigns positions from its intended order after insertion, removal or reordering. Empty templates have no positions to assign. Synchronization preserves the accepted whole aggregate and its positions; it does not merge entry lists or renumber collections merely because synchronization occurred. This rule applies only to TransactionEntry and TemplateEntry, not catalog elements or child groups. The persistent field remains Position in current code; it expresses the entry order requested by the user. Replacing the entry set on save preserves the submitted order, not the old row IDs. Confirmed 2026-10-01: transaction and template entry DTO list/array order is authoritative. On save, assign each replacement entry Position equal to its zero-based index, producing 0..N-1. Entry input DTOs contain no separate Position or Order property and no entry Id. Read projections return entries in persisted Position order so editing preserves that sequence. An empty template list produces no entry rows.

Derived value (confirmed 2026-09-27): BaseAmount = Amount × Rate, rounded per entry to AmountPrecision decimal places using midpoint-to-even. BaseAmount is calculated when needed, not stored as a persistent column or synchronized independently. Balances, reports and the exact-zero transaction check use these rounded per-entry values. It is read-only and cannot be supplied as an authoritative input.

Generated row Id; no entry ID in the mutation DTO. Aggregate edits replace all old entries with newly identified rows; tracking belongs to the parent aggregate and repeat accounts remain allowed. No independent entry service or sync. Lifecycle follows parent; deletion propagation managed at aggregate boundary.

### PM-008 — Template

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| GroupId | uuid | Required | Template group PM-001 | BR-022; proposed |
| Name | string | Required | Trim/nonblank; unique within group | BR-008/022; proposed |
| Description | string | Nullable | User-visible optional text; copied to transaction Description when applying template | BR-022; confirmed role |

Template has one visible optional Description. No hidden description, separate Comment or automatic Name-to-Description assignment remains.

Confirmed 2026-10-01: combining Templates is unsupported. The inherited ITemplateService.CombineElements member is a stub; calling it fails with NotSupportedException before changing data or synchronization flags. Its presence through interface inheritance does not define a supported template-merge capability. This restriction does not affect supported TemplateGroup merging or Account replacement inside TemplateEntries. Interface restructuring is outside this requirements decision.

Shared Id/sync tracking. Owns zero or more PM-009; aggregate concurrency/sync. Applying does not save transaction. Create, update and delete follow the shared Business lifecycle. On editor update, replace the complete entry collection under the aggregate rule.

### PM-009 — TemplateEntry

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| TemplateId | uuid | Required | Exactly one PM-008; parent reference and identity match | Legacy TRD; type proposed |
| AccountId | uuid | Required | Exactly one PM-005; account currency is already immutable | BR-011/022; proposed |
| Amount | decimal | Required | Scale AmountPrecision; template need not balance | BR-022; proposed |
| Position | int32 | Required | Persisted order within the parent Template; shared entry-order rule in PM-007 applies; survives save/load and whole-aggregate sync | Confirmed 2026-09-27; shared entry-order rule below |

Generated row Id; no entry ID in the mutation DTO. Aggregate edits replace all old entries with newly identified rows; tracking belongs to the template aggregate. Rates are resolved on applying template, not copied as template rate fields. Parent lifecycle/concurrency.

### PM-010 — Report

BRD 0.177 extends Report to ReportGroup membership and favorites with saved instructions/period and readonly results/export. The earlier Id/Name/Json-only discussion limit is superseded. Report GroupId, IsFavorite and Order follow the same catalog conventions as the other catalogs, confirmed 2026-10-08. JSON serialization remains unresolved; no JSON version field is selected.

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| Name | string | Required | Nonblank; default yyyy-MM-dd HH:mm:ss; duplicates allowed | BR-028; proposed |
| Json | string | Required | JSON shape defined by DTO-012; instructions only, no result cache | BR-025/028 and user JSON direction; shape proposed |
| GroupId | uuid | Required | One ReportGroup; root may contain reports | Confirmed 2026-10-08: same required group identity convention as other catalogs |
| IsFavorite | bool | Required | Default false; ordinary content synchronization | Confirmed 2026-10-08: same required bool/default-false convention as other catalogs |
| Order | int32 | Required | Shared separate zero-based catalog ordering and synchronization rules | Confirmed 2026-10-08: same ordering convention as other catalogs; initialize fresh data, no legacy-report migration |

Shared Id/sync tracking. Referenced group/element identities are soft references, do not protect deletion. Read/run never normalizes saved classification instructions; full settings Save recalculates minimal override lists from edited actual state. A pre-run period save changes only From/To and preserves all other values and saved IDs/overrides. Missing identities retained until explicit save may remove obsolete references. Rename-only handling must preserve definition unless owner saves changed selection; exact mutation shape TQ-05. Concurrency/sync tracking shared.

### PM-011 — SystemConfiguration

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| Id | uuid | Required | Single SystemConfig row identity generated on Master during initialization; retained by all Local copies and through synchronization merges | User confirmation 2026-09-30 |
| MasterDatasetKey | string | Required | Generate once during dataset initialization using Guid.ToString(); preserve unchanged in Master and all Local copies. Reject synchronization between different dataset keys. Not the SystemConfiguration primary key. | User naming/type decision and lifecycle confirmed 2026-09-29 |
| BaseCurrencyId | uuid | Required | References the dataset base Currency (PM-003); stored in SystemConfiguration, selected during initialization and immutable afterward | BR-004; type, requiredness and System placement confirmed 2026-09-29 |
| BalancingAccountId | uuid | Nullable | Initially references the generated rebalancing PM-005 in Account root; base-currency account selection shared through System sync; Business reads expose null when the selected account is missing/deleted; Setup alone clears or changes the stored selection; selection alone does not prevent deletion | BR-018; System ownership confirmed 2026-09-28; nullable GUID and clearing accepted 2026-09-29 |
| AmountPrecision | int32 | Required | Amount and rounded BaseAmount fractional places; range 0–4 inclusive, default 2; selected at creation, immutable | BR-017; confirmed |
| RatePrecision | int32 | Required | Rate fractional places; range 0–4 inclusive, default 4; selected at creation, immutable | BR-017; confirmed |
| EditRevision / DeleteRevision / ModificationType | Shared tracking contract | As defined above | Synchronized configuration tracking | Confirmed 2026-09-27 |

Identifier naming convention (2026-09-29): use Id for entity primary keys, {Entity}Id for foreign keys/references between entities, and Key for other identifiers. MasterDatasetKey is a GUID-formatted string using Guid.ToString(), separate from the table primary key. Corresponding dataset identifiers elsewhere in this TRD use the same name and representation; their proposed contracts remain proposed. Other identifier roles and their final names require review before finalizing schemas.

Purpose: synchronized System table with exactly one SystemConfig row per dataset, present in Master and each Local copy. Master generates its Id once; Local copies retain that identity. The application merges SystemConfig using the shared synchronization rules, preserving immutable base currency and precision settings. Lifecycle initialize on Master, edit mutable settings, sync. Shared synchronization contract applies; local transaction integrity and Master publication mechanisms remain TQ-02/TQ-04.

Configuration entity references are ID-only: BaseCurrencyId and nullable BalancingAccountId. SystemConfig exposes no Currency or Account navigation properties. Services resolve and validate the identities when needed; removing navigation properties does not remove the foreign-key relationships. Business normalizes an unavailable balancing account to null only in its read result; persisted correction belongs to Setup.

### PM-012 — LocalConfiguration

| Property | Canonical type | Nullability | Constraint / relationship | Source/status |
| --- | --- | --- | --- | --- |
| Id | uuid | Required | Single LocalConfig row identity generated on Local during initialization; retained locally through database replacement; never merged | User confirmation 2026-09-30 |
| LocalDatasetKey | string | Required | Local copy identity generated with Guid.NewGuid().ToString() when registering a new Local copy; stored in LocalConfiguration and registered on Master. Preserve through synchronization database replacement; a fresh installation gets a new key. Not the configuration primary key. | BR-001/005; name, type, placement and lifecycle confirmed 2026-09-29 |
| SnapshotRevision | int64 | Required; default 0 | Successfully installed snapshot Version; placement here proposed | Confirmed 2026-09-27 |
| AccountNameOrder | enum | Required | Stored component order; AccountNameOrder values are defined in the confirmed enum table below: Undefined = 0 and six component permutations = 1–6. Default CorrespondentCategoryProject = 1. | BR-012; enum and property renamed, Undefined and numeric values confirmed 2026-09-30 |
| AccountNameSeparator | string | Required | Stored separator; one non-whitespace character; UI empty/whitespace defaults to / on focus loss or Save; Unicode counting TQ-01 | BR-012; stored setting/name confirmed 2026-09-29; type proposed |
| AccountNameAddCurrency | bool | Required | Local flag, default false, append selected account currency identifier | LocalConfig property name and bool type confirmed 2026-10-08; required with existing default false; BR-005/012 |
| SyncTrigger | enum | Required | ManualOnly = 0, OnStart = 1, OnExit = 2; default ManualOnly | BR-034; names and numeric values confirmed 2026-09-30 |
| ConflictPriority | enum | Required | Undefined = 0, Master = 1, Local = 2; default Local | User correction 2026-09-30 supersedes BR-029 default; names and numeric values confirmed 2026-09-30 |

Confirmed account-name format (2026-09-29): derive the format from stored AccountNameOrder and AccountNameSeparator, with the Local Add currency behavior. Do not persist a separate format string. LocalConfig.AccountNameAddCurrency supplies the flag; all-empty checked slash format is //(UAH), unchecked is //.

Default ConflictPriority is Local (user correction, 2026-09-30), matching the existing code. This changes only the initial preference; existing conflict-resolution and validity rules remain. The user reaffirmed Local as the default on 2026-10-08; BRD 0.218 BR-029 is aligned.

Exactly one LocalConfig row per local copy. Local generates its Id once. Preserve the row and its identity through database replacement; never merge or synchronize it with Master configuration. No login/password assumed here. Lifecycle initialize/update locally. Local settings use the confirmed single-application editing scope; transaction integrity remains TQ-02 and auditing remains unresolved. Defaults/format constraints are confirmed business rules; physical columns are proposals.

#### Confirmed model enums — 2026-09-30

The requesting user confirmed the current Business.Models enums as the source for these names and numeric values. These are explicit stable assignments, not declaration-order assumptions. AccountNameOrder replaces DefaultAccountNameOrder as both the enum name and the Local configuration property name; The later 2026-10-08 naming decision uses AccountNameSeparator and AccountNameAddCurrency for the separator and currency flag; AccountNameOrder remains unchanged. The corresponding DTO-015 property uses AccountNameOrder too. Undefined is an unset sentinel, not a seventh component permutation or a third conflict source. This confirmation does not enable the reserved Planned transaction workflow or change existing defaults.

| Enum | Names and numeric values | Usage / default |
| --- | --- | --- |
| AccountNameOrder | Undefined = 0; CorrespondentCategoryProject = 1; CorrespondentProjectCategory = 2; CategoryCorrespondentProject = 3; CategoryProjectCorrespondent = 4; ProjectCorrespondentCategory = 5; ProjectCategoryCorrespondent = 6 | PM-012; default CorrespondentCategoryProject = 1 |
| ConflictPriority | Undefined = 0; Master = 1; Local = 2 | PM-012; default Local = 2 |
| SyncTrigger | ManualOnly = 0; OnStart = 1; OnExit = 2 | PM-012; default ManualOnly = 0 |
| ModificationType | None = 0; Content = 1; Order = 2 | Shared tracking; flags combine as Content plus Order = 3; None indicates no uncaptured edits; new creations are detected separately by EditRevision null |
| TransactionState | Undefined = 0; Draft = 1; Planned = 2; Confirmed = 3 | PM-006; current persisted states remain Draft or Confirmed; Undefined is unset and Planned reserved |

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
| SnapshotRevision | int64 | Required; default 0 | Per-registration installed snapshot Version; advance only after receipt | BR-040; confirmed 2026-09-27 |

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

Windows diagnostic logs: the application creates a Logs directory beside its executable, resolving it from the executable location rather than the process working directory. Write one UTF-8 text file per day named yyyy-MM-dd.log, with English technical messages and business text preserved in its original language. Use device-local calendar dates for daily filenames and local timestamps with their UTC offset inside entries. Retain today's log file and the previous six local calendar dates; remove older dated log files at application startup and once daily while the application remains open. Never log passwords, authentication tokens or authorization headers. Include operation names and error details, excluding those sensitive values.

The first Windows version runs unpackaged from a writable folder, as confirmed by the requesting user, 2026-10-07. Target Windows 11 only initially; Windows 10 support is not required. Use the same .NET major version as the existing backend. Use stable Windows App SDK and CommunityToolkit.Mvvm releases with explicit package versions, excluding preview packages. Exact compatible version pins and minimum Windows 11 build remain to be selected. This permits the application-created Logs directory beside the executable. Do not use MSIX deployment for this first-version delivery. Distribute a ZIP containing the application folder; the user extracts it into a writable location and runs the executable. Bundle the .NET and Windows App SDK runtimes with the application so these runtimes do not require separate installation. Use ordinary managed .NET compilation without Native AOT, and publish an x64 application ZIP for the first Windows release, including matching native/runtime dependencies. Native ARM64 and 32-bit packages are not part of this initial delivery. Store the Windows application local SQLite database in a Data directory beside the executable. The release ZIP contains application files and runtimes, but no user database, logs or personal settings. Updates are manual in version one: close the app and replace application files while preserving data, logs and settings. Do not provide a built-in updater. Existing UI-preference storage under the Windows user profile remains unchanged. Database compatibility checks and automatic all-or-nothing supported migrations follow the confirmed migration rule; remaining supported migration mappings and synchronization coordination stay under TQ-06. This is a deployment requirement, not an application/project modification in this documentation turn.

## DTO Schemas

Except for references to confirmed int64 snapshot-version/entity-revision semantics, DTO directions, operation assignments, fields, types and nullability below are **proposed**; business validation sources remain binding. DTOs are separate projections, not persistent records or permission to expose administrative/audit fields. No OP uses a PM as input/output. `None` means no payload, not no authorization.

Each property cell uses `name: canonical type`; the following column lists nullability in the same order. This compact schema is exhaustive for this draft; no undeclared fields are implied. `array<object>` items refer to an explicitly named DTO projection or nested object below. DTO fields never expose PasswordHash, cloud file locations or other owners' registration data.

| ID / name / direction | Properties and canonical types | Nullability (same order) | Validation / domain mapping / source | Consuming operations |
| --- | --- | --- | --- | --- |
| DTO-001 Read parameters / input | Method-specific uuid and date parameters | Required except explicitly nullable parameters | GetAllCurrencies and GetDefinitions take no parameters. GetRates takes currencyId; GetRate takes accountId and date. Transaction-list navigation reads take a selected inclusive end date plus the selected identity for By methods; C# signatures now return TransactionListInfo and accept DateOnly date. Post-edit refresh uses TransactionRefreshParam and TransactionRefreshInfo through ITransactionService.RefreshTransactions under the refresh rule below; a separate OP identifier remains to be assigned. Balance reads take date and optionally the required accountId for the single-account method. Date ranges are inclusive; reversed ranges fail validation. | OP-015,019,026,030,031,038,057–061 |
| DTO-002 Typed lists / output | List of the operation-specific Info record | Required; empty allowed | AvailableCurrencyInfo, CurrencyInfo, CurrencyRateInfo, TransactionInfo or ReportInfo as selected by the operation. Transaction-list reads OP-026/057–060 additionally require an exceeded-limit indication alongside their capped TransactionInfo collection; its exact wrapper/field declaration is unresolved. Other typed-list results are unchanged. | OP-014,015,019,026,038,057–060 |
| DTO-003 IdentityCommand / request | Id: uuid | Required | Target identity; no local expected-revision token under the confirmed single-application editing scope. Delete/apply behavior supplied by OP. | OP-008,013,018,023,028,029,034,035,036,040 |
| DTO-004 GroupParam / GroupInfo | GroupParam: ParentId: uuid; IsFavorite: bool; Name: string; Description: string<br>GroupInfo: Id: uuid; ParentId: uuid; ParentName: string; Name: string; Description: string; Order: int32; IsFavorite: bool; IsRoot: bool | GroupParam: Required; Required; Required; Nullable<br>GroupInfo: Required; Required; Required; Required; Nullable; Required; Required; Required | GroupParam is mutation input; identity is a separate Update argument. GroupInfo is read-only output. Root, uniqueness and hierarchy rules BR-006–009 apply; Order and favorites also have separate actions. | OP-006; tree reads use DTO-031 instead |
| DTO-005 ElementParam / ElementInfo | ElementParam: GroupId: uuid; IsFavorite: bool; Name: string; Description: string<br>ElementInfo: Id: uuid; GroupId: uuid; GroupName: string; Name: string; Description: string; Order: int32; IsFavorite: bool | ElementParam: Required; Required; Required; Nullable<br>ElementInfo: Required; Required; Required; Required; Nullable; Required; Required | ElementParam is mutation input; identity is separate on Update. ElementInfo serves Category, Project and Correspondent reads. Group/name/description rules BR-008–010 apply. | OP-010–OP-012; tree reads use DTO-032 instead |
| DTO-006 CurrencyParam / CurrencyInfo / AvailableCurrencyInfo | CurrencyParam: Code: string; Symbol: string; Name: string<br>CurrencyInfo: Id: uuid; Code: string; Name: string; Symbol: string; Order: int32; IsFavorite: bool<br>AvailableCurrencyInfo: Code: string; Name: string; Symbol: string | CurrencyParam: Required; Required; Required<br>CurrencyInfo: Required; Required; Required; Required; Required; Required<br>AvailableCurrencyInfo: Required; Required; Required | CurrencyParam is mutation input; Add also requires initialRate: decimal. CurrencyInfo is the saved-currency projection; AvailableCurrencyInfo is the regional catalog projection. Code is immutable on update. BR-013/014/017. | OP-014/015 via DTO-002, OP-016 |
| DTO-007 CurrencyRateParam / CurrencyRateInfo | CurrencyRateParam: CurrencyId: uuid; Date: date; Rate: decimal; Description: string<br>CurrencyRateInfo: CurrencyId: uuid; Date: date; Rate: decimal; Description: string; IsInitial: bool | CurrencyRateParam: Required; Required; Required; Nullable<br>CurrencyRateInfo: Required; Required; Required; Nullable; Required | Currency/date select the upsert target. Description is optional; IsInitial is output-only and derived from the fixed initial date. Positive rounded rate and base rate 1 rules apply. BR-014/017/021. | OP-017, OP-019 via DTO-002 |
| DTO-008 AccountParam / AccountInfo | AccountParam: CurrencyId: uuid; CategoryId: uuid; CorrespondentId: uuid; ProjectId: uuid; GroupId: uuid; IsFavorite: bool; Name: string; Description: string<br>AccountInfo: Id: uuid; GroupId: uuid; GroupName: string; Name: string; Description: string; Order: int32; IsFavorite: bool; CurrencyId: uuid; CurrencyName: string; CategoryId: uuid; CategoryName: string; CorrespondentId: uuid; CorrespondentName: string; ProjectId: uuid; ProjectName: string | AccountParam: Required; Nullable; Nullable; Nullable; Required; Required; Required; Nullable<br>AccountInfo: Required; Required; Required; Required; Nullable; Required; Required; Required; Required; Nullable; Nullable; Nullable; Nullable; Nullable; Nullable | AccountParam is mutation input; AccountInfo is editor output. Currency is immutable after first save. GetDefaultName retains classification inputs/string output and adds CurrencyId; Business resolves the code and reads AccountNameAddCurrency from LocalConfig. No bool method argument. CurrencyId may be null when AccountNameAddCurrency is false; a valid currency is required when true. BR-010–012. | OP-022,025; tree reads use DTO-032/033 instead |
| DTO-009 TransactionParam / TransactionInfo / TransactionEntryParam / TransactionEntryInfo / DuplicateTransactionInfo / ApplyTemplateInfo / ApplyTemplateEntryInfo | TransactionParam: DateTime: datetime; Description: string; Entries: array<TransactionEntryParam><br>TransactionInfo: Id: uuid; DateTime: datetime; State: TransactionState; Description: string; Entries: array<TransactionEntryInfo><br>TransactionEntryParam: AccountId: uuid; Amount: decimal; Rate: decimal<br>TransactionEntryInfo: AccountId: uuid; AccountName: string; CurrencyId: uuid; CurrencyName: string; Amount: decimal; CumulativeAmount: decimal; Rate: decimal<br>DuplicateTransactionInfo: DateTime: datetime; Description: string; Entries: array<TransactionEntryInfo><br>ApplyTemplateInfo: DateTime: datetime; Description: string; Entries: array<ApplyTemplateEntryInfo><br>ApplyTemplateEntryInfo: AccountId: uuid; Amount: decimal; Rate: decimal | TransactionParam: Required; Nullable; Required<br>TransactionInfo: Required; Required; Required; Nullable; Required<br>TransactionEntryParam: Required; Required; Required<br>TransactionEntryInfo: Required; Required; Required; Required; Required; Required; Required<br>DuplicateTransactionInfo: Required; Nullable; Required<br>ApplyTemplateInfo: Required; Nullable; Required<br>ApplyTemplateEntryInfo: Required; Required; Required | Input and output records are separate. Entry list order determines Position on save. No entry Id or Position input. BaseAmount is calculated, not an input field. State is output-only in TransactionInfo and omitted from TransactionParam. Business derives it on every Add/Update under PM-006: at least two valid entries and exact zero produce Confirmed; fewer entries or imbalance produce Draft. Every present entry remains valid; Confirmed-to-Draft is allowed. Duplicate/apply outputs prepare unsaved editor content. BR-015–021. | OP-026/057–060 via DTO-002, OP-027,029,034 |
| DTO-010 MergeCommand / request | SourceId: uuid; DestinationId: uuid | Required; Required | Same derived group type in PM-001; type-specific operation selection remains TQ-05. Source/destination explicit; root source/descendant destination invalid. Equal source and destination IDs return immediately without exception or changes under the confirmed no-op rule. BR-007/008. | OP-007 |
| DTO-011 TemplateParam / TemplateInfo / TemplateEntryParam / TemplateEntryInfo / FromTransactionInfo | TemplateParam: GroupId: uuid; IsFavorite: bool; Name: string; Description: string; Entries: array<TemplateEntryParam><br>TemplateInfo: Id: uuid; GroupId: uuid; GroupName: string; Name: string; Description: string; Order: int32; IsFavorite: bool; Entries: array<TemplateEntryInfo><br>TemplateEntryParam: AccountId: uuid; Amount: decimal<br>TemplateEntryInfo: AccountId: uuid; AccountName: string; CurrencyId: uuid; CurrencyName: string; Amount: decimal<br>FromTransactionInfo: Description: string; Entries: array<TemplateEntryInfo> | TemplateParam: Required; Required; Required; Nullable; Required<br>TemplateInfo: Required; Required; Required; Required; Nullable; Required; Required; Required<br>TemplateEntryParam: Required; Required<br>TemplateEntryInfo: Required; Required; Required; Required; Required<br>FromTransactionInfo: Nullable; Required | Input and output records are separate. Description is optional. Entries may be empty; list order determines Position on save. FromTransaction prepares unsaved template content. BR-022. | OP-033,036; tree reads use DTO-032 instead |
| DTO-012 ReportDefinition / shared | Category: object; Project: object; Correspondent: object; From: date; To: date; CurrencyFirst: bool; GroupBy: enum | Required; Required; Required; Nullable; Nullable; Required; Required | Each dimension shape S below. GroupBy Category,Project,Correspondent,Day,Week,Month,Year. PM-010 JSON definition; BR-023–027. | OP-037; nested DTO-013 in OP-038/039 |
| DTO-013 SaveReportDefinition / ReportInfo | SaveReportDefinition: Id: uuid; Name: string; GroupId: uuid; IsFavorite: bool; Definition: ReportDefinition<br>ReportInfo: Id: uuid; Name: string; GroupId: uuid; IsFavorite: bool; Order: int32; Definition: ReportDefinition | SaveReportDefinition: Nullable; Required; Required; Required; Required<br>ReportInfo: Required; Required; Required; Required; Required; Required | SaveReportDefinition uses null Id for creation. ReportInfo is output. Definition uses DTO-012. Reading preserves selection intent; full settings Save normalizes edited selection, while OP-070 changes dates only. Group/favorite/read-order fields follow the confirmed shared catalog conventions. BR-025/028. | OP-038 via DTO-002, OP-039 |
| DTO-014 CalculationResult / response | Rows: array<object>; Warnings: array<string> | Required; Required | For report calculation OP-037, Rows is one flat ordered list of row shape R, ready for display and CSV export; each row has an explicit Kind. Warnings is separate from Rows and never exported as CSV data rows. These report-result decisions were confirmed 2026-10-08. Signed amounts; no transaction drill-through. For OP-031/061, each account result must include both the sum of entry Amount in account currency and the sum of per-entry rounded BaseAmount in base currency; both amounts confirmed 2026-10-01, AccountBalanceInfo is the balance projection. Balances/report preview only, not stored business data; BR-019/026/027. | OP-031,037,061 |
| DTO-015 SaveLocalConfiguration / LocalConfigurationInfo / SaveSystemConfiguration / SystemConfigurationInfo | SaveLocalConfiguration: AccountNameOrder: AccountNameOrder; AccountNameSeparator: string; AccountNameAddCurrency: bool; ConflictPriority: ConflictPriority; SyncTrigger: SyncTrigger<br>LocalConfigurationInfo: AccountNameOrder: AccountNameOrder; AccountNameSeparator: string; AccountNameAddCurrency: bool; ConflictPriority: ConflictPriority; SyncTrigger: SyncTrigger<br>SaveSystemConfiguration: BalancingAccountId: uuid<br>SystemConfigurationInfo: BaseCurrencyId: uuid; AmountPrecision: int32; RatePrecision: int32; BalancingAccountId: uuid | SaveLocalConfiguration: Required; Required; Required; Required; Required<br>LocalConfigurationInfo: Required; Required; Required; Required; Required<br>SaveSystemConfiguration: Nullable<br>SystemConfigurationInfo: Required; Required; Required; Nullable | The Local shape includes required bool AccountNameAddCurrency, default false, and never synchronizes; the flag is read by account-name generation from saved LocalConfig. System read exposes immutable base currency and precisions; System save accepts only nullable BalancingAccountId. SaveConfiguration returns no payload. PM-011/012; BR-005/012/017/018. | OP-041,042,055,056 |
| DTO-016 SetupRequest / request | Login: string; Password: string; BaseCurrencyCode: string; LocalDatasetKey: string; RequestId: uuid | Required; Required; Nullable; Required; Required | BR-003 required credentials; base code and proposed precision fields are required only in the creation branch, not attachment; precision request extension remains TQ-16. Registration/request IDs proposed for retry; password transport/local retention TQ-03. | OP-003,004 |
| DTO-017 SetupResult / response | MasterDatasetKey: string; LocalDatasetKey: string; AuthorizationHandle: string; Transfer: object | Required; Required; Required; Required | Owner dataset/registration, proposed opaque authorization handle, Transfer DTO-021; do not expose hash/Admin.db. BR-003/005. | OP-003,004 |
| DTO-018 StatusResult / response | State: enum; MasterExists: bool; SyncKey: string; PublishedVersion: int64; InstalledVersion: int64; LastSuccessAt: datetime; Message: string | Required; Nullable; Nullable; Nullable; Nullable; Nullable; Nullable | State CreatingMode,OpenMode,MasterAbsent,MasterPresent,Waiting,Running,OutcomeUnknown,Published,Installed,Complete,Expired,Failed. Per-OP allowed subset TQ-04; ownership-scoped, no global directory listing. | OP-001,002,044,045,047,051,052 |
| DTO-019 SyncRequest / request | SyncKey: string; LocalDatasetKey: string; KnownVersion: int64; Priority: enum; Trigger: enum; Changes: object | Required; Required; Nullable; Required; Required; Required | Priority Master,Local; Trigger Manual,OnStart,OnExit,Recovery. Changes closed wire shape unresolved TQ-04 (not PM exposure); do not activate this input shape until specified. BR-029/034/038. | OP-043 |
| DTO-020 SyncIdentity / request | SyncKey: string; LocalDatasetKey: string | Required; Required | Owned registration/operation identity; auth separate from payload. BR-038–040. | OP-044,045,048 |
| DTO-021 TransferDescriptor / response | Version: int64; TransferToken: string; IntegrityProof: string | Required; Required; Nullable | Logical authorized snapshot transfer, never Admin.db. Bytes travel outside this descriptor; encoding/checksum/expiry TQ-04. BR-039/040. | OP-043,046,048,049; nested OP-003/004 |
| DTO-022 Receipt / request | SyncKey: string; LocalDatasetKey: string; InstalledVersion: int64 | Required; Required; Required | Confirm actual installed snapshot version; repeat safely after lost reply; BR-040. | OP-047 |
| DTO-023 TransferRequest / request | LocalDatasetKey: string; Version: int64; TransferToken: string | Required; Nullable; Nullable | Current owner copy; version/token required when downloading selected snapshot, absent in initial expiry replacement request. Precise handshake TQ-04. | OP-046,049 |
| DTO-024 SyncReport / response | SyncKey: string; Items: array<object>; Messages: array<string> | Nullable; Required; Required | Items shape C below; latest attempt/session only. BR-043. | OP-050 |
| DTO-025 DiagnosticEvent / request | OccurredAt: datetime; OperationId: string; Message: string; Detail: string | Required; Required; Required; Nullable | Local log; secret redaction/content policy TQ-08. No password/token logging intended. BR-043. | OP-052 |
| DTO-026 ValueResult / response | Name: string; Rate: decimal | Nullable; Nullable | Name only for account-name operations; Rate only for GetRate. No other field implied. BR-012/015. | OP-025,030 |
| DTO-027 ClassificationMerge / request | SourceId: uuid; DestinationId: uuid | Required; Required | Source element to replace and destination element to retain within Category, Correspondent or Project. Classification merge behavior confirmed 2026-10-01; DTO shape proposed. | OP-053 |
| DTO-028 AccountReplacement / request | SourceId: uuid; DestinationId: uuid | Required; Required | Source and destination Accounts must share a currency. Replacement behavior confirmed 2026-10-01; DTO shape proposed. | OP-054 |
| DTO-029 RateDeletion / request | CurrencyId: uuid; FromDate: date; ToDate: date | Required; Required; Required | Currency/date-range selector matching the existing rate Delete signature. Exclude the initial rate. Range behavior confirmed 2026-10-01; DTO packaging proposed, inclusive endpoints confirmed 2026-10-01 (FromDate <= Date <= ToDate); FromDate > ToDate throws a validation exception without mutations or a commit (confirmed 2026-10-01). | OP-020 |
| DTO-030 TreeInfo / response | Groups: array<DTO-031>; Elements: array<DTO-032> | Required; Required | Current code and confirmed lightweight tree design: stable, initially empty collections; flat groups and elements for one catalog family. No mixed catalog types or editor-specific detail payloads. | OP-009 |

| DTO-031 GroupInfo / response | Id: uuid; ParentId: uuid; ParentName: string; Name: string; Description: string; Order: int32; IsFavorite: bool; IsRoot: bool | Required; Required; Required; Required; Nullable; Required; Required; Required | Shared group projection in Services/Trees, as in current code. Root included once; ParentId and Order construct the hierarchy. Only Name, Description and IsFavorite are visible columns; ParentName is auxiliary current-code metadata, not an extra tree column. No sync revisions. | OP-005,009,062 |
| DTO-032 ElementInfo / response | Id: uuid; GroupId: uuid; GroupName: string; Name: string; Description: string; Order: int32; IsFavorite: bool | Required; Required; Required; Required; Nullable; Required; Required | Shared lightweight element projection in Services/Trees, as in current code. Id/GroupId support selection and placement; Order controls sibling position. GroupName is auxiliary current-code metadata, not an extra tree column. | OP-009; base of DTO-033 |
| DTO-033 AccountElementInfo / response | DTO-032 fields; CurrencyId: uuid; CurrencyName: string | Inherited; Required; Required | Account tree element extends ElementInfo. CurrencyName is the fourth visible column; CurrencyId is identity metadata. CategoryId, CorrespondentId and ProjectId are absent from this tree projection, so it cannot supply the full account editor. | OP-062 via DTO-034 |
| DTO-034 AccountTreeInfo / response | Groups: array<DTO-031>; Elements: array<DTO-033> | Required; Required | Flat account tree with shared groups and account elements, including CurrencyName. Root appears once; groups have no account currency value. Separate stable collections, matching current code. | OP-062 |
| DTO-035 ConfigurationInfo / output | BaseCurrencyId: uuid; BalancingAccountId: uuid; AmountPrecision: int32; RatePrecision: int32; AccountNameOrder: enum; AccountNameSeparator: string; AccountNameAddCurrency: bool; ConflictPriority: enum; SyncTrigger: enum | Required; Nullable; Required; Required; Required; Required; Required; Required; Required | Existing flat detached snapshot combining System and Local settings, reused for Settings reads. AccountNameAddCurrency is included in the implementation together with AccountNameOrder and AccountNameSeparator. All C# properties are init-only. No persistent models, navigation objects, configuration row IDs, dataset keys or sync revisions. Enum meanings follow PM-012. Reading does not save. | Internal IConfigOperation; OP-071; configuration used by OP-025 |

### Nested DTO objects — decision status per field

| Shape | Property | Canonical type | Nullability | Validation / mapping |
| --- | --- | --- | --- | --- |
| E transaction entry | AccountId | uuid | Nullable | Required on save; nullable only for unfinished editor |
| E | Amount | decimal | Nullable | Blank becomes zero on save |
| E | Rate | decimal | Required | Positive after rounding; base fixed 1 |
| E | BaseAmount | decimal | Nullable | Derived/read-only: Amount × Rate rounded per entry to AmountPrecision places, midpoint-to-even; not persisted or trusted from input |
| T template entry | AccountId | uuid | Required | PM-005 |
| T | Amount | decimal | Required | AmountPrecision fractional places |
| S selection dimension | IncludedGroupIds | array<uuid> | Required | Saved explicit group inclusions |
| S | ExcludedGroupIds | array<uuid> | Required | Saved explicit exclusions |
| S | IncludedElementIds | array<uuid> | Required | Explicit element inclusions |
| S | ExcludedElementIds | array<uuid> | Required | Explicit element exclusions |
| S | IncludeUnassigned | bool | Required | Explicit null-classification choice |
| R calculation row | Kind | enum | Required | Confirmed for OP-037: Data, CurrencyHeader, CurrencySubtotal, GrandTotal. Numeric enum assignments are not selected. |
| R | Label | string | Required | Display caption |
| R | CurrencyCode | string | Nullable | Own-currency column if applicable |
| R | Amount | decimal | Nullable | Null for CurrencyHeader and wherever an own-currency amount is inapplicable under BR-027; otherwise the signed amount. Render/export null as an empty cell. |
| R | BaseAmount | decimal | Nullable | Null for CurrencyHeader; required signed net rounded-entry total for Data, CurrencySubtotal and GrandTotal rows. Render/export header null as an empty cell, not zero. Confirmed 2026-10-08. |
| C sync report count | EntityType | enum | Required | Group,Classification,Currency,Rate,Account,Transaction,Template,Report,SystemConfiguration |
| C | Created | int32 | Required | Received local business changes; zero omitted in display |
| C | Updated | int32 | Required | Same |
| C | Deleted | int32 | Required | Same |

Confirmed report-result contract (2026-10-08): OP-037 returns one flat row list in final display/export order. Data rows represent the grouped signed totals, not individual transactions. CurrencyHeader identifies a currency section, CurrencySubtotal its total, and GrandTotal the final total where required by BR-027. Existing rules determine which kinds appear; this does not create new subtotal/grouping levels or change mixed-currency totals. Warnings, including empty-classification-filter warnings, are separate from the row list and are not CSV data. An empty result has no synthetic No matching data row; existing header-only CSV behavior remains. UI and CSV consume the same ordered rows. These presentation kinds apply to report results, not AccountBalanceInfo from OP-031/061. Key and Level are removed by user confirmation: result rows are temporary/read-only and Kind supplies the presentation role. CurrencyHeader rows have Amount = null and BaseAmount = null. UI and CSV show empty numeric cells, not zero; this was confirmed 2026-10-08. Actual numeric zero totals retain the existing zero-total display rule. Existing numeric formatting rules remain unchanged.

Local mutation DTOs have no expected-revision fields under the confirmed single-application editing scope; cross-device conflict detection continues to use the synchronization contract.

Mutation inputs and read outputs are separate records. Operations must reject/ignore client-owned audit/derived values per a final TQ-02 contract. Response field exposure must be approved before implementation. DTO-019 Changes is deliberately unresolved rather than an arbitrary `object` schema claimed complete.

### UI-driven DTO extensions and DTO-036 — ReportRunPeriod

The following property names, directions, field exposures, types/nullability and operation assignments are proposals until confirmed. BRD behavior is confirmed; no persistent model is an operation payload.

- DTO-013: include GroupId: uuid Required and IsFavorite: bool Required in save/read report shapes, and Order: int32 Required in reads, following the confirmed shared catalog conventions. Reordering uses the shared catalog action rather than changing Order through the editor. Existing Name/Definition remain; results are never persisted report data.
- DTO-015: include AccountNameAddCurrency: bool Required in Local read/save shapes, with existing initialization default false. The setting is persisted in LocalConfig and is not synchronized. Migration mechanics remain TQ-06.
- DTO-016: propose AmountPrecision: int32 Nullable and RatePrecision: int32 Nullable, conditionally required for creation with ranges/defaults from BR-017. Attachment cannot alter existing Master values. Auth/registration/retry payloads remain proposed.
- DTO-035 retains the existing flat ConfigurationInfo POCO/record with mixed Local/System properties. Extend its Local settings with AccountNameAddCurrency: bool Required. OP-025 reads the saved flag through existing internal configuration access; the UI does not pass a bool to GetDefaultName. Reuse this flat read shape for OP-071; no nested Local/System result wrappers.
- Confirmed 2026-10-08: Settings uses one Setup read (OP-071) returning Local and System settings and one Setup save (OP-072) persisting both sections in one database transaction. Both changes commit or neither does; the UI must not compose separately committing OP-042/056 calls. OP-071 reuses flat DTO-035 ConfigurationInfo. OP-072 accepts editable settings only; base currency and amount/rate precisions are excluded.
- Corrected 2026-10-08: IAccountService.GetDefaultName adds CurrencyId alongside the three classification IDs and reads the saved LocalConfig.AccountNameAddCurrency flag through existing configuration access. No AddCurrency bool parameter. Business resolves the currency code and returns the generated string. CurrencyId is nullable but must identify an existing non-deleted currency when AccountNameAddCurrency is true; the suffix uses UAH/USD/EUR as in the confirmed examples.

DTO-036: proposed Request for OP-070. Direction/operation assignment and every field below remain proposed.

| Property | Canonical type | Nullability | Validation / mapping | Source / decision status |
| --- | --- | --- | --- | --- |
| ReportId | uuid | Required | Existing Report identity, not create | Shared identity convention; DTO shape/type/nullability/exposure proposed |
| From | date | Nullable | Inclusive lower boundary; change only saved From | BR-026 optional concept confirmed; DTO type/nullability/exposure proposed |
| To | date | Nullable | Inclusive upper boundary; reject From > To when both set | BR-026 optional concept confirmed; DTO type/nullability/exposure proposed |

Authorization: ROLE-001/current local dataset under BR-001/002/039. Service validates independently of disabled UI Run. Side effects are saved dates only with ordinary report content tracking. One atomic date mutation and same-period no-op are proposals under existing local mutation rules. Calculation/export are separate non-mutating work. Missing-report/error/output details remain TQ-07/16.

### Combined Settings operations — confirmed 2026-10-08

Source: requesting user agreed to all three proposed Settings contracts. Settings reuses the existing flat configuration read object, saves editable Local/System values in one Setup call and one database transaction, and passes CurrencyId to IAccountService.GetDefaultName. The function reads AccountNameAddCurrency from LocalConfig. The correction supersedes the earlier explicit bool argument and proposed nested read result; BRD 0.218 records the corrected naming-setting source.

| OP | Logical owner/action | Input and output | Authorization and effects |
| --- | --- | --- | --- |
| OP-071 | Setup: Read Settings | No selection identity; returns the existing flat ConfigurationInfo shape (DTO-035), mixing Local/System properties. Extend it with the Local flag; no nested section wrappers. | ROLE-001, own local dataset; local read, no login or writes. Existing Settings access during recovery question remains TQ-05. |
| OP-072 | Setup: Save Settings | Receives editable Local/System settings in one call: account-name order, separator, AccountNameAddCurrency, conflict priority, sync trigger and nullable balancing account. BaseCurrencyId, AmountPrecision and RatePrecision are read-only and excluded from the save input. Exact request record name/return shape remains proposed. | ROLE-001; existing local editing gates and L/W validation/no-automatic-retry policy. Validate both sections; save Local and System changes and applicable System synchronization flags in one database transaction. Commit all or roll back all; Local settings remain outside synchronization. Failed save keeps the editor and entered values and leaves both saved sections unchanged. |

One data-service scope covers the complete logical OP-072 call and its transaction; separate scopes/commits per section cannot implement this boundary. Existing immutable base currency/precision and balancing-account rules remain binding. OP-041/042/055/056 retain their existing section-specific meanings and stable IDs; this decision neither retires them nor permits the Settings screen to implement Save as two separately committed calls. No public Business configuration service is restored.

OP-025 still only generates a name and never saves the account. It receives the three classification IDs and CurrencyId, resolves the currency code in Business, and reads saved Local naming order/separator/AccountNameAddCurrency. Existing internal IConfigOperation.GetConfiguration and flat ConfigurationInfo were observed during this documentation update; the new flag is a required extension, not a claim of implemented code. OP-071 is the Settings-facing contract and does not restore retired public Business OP-065. CurrencyId may be null with AccountNameAddCurrency false. With it true, a missing, unknown or deleted currency causes a validation failure; no name is returned and no data is changed. Disable Restore default name until a valid currency is selected. The ordinary account-save currency requirement is unchanged.

## API/BFF Service Contracts

### Shared operation contract profiles

Every OP below names DTO input/output and source behavior. The operation list and all DTO selections are proposed. Transport metadata for every OP is **unresolved** (local invocation vs transport adapter, route, status mapping); local operations must remain offline-capable. No persistent model is an API argument/result.

- **L/R — local read:** ROLE-001; no login; only active usable local business state, except setup/status/configuration/report/log administration exceptions explicitly listed. No business mutation; consistent read snapshot proposed. Repeatable read with current-data result, not identical cached response. BR-002/005/039. Read consistency and transaction boundaries remain TQ-02; no local expected-revision token is required.
- **L/W — local mutation:** ROLE-001; usable local state, no editing during sync/wait. UI actions are separate API operations: drag-and-drop reorder, add, edit and setFavorite each save their own result to local persistence. Validate before commit. Confirmed 2026-09-30: an entity change and its applicable ModificationType flags are saved together by that operation; a failure must not persist the content without its sync flag. For example, setFavorite sets IsFavorite and adds Content while preserving any Order bit, then saves the entity. This does not combine separate UI actions into one save. Confirmed 2026-10-01: a reorder operation commits all affected rows and their applicable flags together, with rollback of the entire reorder on failure. Supported merges also commit all affected rows and flags once under the confirmed merge transaction rule. Confirmed 2026-10-01: each successful state-changing local API function persists its complete result through AcceptChanges; the current application uses one call and one local transaction per action. All affected rows, dependent changes and applicable sync flags commit together or roll back together. For transaction/template edits this includes parent changes, removal of old entries, insertion of the complete replacement set, assigned positions and parent Content flag. Successful state-changing actions cannot omit persistence. Previously confirmed early no-op returns, validation failures, unsupported operations, reads and previews do not require a commit. This local action boundary does not collapse the separate durable stages of synchronization or setup into one transaction. Existing data unchanged on numeric failure (BR-017). Confirmed local-write retry policy (2026-10-01): do not automatically retry Add/Update/Delete after failure. Surface the error and let the user explicitly retry. Do not assume repeated Add is idempotent or introduce automatic replay of failed local writes. Synchronization retains its separate durable SyncKey retry/recovery rules. Effects supplied in row.
- **P — preview:** same local authorization as L/R, no persistence; recalculated from current inputs; repeated call may reflect changed rates/catalog. Transaction save remains separate.
- **S — setup:** ROLE-001 as prospective/existing owner; no local login gate. Create permitted only absent master; Open requires credential authentication; registration before download. Proposed request identity prevents duplicate partial setup; serialization/transaction boundary/idempotency TQ-03/TQ-04. Initial retry manual only.
- **C — cloud sync:** ROLE-001 authenticated owner and owned LocalDatasetKey/SyncKey/version; remembered authorization. Durable operation outcome; before-publication and after-publication boundaries are BR-038. Same SyncKey must not reapply published batch; receipts repeat safely. Exact durable protocol TQ-04. No implicit rollback after cloud publication.
- **D — local diagnostics/status:** ROLE-001 or application acting for that owner; no cloud auth. Does not grant blocked business access. Session report read/log writes/cleanup effects specified below; timing and filesystem transaction policy TQ-08.

Confirmed merge no-op rule (2026-10-01): supported group, classification and account merge operations (OP-007, OP-053, OP-054) compare source and destination IDs first. If they are equal, return immediately without exception, loading or updating entities, deletion, commit, or modification-flag changes. Do not run the distinct-source merge workflow. This is normal completion, not a validation failure. The unsupported Template CombineElements stub remains unsupported; this rule does not enable template merging.

Confirmed merge transaction boundary (2026-10-01): each actual group, classification or account merge (OP-007/053/054) prepares all required changes and commits once to local SQLite. The transaction includes reference replacements or group-member moves, required collision renaming and order normalization, affected aggregate/content/order flags, source deletion under its normal lifecycle, and any required System configuration update. Either the entire merge commits or all its persistent changes roll back. The user described this as one unit-of-work AcceptChanges call. Equal-ID no-op calls make no commit. Separate API actions retain separate save boundaries.

For each operation the profile is its explicit authorization, transaction and idempotency policy reference. Where marked unresolved, approval is blocked until specified. No capability acquires an automatic new cloud endpoint just because listed here.

| SVC | Logical service | Purpose / source |
| --- | --- | --- |
| SVC-001 | StartupService | BC-001/002; BR-001–005 |
| SVC-002 | GroupService | BC-003; BR-006–009 |
| SVC-003 | ClassificationService | BC-003; BR-010/011 |
| SVC-004 | CurrencyService | BC-004; BR-013–017/021 |
| SVC-005 | AccountService (interface IAccountService) | BC-004; BR-010–012 |
| SVC-006 | TransactionService | BC-005; BR-015–021 |
| SVC-007 | TemplateService | BC-006; BR-022 |
| SVC-008 | ReportService | BC-007/008; BR-023–028 |
| SVC-009 | LocalConfigService (user-selected interface name ILocalConfigService) | Local settings; BC-009; BR-005/012/034 |
| SVC-010 | SynchronizationService | BC-009–011; BR-029–042 |
| SVC-011 | DiagnosticsService | BC-012; BR-043 |
| SVC-012 | SystemConfigService (user-selected interface name ISystemConfigService) | System settings and related helpers; BC-009; BR-005/017/018 |
| SVC-013 | Retired | Business has no public configuration service. Internal access uses IConfigOperation; external access belongs to Setup. |

| OP | Service.operation | Profile | Input → output (proposed) | Validation/domain errors and observable effects / BRD source |
| --- | --- | --- | --- | --- |
| OP-001 | SVC-001.GetStartupState | D | None → DTO-018 | BR-002/039/041: mode from existence and pending/expiry state; no business data mutation. |
| OP-002 | SVC-001.CheckMasterExists | S read | Current proposed None → DTO-018; credential-scoped payload/exposure TQ-03/16 | Invoked after Continue for BR-003 routing: existence selects attachment, confirmed absence selects creation fields. Failure means unknown and prevents creation. Bootstrap/check-create races and identity enforcement remain TQ-03/04. |
| OP-003 | SVC-001.CreateBooks | S | DTO-016 → DTO-017 (proposed) | Absent-Master branch: require credentials, explicit currency and precisions; initialize six roots/config/balancing account, register/download. Existing/uncertain Master prevents duplicate creation. Partial cloud success is preserved; retry attaches to created Master. Sixth root TQ-16; bootstrap/idempotency TQ-03/04. |
| OP-004 | SVC-001.OpenBooks | S | DTO-016 → DTO-017 (proposed) | Existing-Master attachment starts automatically after Continue; authenticate before registration/download. Reinstallation creates a new registration. Failure/cancellation returns Setup preserving input; retry manual. No separate user Open choice. |
| OP-005 | SVC-002.GetAllGroups | L/R | None → array<DTO-031> | Confirmed 2026-10-01: parameterless GetAllGroups in generic IGroupService, inherited by all five group services. Return a flat list of groups including the root once, with ParentId and Order for hierarchy and sibling ordering. Used for choosing parent/destination groups. Does not save. BR-006. |
| OP-006 | SVC-002.SaveGroup | L/W | Add(GroupParam) → uuid; Update(groupId, GroupParam) → None; MoveToAnotherParent(groupId, toParentId) → None | BR-006–009: create/edit/move non-root; reject cycle/type/name violation; no root edit. |
| OP-007 | SVC-002.MergeGroups | L/W | CombineGroups(toGroupId, fromGroupId) → None | BR-007/008: equal IDs return immediately without changes; otherwise move children/elements, suffix incoming conflicts, delete source; all merge changes and sync flags commit in one transaction or all roll back. |
| OP-008 | SVC-002.DeleteGroup | L/W | DTO-003 → None | BR-007: only empty non-root, deletion tracking for sync. |
| OP-009 | SVC-002.GetTree | L/R | None → DTO-030 | Confirmed 2026-10-01: parameterless GetTree belongs to generic IGroupService for all five catalogs. Return groups (root once) and lightweight elements in separate flat collections, connected by ParentId/GroupId and ordered using Order. Common tree columns are Name, Description and IsFavorite; Order affects position, never a numeric column. Used for browsing and selecting elements while editing another entity. Selecting a correspondent returns its identity/name to the account editor and closes the chooser; selection alone does not save the account. Account trees needing the CurrencyName column use OP-062. No account classification details or template entries are returned by these tree reads. Full edit-data retrieval is declared separately through OP-063; tree payloads remain lightweight. |
| OP-010 | SVC-003.CreateElement | L/W | Add(ElementParam) → uuid | BR-008–010: correct group, nonblank unique Name; Description optional. |
| OP-011 | SVC-003.UpdateElement | L/W | Update(entityId, ElementParam) → None | BR-009/010/012: edit metadata, preserve stored account names. |
| OP-012 | SVC-003.MoveElement | L/W | MoveToAnotherGroup(entityId, toGroupId) → None | BR-006–008: same-type destination and individual uniqueness. |
| OP-013 | SVC-003.DeleteElement | L/W | DTO-003 → None | BR-011: reject while account uses it; saved report refs do not block. |
| OP-014 | SVC-004.GetAvailableCurrencies | P | None → array<AvailableCurrencyInfo> | Operation/name confirmed 2026-10-01: return the system currency catalog for adding currencies, one item per ISO Code with Name and Symbol. BR-013: regional enumeration, skip neutral/invalid regions, retain the first Name/Symbol per ISO code. Catalog access works offline and saves nothing. Exact DTO packaging remains proposed. |
| OP-015 | SVC-004.GetAllCurrencies | L/R | None → array<CurrencyInfo> | Confirmed operation/name 2026-10-01: return all currencies already added to the current database, including the base currency, sorted by persisted Order ascending. BR-013: local currency projections; distinct from the available system currency catalog in OP-014. Read only; no commit. Exact DTO packaging remains proposed. |
| OP-016 | SVC-004.SaveCurrency | L/W | Add(CurrencyParam, initialRate) → uuid; Update(currencyId, CurrencyParam) → None | BR-013/014: create with fallback rate or edit metadata; code immutable, reject duplicate. Currency, initial rate and applicable sync flags commit together under the confirmed local action transaction rule. |
| OP-017 | SVC-004.SaveRate | L/W | AddOrUpdate(CurrencyRateParam) → uuid | BR-014/017/021: upsert by CurrencyId and Date without a caller-supplied row Id; update the matching rate or create one if absent. Positive rounded rate, base fixed 1, unique pair, initial date immutable. |
| OP-018 | SVC-004.DeleteCurrency | L/W | DTO-003 → None | BR-013: unused and non-base only; dependent rates lifecycle TQ-02. |
| OP-019 | SVC-004.GetRates | L/R | currencyId: uuid → array<CurrencyRateInfo> | Confirmed operation/scope 2026-10-01: return all rates for one currency, including its initial rate, sorted by Date descending. Currency selection is required; no date-range filter applies to this operation. BR-014: show the initial rate value but keep its sentinel date hidden in the UI. Read only; no commit. The selector is the currencyId method parameter. |
| OP-020 | SVC-004.DeleteRates | L/W | DTO-029 → None | Confirmed 2026-10-01: separate delete action removes all ordinary rates matching the currency/date range; always skip the initial rate, even when its date is included. Apply shared deletion tracking and one transaction per call; existing entry rates unchanged. Both endpoints are inclusive (FromDate <= Date <= ToDate); FromDate > ToDate throws a validation exception before changes; no commit occurs. |
| OP-022 | SVC-005.SaveAccount | L/W | Add(AccountParam) → uuid; Update(entityId, AccountParam) → None | BR-008–012: required group/currency/name; Description optional; currency immutable after first successful account save; duplicate names allowed. |
| OP-023 | SVC-005.DeleteAccount | L/W | DTO-003 → None | BR-011: reject any transaction/template reference. |
| OP-025 | SVC-005.GetDefaultName | P | Three nullable classification IDs plus CurrencyId → string; read AccountNameAddCurrency from LocalConfig; CurrencyId nullable; required/valid when saved AccountNameAddCurrency is true | BR-012 adds optional account-currency suffix, including base currency; missing slots keep separators. Blank new Name on Save and explicit Restore use this operation; no persistence/AcceptChanges. User corrected the flag source on 2026-10-08: CurrencyId is an input; AccountNameAddCurrency is read from LocalConfig, with no bool parameter. Exact currency is optional with the flag off and required/valid with it on. |
| OP-026 | SVC-006.GetTransactions | L/R | date: date → capped array<TransactionInfo> plus exceeded-limit indication (wrapper unresolved) | Updated 2026-10-02: return the latest matching transactions through the selected date, with no additional identity filter; declaration alignment pending. Shared transaction-read rules below apply. BR-016–021. |
| OP-027 | SVC-006.SaveTransaction | L/W | Add(TransactionParam) → uuid; Update(entityId, TransactionParam) → None | BR-015–021: aggregate validation, rounding, Confirmed/Draft policy from PM-006, account currency immutability. TransactionParam contains no State; Business derives Draft or Confirmed from the validated entry count and rounded base total on every save; parent changes, complete entry replacement and parent sync flags commit together; no partial aggregate save. After successful commit, the client refreshes the displayed identities without refilling under the list-refresh rule below; this does not change the save response shape. |
| OP-028 | SVC-006.DeleteTransaction | L/W | DTO-003 → None | BR-019/032: remove calculation contribution, retain sync deletion evidence until eligible. Recalculate affected subsequent account cumulative amounts under PM-007 within the same database transaction; deletion and recalculation roll back together on failure. |
| OP-029 | SVC-006.DuplicateTransaction | P | transactionId: uuid → DuplicateTransactionInfo | Confirmed 2026-10-01: ITransactionService.DuplicateTransaction(id) copies the source accounts, amounts, stored rates and description, preserving entry order, and sets DateTime to now. Return a new unsaved transaction for editing; do not modify the source, persist a duplicate or call AcceptChanges. Persist only when the user chooses Save through the normal transaction-add operation, with normal validation and fresh persistent identities. BR-020. |
| OP-030 | SVC-004.GetRate (ICurrencyRateService) | P | accountId: uuid; date: date → decimal | Current code ownership aligned 2026-10-01: required account identity and device-local calendar date; latest currency rate on/before date, base currency returns 1. No stored entry mutation. BR-015. |
| OP-031 | SVC-006.GetBalancesForAllAccounts | L/R | date: date → array<AccountBalanceInfo> | Balance-read capability confirmed 2026-10-01 is retained; BRD 0.177 supersedes Accounts-screen placement. Accounts UI manages accounts only; Ledger account filtering displays the confirmed cumulative amount. Calculate each account balance on demand from Confirmed transactions through the end of the selected date; exclude Draft transactions. This is a cumulative balance, not movement within a From/To period. Read only; no AcceptChanges. Account-currency balances may use the PM-007 CumulativeAmount cache; base-currency balances remain calculated on demand. Owner confirmed as ITransactionService; date is required. Return balances for all accounts. Each account result includes both its account-currency balance (sum of entry Amount) and base-currency balance (sum of per-entry rounded BaseAmount using stored entry rates). This follows the established BaseAmount calculation; do not revalue the total using a current catalog rate. Both amounts are confirmed; exact DTO packaging remains proposed under TQ-05. BR-019. |
| OP-033 | SVC-007.SaveTemplate | L/W | Add(TemplateParam) → uuid; Update(entityId, TemplateParam) → None | BR-022: unique name/accounts, empty allowed; save visible optional Description independently of Name; preserve existing account currency immutability. Parent changes, complete entry replacement and parent sync flags commit together under the local action transaction rule. |
| OP-034 | SVC-007.ApplyTemplate | P | templateId: uuid → ApplyTemplateInfo | Confirmed 2026-10-01: ITemplateService.ApplyTemplate(templateId) returns an unsaved transaction with the template accounts, amounts and description, preserving entry order. Set DateTime to now and obtain applicable rates for that date using the established currency-rate lookup rules. User edits, then saves through the normal transaction-add operation or cancels. Applying the template does not persist a transaction, modify the template or call AcceptChanges; Cancel makes no changes. Normal entry/date/numeric validation applies on Save. Zero/single-entry or unbalanced results save as Draft; at least two valid entries and exact balance are required only for Confirmed. BR-022. |
| OP-035 | SVC-007.DeleteTemplate | L/W | DTO-003 → None | Confirmed shared deletion lifecycle: validate the target, delete the template aggregate under normal deletion/tracking rules, and commit atomically. Already-created transactions remain independent. Missing/deleted target throws under the shared rule; account currency immutability is preserved. |
| OP-036 | SVC-007.FromTransaction | P | transactionId: uuid → FromTransactionInfo | Confirmed 2026-10-01: ITemplateService.FromTransaction(transactionId) returns an unsaved template copied from a Confirmed or Draft transaction: accounts, amounts and description, preserving entry order. User chooses the template name and group, then saves through the normal template-add operation (OP-033) or cancels. Preparation does not persist a template, modify the source transaction or call AcceptChanges; Cancel makes no changes. Normal template validation applies on Save. BR-022. |
| OP-037 | SVC-008.Calculate | L/R | DTO-012 → DTO-014 | BR-023–027: current tree/classifications, read-only selection evaluation, empty-warning and currency grouping. Return the flat ordered rows with explicit kinds and separate warnings under DTO-014; display and CSV preserve that row order. |
| OP-038 | SVC-008.GetDefinitions | L/R | None → array<ReportInfo> | BR-025/028: preserve JSON intent exactly on read. |
| OP-039 | SVC-008.SaveDefinition | L/W | SaveReportDefinition → ReportInfo | BR-025/028: user save, name validation, recompute minimal override IDs for selection save; rename-only handling TQ-05. |
| OP-040 | SVC-008.DeleteDefinition | L/W | DTO-003 → None | BR-028: only definition deleted; bookkeeping unchanged. |
| OP-070 | SVC-008.SaveRunPeriod | L/W | DTO-036 → None (input/output proposed) | BR-026/028 requires a date-only existing-report mutation before Run: change From/To only, preserving Name/group/favorite/grouping and all saved classification IDs/overrides. ROLE-001/current local dataset and ordinary recovery/editing gates. Atomic update is proposed under the shared local-write rule; method/shape/errors/idempotency remain TQ-07/16. |
| OP-041 | SVC-009.GetConfiguration | D read | None → LocalConfigurationInfo | Confirmed 2026-10-01: ILocalConfigService.GetConfiguration() returns the single Local configuration for the settings screen; no Id or other input parameter. Read only; no AcceptChanges. Local settings only; no business data access bypass. Local DTO fields are recorded in the current C# contract mapping. |
| OP-042 | SVC-009.SaveConfiguration | L/W | SaveLocalConfiguration → None | Confirmed 2026-10-01: ILocalConfigService.SaveConfiguration(settings) updates the single Local configuration without an Id parameter. Validate settings before saving and commit the complete successful state-changing action with one AcceptChanges call, under the shared mutation/no-op rules. Preserve the existing LocalConfig identity. Local settings do not synchronize. BR-005/012/034; admission during recovery remains TQ-05; DTO fields are recorded in the current C# contract mapping. |
| OP-043 | SVC-010.Synchronize | C | DTO-019 → DTO-021 | BR-029–038: queue, change detection, priority/validity, publication; no-change result may need status rather than transfer, unresolved TQ-04. |
| OP-044 | SVC-010.GetOutcome | C read | DTO-020 → DTO-018 | BR-038–041: durable known/unknown outcome and expiry; never infer from equal timestamps. |
| OP-045 | SVC-010.Cancel | C | DTO-020 → DTO-018 | BR-036/038: stop current work where possible; no rollback after publication; post-publication pending recovery. |
| OP-046 | SVC-010.Download | C | DTO-023 → DTO-021 + snapshot stream | BR-039/040: consistent selected version; byte transport unresolved, do not expose Admin.db; restart incomplete transfer. A downloaded merged database becomes usable only after the local cumulative rebuild defined in PM-007 completes. |
| OP-047 | SVC-010.Acknowledge | C | DTO-022 → DTO-018 | BR-040/041: idempotent receipt of actual installed version, master-clock success/expiry, deletion eligibility. |
| OP-048 | SVC-010.Recover | C | DTO-020 → DTO-021 | BR-038–040: resolve prior outcome then latest snapshot; editing/viewing/report block remains until completion. |
| OP-049 | SVC-010.ReplaceExpired | C | DTO-023 → DTO-021 | BR-041/042: only user Download; expired copy disposal, new registration handling and preserved Local settings; handshake TQ-04. |
| OP-050 | SVC-011.GetLatestSyncReport | D read | None → DTO-024 | BR-043: latest in-session attempt only; absent-report representation TQ-08. |
| OP-051 | SVC-011.CleanupLogs | D | None → DTO-018 | BR-043: delete logs beyond 7-day retention; local operation, no network restriction implied. |
| OP-052 | SVC-011.WriteDiagnostic | D | DTO-025 → DTO-018 | BR-043: detailed local log in folder; no business mutation; sanitized fields TQ-08. |
| OP-053 | SVC-003.CombineElements | L/W | DTO-027 → None | Confirmed 2026-10-01: equal source/destination IDs return immediately without exception or changes; otherwise replace source references in all Accounts with the same-type destination Category, Correspondent or Project, then delete the source under the shared tracking/deletion rules. Preserve the other classifications and Account names. All replacements, source deletion and sync flags commit in one transaction or all roll back; other invalid-input handling remains TQ-10; Account replacement is defined separately by OP-054; Template merging is unsupported as confirmed under PM-008. |
| OP-054 | SVC-005.CombineElements | L/W | DTO-028 → None | Confirmed 2026-10-01: equal source/destination IDs return immediately without exception or changes; otherwise require equal account currencies, then replace source references in all TransactionEntries and TemplateEntries. Preserve amounts, rates and positions; mark affected parent aggregates Content. Delete the source Account after replacement using the shared deletion/tracking rules; retain the destination. Preserve the saved balancing-account selection; expose null through Business configuration reads if its account is now deleted. Setup owns persisted correction. Recalculate the destination account's affected cumulative amounts under PM-007; cache-only changes do not set synchronization flags. All replacements, affected parent flags, destination cumulative-amount recalculation, source deletion commit in one transaction or all roll back. Other invalid-input handling remains open. |


Confirmed transaction reads (2026-10-01): OP-026 and OP-057–OP-060 belong to ITransactionService. Each navigation read requires an inclusive end date and filters in the database before loading; no lower date boundary is selected by this navigation model. Each By operation also requires the selected element identity. For OP-057–060, validate that the selected Account, Category, Correspondent or Project exists and is not deleted; otherwise raise a validation exception for an invalid filter. An existing valid identity with no matching transactions returns an empty list. Validation and reading make no changes or commit. Classification filters match through the transaction entry's Account and its current Category, Correspondent or Project. Return each matching transaction once with all its entries in Position order, including entries that do not match the selected account/classification. The fixed 300-transaction cap and newest-first ordering apply. The selected date is a calendar date in the current device timezone: include UTC Transaction.DateTime values before the local start of the following day, converted with its applicable UTC offset. This includes the whole selected day without assuming every day is 24 hours. These navigation reads no longer take From/To; existing bulk-deletion date ranges retain their own rules. Exact DTO packaging and displayed-ID refresh contracts remain TQ-05.

| Operation | Owner/function | Mode | Input → output | Behavior |
|---|---|---|---|---|
| OP-057 | SVC-006.GetTransactionsByAccount | L/R | accountId: uuid; date: date → capped array<TransactionInfo> plus exceeded-limit indication (wrapper unresolved) | Required account Id and inclusive end date; match transactions with an entry using that account. Shared transaction-read rules apply. BR-016–021. |
| OP-058 | SVC-006.GetTransactionsByCategory | L/R | categoryId: uuid; date: date → capped array<TransactionInfo> plus exceeded-limit indication (wrapper unresolved) | Required category Id and inclusive end date; match transactions with an entry whose account has that category. Shared transaction-read rules apply. BR-016–021. |
| OP-059 | SVC-006.GetTransactionsByCorrespondent | L/R | correspondentId: uuid; date: date → capped array<TransactionInfo> plus exceeded-limit indication (wrapper unresolved) | Required correspondent Id and inclusive end date; match transactions with an entry whose account has that correspondent. Shared transaction-read rules apply. BR-016–021. |
| OP-060 | SVC-006.GetTransactionsByProject | L/R | projectId: uuid; date: date → capped array<TransactionInfo> plus exceeded-limit indication (wrapper unresolved) | Required project Id and inclusive end date; match transactions with an entry whose account has that project. Shared transaction-read rules apply. BR-016–021. |
| OP-061 | SVC-006.GetBalanceForAccount | L/R | accountId: uuid; date: date → AccountBalanceInfo | Confirmed 2026-10-01: ITransactionService.GetBalanceForAccount(accountId, date). Both inputs required. Calculate the selected account cumulative balance from Confirmed transactions through the end of the selected date, using the same calculation as OP-031. Read only; no AcceptChanges. Account-currency balances may use the PM-007 CumulativeAmount cache; base-currency balances remain calculated on demand. Return both account-currency and base-currency balances, calculated as in OP-031. Both amounts are confirmed. An unknown accountId throws an exception (confirmed 2026-10-01); do not return a zero balance for a nonexistent account. Exact DTO packaging remains proposed under TQ-05; exception type/transport mapping remains TQ-10. BR-019. |
| OP-062 | SVC-002.GetAccountsTree (IAccountGroupService) | L/R | None → DTO-034 | Confirmed 2026-10-01: account-specific tree read adds CurrencyName to the common Name, Description and IsFavorite columns. No other data columns; Order controls placement indirectly. Inherited GetTree remains available with the common lightweight projection. This read does not save changes. |
| OP-063 | Entity service GetById via IReadEntityService<TInfo> | L/R | id: uuid → service-specific Info record | Current code contract 2026-10-01: read a detached record for an existing-entity edit dialog. Applies to all five group services, Category/Correspondent/Project, Account, Template, Transaction and Currency. Exact service/result mapping is below. Does not save; mutation remains separate. Implementations are incomplete. Confirmed: an unknown or deleted id throws a not-found exception; never return null, an empty record or a deleted entity. Applies to every service using this GetById contract. Exact exception class/error mapping remains TQ-07. |
| OP-064 | SVC-006.DeleteTransactions (ITransactionService) | L/W | fromDate: date; toDate: date → None (Task) | Confirmed range-only bulk deletion; no identity filter. Uses the shared filtered-deletion rules below. The C# declaration now uses DeleteTransactions(fromDate, toDate); the ID-list deletion declaration is removed. BR-019/032; FR-005. |
| OP-065 | Retired | — | — | Public Business configuration reading removed. IConfigOperation remains internal; external configuration services belong to Setup. ID reserved and not reused. |
| OP-066 | SVC-006.DeleteTransactionsByAccount | L/W | accountId: uuid; fromDate: date; toDate: date → None (Task) | Same matching rule as OP-057: at least one entry belongs to the selected account. Deletes complete matching transactions. Shared filtered-deletion rules apply. |
| OP-067 | SVC-006.DeleteTransactionsByCategory | L/W | categoryId: uuid; fromDate: date; toDate: date → None (Task) | Same matching rule as OP-058: at least one entry account currently has the selected category. Deletes complete matching transactions. Shared filtered-deletion rules apply. |
| OP-068 | SVC-006.DeleteTransactionsByCorrespondent | L/W | correspondentId: uuid; fromDate: date; toDate: date → None (Task) | Same matching rule as OP-059: at least one entry account currently has the selected correspondent. Deletes complete matching transactions. Shared filtered-deletion rules apply. |
| OP-069 | SVC-006.DeleteTransactionsByProject | L/W | projectId: uuid; fromDate: date; toDate: date → None (Task) | Same matching rule as OP-060: at least one entry account currently has the selected project. Deletes complete matching transactions. Shared filtered-deletion rules apply. |

Confirmed filtered transaction deletion (2026-10-01): OP-064/066–069 provide the same five variants as transaction reads: date range alone, or date range plus one Account, Category, Correspondent or Project identity. Both dates are required. Include both selected device-local days in full using the existing UTC-boundary conversion; fromDate > toDate raises a validation exception before deletion. Each By variant requires its corresponding identity. An unknown or deleted Account, Category, Correspondent or Project identity is an invalid filter and raises a validation exception before deletion, without any changes or commit. A valid existing filter identity with no matching transactions remains a successful no-op. No combined multi-classification filter or explicit transaction-ID list is introduced. Method names above follow the read naming pattern.

Evaluate the filter against the database before loading matching aggregates. Delete each matching Draft or Confirmed transaction once, including all its entries, even when only one entry matched. Empty Draft transactions can match the date-only variant but cannot match an entry-based variant. No matches is a successful no-op without a commit. All matched aggregates, applicable synchronization flags/deletion evidence and affected cumulative-amount recalculations under PM-007 commit together in one SQLite transaction and one AcceptChanges; a database failure rolls back the entire operation and raises a critical exception. The operation never returns partial success. Normal Business access/editing gates apply. Concrete exception mapping remains TQ-07; local write failures are not automatically retried; a user retry is explicit under TQ-02.

Confirmed GetById missing-record rule (2026-10-01): OP-063 throws a not-found exception for both unknown and deleted identities across all entity services. This read-only outcome does not change data or call AcceptChanges. It does not define missing-ID behavior for mutation or bulk-delete operations.

Confirmed single-entity mutation missing-record rule (2026-10-01): Business Update and single-record Delete operations targeting an unknown or already-deleted identity throw a not-found exception. Validate the target before applying changes; no persistent changes, synchronization-flag changes or AcceptChanges occur on this failure. Applies to group, classification, account, currency, transaction and template entity updates/deletes. This does not change currency/date upsert or filtered range/bulk deletion, whose own validation and no-match rules apply. Concrete exception class/mapping remains TQ-07.

Confirmed Business save-reference validation (2026-10-01): Add/Update validates supplied foreign-key identities before saving. A supplied reference to an unknown or deleted entity raises a validation exception and saves nothing, including no partial aggregate or synchronization-flag changes. This includes group/parent selection, account currency/classification selections and transaction/template entry accounts. Optional references may remain null where the contract permits; a supplied non-null identity must resolve to an existing, non-deleted entity. Required references remain required. The target identity of Update itself follows the separate not-found rule. Draft transactions do not bypass entry-reference validation.

Confirmed common Business CRUD lifecycle (2026-10-01): use the same create, update and delete rules for all applicable entities, including Transactions and Templates. Shared identity/reference validation, synchronization tracking, atomic commit/rollback, failure reporting and no-automatic-retry rules apply. Entity-specific invariants already defined elsewhere remain binding; no separate template lifecycle policy is required.

Transaction and Template are aggregates. Their editor Update preserves the parent identity and updates its editable properties normally, but deletes the previous entry collection and creates the entire submitted collection from scratch, with new entry identities and positions assigned from list order. Do not match or patch individual entries by identity. Parent changes, entry replacement and parent tracking flags form one transaction and one AcceptChanges; failure preserves the complete prior aggregate. An empty submitted collection replaces the old collection with an empty one subject to the existing parent-state rules. Entry rows have no independent synchronization tracking. The separately defined account-merge reference replacement remains its own operation, not an editor Update.

### Current C# contract mapping (2026-10-01)

This is dated contract evidence, not verification of current code. New UI-required extensions are proposed separately; no declarations/implementations have been changed or freshly validated in this revision.

The following mapping lists the current C# input and output contracts. Business rules and stable requirement IDs are unchanged.

- Base interfaces reside in `backend/Business.Contracts/Base/Services`. `IUpdateEntityService<in T>` replaces `IEntityService` and declares Add, Update and Delete. `IReadEntityService<T>` separately declares `Task<T> GetById(Guid id)`; T is invariant because Task<T> is invariant. Concrete service interfaces compose read, update and catalog capabilities. IGroupService and IElementService retain only their two entity-type parameters; no DTO parameter is propagated through them.
- Generic GroupService supplies the GroupInfo read stub; generic ElementService supplies the ElementInfo read stub; AccountService supplies the AccountInfo read stub. There is no implementation of entity editing merely because its DTO and interface compile.
- DTOs and parameter objects are POCO records, initialized through properties. Collection properties retain stable get-only List instances. Service DTO folders/namespaces use plural names: Accounts, Currencies, Trees, Templates, Transactions, LocalConfigs and SystemConfigs. Shared parameter interfaces reside in Base/Params.
- Selection and editing are separate reads. TreeInfo holds Groups and lightweight Elements; AccountTreeInfo holds Groups and AccountElementInfo rows. AccountElementInfo extends ElementInfo with CurrencyId and CurrencyName. AccountInfo is an independent edit record, not a subclass of AccountElementInfo. Its duplicated display fields intentionally keep the edit contract independent from the tree projection.

| Service(s) using OP-063 | Read record | Edit-dialog content |
|---|---|---|
| All five group services | GroupInfo | Id, ParentId, ParentName, Name, Description, Order, IsFavorite, IsRoot |
| Category, Correspondent, Project | ElementInfo | Id, GroupId, GroupName, Name, Description, Order, IsFavorite |
| Account | AccountInfo | Id, GroupId/GroupName, Name, Description, Order, IsFavorite, CurrencyId/CurrencyName and nullable CategoryId/CategoryName, CorrespondentId/CorrespondentName, ProjectId/ProjectName |
| Template | TemplateInfo | Id, GroupId/GroupName, Name, Description, Order, IsFavorite and the complete ordered TemplateEntryInfo list |
| Transaction | TransactionInfo | Id, UTC DateTime, State, Description and the complete ordered TransactionEntryInfo list |
| Currency | CurrencyInfo | Id, Code, Name, Symbol, Order, IsFavorite |

Confirmed capped transaction reads (2026-10-02): OP-026/057–060 apply the selected inclusive end date and applicable account/classification filter first, then select at most the maximum number of distinct transactions defined by a fixed application constant. This limit is not a Local or System configuration setting and is not user-editable. Return the latest matching transactions ordered by Transaction.DateTime descending with Transaction.Id as a deterministic tie-breaker. Implementation choice (2026-10-03): equal timestamps use Transaction.Id descending, comparing the parameterless Guid.ToByteArray() bytes lexicographically as unsigned bytes. This makes list ordering the reverse of cumulative transaction ordering. Count transactions rather than joined entries, and return each selected transaction's complete entry list in stored Position order. If the number of matching transactions exceeds the cap, provide an exceeded-limit indication so the UI displays a warning. Exactly the cap is not overflow. No offset/page-number paging is introduced. Confirmed initial constant value (2026-10-02): 300 transactions. This is the initial application limit; the requesting user may revise the constant after checking performance. Such a later change is a development change, not a runtime setting or automatic adjustment. With the initial value, 300 or fewer matches do not exceed the limit; more than 300 matches return the latest 300 and an exceeded-limit indication. The implemented response wrapper is TransactionListInfo with Transactions and LimitExceeded; the query reads at most 301 complete aggregates and returns at most 300. No configuration field or settings UI is required for the cap.

After save, retain the selected end date, active filter and displayed identities under the no-refill refresh rule below; a fresh navigation query may select a different set. CumulativeAmount values still include the complete applicable account history, including entries outside the displayed filter or hidden by the cap. This display limit does not limit cumulative maintenance, account balance calculations, reporting or existing filtered bulk-deletion scope; deletion still uses its separately defined whole-filter contract.

Proposed efficient overflow detection: request at most limit + 1 distinct matching transactions in the selected order, return at most limit, and derive the overflow indicator from the extra match. No exact total-count requirement is introduced. Limit application must precede loading complete entry collections and must not truncate a transaction's entries. Exact query implementation and DTO names remain proposals, not implemented behavior.

Confirmed selected-date navigation (2026-10-02): the user chooses an inclusive end date and the app loads the latest up to 300 matching transactions through that date. Today selects the current device-local date; +/- week, month, quarter and year buttons move the selected date by the chosen interval. Selecting a date or pressing a navigation button performs a fresh local database read with complete transaction DTOs; do not compute or download a client-list delta. Each fresh navigation read may fill the list up to the cap again. This replaces the earlier calendar-period and chosen-start preset proposal for this list. Exact month/quarter/year shift behavior at month ends and leap days remains unresolved. BRD alignment remains outstanding in its own controlled requirements turn.

Confirmed transaction-list refresh after editing (2026-10-02): after successful save and atomic cumulative maintenance, refresh complete TransactionInfo/TransactionEntryInfo data for the identities already displayed, including updated CumulativeAmount values. Retain the selected end date and active account/classification filter. Remove a displayed transaction that no longer matches and show a warning when the edited transaction falls outside the current selection; do not fetch replacement transactions. Thus, if one of 300 displayed transactions leaves, retain 299. Reorder retained rows under the current list order when their timestamps change; do not evict an otherwise matching retained row merely because a fresh top-300 query might rank a different identity ahead of it. Navigation starts a fresh capped query and can fill available places again. Refresh reads committed values and does not trigger cumulative recalculation. It uses complete fresh DTOs, not a specialized delta response. The implemented service method is RefreshTransactions(TransactionRefreshParam): Date, up to 300 TransactionIds and at most one optional AccountId/CategoryId/CorrespondentId/ProjectId filter. TransactionRefreshInfo returns fresh complete Transactions and RemovedTransactionIds. It queries only those identities and never refills. Database cumulative maintenance still covers all affected later account entries, including undisplayed ones. Add/Update response shapes remain unchanged; exact post-edit overflow-indicator behavior remains unresolved. This supersedes the earlier instruction to re-run the full capped filter and refill after editing.

TemplateEntryInfo supplies AccountId, AccountName, CurrencyId, CurrencyName and Amount. TransactionEntryInfo also supplies Rate and CumulativeAmount. Confirmed 2026-10-02: expose CumulativeAmount as a non-nullable decimal in account-currency units in persisted transaction-entry read results, mapped from PM-007; do not expose the scaled database integer. This covers TransactionInfo entries returned by transaction reads and GetById. It is calculated output only and is absent from TransactionEntryParam and other save inputs. Template entries and aggregate AccountBalanceInfo do not acquire a per-entry cumulative field. DuplicateTransactionInfo already reuses TransactionEntryInfo, so its declaration also contains the field; an unsaved duplicate has no authoritative persisted cumulative balance and must not present the source balance as its own. Implementation choice (2026-10-03): unsaved duplicates return CumulativeAmount = 0; the value becomes authoritative only after persistence and recalculation. Reference names are display values; mutation parameter records carry identities and editable values, not commands to rename referenced entities. GetById uses the original identity; DuplicateTransaction and FromTransaction instead prepare unsaved copies. The recorded 2026-10-01 GetDefaultName used three nullable classification IDs/string output. CurrencyId input is retained; the later 2026-10-08 correction moves AccountNameAddCurrency to LocalConfig instead of a bool argument. CurrencyId may be null when AccountNameAddCurrency is false; a valid currency is required when true. It still does not persist changes. Currency-rate editing continues to use currency/date upsert and has no independent GetById contract. Configuration singletons retain parameterless GetConfiguration rather than entity-ID reads.

The recorded LocalConfigurationInfo and SaveLocalConfiguration have AccountNameOrder, AccountNameSeparator, ConflictPriority and SyncTrigger. Extend Local read/save and flat ConfigurationInfo with required bool AccountNameAddCurrency, default false, as confirmed 2026-10-08. SystemConfigurationInfo contains BaseCurrencyId, AmountPrecision, RatePrecision and nullable BalancingAccountId; SaveSystemConfiguration contains only nullable BalancingAccountId. SaveConfiguration returns Task with no response payload. Dataset keys, singleton Ids and sync revisions are not editable inputs. System precision/base currency remain immutable; Local settings remain outside sync.

Verification snapshot: the whole solution builds with zero warnings/errors and 1,220 Local tests pass after the folder/record refactoring. These tests cover existing behavior; GetById and tree reads remain NotImplementedException stubs, so this is not evidence of completed read behavior. XML-comment cleanup remains deferred at the user's request. Device timezone comes from local execution; GetById throws a not-found exception for unknown/deleted identities; exact exception class/error mapping remains TQ-07; no rule is inferred from the absence of code.

Operation continuity (2026-10-01): OP-021 (GetAccounts) and OP-032 (GetTemplates) are retired, with their read capability consolidated into OP-009. Their IDs remain reserved and must not be reused. Broad historical operation ranges refer only to active operations; account/template tree references resolve to OP-009, with the account currency-column variant OP-062. Full editor reads are declared separately through OP-063 and do not restore the retired list operations. OP-024 (PreviewRestoredName) is also retired and reserved; its capability is consolidated into OP-025 (GetDefaultName) in IAccountService.

Confirmed operations for the System configuration service (exact DTO shapes remain proposed):

| OP | Service.operation | Profile | Input → output (proposed) | Contract |
| --- | --- | --- | --- | --- |
| OP-055 | SVC-012.GetConfiguration | D read | None → SystemConfigurationInfo | Confirmed 2026-10-01: ISystemConfigService.GetConfiguration() returns the single System configuration without an Id or other input parameter. Read only; no AcceptChanges. System DTO fields are recorded in the current C# contract mapping; access during recovery remains TQ-05. |
| OP-056 | SVC-012.SaveConfiguration | L/W | SaveSystemConfiguration → None | Confirmed 2026-10-01: ISystemConfigService.SaveConfiguration(settings) updates the single System configuration without an Id parameter. Validate editable settings and save changes with sync tracking in one AcceptChanges call under the shared mutation/no-op rules. Preserve the existing SystemConfig identity generated on Master. System settings participate in synchronization; base currency and AmountPrecision/RatePrecision remain immutable after creation. DTO fields are recorded in the current C# contract mapping; access during recovery remains TQ-05. |

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
| Invalid transaction | OP-027 | Missing/unknown entry account, invalid date/rate or invalid numeric value | Reject without changes in either state. Fewer than two valid entries or imbalance instead saves as Draft for both new and existing transactions. | BR-016–021 |
| Empty report selection | OP-037 | Any dimension has no selected elements/unassigned | Empty result with warning, not unrestricted | BR-023 |
| Missing report reference | OP-037/038 | Saved identity absent from actual tree | Ignore without rewriting definition; returning identity reactivates choice | BR-025 |
| Invalid saved report JSON | OP-037/038 and report definition loading | Saved JSON cannot be read as a valid definition | Show error; preserve stored JSON; never substitute empty/default settings | BR-028; confirmed 2026-10-08 |
| Report calculation failure after period save | OP-070 followed by OP-037 | Date save succeeded; subsequent calculation fails | Keep saved dates, show error and return to report list; no successful results popup | BR-028; confirmed 2026-10-08 |
| Report CSV export failure | UI export of OP-037 results | Saving the generated table to the chosen file fails | Keep results popup and generated rows, show error, allow explicit save retry | BR-028; confirmed 2026-10-08 |
| Cloud authorization failure | OP-003/004/043–049 | Invalid credentials/owner scope | Reject access; no other owner data returned; normal local opening still no login | BR-003/005 |
| Setup partial success | OP-003/004 | Master created, local download failed | Create disabled; user retries Open manually; no normal use without LocalDb | BR-003 |
| Waiting for master | OP-043/048 | Another sync active | Waiting message, no editing, cancellable unlimited healthy wait | BR-034/036 |
| Communication inactivity | OP-043–049 | No response/data for 30 seconds, healthy queue waiting excluded | Stop attempt; pre/post-publication state decides recovery | BR-036/038 |
| Conflict cannot be validly repaired | OP-043 | Existing policies cannot yield valid data | Explain issue/remedy; diagnostic log; preserve publication/recovery boundaries | BR-030/038 |
| Recovery pending | OP-001/local business operations | Uncertain or partly installed publication | Block all business access; preserve state; do not imply rollback | BR-039 |
| Expired registration | OP-043/044/048/049 | Master confirms 90 days elapsed | Delete expired local data; no viewing/export; Download action required | BR-041/042 |

## Security and Authorization

All operations use ROLE-001 as business principal; application-triggered operations act for that owner, not an invented administrator role. ROLE-002 has no API rights.

| Protected operations | Roles | Ownership/scope | Source | Enforcement decisions |
| --- | --- | --- | --- | --- |
| OP-001 | ROLE-001 | Local existence/recovery state only | BR-002 | Local startup flags TQ-04 |
| OP-002/003 | ROLE-001 prospective owner | Hardcoded single-user deployment; Create only when master absent | BR-001/003 | Safe first-owner bootstrap and races TQ-03/TQ-04 |
| OP-004 | ROLE-001 | Authenticate existing owner before registration/download | BR-003/005 | Credential verification/authorization retention TQ-03 |
| OP-005–040, OP-064–069 | ROLE-001 | Current local dataset; no app-opening authentication; business access gates apply | BR-002/034/039 | Gate enforcement/transaction integrity TQ-02/TQ-04 |
| OP-041/042/055/056 | ROLE-001 | Own Local/System settings; Local never synchronizes | BR-005 | Configuration access while recovery pending TQ-05 |
| OP-043–049 | ROLE-001 | Owner dataset plus owned registration, operation and version; never trust IDs alone | BR-005/038–041 | Auth handle/expiration, upload/download authorization TQ-03/TQ-04 |
| OP-050–052 | ROLE-001/application for owner | Session diagnostics/log folder; no remote log-sharing API | BR-043 | Secret redaction/file permissions TQ-08 |
| OP-070 | ROLE-001 | Existing report in current local dataset; ordinary access/editing/recovery gates | BR-002/026/028/039 | Proposed DTO/transaction/errors TQ-07/16 |

Login/password requiredness remains the approved credential-input policy; do not impose invented length/complexity constraints. Login matching is case-insensitive, so Gena and gena identify the same login. Remove leading and trailing spaces from the login before account lookup or creation. Passwords are case-sensitive and are not trimmed or otherwise normalized; preserve the entered password exactly. Existing required-field validation remains unchanged. Remember synchronization authorization through a server-issued token after successful authentication, without storing the password. Protect the retained token with Windows DPAPI scoped to the current Windows user. Store the protected token in a separate file under the Windows user profile, outside the database and UI settings JSON. If a saved token is missing or unreadable, request login when synchronization is attempted while retaining the existing Local registration and database; do not create a new Local copy. A network failure or unreachable Master does not constitute rejected authorization: retain the token and report the connection error. Configure the Master URL in a local configuration file; version one has no UI editor for that address. Read this URL at startup; configuration-file changes require an application restart. If the URL is missing or invalid, show a configuration error when synchronization is requested; existing local work remains available subject to the normal expiry/recovery rules. Require HTTPS for remote Master connections, allowing HTTP only for localhost development. Never send a saved token to a different Master address. Changing the Master address requires login again while preserving the local database and pending changes. After reauthorization to a different server address, verify that the server MasterDatasetKey matches the existing Local copy. A matching login alone does not permit merging into another dataset. This authorization applies to synchronization with Master, not ordinary local work or local schema migration; no local-login requirement is introduced in version one. Expired or rejected synchronization authorization prompts for login again while preserving the local database and pending changes. Each registered Local copy receives its own synchronization token; replacing one Local copy's authorization does not affect other devices. Generate a cryptographically random 256-bit token containing no login or business data. Master stores only its SHA-256 hash, associated with the owner and Local registration, rather than persisting the raw token. Successful reauthorization issues a new token and invalidates the previous token for that Local copy only. A synchronization token is valid for 90 days from issue, without sliding renewal on synchronization; after expiration request login at the next synchronization. Token expiration is separate from Local-copy expiry. Master UTC time is authoritative for token expiration, independent of the Local computer clock. Each token authorizes only its associated owner, Master dataset and Local registration; Master validates these associations on every synchronization request. Token text encoding and exact storage schema remain to be specified. Reauthorization shows the existing login as read-only and an empty password field, and must authenticate the same Master account. Canceling that login cancels synchronization without discarding local work; offline use continues only where the existing expiry/recovery rules permit it. Existing first-use cloud authentication and recovery/expiry admission rules remain unchanged. Use ASP.NET Core PasswordHasher on Master and persist its salted password-hash representation rather than the password. After successful login, replace an older hash when the hasher reports that rehashing is needed; the user password remains unchanged. Version one has no failed-login attempt limit, throttling cooldown or account lockout caused by repeated failed attempts. Do not implement the proposed count/window, per-login/IP scope or failure-counting rules in this version. Exact hasher version/work parameters, token encoding/storage schema, remaining revocation details and exact token filename/subdirectory remain TQ-03. Initial provisioning of a single-user endpoint must be protected; the exact bootstrap control is unresolved. Version-two password change/recovery is not introduced here.

#### Current pre-production initialization scope — confirmed 2026-10-08

The application is not in production. For these pre-release schema changes, including ReportGroup and new report/catalog fields, update the initial schema script and initial seed data and create databases from scratch. Do not implement a legacy-report conversion, assign groups to old development reports, or backfill their favorites/order. Include the sixth root and required fields/defaults directly in fresh initialization; optional sample data must follow the same schema. This documents the implementation approach and does not delete or recreate any database during requirements work. Previously agreed compatibility and migration rules remain requirements for later supported released schemas; the current change has no production upgrade path to support.

## Data and Integration Requirements

1. **Ownership:** one dataset owner; complete local business copy. Administration/credential registry is not a business snapshot. System configuration travels with business state; preserve Local settings across replacement.
2. **Identifiers:** use the confirmed shared GUID business-entity identity contract, including stable report references. Configuration identities follow PM-011/012: SystemConfig.Id is generated on Master and shared through merges; LocalConfig.Id is generated and retained on Local without merging. Remaining registration/operation identity choices retain their stated decision status; this decision does not replace integer snapshot Versions with GUIDs.
3. **Lifecycle/deletion:** apply the confirmed shared tracking and snapshot acknowledgement contracts. Full snapshots must not leak admin metadata or resurrect accepted deletion markers as live local rows. TQ-04 specifies safe preparation/installation.
4. **Master publication:** obtain active snapshot, serialize one writer per master, prepare valid candidate, publish active version and durable operation outcome atomically. Local commit is separate; later failure never rolls master back. Primitive, hosting, lock ownership and atomicity unresolved.
5. **Transfer:** pin selected immutable version for a consistent download; safe local activation before receipt. Restart partial transfer. Keep prior non-expired outgoing data while outcome unresolved. Confirm installed snapshot version, not merely latest master known. State machine and file switching TQ-04.
6. **Recovery:** resolve prior SyncKey before duplicate upload or disposal; recover latest master; repeat lost receipt safely. 90-day confirmed expiry supersedes preservation/recovery. Expiry is evaluated by master, not guessed by local wall clock.
7. **Cleanup:** obsolete published files removable only after download/dependency safety; publication outcomes outlive files as needed. No backup retention; backup/restore version two. Registration that never completed initial download needs explicit lifecycle policy TQ-04.
8. **Catalog:** local system culture/region data from user's example. First value per code as enumerated; platform differences do not rewrite already saved Currency Name/Symbol unless user restores/edits or normal sync conflict rules apply.
9. **Migration — confirmed 2026-10-08:** store a database schema version and check compatibility before normal database use or accepting/activating a downloaded copy. If the database is newer than this application supports, show Please update the application, block use of that database and leave it unchanged. For an older database, automatically execute a provided supported migration when available; apply the migration completely or roll it back. If no supported migration exists, explain the incompatibility, block use and leave the database unchanged. Never activate an incompatible downloaded copy. These checks do not replace existence-only local setup mode selection. The schema version is an increasing integer, separate from the application version, changed only when database compatibility changes. Persist the new schema version in the same transaction as the migration; rollback restores the old version as well. Schema-only migrations preserve existing unsynchronized changes and their modification flags and do not mark every record as user-modified. In version one, Local and Master schema versions must match before exchanging business changes. On mismatch, stop synchronization, preserve unsynchronized work and explain which side needs updating; do not automatically downgrade either database. Only the Master service migrates the Master database; a Local application never directly upgrades it. Store the application schema version in SQLite PRAGMA user_version, the application-managed integer in the database header; no extra version table is required. SQLite does not run application migrations automatically. Treat version 0 in an existing database as unrecognized unless an explicit migration supports its known structure; never merely stamp it as current. Create a new database's current structure and set its user_version together before making it available for use. The first released database structure is schema version 1. When an upgrade spans multiple versions, apply the required migrations sequentially within one transaction for the full chain; any step failure rolls back the entire upgrade, including its version changes. On migration failure, show the source/target schema versions and a short error, log technical details subject to the existing sensitive-data exclusions, and do not retry automatically. Check and migrate an existing Local database at application startup before opening Ledger or initiating automatic synchronization. Master migrations and synchronization use the same per-dataset exclusive lock, preventing concurrent modification of that dataset. Migrate Master using a separate candidate copy; make the candidate active only after success, leaving the current Master copy unchanged on migration failure. After a Master service update, migrate each dataset on its first access before processing synchronization. A successful Master migration publishes a new snapshot version; existing published snapshots remain unchanged. Migration alone is not successful synchronization, does not renew Local expiry and does not clear Recovery required. Concrete supported migration mappings, publication primitives and coordination with already-pending synchronization recovery remain TQ-06. Reference: [SQLite user_version](https://www.sqlite.org/pragma.html#pragma_user_version).

Confirmed migration scenario: after Local1 and Master have migrated, a non-expired Local2 may still contain an older schema and unsynchronized business changes. Update the Local2 application, migrate its own existing database at startup while preserving those changes and flags, then synchronize once schema versions match. Do not replace Local2 with a fresh Master copy before merging its pending changes. Local1 migrates only its own database; the updated Master service owns the Master migration. Existing expiry/recovery admission rules remain binding.
10. **Precision:** SQLite mapping must preserve approved decimal semantics, including exact rounded zero test. Use confirmed int64 scale 10,000 for persisted Amount/Rate; apply the confirmed decimal calculation bounds and reject overflow without mutation; binary floating approximation is not permitted to relax BR-018. No fixed performance limits inferred from SQLite choice.
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
| SC-002 | Exercise hierarchy/locking/rates/draft/active arithmetic cases | Include rounding, zero rate, duplicate account and overflow cases | Decimal/storage bounds and overflow rejection confirmed; exercise boundary failures |
| SC-003 | Exercise templates and report inheritance/current-data behavior | Empty/single-entry template; moved/missing/reappearing IDs; deletion of definition only | Serialization/editor mapping TQ-05 |
| SC-004 | Exercise concurrent-device conflicts, cancellation, lost responses and expiry | Distinguish before publication, after publication, after installation/before receipt | Fault verification later; protocol TQ-04 |

No arbitrary latency/throughput/storage-capacity target is added. Diagnostic reports count received changes, not uploads or all rows in a replacement file. Confirmed report behavior (2026-09-28): show "Currency reordering" once per synchronization report if any currency receives a changed Order during synchronization. Do not count or emit one message per renumbered currency; omit the message when no received currency Order changed. Upload-only activity is not reported as received changes. Future only: a message such as "Reordering elements in group {name of group}", once per affected group when any of its elements receives a changed Order. Group-reordering messages are deferred and are not required in the current release; this deferral does not defer group/element ordering synchronization itself. Count derivation TQ-08 must retain that distinction.

## Assumptions and Open Technical Questions

These questions identify concrete draft contract gaps. They do not reopen settled BRD decisions. All proposals remain inactive until accepted in an exact TRD version. Owner: requesting user with technical design input.

| ID | Item / type | Affected contracts | Approval/handoff impact |
| --- | --- | --- | --- |
| TQ-01 | Partly resolved: display follows AmountPrecision/RatePrecision and SQLite Amount/Rate storage is int64 scaled by 10,000. Resolved: decimal calculation bounds, checked scaled-int64 storage conversion and rejection of overflow without mutation; no extra arbitrary amount cap. Open: Unicode definition of one separator. Account-name format is derived from the two stored settings in PM-012. BaseAmount derivation is confirmed in PM-007; initial-rate date and derived IsInitial are confirmed in PM-004. | PM-003–009/012; DTO-006–011/014/015; OP-016–037 | Only separator character-count semantics remain open in this item; numeric limits and failure policy are settled. BRD scale/rounding rules remain binding. |
| TQ-02 | Partly resolved: shared sync fields, entity revisions, configuration singleton identities and single-application local editing scope confirmed above; no local expected-revision token is required. Confirmed: separate UI actions call separate API operations, each persisting entity changes together with applicable sync flags; all rows affected by one reorder commit in one transaction or all roll back. Open: remaining identifier role/name alignment; local mutation deduplication is not promised: Add/Update/Delete failures are shown to the user and never automatically retried; user retries are explicit. Group, classification and account merges commit all dependent changes, source deletion and flags in one transaction; their multi-row boundary is confirmed. Each current state-changing local API action uses one AcceptChanges commit for its entire result; aggregate replacement and deletion boundaries are confirmed, including all affected rows and flags. | PM-001–012; L/W operations | Blocks schemas and mutation safety; never substitutes timestamps for sync outcomes. |
| TQ-03 | Confirmed: sync-only remembered server-issued token, no saved password, current-user Windows DPAPI protection, reauthentication on expiration/rejection preserving local data, and no local login. Confirmed: independent per-Local tokens, same-Master reauthorization with read-only login and blank password, cancellation stops sync while retaining local work and existing offline admission rules. Open: first-owner bootstrap, exact culture-independent login comparison/normalization mapping, exact PasswordHasher version/work parameters, token encoding/storage schema, remaining revocation details, exact token filename/subdirectory and remaining operation-specific authorization mappings. | PM-013; DTO-016–020/023; OP-002–004/043–049 | Blocks secure setup/cloud contract; no new password complexity rule. |
| TQ-04 | Open: Admin.db hosting/serialization, durable sync state machine, typed Changes DTO, no-change response, snapshot byte protocol, atomic publication/outcome and receipt persistence, writer wait/liveness, reader leases/cleanup, incomplete registration retry/expiry, null-revision identity collisions. Int64 snapshot versions and deletion acknowledgement thresholds confirmed above. | PM-014–016; DTO-018–023; OP-003/004/043–049 | Blocks complete sync contracts; combined conflicts must implement BR-029–033 or fail/report without invalid publication. |
| TQ-05 | Tree contracts are settled as OP-005/009/062 and DTO-030–034. Separate editor reads are now declared through OP-063 and the code contract mapping below; implementations remain stubs.  Timezone source is resolved: local Business/Reporting use the device timezone; remote synchronization uses UTC, with no timezone parameter required. Configuration settings read/write fields are recorded in DTO-015/035; Business internal reads use IConfigOperation; Setup owns external configuration reads and writes. Proposal/open: setup creation details; type-specific DTO/service selection for derived group/element models (no Kind enum), final DTO read/write split, exact exceeded-limit response shape, equal-timestamp ID tie-break comparison/direction and selected-date month-end/leap-day shifts, displayed-ID refresh input/output and operation assignment, post-edit overflow-indicator behavior, selection-save versus rename, settings admission during recovery, ID remapping in reports. | DTO-001–015/026; OP-005–042 | Blocks exposed shapes/editor contracts; saved JSON preservation rules unchanged. |
| TQ-06 | Confirmed: persist/check schema version before database use or downloaded-copy activation; unsupported newer database blocks with update message and no mutation; older database uses an available supported automatic migration, completely applied or rolled back; missing migration blocks unchanged. Confirmed: increasing integer version independent of application version, version change atomic with migration, and preservation of pending data/flags without blanket modification marking. Confirmed: matching Local/Master schemas before exchanging business changes, mismatch stops sync while preserving pending work and explaining the required update, no automatic downgrades, Master migrations owned solely by Master service. Confirmed: SQLite user_version storage, existing version 0 requires an explicit known-structure migration, and new databases establish current structure/version together before use. Confirmed: initial released schema version 1, sequential multi-version migration chain in one transaction with full rollback on failure, and source/target error reporting with diagnostic logging and no automatic retry. Confirmed: Local migration at startup before Ledger/automatic synchronization, shared per-dataset exclusive lock for Master migrations/synchronization, and candidate-copy Master migration with activation only after success. Confirmed: Master migration on first dataset access after service update, new snapshot publication without rewriting existing snapshots, and no sync-success/expiry-renewal/recovery-clear effect from migration alone. Current pre-production schema changes use revised initial scripts/data and fresh databases; no legacy-development migration mapping is required. Future supported-version migration mappings, publication primitives and already-pending synchronization recovery coordination remain open. | All PMs; snapshot operations | Blocks safe compatible local installation; no implementation schema inferred. |
| TQ-07 | Proposal/open: domain error DTO/code taxonomy and local/transport mappings; canceled/absent result representation. | All OPs | Observable BRD errors fixed; exact response contracts incomplete. |
| TQ-08 | Windows Logs beside executable, daily UTF-8 files and cleanup at startup/daily confirmed. First Windows release is unpackaged in a writable folder. Confirmed: local-date filenames, offset-bearing local entry timestamps, retention of today plus six prior calendar dates, and exclusion of passwords/tokens/authorization headers. Open: other-platform log locations and latest report counts after full replacement. | DTO-024/025; OP-050–052 | Blocks diagnostic details; no new Share/Export feature. |
| TQ-09 | Confirmed: first Windows release runs unpackaged from a writable folder, delivered as ZIP with bundled .NET and Windows App SDK runtimes. Confirmed: ordinary managed .NET compilation without Native AOT, initial x64 package, executable-adjacent Data directory, release ZIP without user data and manual updates preserving data/logs/settings. Confirmed: Windows 11 only initially, same .NET major version as backend, stable explicitly pinned Windows App SDK/CommunityToolkit.Mvvm packages without previews. Open: exact compatible version pins/minimum Windows 11 build and verification workload; only propose capacity/latency limits if needed. Transaction reads OP-026/057–060 use a required inclusive end date and a fixed result cap defined by an application constant initially set to 300 transactions, returning the latest matching transactions newest first with a deterministic Transaction.Id tie-breaker and an exceeded-limit warning (confirmed 2026-10-02). No offset/page-number pagination is introduced; the earlier unlimited-list interpretation is superseded. | NFR-001–005/SC-001–004 | Runtime/deployment constraints unresolved; no new business target. |
| TQ-10 | Template create/update/delete is confirmed under the shared Business lifecycle; entry collection replacement is the aggregate exception. Proposal to review: exact preview input/output requiredness; other invalid-input handling for OP-053/054; OP-020 is resolved for range validation: inclusive dates, initial rate excluded, and FromDate > ToDate throws a validation exception without changes. Self-merge is resolved for OP-007/053/054: equal IDs return immediately without exception or changes. Template CombineElements remains unsupported, with failure and no mutation. | UC-005-04; UC-006-04; OP-007/020/025/035/036/053/054/064/066–069 | These details are labeled proposals, not silent additions to BRD. Keep unsupported operations inactive until resolved. |
| TQ-11 | Resolved 2026-09-27: int32 Position, consecutive 0..N-1 on saving edited collections; preserve accepted positions during whole-aggregate sync. Scope excludes catalog element/group ordering. | PM-007/009; DTO-009/011; OP-027/033 | Entry ordering and DTO order representation resolved: list/array order maps to Position on save, confirmed 2026-10-01. |
| TQ-12 | Partly resolved: per-entity ModificationType flags, Local-wins flagged Order independent of content priority, int32 separate zero-based catalog sequences, normalization and GUID tie-breaks. Merged per-member Order values sort ascending with GUID tie-break, then normalize; changed intended placement is accepted. Canonical lowercase GUID strings with ordinal ascending comparison are confirmed. Order-only changes require synchronization, whether represented by uncaptured flags or a queued batch. Open: order transport and change-detection implementation. | PM-001/002/003/005/008; catalog DTOs and mutation/sync operations | Complete collection ordering protocol; BRD 0.218 BR-029 records the accepted independent ordering policy. |
| TQ-13 | Favorite behavior confirmed: default false, six roots non-favorite, ordinary Content sync and ancestor-path filtering. Inline stars, failure restoration and filter/selection rules are in Windows UI integration. Report persistence/projections remain TQ-16. | PM-001/002/003/005/008/010; catalog DTOs/mutations | Behavior resolved; new report fields still proposed. |
| TQ-14 | CumulativeAmount: cache scope, binary transaction ordering, atomic Add/Update/Delete maintenance, Confirmed deletion recalculation, zero Draft entry values, exclusion of Draft entries from cache lookups, recalculation on both Draft/Confirmed transitions and no synchronization flags from cache-only updates confirmed in PM-007. Atomic destination-account recalculation during account combination is confirmed in PM-005/OP-054. PM-007 confirms a complete local cumulative rebuild before a downloaded merged database becomes usable. Signed-int64 storage scaled by 10,000 and rejection of cumulative overflow with complete rollback are confirmed in PM-007. Exact binary GUID comparison is confirmed: unsigned lexicographic comparison of the 16 parameterless Guid.ToByteArray() bytes, identically in SQLite. Open: logical property type/nullability; stored values on retained deleted entries; Master cache representation and transfer exclusion; local rebuild orchestration and restart/recovery mechanics; higher-level maintenance method shapes, exact opening-balance DAL function declaration and absence representation, internal flush versus AcceptChanges semantics, exact command declarations and SQL. Opening-balance retrieval is confirmed: ICumulativeOperation calls the DAL lookup for the last live Confirmed entry strictly before the account/start boundary and passes the seed to ICumulativeAmountCommand; the DAL reports an absent predecessor and Business ICumulativeOperation converts absence to a zero seed. Service-owned transaction creation/commit/rollback through the unit of work in the same service function and the Service-to-Operation-to-Command call chain are confirmed; operations and commands have no transaction-context responsibilities. ICumulativeAmountCommand range/full-account variants, parameterized SQL execution and CumulativeAmountCommand placement in DataAccess.EntityFramework.SqLite are confirmed above. Initial affected-account/date-boundary and remaining merge proposals remain under discussion. | PM-007; OP-027/028/031/054/061/064/066–069; synchronization operations | Blocks complete cache lifecycle and implementation handoff; no CumulativeBaseAmount cache. |

| TQ-15 | Confirmed Windows UI behavior includes MVVM/CommunityToolkit.Mvvm, modal coordinator, per-call service scopes, decimal editing, date/time rules, flattened ListView columns/tree interactions, local synchronous operations, editor account-reference guards and refresh behavior. Remaining SDK/package selection and wiring are implementation tasks. Identify a concrete conflicting or missing contract before requesting further clarification; settled flows are not reopened. | Business-only Windows increment; applicable read/mutation contracts | Track implementation integration; deferred-domain gaps do not block Business by default. |
| TQ-16 | Open UI data/API changes: Reporting service/tree projection alignment; period-only OP-070/DTO-036; exact OP-072 request naming/return shape; window-state storage; creation precision DTO fields/check input; report JSON and remaining CSV/TXT formatting. Preserve retired SVC-013 and OP-021/024/032 IDs. The four-rule cleanup on 2026-10-08 aligns BRD/TRD on Local default priority, no Currency.Description, calculation/display precision and Draft entry count; those contradictions are closed. Other listed contract gaps remain open. | PM-001/010/012; DTO-013/015/016/035/036; OP-002/003/025/039/041/042/055/056/070 | Blocks extensions/full alignment/handoff; business behavior stays source-backed while shapes/mechanisms are proposed. |

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
| BR-005 | PM-011–PM-016; OP-001–OP-004/041–OP-049; OP-071/072 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-006 | PM-001/002; OP-005–OP-013 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-007 | PM-001/002; OP-005–OP-013 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-008 | PM-001/002; OP-005–OP-013 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-009 | PM-001/002/005/008; OP-005–OP-013/022/033 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-010 | PM-003–PM-005/012; OP-009, OP-014–OP-020, OP-022–OP-023, OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-011 | PM-003–PM-005/012; OP-009, OP-014–OP-020, OP-022–OP-023, OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-012 | PM-003–PM-005/012; OP-009, OP-014–OP-020, OP-022–OP-023, OP-025/030; OP-071/072 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-013 | PM-003–PM-005/012; OP-009, OP-014–OP-020, OP-022–OP-023, OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-014 | PM-003–PM-005/012; OP-009, OP-014–OP-020, OP-022–OP-023, OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-015 | PM-003–PM-005/012; OP-009, OP-014–OP-020, OP-022–OP-023, OP-025/030 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-016 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-017 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-018 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-019 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-020 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-021 | PM-004/006/007; OP-017/026–OP-031 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-022 | PM-008/009; OP-009, OP-033–OP-036 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-023 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-024 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-025 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-026 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-027 | PM-010; DTO-012–DTO-014; OP-037–OP-040 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-028 | PM-010; DTO-012–DTO-014; OP-037–OP-040/070 | Partial | UI and period-only mutation mapped; ReportGroup/projections and TQ-15/16 remain partial. |
| BR-029 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements; OP-071/072 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-030 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-031 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-032 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-033 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-034 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements; OP-071/072 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-035 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-036 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-037 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-038 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-039 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-040 | PM-011–PM-016; OP-041–OP-048; Data and Integration Requirements | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-041 | PM-014/015; OP-044/049; UC-011-01/02 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-042 | PM-014/015; OP-044/049; UC-011-01/02 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-043 | DTO-024/025; OP-050–OP-052 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BR-044 | Shared favorites; PM-001/002/003/005/008/010; Windows UI integration; TQ-13/16 | Partial | Favorite behavior confirmed; report persistence/projection extension proposed. |
| BC-001 | UC-001-01–UC-001-03; OP-001–OP-004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-002 | UC-002-01–UC-002-03; OP-001/004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-003 | UC-003-01–UC-003-06; OP-005–OP-013 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-004 | UC-004-01–UC-004-09; OP-009, OP-014–OP-020, OP-022–OP-023, OP-025 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-005 | UC-005-01–UC-005-06; OP-026–OP-031, OP-057–OP-060, OP-061, OP-064/066–069 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-006 | UC-006-01–UC-006-04; OP-009, OP-033–OP-036 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-007 | UC-007-01–UC-007-02; OP-037 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-008 | UC-008-01–UC-008-04; OP-038–OP-040/070 | Partial | UI and period-only mutation mapped; ReportGroup/projections and TQ-15/16 remain partial. |
| BC-009 | UC-009-01–UC-009-05; OP-041–OP-047; OP-071/072 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-010 | UC-010-01–UC-010-03; OP-044–OP-048 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-011 | UC-011-01–UC-011-02; OP-044/047/049 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| BC-012 | UC-012-01–UC-012-02; OP-050–OP-052 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-001 | UC-001-01–UC-001-03; OP-001–OP-004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-002 | UC-002-01–UC-002-03; OP-001/004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-003 | UC-003-01–UC-003-06; OP-005–OP-013 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-004 | UC-004-01–UC-004-09; OP-009, OP-014–OP-020, OP-022–OP-023, OP-025 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-005 | UC-005-01–UC-005-06; OP-026–OP-031, OP-057–OP-060, OP-061, OP-064/066–069 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-006 | UC-006-01–UC-006-04; OP-009, OP-033–OP-036 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-007 | UC-007-01–UC-007-02; OP-037 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-008 | UC-008-01–UC-008-04; OP-038–OP-040/070 | Partial | UI and period-only mutation mapped; ReportGroup/projections and TQ-15/16 remain partial. |
| FR-009 | UC-009-01–UC-009-05; OP-041–OP-047; OP-071/072 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-010 | UC-010-01–UC-010-03; OP-044–OP-048 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-011 | UC-011-01–UC-011-02; OP-044/047/049 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| FR-012 | UC-012-01–UC-012-02; OP-050–OP-052 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| NFR-001 | Performance, Reliability, and Observability; row NFR-001 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| NFR-002 | Performance, Reliability, and Observability; row NFR-002 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| NFR-003 | Performance, Reliability, and Observability; row NFR-003 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| NFR-004 | Performance, Reliability, and Observability; row NFR-004 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |
| NFR-005 | Performance, Reliability, and Observability; row NFR-005 | Partial | Behavior mapped; schema/operation proposals and applicable TQ items remain. |

## Development Handoff Readiness

This assessment covers the complete product. The confirmed Business-only increment above proceeds independently of deferred-domain discovery; current documentation work does not start code implementation.

- **Full-product handoff: Not ready**. BRD 0.218 and TRD 0.232 are Draft; prior full-version approval covers BRD 0.32 only. Current user authorizes draft alignment, not document promotion.
- Blockers: TQ-01–TQ-10, TQ-12 and TQ-14–TQ-16. Entry-order and favorite behavior TQ-11/13 are resolved; new report favorite fields remain TQ-16. Proposed schemas/directions/exposed fields, concurrency, authentication, sync wire protocol and transport/error definitions remain incomplete.
- DTO/PM boundary: persistent models are not operation inputs/outputs. DTO-019 Changes and snapshot streaming are explicitly unresolved and cannot be treated as implemented contracts.
- Traceability: all 79 applicable BRD IDs enumerated (2 roles, 12 capabilities, 44 rules, 12 functional, 5 nonfunctional, 4 success criteria). One non-system approval role; 78 Partial; zero Covered. This is drafting coverage, not a passing contract review or development handoff.
- Detailed use cases are concise scenario specifications linked to operations. Proposed additions and unresolved technical behavior remain visible instead of masquerading as complete manual test procedures.
- Handoff requires approved BRD, approved TRD with material gaps closed, and passing review results. No implementation/code/test generation is authorized here.

## Clarification Log

### 2026-10-08 — Backend alignment before WinUI

User corrected the implementation sequence: persistent models, Business and DAL must first match current BRD/TRD, including ReportGroup, configuration property names and fresh-database SQL. Report/ReportGroup now have persistent models, mappings, repositories and a distinct seeded root. LocalConfig and related projections use AccountNameOrder, AccountNameSeparator and AccountNameAddCurrency; account-name generation reads the flag and accepts the optional currency identity, requiring a live currency when enabled. Fresh SQLite scripts include the new tables/fields and user_version = 1. Existing System precision, Local conflict default, Currency/Rate description distinction and valid Draft entry-count behavior already match the confirmed rules. Report calculation, report UI and other deferred subdomain workflows are not implemented by this alignment. Existing databases are not modified; use the fresh initialization scripts as previously agreed.


### 2026-10-08 — Business-only increment and account-reference guards

User accepted tracking current account references in open existing transaction/template editors, checking every UI account-delete path before the service call, and removing protection when the editor closes. Persisted-reference checks remain in the service. User then requested a complete Business UI flow using an existing Local database and postponement of all other subdomains, including Reports, Synchronization and Help. Existing decisions are preserved; only actual Business integration gaps require further discussion.


### 2026-10-08 — Report failures

User agreed to all three report failure proposals: invalid saved JSON produces an error without replacing saved settings; calculation failure after successful date saving keeps those dates and returns to the report list with an error; failed CSV export retains the results popup and allows retry.



### 2026-10-08 — Blank currency-header numeric cells

User agreed after the USD heading example: currency-header Amount and BaseAmount are null, displayed/exported as empty cells rather than 0.00. Numeric data/subtotal/grand-total rows keep their existing BR-027 amount rules. This closes the remaining question from the report-row pack.



### 2026-10-08 — Remove report-result Key and Level

User accepted questions 2 and 3: remove Level because Kind supplies the display role, and remove Key because results are temporary/read-only. User requested explanation of question 1 about numeric cells in currency-header rows; that question remains open.



### 2026-10-08 — Flat report-result contract

User agreed to all three proposals: one flat ordered row list shared by display/CSV; explicit Data, Currency header, Currency subtotal and Grand total row kinds; warnings returned separately so they do not become CSV rows. Recorded in DTO-014/R and OP-037. No business calculation, stored report definition or AccountBalanceInfo change is implied.



### 2026-10-08 — Reports initialized from scratch

User accepted ReportGroup and report catalog fields under the same conventions as the other five catalogs, then clarified that the application is not in production. Update initial scripts/data and create fresh databases; the proposed conversion of old reports into the root with default favorite/order is not required. Future released-schema compatibility rules are not removed by this pre-production decision.



### 2026-10-08 — Account-name properties and currency validation

Source: requesting user explicitly agreed to conditional CurrencyId validation and selected AccountNameOrder, AccountNameSeparator and AccountNameAddCurrency. The order name is unchanged; the other two names replace DefaultAccountNameSeparator and AddCurrencyToAccountName in current contracts. The saved LocalConfig bool controls the suffix. Empty currency is allowed for name generation with the flag off; flag on requires a valid currency, and Restore default name is disabled until it is chosen. Historical names below remain provenance.



### 2026-10-08 — Flat Settings object and LocalConfig naming flag

Source: user correction to the three-question pack. Reuse the existing flat POCO with mixed Local/System properties. Save only editable fields. Add required bool LocalConfig.AddCurrencyToAccountName with existing default false, and read it in GetDefaultName through existing configuration access instead of accepting a bool argument. CurrencyId conditional validation was not answered and remains open. No application source was changed.



### 2026-10-08 — Settings service contracts

The requesting user agreed to the three proposed contracts: one Setup call reads Local/System settings; one Setup call saves both in one database transaction; IAccountService.GetDefaultName receives CurrencyId and AddCurrency bool alongside classification IDs, and Business resolves the currency code. Recorded as OP-071/072 and the OP-025 extension. Exact DTO layout and conditional currency requiredness are not inferred.



### 2026-10-08 — Four-rule consistency cleanup

Source: requesting user explicitly confirmed the four rules and requested cleanup. Updated current precision, Currency scope, PM-006 cardinality, transaction scenario and source-alignment notes. Previously accepted ordering and same-Local conflict exceptions now align with BRD 0.213. Historical superseded discussion remains below. Checklists reviewing BRD 0.212/TRD 0.222 remain historical; no full re-review or handoff approval is claimed.



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
- Historical decision: immediate local hard deletion; superseded on 2026-10-01 by soft deletion in application operations and physical cleanup during delta preparation. No Master deletion marker is needed for an entity whose creation was never captured. Existing reference validation and uncertain-outcome recovery rules remain binding.
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
- Decision: adopt atomic batch capture and captured-flag clearing; use queued batches OR uncaptured flags for pending-sync detection. Supersede clearing only after installation. Updated 2026-10-01: also include new entities with EditRevision null; creation initializes ModificationType to None.
- Affected IDs: shared tracking/order contracts; PM-015; OP-043; TQ-04/TQ-12.
- TRD 0.56 becomes Draft 0.57; full-version approval remains outstanding.

### 2026-09-30 — Unified snapshot Revision and field names

- User direction: use EditRevision / DeleteRevision; define Revision in BRD or TRD and check assignment rules. EditRevision null (new) or 0 (unknown Master revision) is set on Local; >=1 is assigned by Master. DeleteRevision null (alive) is set on both, 0 (soft deletion) on Local, >=1 on Master.
- Source: requesting user, following agreement that each accepted batch produces a snapshot.
- Decision: define one snapshot Revision counter; rename DeleteVersion to DeleteRevision throughout active contracts. Clarify that Local can copy positive values from Master, that ordinary edits retain known revisions, and that accepted deletion sets both fields to the same Revision. Preserve historical logs.
- Affected IDs: shared tracking and snapshot contracts; PM-001–016; OP-043–049.
- TRD 0.57 becomes Draft 0.58; full-version approval remains outstanding.

### 2026-09-30 — Code names, Report and Currency description

- User direction: remove Description from Currency; rename SavedReport to Report; use the existing code names Date, DateTime, AmountPrecision and RatePrecision; fix TRD and add an explanation.
- Source: requesting user in the persistent-model review chat, 2026-09-30.
- Decision: apply the requested names throughout active model and corresponding DTO contracts; preserve date/UTC and precision semantics. Remove Currency.Description from PM-003, DTO-006 and currency editing; keep CurrencyRate.Description. PM-010/DTO-013 become Report/ReportInfo without changing their purpose or stable IDs.
- BRD impact: BRD 0.49 BR-009/BR-013 still mention currency descriptions and need alignment before full approval. No BRD or code change is included in this TRD-only correction.
- Affected IDs: PM-003/004/006/010/011; DTO-006/007/009/013/015; UC-004-03; OP-016/038 and shared precision/report references.
- TRD 0.58 becomes Draft 0.59; full-version approval remains outstanding. Historical clarification records remain unchanged.

### 2026-09-30 — SnapshotRevision naming

- User direction: rename AcknowledgedVersion to SnapshotRevision, following confirmation that it identifies the last successfully installed snapshot and maps to C# long.
- Source: requesting user in the persistent-model review chat, 2026-09-30.
- Decision: use SnapshotRevision in all active contracts, including LocalConfiguration (PM-012), LocalRegistration (PM-014), snapshot acknowledgement and deletion-marker retention rules. Type remains required int64 (C# long), initially 0. Local records successful installation; Master advances its per-Local value only after installation confirmation. This is not the latest published Master revision unless that snapshot has been installed and acknowledged.
- Other snapshot fields (Version, PublishedVersion, InstalledVersion) retain their existing names. Historical clarification entries retain the former name.
- TRD 0.59 becomes Draft 0.60; full-version approval remains outstanding.

### 2026-09-30 — Local conflict default and Report.Json

- User direction: make ConflictPriority.Local the default; rename DefinitionJson to Json.
- Source: requesting user in the persistent-model review chat, 2026-09-30.
- Decision: PM-012 defaults ConflictPriority to Local, matching LocalConfig. PM-010 uses Json for its required JSON instructions string, matching Report. Report behavior, field type/nullability and stable IDs remain unchanged.
- BRD impact: BRD 0.49 BR-029 still states default Master; this user decision supersedes that default for the revised TRD. BRD alignment remains outstanding before full approval.
- Affected IDs: PM-010/012; configuration default under DTO-015 and OP-041/042.
- TRD 0.60 becomes Draft 0.61; full-version approval remains outstanding. Historical clarification entries remain unchanged.

### 2026-09-30 — Configuration singleton identities

- Question: should each configuration row receive a generated GUID at creation and retain it through database replacement, with SystemConfig.Id shared across copies and LocalConfig.Id remaining local?
- Accepted answer: app must have a single SystemConfig and single LocalConfig; SystemConfig.Id is generated on Master and the app merges SystemConfig; LocalConfig.Id is generated on Local and the app does not merge LocalConfig.
- Source: requesting user in the persistent-model clarification chat, 2026-09-30.
- Decision: record required GUID Id fields, singleton cardinality and generation authority in PM-011/012; retain their identities under the existing synchronization/replacement lifecycles. Close configuration singleton-key choices in TQ-02. Other concurrency and mutation questions remain open.
- Affected IDs: PM-011/012; TQ-02; Data and Integration Requirements.
- TRD 0.61 becomes Draft 0.62; full-version approval remains outstanding.

### 2026-09-30 — Single-application local editing

- Question: can two editors modify the same item simultaneously on one device, or does the app allow only one editor at a time?
- Accepted answer: one user per database and one app; external SQLite edits through another editor are the user's responsibility.
- Source: requesting user in the persistent-model clarification chat, 2026-09-30.
- Decision: no separate local optimistic-concurrency token or expected-revision check. Remove proposed expected-revision fields from DTO-003/010 and the stale-local-revision error. External concurrent database modifications are unsupported. Keep cross-device sync conflict handling, transaction integrity, recovery and the existing editing block during sync/wait.
- Affected IDs: PM-001–012; DTO-003/010; local operation profiles and error contracts; TQ-02.
- TRD 0.62 becomes Draft 0.63; full-version approval remains outstanding.

### 2026-09-30 — Confirm enum names and numeric assignments

- Question: use the existing enum numeric values in code as the definitive TRD values?
- Accepted answer: yes, for Business.Models/Enums; record the values in TRD, check the rename DefaultAccountNameOrder to AccountNameOrder and the addition of Undefined = 0.
- Source: requesting user in this clarification chat; inspected all five enum files and LocalConfig on 2026-09-30.
- Decision: record all five enums in the confirmed enum table. Rename the active PM-012 and DTO-015 property to AccountNameOrder, matching LocalConfig. Close the unresolved numeric assignments. Keep current defaults and reserved/unset meanings; preserve historical wording in earlier clarification entries.
- Affected IDs: PM-006/012; shared tracking; DTO-009/015 and other projections using these enums.
- TRD 0.63 becomes Draft 0.64; full-version approval remains outstanding.

### 2026-09-30 — Separate reporting clarification from edit and sync work

- Question: should Report.Json include a format version, for example Version = 1?
- User direction: discuss reporting later as a standalone feature; the current three properties are enough; now identify issues in edit mode and synchronization.
- Decision: leave report format details for a separate discussion. Keep Id, Name and Json with existing shared sync tracking; do not add a JSON format-version requirement. Continue clarification of edit and sync contracts. No reporting behavior is removed or marked fully approved.
- Source: requesting user in this clarification chat, 2026-09-30.
- Affected IDs: PM-010; DTO-012/013; reporting portion of TQ-05/TQ-06.
- TRD 0.64 becomes Draft 0.65; full-version approval remains outstanding.

### 2026-09-30 — Separate actions save entity changes with sync flags

- Clarification: drag-and-drop reorder, add, edit and setFavorite are separate UI actions calling separate API functions; each function saves its result to SQLite.
- Exact follow-up question: should the IsFavorite change and ModificationType.Content flag save together, so a failure cannot save the favorite without its sync flag?
- Accepted answer: correct; set the favorite, combine Content into ModificationType, then update the entity through its repository.
- Source: requesting user in this clarification chat, 2026-09-30.
- Decision: entity content changes and applicable sync flags persist together within their individual API operation. Existing bits are preserved. This confirms the entity-and-flags boundary, not one transaction across separate UI actions; multi-row operation boundaries remain open.
- Affected IDs: shared L/W profile; favorite contract; PM-001/002/003/005/008; TQ-02.
- TRD 0.65 becomes Draft 0.66; full-version approval remains outstanding.

### 2026-10-01 — Atomic reorder save

- Exact question: should all rows changed by one reorder call save in one SQLite transaction, so failure leaves the previous order intact?
- Accepted answer: yes; the app updates each child row and then calls the unit of work once to accept changes.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: one reorder operation persists all changed Order values and applicable modification flags together. Failure rolls back the entire reorder. Row-by-row updates are preparation for the single commit, not independent commits. Existing Content flags remain set. This does not combine separate API actions.
- Affected IDs: shared catalog-order contract; PM-001/002/003/005/008; L/W profile; TQ-02.
- TRD 0.66 becomes Draft 0.67; full-version approval remains outstanding.

### 2026-10-01 — Entry replacement, rate upsert and category merge

- User directions: fix the remaining DTO Comment fields; transaction/template edits delete the old entries and create a full new set, generating IDs on add/edit; rate upsert finds by currency and date without an ID; category merge replaces CategoryA with CategoryB in all Accounts and deletes CategoryA. Missing API functions will be discussed after these topics.
- Source: requesting user in the service-interface review chat, 2026-10-01.
- Decision: entry row IDs remain persistence identities but are regenerated on aggregate replacement and are absent from entry DTOs. Existing parent identity and entry-order requirements remain. Rate upsert uses CurrencyId plus Date; it updates the matching row or creates one. Record category merge without inferring merge semantics for other entity types. TransactionParam and CurrencyRateParam use Description to match the existing text-field contract.
- Cross-artifact impact: BRD rate duplicate wording and category-merge scope need alignment before full approval; BRD is not revised here.
- Open: entry-order DTO representation, aggregate/category-merge multi-row atomicity, other CombineElements types and detailed rate deletion contract. Missing API coverage discussion is deferred until these topics are settled.
- Affected IDs: PM-002/004/007/009; DTO-007/009/011/027; OP-017/027/033/053; UC-004-05; TQ-02/05/10.
- TRD 0.67 becomes Draft 0.68; full-version approval remains outstanding. Earlier entry-identity decisions are superseded only as specified above.

### 2026-10-01 — CombineElements for all account classifications

- Question: does the same reference-replacement rule apply to merging Projects and Correspondents?
- Accepted answer: yes, for CombineElements; each Account has Correspondent, Category and Project, and the user can replace one element with another in all Accounts.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: extend the established source-to-destination replacement and source-deletion contract to all three classification types. Update only the matching account classification; preserve other classifications and account names. Retain OP-053 and DTO-027 identities, naming them CombineElements and ClassificationMerge. Account and Template merge behavior remains unconfirmed.
- Affected IDs: PM-002/005; DTO-027; OP-053; TQ-02/10.
- TRD 0.68 becomes Draft 0.69; full-version approval remains outstanding.

### 2026-10-01 — Replace account references within the same currency

- Question: should IAccountService.CombineElements support merging Accounts?
- Accepted answer: it substitutes one account with another in all TransactionEntries and TemplateEntries, but both accounts must have the same currency.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: record account-reference replacement and the same-currency precondition in PM-005 and OP-054. Apply existing entry-value/order and parent-aggregate tracking rules. This does not change the currencies of accounts or perform conversion. Source-account deletion after replacement is not inferred and remains an explicit question.
- Affected IDs: PM-005/006/007/008/009; DTO-028; OP-054; TQ-02/10.
- TRD 0.69 becomes Draft 0.70; full-version approval remains outstanding.

### 2026-10-01 — Delete source account after replacement

- Exact question: after replacement, should the source account be deleted, like the source classification?
- Accepted answer: yes.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: after replacing all source-account references in transaction and template entries, delete the source Account through the existing deletion/tracking lifecycle and retain the destination. Existing balancing-account deletion behavior continues to apply. Close the source-deletion question; atomicity and invalid/self-merge handling remain open.
- Affected IDs: PM-005/011; OP-054; TQ-10.
- TRD 0.70 becomes Draft 0.71; full-version approval remains outstanding.

### 2026-10-01 — Template CombineElements is unsupported

- Question: should template merging be supported, or should that operation be unavailable for templates?
- Accepted answer: the inherited function is a stub that throws NotSupportedException; the user will determine how to exclude it from ITemplateService.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: Template CombineElements is not a supported operation. Calls fail without changing data or sync flags. Close the Template merge-semantics question in TQ-10; no interface restructuring or implementation change is requested. TemplateGroup merging and Account replacement inside TemplateEntries remain separate supported behavior.
- Affected IDs: PM-008; OP-053 scope note; TQ-10.
- TRD 0.71 becomes Draft 0.72; full-version approval remains outstanding.

### 2026-10-01 — Same-identity merge returns without changes

- Exact question: if source and destination IDs are identical, should supported merge functions reject the call without changes?
- Accepted answer: simple return from the function without any exceptions.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: supported group, classification and account merge calls with equal IDs return immediately as a no-op, without mutations or synchronization-flag changes. Supersede the proposed self-merge rejection. Unsupported Template CombineElements behavior remains unchanged.
- Affected IDs: DTO-010/027/028; OP-007/053/054; shared operation profiles; TQ-10.
- TRD 0.72 becomes Draft 0.73; full-version approval remains outstanding.

### 2026-10-01 — One transaction per supported merge

- Exact question: for a real merge, should all reference replacements and source deletion commit together through one AcceptChanges call?
- Accepted answer: yes.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: each supported group, classification or account merge commits all affected rows, dependent changes, source deletion and applicable synchronization flags together. Failure rolls back the entire merge. Equal-ID calls remain no-ops without a commit. Other API actions are not combined into this transaction.
- Affected IDs: PM-001/002/005/006/008/011; OP-007/053/054; shared L/W profile; TQ-02.
- TRD 0.73 becomes Draft 0.74; full-version approval remains outstanding.

### 2026-10-01 — Entry DTO list order determines Position

- Exact question: for transaction/template entry DTOs, use their list order to assign Position = 0..N-1 on save, avoiding a separate order property in each input entry?
- Accepted answer: yes.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: list/array order defines the intended entry order. Save generates replacement rows with Position equal to each zero-based index; entry input DTOs need neither IDs nor Position/Order fields. Read projections preserve persisted Position order. Empty templates have no positions to assign. Close the entry-order representation question in TQ-05/TQ-11; synchronization still preserves the accepted whole aggregate and its order.
- Affected IDs: PM-007/009; DTO-009/011, nested E/T shapes; OP-027/033; TQ-05/11.
- TRD 0.74 becomes Draft 0.75; full-version approval remains outstanding.

### 2026-10-01 — Commit the complete local API action once

- Exact question: when editing a transaction/template, should deleting old entries, inserting replacements, and updating the parent sync flag commit together in one AcceptChanges?
- Accepted answer: yes; each function must use AcceptChanges at least once, and in the current app the user sees only one call per function.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: every successful state-changing local function persists its complete result; current actions use one commit boundary, including dependent rows and sync flags. Confirm whole-aggregate entry replacement and deletion boundaries. Existing no-op returns and unsupported/invalid calls do not commit; reads/previews remain non-mutating. Separate synchronization/setup durability stages retain their existing contracts.
- Affected IDs: shared L/W profile; OP-016/027/033 and other local mutations; TQ-02.
- TRD 0.75 becomes Draft 0.76; full-version approval remains outstanding.

### 2026-10-01 — Delete rates in a range, excluding the initial rate

- Clarification: deleting rates is a separate UI button/action and API call.
- Exact question: should that button delete one selected rate or all rates in a selected date range?
- Accepted answer: all rates, except initial.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: OP-020 selects by currency and date range and deletes all matching ordinary rates. Always exclude the initial rate without rejecting the range because it includes that row. Preserve stored entry rates. Use the existing deletion tracking and single-commit rule. Keep OP-020 identity, rename its logical operation to DeleteRates, and define proposed DTO-029 packaging for the existing selector fields.
- Open: inclusive/exclusive endpoints and reversed-range handling. No missing-API work is started.
- Affected IDs: UC-004-05; PM-004; DTO-003/029; OP-020; TQ-10.
- TRD 0.76 becomes Draft 0.77; full-version approval remains outstanding.

### 2026-10-01 — Inclusive rate-deletion dates

- Exact question: should both boundary dates be included, so October 1–5 also deletes rates dated October 1 and October 5?
- Accepted answer: yes.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: rate deletion selects FromDate <= Date <= ToDate and always excludes the initial rate. Equal endpoints select one calendar date. Reversed-range handling remains open.
- Affected IDs: PM-004; DTO-029; OP-020; TQ-10.
- TRD 0.77 becomes Draft 0.78; full-version approval remains outstanding.

### 2026-10-01 — Reject reversed rate-deletion ranges

- Exact question: if FromDate > ToDate, should the function return without changes or throw a validation exception?
- Accepted answer: throw exception.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: reject reversed rate-deletion ranges with a validation exception before deleting rows or changing sync metadata. No commit occurs. Keep inclusive endpoints, same-date selection and initial-rate exclusion unchanged; do not invent an exception class or code.
- Affected IDs: PM-004; DTO-029; OP-020; TQ-10.
- TRD 0.78 becomes Draft 0.79; full-version approval remains outstanding.

### 2026-10-01 — GetAllGroups operation name

- User direction: discuss missing functions one by one; agree to GetTree but name it GetAllGroups.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-005 and rename it GetAllGroups for each group type. Preserve its group-hierarchy meaning, root-once rule and stable operation ID. Exact DTO shape remains proposed; no service-interface implementation is included in this decision.
- Affected IDs: OP-005; SVC-002; DTO-001/002/004 references.
- TRD 0.79 becomes Draft 0.80; full-version approval remains outstanding.

### 2026-10-01 — Groups-only and groups-with-elements reads

- Prior accepted answer: GetAllGroups returns a flat list including the root, with ParentId and Order, and the UI builds the hierarchy.
- Proposal accepted: keep GetAllGroups for parent/destination selection; use GetAllGroupsWithElements for browsing and selecting Categories, Correspondents and Projects; remove the separate GetElements operation for these three catalogs.
- Accepted answer: agree.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: retain OP-005 as the groups-only read and revise OP-009 into the combined read, preserving its classification browsing purpose and existing use-case links. Add Order to the group projection and introduce proposed DTO-030 packaging for the combined result. Exact combined-result nesting is not inferred as approved. Account/Template reads are not changed by this decision.
- Affected IDs: OP-005/009; SVC-002/003; DTO-002/004/005/030; TQ-05.
- TRD 0.80 becomes Draft 0.81; full-version approval remains outstanding.

### 2026-10-01 — Combined group reads include Accounts and Templates

- Exact question: should GetAllGroupsWithElements also cover Accounts and Templates?
- Accepted answer: I think yes.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: extend OP-009 to all five catalog types. Each call returns one type's groups and corresponding elements. Extend the proposed DTO-030 item mappings to AccountParam / AccountInfo and TemplateParam / TemplateInfo. Do not change record mutation contracts. Combined-result packaging and retention of separate GetAccounts/GetTemplates remain open.
- Affected IDs: OP-005/009/021/032; DTO-008/011/030; TQ-05.
- TRD 0.81 becomes Draft 0.82; full-version approval remains outstanding.

### 2026-10-01 — Retire separate Account and Template list reads

- Exact question: can we remove separate GetAccounts and GetTemplates reads too, since their lists can come from the combined result?
- Accepted answer: yes.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: remove OP-021 and OP-032 from the active operation catalog and reserve their IDs as retired. Redirect use-case, DTO-consumer and traceability references to OP-009 GetAllGroupsWithElements. Preserve account projection content and template entry content/order in the combined result. No mutation operations are removed.
- Affected IDs: OP-009/021/032; DTO-001/002/008/011/030; BC-004/006; TQ-05.
- TRD 0.82 becomes Draft 0.83; full-version approval remains outstanding.

### 2026-10-01 — Confirm GetAvailableCurrencies

- Exact question: GetAvailableCurrencies returns the system currency catalog for adding currencies, one item per ISO code with Name and Symbol; keep this function?
- Accepted answer: yes.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-014 and its name. Preserve the previously confirmed system culture/region source, first-per-ISO deduplication and offline behavior. This is a read with no persistence; exact DTO packaging remains proposed.
- Affected IDs: OP-014; SVC-004; DTO-006 via DTO-002.
- TRD 0.83 becomes Draft 0.84; full-version approval remains outstanding.

### 2026-10-01 — Confirm GetAllCurrencies

- Exact question: GetAllCurrencies returns all currencies already added to this database, including the base currency, in Order sequence, replacing the name GetCurrencies; agree?
- Accepted answer: agree.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: rename OP-015 to GetAllCurrencies and confirm its all-currencies scope, inclusion of the base currency and ascending persisted Order. Preserve the operation ID and read-only behavior; exact DTO packaging remains proposed.
- Affected IDs: OP-015; SVC-004; DTO-001/002/006.
- TRD 0.84 becomes Draft 0.85; full-version approval remains outstanding.

### 2026-10-01 — Confirm GetRates scope and ordering

- Exact question: should GetRates return all rates for one currency, including its initial rate, sorted by Date descending?
- Accepted answer: I think yes.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-019 as a read of all rates for the selected currency, including the initial rate, in descending Date order. Preserve the initial-date hiding rule in the UI and read-only behavior. Exact selector DTO packaging remains proposed.
- Affected IDs: OP-019; SVC-004; DTO-001/002/007.
- TRD 0.85 becomes Draft 0.86; full-version approval remains outstanding.

### 2026-10-01 — Separate Local and System configuration services

- Question: keep PreviewDefaultName as a service function, generating an account name from selected classifications and Local naming settings without saving?
- Accepted answer: yes; create ILocalConfigService for this function and ISystemConfigService for similar functions.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-025 under SVC-009 LocalConfigService and introduce SVC-012 SystemConfigService with the user-selected interface names. Split the previous configuration service ownership: OP-041/042 cover Local, and proposed OP-055/056 cover System. Separate DTOs and exact read/write names remain to be discussed. No unspecified System helper is invented. OP-024 PreviewRestoredName ownership remains unchanged pending clarification.
- Scope: requirements/service-contract design only; no C# interface files are created in this discussion turn.
- Affected IDs: SVC-009/012; OP-025/041/042/055/056; DTO-015; TQ-05.
- TRD 0.86 becomes Draft 0.87; full-version approval remains outstanding.

### 2026-10-01 — One account default-name function

- Question: keep separate PreviewDefaultName and PreviewRestoredName functions, or use one function for both account creation and restoring a default name?
- Accepted answer: "So. We leave only single functions. Give the name GetDefaultName. And put this functions into IAccountService".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: OP-025 becomes IAccountService.GetDefaultName; retire OP-024 without reusing its ID. Both UI actions use the supplied classification selections and current Local naming settings to generate a name without saving. LocalConfigService and SystemConfigService retain their configuration responsibilities. This supersedes the previous placement of OP-025 in ILocalConfigService.
- Scope: TRD service-contract design; C# interfaces unchanged.
- Affected IDs: SVC-005/009; OP-024/025; DTO-003/008/026; UC-004-09; TQ-05.
- TRD 0.87 becomes Draft 0.88; full-version approval remains outstanding.

### 2026-10-01 — Filtered transaction reads

- Exact question: "Should it return transactions for a selected date range, with an optional account filter?"
- Accepted answer: "I think yes. We should return with appropriate filter by range. It will be performance hole if we return whole set".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-026 with a required date range and optional account filter; filter before loading and return complete entries for matching transactions. Pagination and result-size limits remain unresolved.
- Affected IDs: OP-026; DTO-001; TQ-09.
- TRD 0.88 becomes Draft 0.89; full-version approval remains outstanding.

### 2026-10-01 — Separate transaction read functions; no pagination in version 1

- Exact question: "Should GetTransactions support pagination, loading results in batches?"
- Accepted answer: "No in the first version." User additionally requested ITransactionService.GetTransactions, GetTransactionsByAccount, GetTransactionsByCategory, GetTransactionsByCorrespondent and GetTransactionsByProject, each filtered by range.
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: retain OP-026 for range-only reads; add OP-057–OP-060 for the four selected-element reads. All require date ranges and return complete matching transaction aggregates without pagination in version 1. This supersedes the optional account filter on OP-026. Exact DTO packaging and date-range boundary/validation details remain unresolved.
- Affected IDs: OP-026/057–060; DTO-001/002/009; BC-005; UC-005-01/03; FR-005; TQ-05/09.
- TRD 0.89 becomes Draft 0.90; full-version approval remains outstanding.

### 2026-10-01 — Transaction read date boundaries

- Exact question: "For the range: include both selected days in full, using the device timezone, and throw if From > To—agree?"
- Accepted answer: "yes".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: apply inclusive calendar dates in the current device timezone to all five transaction read functions; interpret UTC timestamps using the corresponding local-day boundaries. A reversed range throws a validation exception before querying. Reads do not save or call AcceptChanges.
- Affected IDs: OP-026/057–060; DTO-001.
- TRD 0.90 becomes Draft 0.91; full-version approval remains outstanding.

### 2026-10-01 — Duplicate transaction for editing

- Exact question: "Next: ITransactionService.DuplicateTransaction(id). Copies accounts, amounts, rates and description; sets date/time to now. Returns an unsaved transaction for editing. Saves only when the user presses Save. Agree?"
- Accepted answer: "yes".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-029 as preparation of an unsaved transaction for editing; preserve copied stored rates and entry order, set DateTime to now, and persist only through the normal transaction-add operation when Save is selected.
- Affected IDs: OP-029; DTO-003/009.
- TRD 0.91 becomes Draft 0.92; full-version approval remains outstanding.

### 2026-10-01 — Name the rate lookup GetRate

- Exact question: "Next: ITransactionService.RestoreRate(accountId, date). Returns the latest rate for the account’s currency on or before that date. UI puts it into the entry; database changes only on Save. Agree?"
- Accepted answer: "GetRate".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: rename OP-030 to ITransactionService.GetRate. Preserve the existing lookup behavior and stable operation ID; this naming correction introduces no persistence changes.
- Affected IDs: OP-030; DTO-026.
- TRD 0.92 becomes Draft 0.93; full-version approval remains outstanding.

### 2026-10-01 — Account balances for version 1

- Exact question: "Should your Accounts screen show balances, or only account names and properties?" Explanation: GetBalances calculates each account balance from confirmed transactions up to the selected day.
- Accepted answer: "I see. Yes we need it".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-031 for the Accounts screen in version 1, returning cumulative balances through the end of the selected date from Confirmed transactions. Keep detailed DTO shape and service ownership proposed; do not reopen deferred reporting.
- Affected IDs: OP-031; DTO-001/014; UC-005-06; TQ-05.
- TRD 0.93 becomes Draft 0.94; full-version approval remains outstanding.

### 2026-10-01 — Two balance functions in TransactionService

- Exact question: "Should GetBalances(date) belong to IAccountService, since it returns account balances?"
- Accepted answer: "I think to the TransactionService.2 functions GetBalancesForAllAcounts(date) GetBalanceForAccount(accountId, date)".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: keep both functions in ITransactionService. Rename OP-031 to GetBalancesForAllAccounts(date), correcting the spelling of Accounts, and add OP-061 GetBalanceForAccount(accountId, date). Preserve the confirmed cumulative calculation through the selected date from Confirmed transactions; results use AccountBalanceInfo.
- Affected IDs: OP-031/061; SVC-006; DTO-001/014; UC-005-06; BC-005; FR-005; TQ-05/10.
- TRD 0.94 becomes Draft 0.95; full-version approval remains outstanding.

### 2026-10-01 — Balance results in both currencies

- Exact question: "Should each result contain the balance in the account’s currency only, or also in the base currency?"
- Accepted answer: "in both".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: both OP-031 and OP-061 return an account-currency amount and a base-currency amount for each account. Apply the established sum of Amount and sum of per-entry rounded BaseAmount using stored entry rates; exact DTO packaging remains proposed.
- Affected IDs: OP-031/061; DTO-014; TQ-05.
- TRD 0.95 becomes Draft 0.96; full-version approval remains outstanding.

### 2026-10-01 — Unknown account in balance lookup

- Exact question: "If GetBalanceForAccount receives an unknown account ID, should it throw an exception?"
- Accepted answer: "yes".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: OP-061 throws an exception for an unknown accountId instead of returning a zero balance. Exception type/transport mapping remains proposed.
- Affected IDs: OP-061; TQ-10.
- TRD 0.96 becomes Draft 0.97; full-version approval remains outstanding.

### 2026-10-01 — Apply template into an unsaved transaction

- Exact question: "Next: ITemplateService.ApplyTemplate(templateId). Returns an unsaved transaction with template accounts, amounts and description, date/time set to now, and applicable rates. User edits it, then saves or cancels. Agree?"
- Accepted answer: "agree".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-034 under ITemplateService as preparation of an unsaved transaction, with DateTime set to now and applicable rates. Preserve entry order; Save uses normal transaction validation and persistence, while Cancel makes no changes.
- Affected IDs: OP-034; DTO-003/009; UC-006-02.
- TRD 0.97 becomes Draft 0.98; full-version approval remains outstanding.

### 2026-10-01 — Prepare template from a transaction

- Exact question: "Next: ITemplateService.FromTransaction(transactionId). Returns an unsaved template copied from a transaction: accounts, amounts and description. User chooses its name and group, then saves or cancels. Agree?"
- Accepted answer: "agree".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-036 under ITemplateService as preparation of an unsaved template. Preserve accounts, amounts, description and entry order; user supplies name/group and saves through the normal template-add operation or cancels without changes.
- Affected IDs: OP-036; DTO-003/011.
- TRD 0.98 becomes Draft 0.99; full-version approval remains outstanding.

### 2026-10-01 — Read the single Local configuration

- Exact question: "Next: ILocalConfigService.GetConfiguration(). Returns the single Local configuration for the settings screen. No ID parameter needed. Agree?"
- Accepted answer: "correct".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-041 name, ownership and parameterless read of the single Local configuration. Exact Local response DTO remains proposed.
- Affected IDs: OP-041; SVC-009; DTO-015; TQ-05.
- TRD 0.99 becomes Draft 0.100; full-version approval remains outstanding.

### 2026-10-01 — Save the single Local configuration

- Exact question: "Next: ILocalConfigService.SaveConfiguration(settings). Updates the single Local configuration, without an ID parameter. Validates settings and calls AcceptChanges once. These settings do not synchronize. Agree?"
- Accepted answer: "correct".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm OP-042 name, ownership and update of the single Local configuration without an Id parameter. Validate settings and commit once under the established mutation/no-op rules; Local settings remain outside synchronization. Exact DTO shapes remain proposed.
- Affected IDs: OP-042; SVC-009; DTO-015; TQ-05.
- TRD 0.100 becomes Draft 0.101; full-version approval remains outstanding.

### 2026-10-01 — Matching System configuration functions

- Exact question: "Next: ISystemConfigService.GetConfiguration(). Returns the single System configuration. No ID parameter; saves nothing. Agree?"
- Accepted answer: "ISystemConfigService should contain the same 2 fucctions".
- Source: requesting user in this clarification chat, 2026-10-01.
- Decision: confirm ISystemConfigService.GetConfiguration() and SaveConfiguration(settings), matching the Local service function names and absence of an Id parameter. Retain established System-specific rules: synchronized settings, Master-generated identity, immutable base currency and precisions, and one commit with sync tracking for a successful state-changing save.
- Affected IDs: OP-055/056; SVC-012; DTO-015; TQ-05.
- TRD 0.101 becomes Draft 0.102; full-version approval remains outstanding.

### 2026-10-01 — Shared lightweight trees and account currency column

- Source: requesting user, this chat, 2026-10-01; current Business.Contracts tree records/interfaces reviewed in place.
- Accepted decision: all five trees share Name, Description and IsFavorite columns, with Order reflected only by row placement. Account tree additionally shows CurrencyName. GetAllGroups and renamed GetTree belong to generic IGroupService; IAccountGroupService also exposes GetAccountsTree.
- Selection flow: opening the correspondent chooser from an account editor, selecting an element and closing the chooser updates the editor selection; account persistence remains a separate save action.
- Applied: retain OP-009 identity while renaming to GetTree; add OP-062 for the account variant. Replace former full editor projections in tree results with the shared lightweight DTOs reflected in code. Retired operation IDs remain reserved.
- Review gap: current AccountInfo omits classifications and GetTree omits template entries; no separate read of existing account/template edit data is declared. Record this under TQ-05 without inventing or approving a new API.
- Affected IDs: OP-005/009/062; DTO-004/005/008/011/030–034; UC-003-02; TQ-05.
- TRD 0.102 becomes Draft 0.103; full-version approval remains outstanding.

### 2026-10-01 — Record reviewed read/update segregation and DTO refactoring

- Request: "Do you need to add some notes to the TRD/BRD. If need, write these notes".
- Source: requesting user and the service/DTO changes reviewed in this chat, 2026-10-01.
- Decision: record IUpdateEntityService and IReadEntityService composition, Guid GetById, shared versus account-specific tree/edit DTOs, plural folders, record inputs, configuration shapes and current ICurrencyRateService.GetRate ownership. Add OP-063 for the per-entity editor-read family; reserve all retired IDs.
- Read-contract gap is closed at declaration level only. Tree/GetById implementations remain stubs; passing builds/tests do not establish their behavior. Unknown/deleted-ID semantics and device-timezone transport remain open.
- BRD unchanged: these are technical contract and implementation-status notes within existing business scope.
- Affected IDs: OP-030/041/042/055/056/063; DTO-015/031–034; TQ-05/10.
- TRD 0.103 becomes Draft 0.104; full-version approval remains outstanding.

### 2026-10-01 — Bulk transaction deletion contract

- Request: add the existing ITransactionService.DeleteTransactionList function to TRD.
- Added OP-064 with the existing List<Guid> input and Task result; no new DTO or C# changes.
- The shared local-action rule applies: all selected aggregates, entries and synchronization flags commit once or all roll back.
- Null/invalid/missing/deleted-ID rejection, duplicate normalization and empty-list no-op are proposed validation choices, not previously confirmed behavior. Recorded under TQ-10.
- Updated UC-005-04, transaction capability/function traceability and authorization coverage. TRD 0.105 becomes Draft 0.106; full-version approval remains outstanding.

### 2026-10-01 — Business-first completion and configuration ownership

- Source: requesting user in this chat, 2026-10-01.
- Accepted directions: "Business can read configuration only. But the main write operations are in Setup.Contracts"; "remove References from Configs ... leave only Foreign kyes"; add IConfigService returning all settings in one read-only DTO; "At first finish Common (Business) Subdomain. And start implementation. Then we can continues with others."
- Recorded current subdomain ownership, ID-only configuration relationships and OP-065/SVC-013/DTO-035. Account balances remain in Business. The existing deletion-integrity exception remains unchanged.
- Sequence: resolve Business decisions through discussion, implement Business next, then continue other subdomains. Remaining questions are not silently approved.
- TRD 0.106 becomes Draft 0.107. BRD unchanged because this records technical ownership, existing contracts and work sequence; business scope is unchanged.

### 2026-10-01 — Align transaction Draft rules with BRD 0.51

- Source: accepted Business clarification in this chat, recorded in BRD 0.51 BR-016/019/022.
- Draft relaxes only entry count and balancing. Every present entry requires a valid account, positive rate and numeric amount (zero allowed). Empty/single-entry and unbalanced transactions may save as Draft whether new or previously Confirmed. Dates and numeric validation remain mandatory.
- Updated PM-006, DTO-009, UC-005-01/02, UC-006-02, OP-034 and validation outcomes. Existing non-nullable entry AccountId/Amount/Rate contracts remain appropriate; no nullable draft-entry fields are required.
- Drafts have no accounting contribution, show a persistent warning and remain available for later repair. No code changes in this clarification turn.
- TRD 0.107 becomes Draft 0.108; previous full-document approval remains outstanding.

### 2026-10-01 — Local timezone requires no API parameter

- Discussion: whether Business needs a supplied timezone context for local calendar dates.
- Accepted direction: "Responsibility of remote served is only Sync feature. It requires only UtcNow. All reports, operations are making on the local device."
- Source: requesting user, this chat, 2026-10-01.
- Resolved TQ-05 timezone source: local operations use the device timezone directly; persisted transaction timestamps and remote synchronization timestamps remain UTC. Local date boundaries must still honor daylight-saving offsets.
- No additional timezone contract or method parameter is required. The previously created CalculateReport.TimeZoneKey is unnecessary under this decision; remove it when Reporting contracts are next aligned, without expanding the current Business implementation scope.
- Affected: PM-004/006; OP-026/030/031/034/037/057–061; TQ-05. TRD 0.108 becomes Draft 0.109. No code changes in this turn.

### 2026-10-01 — GetById missing or deleted identity

- Exact question: "GetById(id) receives an unknown or deleted ID. I recommend throwing a 'not found' exception, rather than returning null or an empty DTO. Agree?"
- Accepted answer: "exception".
- Source: requesting user, this chat, 2026-10-01.
- Updated OP-063 for all entity read services: unknown/deleted IDs throw a not-found exception. Concrete exception naming and transport mapping remain TQ-07; bulk-delete behavior is a separate decision.
- TRD 0.109 becomes Draft 0.110. No code changes in this clarification turn.

### 2026-10-01 — Filter-based bulk deletion and critical failure

- Source: requesting user, this chat, 2026-10-01.
- Direction: bulk deletion uses filter criteria, for example a date range and account matching at least one transaction entry, instead of exact transaction IDs. Validate the filter at the beginning and raise an exception if invalid; no matches is a successful no-op.
- Failure question: "If the database fails during deletion, we must roll back everything. Should that failure still throw an exception?"
- Accepted answer after SQLite transaction clarification: "In this case yes. We cannot make bulk delete. service throws critical exception".
- Updated OP-064, UC-005-04 and TQ-10. Database failure rolls back the complete batch, including synchronization tracking; the caller receives a critical exception. No partial success.
- Exact filter contract remains under discussion. Existing C# ID-list signature has not been changed. TRD 0.110 becomes Draft 0.111.

### 2026-10-01 — Five bulk-delete filter variants

- Exact question: "should bulk deletion use the same five filter variants as transaction reads?" Date range alone, or date range plus Account, Category, Correspondent or Project.
- Accepted answer: "yes. It's good idea".
- Source: requesting user, this chat, 2026-10-01.
- Updated OP-064 and added OP-066–069, preserving OP-065 for configuration reading. Date semantics and entry/account-classification matching follow the corresponding read operations. Whole-transaction deletion, no-match success and atomic rollback remain binding.
- Updated transaction traceability and authorization coverage. C# signatures require alignment before implementation. TRD 0.111 becomes Draft 0.112.

### 2026-10-01 — Missing configuration is a critical failure

- Exact question: "if IConfigService.GetConfiguration() finds the System or Local configuration row missing, I recommend a critical exception, rather than inventing default settings. Agree?"
- Accepted answer: "correct".
- Source: requesting user, this chat, 2026-10-01.
- Updated OP-065: either missing singleton causes a critical exception; no partial/default response or implicit configuration creation. Concrete exception class and mapping remain TQ-07.
- TRD 0.112 becomes Draft 0.113. No code changes in this clarification turn.

### 2026-10-01 — Bulk-delete filter identities must exist

- Exact question: "a bulk-delete filter contains an unknown or deleted Account/Category/Correspondent/Project ID. Should that count as an invalid filter and throw before deletion?"
- Accepted answer: "yes. It's invalid filter".
- Source: requesting user, this chat, 2026-10-01.
- Updated OP-066–069 shared filter validation: reject unknown/deleted filter identities before deletion; no mutation or commit. An existing valid filter with no transaction matches is still a successful no-op. Removed this resolved ambiguity from TQ-10.
- TRD 0.113 becomes Draft 0.114. No code changes in this clarification turn.

### 2026-10-01 — Transaction-read filter identities must exist

- Exact question: "should transaction reads use the same rule? For example, GetTransactionsByAccount throws for an unknown/deleted account, but returns an empty list for an existing account with no matching transactions."
- Accepted answer: "yes. It OK".
- Source: requesting user, this chat, 2026-10-01.
- Updated shared OP-057–060 read validation for Account, Category, Correspondent and Project filters: unknown/deleted identities are invalid filters and throw; valid identities without matching transactions return an empty list. No mutation or commit.
- TRD 0.114 becomes Draft 0.115. No code changes in this clarification turn.

### 2026-10-01 — Missing target on Update or single-record Delete

- Exact question: "Update or single-record Delete receives an unknown or already-deleted ID. I recommend a not-found exception, with no changes saved. Agree?"
- Accepted answer: "agree".
- Source: requesting user, this chat, 2026-10-01.
- Added the shared Business mutation rule: unknown/deleted targets raise a not-found exception without changes or commit. Currency/date upsert and filtered deletion retain their separate semantics.
- TRD 0.115 becomes Draft 0.116. No code changes in this clarification turn.

### 2026-10-01 — Transaction state is output-only

- Exact question: "TransactionParam currently contains State. But Business determines Draft or Confirmed from valid entries and their balance. I recommend removing State from the save input. Keep it in TransactionInfo for display. Agree?"
- Accepted answer: "agree".
- Source: requesting user, this chat, 2026-10-01.
- Updated DTO-009 and OP-027: TransactionParam omits State; Business derives state on Add/Update. TransactionInfo retains State. Invalid entries still reject saving; Draft is not a client-selected validation bypass.
- Code alignment is pending the Business implementation phase. TRD 0.116 becomes Draft 0.117.

### 2026-10-01 — Decimal and storage numeric limits

- Exact question: "numeric limits. I recommend using decimal calculations and rejecting any value or calculation that exceeds supported calculation/storage limits, leaving data unchanged. No additional arbitrary amount cap. Agree?"
- Accepted answer: "yes".
- Source: requesting user, this chat, 2026-10-01.
- Updated precision/calculation policy and TQ-01. Decimal calculations and the existing signed-int64 scaled storage bounds define limits; overflow rejects without partial mutation. No arbitrary business cap. Separator character-count semantics remain open separately.
- TRD 0.117 becomes Draft 0.118. No code changes in this clarification turn.

### 2026-10-01 — Validate referenced entities on Business saves

- Exact question: "Add/Update references another entity—for example, an account's group or a transaction entry's account. I recommend rejecting unknown or deleted referenced entities with a validation exception, saving nothing. Agree?"
- Accepted answer: "argee" (agreement).
- Source: requesting user, this chat, 2026-10-01.
- Added shared Add/Update reference validation: supplied unknown/deleted references reject the entire save without data or synchronization-flag changes. Optional null references remain allowed by their contracts; Update target identity retains its not-found behavior.
- TRD 0.118 becomes Draft 0.119. No code changes in this clarification turn.

### 2026-10-01 — No automatic retries of local writes

- Exact question: "I recommend no automatic retry of Add/Update/Delete after failure. Show the error; the user can retry. This avoids accidentally repeating an Add. Synchronization keeps its separate retry rules. Agree?"
- Accepted answer: "correct."
- Source: requesting user, this chat, 2026-10-01.
- Updated the L/W profile and TQ-02: local Add/Update/Delete failures surface to the user without automatic retry. Explicit user retries remain allowed; no general Add idempotency guarantee is introduced. Synchronization retry/recovery remains governed by its durable operation identity.
- TRD 0.119 becomes Draft 0.120. No code changes in this clarification turn.

### 2026-10-01 — One CRUD lifecycle with aggregate entry replacement

- Question context: whether deleting a template follows aggregate deletion/tracking without changing previously created transactions.
- Accepted direction: "We use same rules for creating, updating, deleting. There is only one exception. Transactions and Templates are aggregate. In another words properties of transactions user can update, but list of entries must be deleted and created from scratch. This is only difference. All others are the same".
- Source: requesting user, this chat, 2026-10-01.
- Recorded the shared Business CRUD lifecycle and the aggregate editor-update exception. Parent identity remains; all old entries are replaced with new entries in submitted order, atomically with parent fields and tracking. Existing entity-specific invariants remain.
- Closed the separate DeleteTemplate proposal in OP-035, UC-006-04, PM-008 and TQ-10. TRD 0.120 becomes Draft 0.121. No code changes in this turn.

### 2026-10-01 — Align Business model and API declarations before implementation

- User request: make appropriate persistent-model, API/BFF interface and DTO changes before service implementation.
- TransactionParam no longer accepts State; TransactionInfo and the persistent Transaction retain the derived state. Existing entry account/amount/rate fields remain non-nullable, and empty entry collections already support the approved Draft rules.
- ITransactionService now declares DeleteTransactions and the four ByAccount/ByCategory/ByCorrespondent/ByProject variants. DeleteTransactionList(List<Guid>) is removed. Shared read/update/delete and configuration contracts document the accepted failure rules.
- DAL LocalConfig now includes ConflictPriority, SyncTrigger and SnapshotRevision, with bidirectional scalar mapping. Existing database schema migration and service behavior are implementation work; no database was migrated here.
- Template input documents full entry replacement. TransactionState documentation permits Confirmed-to-Draft transitions. These declarations do not implement validation, arithmetic, deletion or aggregate persistence behavior.
- TRD 0.121 becomes Draft 0.122. Earlier clarification log entries describing pending alignment are historical; this entry records the completed declaration alignment.

### 2026-10-01 — Standardize the Business subdomain name

- Accepted direction: use Business as the subdomain name, matching Business.Contracts, Business.Impl and Business.Models.
- Source: requesting user, this chat, 2026-10-01: "Agree. Make changes in TRD".
- Updated subdomain naming. Verbatim historical user quotations remain unchanged; scope and behavior are unchanged.
- TRD 0.122 becomes Draft 0.123.

### 2026-10-02 — CumulativeAmount scope, binary order and atomic save

- Source: requesting user, this clarification chat, 2026-10-02.
- Scope question: "should we cache both `CumulativeAmount` and `CumulativeBaseAmount`?" Accepted answer: "No. Only `CumulativeAmount` . `CumulativeBaseAmount` doesn't make sense".
- Ordering question: "Proposed order: `Transaction.DateTime` → `Transaction.Id` → entry `Position`. Each entry stores the account balance **including that entry**. Agree?" Accepted answer: "correct. But one note . It should be binary order for `Transaction.Id` , not ToString()".
- Atomicity question: "Next: save transaction changes and recalculate affected cumulative amounts **in the same database transaction**. If either fails, roll back both. Agree?" Accepted answer: "agree".
- Updated PM-007 in place with the accepted cache rule and Add/Update boundary; removed conflicting active no-stored-running-balance wording. Preserved existing base-currency read outputs. Initial maintenance proposals and remaining contract details are explicitly unresolved under TQ-14.
- Affected IDs: PM-006/007; OP-027/031/061; TQ-14. No implementation changes.
- TRD 0.123 becomes Draft 0.124; full-version approval remains outstanding.

### 2026-10-02 — Draft cache values and state transitions

- Exact question: "Next: Draft transactions contribute nothing. I suggest storing **0** in their `CumulativeAmount` fields and excluding them from cache lookups. Agree?"
- Accepted answer: "Agree. And if transaction was in confirmed state but now is in draft we should recalculate cumulative amount. And vise versa".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated the PM-007 cache rule in place: Draft entries store zero and cannot supply a cached preceding balance; Confirmed-to-Draft removes the old contribution, and Draft-to-Confirmed adds the new contribution, recalculating affected cumulative amounts within the agreed atomic Update boundary. Removed these resolved gaps from TQ-14.
- Affected IDs: PM-007; OP-027/031/061; TQ-14. No implementation changes.
- TRD 0.124 becomes Draft 0.125; full-version approval remains outstanding.

### 2026-10-02 — Atomic cumulative recalculation on transaction deletion

- Exact question: "Next: deleting a Confirmed transaction also recalculates subsequent cumulative amounts for its accounts, within the same database transaction. Agree?"
- Accepted answer: "agree".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated PM-007, OP-028 and the shared filtered-deletion contract: remove the deleted Confirmed contribution and recalculate affected subsequent account cumulative amounts, atomically with deletion and synchronization evidence. The existing whole-batch rollback rule remains binding for filtered deletion. Removed deletion maintenance from TQ-14; stored values on retained deleted entries remain unresolved.
- Affected IDs: PM-007; OP-028/064/066–069; TQ-14. No implementation changes.
- TRD 0.125 becomes Draft 0.126; full-version approval remains outstanding.

### 2026-10-02 — Cache-only updates do not set synchronization flags

- Exact question: "Next: recalculating cached values must **not mark later transactions as modified for synchronization**. Only actual transaction edits set sync flags. Agree?"
- Accepted answer: "correct".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated PM-007 in place: cache-only maintenance does not mark parent transactions as modified or set synchronization flags; existing flags from actual edits are preserved. Removed cache-only synchronization tracking from the unresolved items in TQ-14. Master representation and rebuild lifecycle remain separate open topics.
- Affected IDs: PM-007; OP-027/028/064/066–069; TQ-14. No implementation changes.
- TRD 0.126 becomes Draft 0.127; full-version approval remains outstanding.

### 2026-10-02 — Atomic cumulative recalculation on account combination

- Exact question: "Next: when combining accounts, entries move to the destination account. Its cumulative amounts must be recalculated in the same database transaction as the account combination. Agree?"
- Accepted answer: "agree".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated PM-005 and OP-054 in place, with a PM-007 cross-reference: destination cumulative-amount recalculation commits or rolls back together with account combination. Existing contribution, ordering, Draft-zero and cache-only synchronization rules apply; actual account-reference changes still mark their parent aggregates under the established contract. Removed account-replacement maintenance from TQ-14.
- Affected IDs: PM-005/007; OP-054; TQ-14. No implementation changes.
- TRD 0.127 becomes Draft 0.128; full-version approval remains outstanding.

### 2026-10-02 — Rebuild downloaded merged database before normal use

- Exact question: "Next: after downloading a merged database, rebuild cumulative amounts locally **before making that database available for use**. Agree?"
- Accepted answer: "agree".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated PM-007 with the local rebuild prerequisite and OP-046 with its cross-reference. All accounts are rebuilt under the established cumulative rules before normal use; incomplete or failed rebuilding cannot expose the downloaded database for use. Existing sync/recovery outcomes remain unchanged. TQ-14 retains implementation and restart/recovery mechanics as unresolved.
- Affected IDs: PM-007; OP-046; TQ-14. No implementation changes.
- TRD 0.128 becomes Draft 0.129; full-version approval remains outstanding.

### 2026-10-02 — Account-scoped SQLite cumulative command variants

- Decision context: the user reviewed EF projections, per-entry parameterized updates and SQLite running-sum UPDATE statements, including an account/date-filtered calculation seeded with the preceding cumulative balance.
- Accepted direction: "We are using ICumulativeAmountCommand for update CumulativeAmount for each TransactionEntry which belonges to the particular amount"; "We should have 2 version. With range and initial value of CumulativeAmount (from the last previous TransactionEntry) and without range and 0 as initial value of CumulativeAmount"; "Implementation of this CumulativeAmountCommand must be in the DataAccess.EntityFramework.SqLite project."
- Source: requesting user, this clarification chat, 2026-10-02. "Particular amount" is interpreted as the particular account discussed immediately beforehand.
- Updated PM-007 with the two account-scoped command variants, seed semantics and explicit SQLite implementation placement. Updated TQ-14 to distinguish the settled DAL approach from outstanding mapping, bounds, declaration, retrieval and orchestration details. Existing atomicity and synchronization rules remain binding.
- Affected IDs: PM-007; TQ-14. No implementation changes.
- TRD 0.129 becomes Draft 0.130; full-version approval remains outstanding.

### 2026-10-02 — Consolidate cumulative discussion and remaining decisions

- Request: "cool. Put all this info in the TRD. Do you have another questions?"
- Source: requesting user, this clarification chat, 2026-10-02.
- Consolidated the original account/date-boundary and Master-cache directions, named service/operation proposals, selected command context, range SQL pattern, parameterization, outer transaction mechanics, SQLite isolation, numeric and ordering prerequisites, performance considerations, alternatives and inspected implementation status under PM-007. Kept proposals and provider facts separate from the already accepted contracts; no blanket approval of unresolved choices is inferred.
- Updated TQ-14 with the internal-flush/AcceptChanges clarification. The next decision is the cache storage range and overflow policy needed by the selected integer-SQL approach.
- Affected IDs: PM-007; TQ-14. No implementation changes.
- TRD 0.130 becomes Draft 0.131; full-version approval remains outstanding.

### 2026-10-02 — Cumulative scaled-int64 storage and overflow rejection

- Exact question: "One important question: **store `CumulativeAmount` as signed `long`, scaled by 10,000, like Amount?** A cumulative balance can overflow even when individual amounts fit. I recommend rejecting the operation and rolling back everything on overflow. Agree?"
- Accepted answer: "agree".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated the PM-007 schema rule and numeric discussion note in place: signed-int64 scaled storage, cumulative range enforcement, rejection and complete outer rollback on overflow. Removed physical storage and overflow-policy uncertainty from TQ-14 while retaining the unresolved logical property type/nullability and implementation validation.
- Affected IDs: PM-007; OP-027/028/054/064/066–069; TQ-14. No implementation changes.
- TRD 0.131 becomes Draft 0.132; full-version approval remains outstanding.

### 2026-10-02 — Service owns transaction; operation calls command

- Clarification context: the proposed opening-balance retrieval flow mentioned the same outer transaction. The user clarified ownership rather than accepting opening-balance retrieval responsibility.
- Accepted direction: "Transaction is responsibility of Service. Operation and command don't know context. We should create transaction in the service via uow and commit in the same function. Inside this function app makes call of operation, operation makes call of command".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated PM-007 and the transaction discussion in place: service creates and completes the transaction through the unit of work in the same function; it calls the operation, which calls the command. Operation and command do not manage transaction context. Infrastructure supplies shared transaction participation. Superseded the command-level transaction check shown in an earlier illustrative code example. Opening-balance retrieval ownership remains unresolved.
- Affected IDs: PM-007; TQ-14. No implementation changes.
- TRD 0.132 becomes Draft 0.133; full-version approval remains outstanding.

### 2026-10-02 — Exact binary transaction GUID ordering

- Exact question: "Yes—**exact binary GUID ordering**. I propose comparing the 16 bytes returned by `Guid.ToByteArray()` from first to last. The first differing byte decides the order. SQLite must use the same byte sequence for sorting. Agree?"
- Accepted answer: "agree".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated the existing PM-007 cumulative ordering rule in place: compare parameterless Guid.ToByteArray() bytes lexicographically as unsigned bytes, identically in SQLite, and reverse that order for predecessor lookup. Removed binary-comparison uncertainty from TQ-14. This decision does not replace the separate catalog-order string comparison or select a global GUID storage migration.
- Affected IDs: PM-007; TQ-14. No implementation changes.
- TRD 0.133 becomes Draft 0.134; full-version approval remains outstanding.

### 2026-10-02 — DAL predecessor lookup supplies the range command

- Exact question: "Next: should **`ICumulativeOperation` read the preceding balance through a DAL query and pass it to the command**? The service still owns the transaction."
- Accepted answer: "Yes. We should have DAL function, where we can find "last" cumulative amount. Then pass it to the command".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated PM-007 in place: a read-only DAL function finds the last live Confirmed account entry before the inclusive start timestamp under the established descending date/binary-ID/Position ordering; ICumulativeOperation obtains its cumulative value and supplies the command. The established zero seed for an absent predecessor and service-owned outer transaction remain binding. Removed retrieval ownership from unresolved TQ-14 items; exact declaration details remain open.
- Affected IDs: PM-007; TQ-14. No implementation changes.
- TRD 0.134 becomes Draft 0.135; full-version approval remains outstanding.

### 2026-10-02 — Business supplies zero for an absent predecessor

- Exact question: "Next: should the DAL function return **zero directly when no previous entry exists**?"
- Accepted answer: "no. It's business responcibility".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated PM-007 and TQ-14 in place: DAL reports absence; Business ICumulativeOperation supplies zero when absent and passes the opening value to the command. Keep a found zero cumulative amount distinguishable from no predecessor. Exact return representation remains to be declared; defaulting ownership is settled.
- Affected IDs: PM-007; TQ-14. No implementation changes.
- TRD 0.135 becomes Draft 0.136; full-version approval remains outstanding.

### 2026-10-02 — Expose cumulative amount in transaction-entry read DTOs

- Request: "BTW. We should add CumulativeAmount to all appropriate DTOs".
- Source: requesting user, this chat, 2026-10-02.
- Added non-nullable decimal CumulativeAmount to the existing TransactionEntryInfo read record, matching the business model's account-currency representation. TransactionInfo consumes this shared entry DTO; DuplicateTransactionInfo also reuses it, but unsaved results cannot represent a persisted running balance. TransactionEntryParam, template DTOs and account-level totals remain unchanged because the cache is derived per persisted transaction entry, not writable data.
- Updated DTO-009 and the current read-contract mapping. No transaction-result mapper or transaction service implementation was found to populate the new field; persistence/recalculation and actual read projection remain future implementation work. Exact unsaved-preview semantics remain unresolved rather than copying a source cumulative value as valid for a duplicate.
- Affected IDs: DTO-009; PM-007; OP-026/029/057–060/063. TRD 0.136 becomes Draft 0.137; full-version approval remains outstanding.

### 2026-10-02 — Reload the current filtered transaction list after save

- Decision context: changing the fifth displayed transaction can change later cumulative amounts and can also change filter membership or ordering.
- Recommendation accepted: "reloading the current filtered list after a successful save"; recalculation covers all affected later database entries, while reloading only reads updated results.
- Accepted answer: "Agree".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated UC-005-03 and OP-027, with the read/DTO refresh rule beside the transaction projection contract. The client re-runs the active filter after successful commit rather than patching individual changed rows; save response shapes and unpaged version-one reads remain unchanged. The edited use case now follows the already-confirmed PM-006 Draft transition policy instead of its superseded rejection wording.
- Affected IDs: UC-005-03; DTO-009; OP-026/027/057–060. No implementation changes.
- TRD 0.137 becomes Draft 0.138; full-version approval remains outstanding.

### 2026-10-02 — Latest-first capped date-range transaction lists

- User direction: navigate by calendar or chosen-start date periods, configure a maximum transaction count (example 300), and show only that many results plus a warning when the requested range exceeds the limit.
- Exact decision question: "First decision: when more than 300 transactions match, which 300 should appear? I recommend the **latest 300**, displayed newest first, with transaction ID as the deterministic tie-breaker. Agree?"
- Accepted answer: "agree".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated shared transaction reads, DTO-002, OP-026/057–060, list-refresh scope and TQ-05/09. Preserve complete entry aggregates and cumulative history outside the display cap. Record period-definition and setting/DTO questions, plus proposed limit + 1 overflow detection, separately from accepted latest-first selection. No C# declarations or implementation changed; response wrapper alignment remains pending.
- Affected IDs: DTO-002/009; OP-026/057–060; TQ-05/09. BRD alignment remains outstanding.
- TRD 0.138 becomes Draft 0.139; full-version approval remains outstanding.

### 2026-10-02 — Transaction list cap is a constant

- Exact question: "Next: should this limit belong to **Local configuration**, so each device can choose its own value?"
- Accepted correction: "It's contant".
- Source: requesting user, this clarification chat, 2026-10-02.
- Updated the capped-read and refresh contracts plus TQ-05/09: the maximum transaction count is a fixed application constant, with no Local/System configuration field or settings UI. The numeric value remains unresolved because 300 was previously an example. Historical references to a configurable cap are superseded.
- Affected IDs: OP-026/057–060; TQ-05/09. No implementation changes.
- TRD 0.139 becomes Draft 0.140; full-version approval remains outstanding.

### 2026-10-02 — Initial transaction list cap of 300

- Exact question: "Should its value be **300 transactions**?"
- Accepted answer: "At the beginning yes. After that I will check performance and may be change this value".
- Source: requesting user, this clarification chat, 2026-10-02.
- Set the initial constant to 300 transactions in the capped-read contract and TQ-09; removed the numeric-value question from TQ-05. A later value change may follow the user's performance checks; no automatic tuning or runtime configuration is introduced. The limit applies to transactions, with complete entry collections and the already agreed latest-first selection and overflow warning.
- Affected IDs: OP-026/057–060; TQ-05/09. No implementation changes or performance measurements.
- TRD 0.140 becomes Draft 0.141; full-version approval remains outstanding.

### 2026-10-02 — Fresh selected-date navigation; no refill after editing

- User proposal: select a date and load up to 300 transactions through it; Today and +/- week/month/quarter/year buttons navigate. If an edit moves a transaction outside the selection, warn and retain 299 without filling the empty place.
- Decision context: the assistant distinguished fresh date-navigation reads from fresh complete reads of displayed IDs after edits, compared delta responses, and recommended fresh data rather than delta transfer.
- Accepted answer: "fresh". Source: requesting user, this chat, 2026-10-02.
- Updated shared reads, DTO-001, OP-026/057–060, UC-005-03, refresh behavior and TQ-05/09. Selected-date navigation replaces the earlier From/To list selection; filtered bulk deletion retains its separate range contract. Post-edit refresh preserves displayed membership except removals and never refills automatically. Retained rows receive fresh complete DTOs. Exact refresh contract and date-shift edge cases remain unresolved.
- No code changed. TRD 0.141 becomes Draft 0.142; full-version approval and BRD alignment remain outstanding.

### 2026-10-06 — Windows UI alignment started from BRD 0.175

- User authorization: "Let's start TRD", following the completed UI discussion. Repository instructions authorize expanding TRD as requirements stabilize. Source is Draft BRD 0.175 and individual confirmations; no full-version approval is inferred.
- TRD 0.159 becomes Draft 0.160. Replaced obsolete picker routing with five final modes across six catalogs and source-linked screen/service responsibilities. Aligned credentials-first Continue/Create/Back, Settings, reports, synchronization, Help and window behavior in existing use-case IDs.
- ReportGroup/report favorites and Local Add currency are required concepts with new persistence/DTO fields explicitly proposed. Existing five root GUIDs remain; the sixth identity/migration is unresolved.
- Recorded the missing selected currency in the documented OP-025 input and atomic Settings-save gap. Added distinct logical period-only mutation OP-070 with proposed DTO-036; it must preserve saved selection IDs and every non-date setting.
- Added TQ-15/16 for Windows foundations, controls, data/API extensions and existing source inconsistencies. Retired IDs stay reserved. Historical code snapshots are not current implementation verification. Handoff remains Not ready.
- Only TRD was edited. BRD, backend source, staged WinUI scaffold, SQL assets and tests are unchanged. No implementation or formal approval is claimed.

### 2026-10-06 — MVVM accepted; dialog/coordinator explanation requested

- Source: requesting user, this conversation: "OK. Agree with MVVM", following the comparison of WinUI XAML/code-behind and a separate ViewModel with Blazor component files.
- Decision: adopt the MVVM pattern for Windows UI. Layout/bindings are the View; initialization/UI-specific events remain code-behind; separate ViewModels hold state/commands; authoritative rules/persistence stay in the in-process services.
- Package/version remains proposed because acceptance names MVVM, not a specific library. The user requested technical questions one at a time and asks what a popup coordinator and ContentDialog mean; no dialog-hosting or DI-lifetime acceptance is inferred.
- Affected items: Windows UI integration, UI foundations and TQ-15. No business requirement, DTO/model/operation, backend code or scaffold change.
- TRD 0.160 becomes Draft 0.161; no full-document approval is inferred.

### 2026-10-06 — Owned dialog windows and shared popup coordinator accepted

- Exact question: "Agree with this approach?", after explaining one shared coordinator for the popup chain, owned WinUI windows for nested editors/pickers and ContentDialog for simple confirmations.
- Accepted answer: "agree."
- Source: requesting user, this conversation. Confirmed dialog hosting/coordination. A single coordinator manages opening, active-chain/routing checks, blocked parents and focus on closing. Each editor/picker keeps its own UI and ViewModel.
- The user then asks whether service lifetime means singleton UI popups. This is a clarification request, not acceptance of per-action Business/DAL scopes or singleton popup registration. Scope lifetime and toolkit/package remain open; technical questions continue one at a time.
- Affected items: Windows UI integration, UI foundations and TQ-15. No business/model/DTO/API or application-code changes.
- TRD 0.161 becomes Draft 0.162; no full-document approval is inferred.

### 2026-10-06 — Scoped Business/DAL graph with one scope per call

- User's clarification: DAL reads are AsNoTracking but writes attach entities; a subsequent call can otherwise encounter an entity already tracked. DbContext has Scoped lifetime, created for the API call and disposed at its end; repositories and services must also be scoped.
- Source: requesting user, this conversation. Confirmed existing scoped DbContext/repository/Business registrations and the call boundary. WinUI applies the equivalent explicit scope per logical service call, awaits it before disposal, and keeps only detached/materialized editor data between calls.
- Preserve a shared context within one call, but never retain its tracked entities/context/scoped services into the next public UI service call. Popup lifetime and the single coordinator are independent; no singleton popup registration is inferred. Existing compatible Shared singleton helpers remain unchanged.
- Updated UI foundations and TQ-15. The service-lifetime decision is settled; exact scope-runner wiring, toolkit/package and other UI technical gaps remain open.
- TRD 0.162 becomes Draft 0.163. No full-document approval or application-code change is inferred.

### 2026-10-06 — CommunityToolkit.Mvvm selected

- Source: requesting user, this conversation: "yes. obviously. manual looks awful", after the manual INotifyPropertyChanged setter and CommunityToolkit.Mvvm ObservableObject/ObservableProperty comparison.
- Decision: use CommunityToolkit.Mvvm for UI ViewModel notification/command infrastructure. WinUI examples use the documented generated partial-property form. Compatible package version remains undecided; no package installation or application-code change occurs in this turn.
- Updated UI foundations/TQ-15 and recorded the next unaccepted proposal: reusable decimal-preserving amount/rate input. Numeric business rules and precision remain unchanged.
- TRD 0.163 becomes Draft 0.164; no full-document approval is inferred.

### 2026-10-06 — Reusable decimal input selected

- Exact question: "We write the component once and reuse it in every amount/rate field. Agree with this approach?", after explaining that TextBox is built in, its decimal parsing/precision/validation behavior is application-owned, and NumberBox.Value is double.
- Accepted answer: "agree".
- Source: requesting user, this conversation. Use the reusable TextBox-based component for amount/rate input with direct decimal parsing, Windows regional separators and configured precision. Existing numeric business validation and empty-value rules remain binding.
- Updated Windows UI integration, UI foundations and TQ-15. Control choice is settled; detailed editing/commit behavior and other control choices remain open. No BRD, model/DTO/API, application code or package changes.
- TRD 0.164 becomes Draft 0.165; no full-document approval is inferred.

### 2026-10-06 — Built-in date and minute-only time controls selected

- User decision: "CalendarDatePicker - OK. TimePicker. Let's change requirements. Will use without seconds TimePicker is OK".
- Source: requesting user, this conversation. Use built-in CalendarDatePicker and TimePicker, keeping the 24-hour clock and replacing transaction UI HH:mm:ss with HH:mm.
- BRD 0.175 becomes Draft 0.176 to record the changed business display/input requirement. No custom seconds-capable time control is required. Existing UTC timestamp storage is not redefined, and existing seconds are not silently assigned a truncation policy; new/edited/unchanged time commit semantics remain TQ-15.
- Updated Windows UI integration, date/time controls and TQ-15. Report default-name timestamps, numeric precision, service contracts and application code remain unchanged.
- TRD 0.165 becomes Draft 0.166. No full-document approval or implementation authorization is inferred.

### 2026-10-06 — Seconds-on-save rule accepted

- Exact question: "Next: I propose seconds = 00 for new transactions or when time changes. If an existing transaction's time stays unchanged, preserve its stored seconds. Agree?"
- Accepted answer: "agree".
- Source: requesting user, this conversation. New/changed time saves seconds as 00; an unchanged existing time retains its stored seconds. The minute-only TimePicker must not make opening the editor or changing other fields count as a time change.
- Updated date/time controls and removed the seconds policy from TQ-15's open items. BRD 0.176 becomes Draft 0.177. UTC storage, existing data, report default-name format and date-only semantics remain unchanged.
- TRD 0.166 becomes Draft 0.167. No full-document approval, schema/API change or application-code change is inferred.

### 2026-10-06 — Flattened ListView tree accepted; indentation pending

- Source: user's TreeGrid.png and generic row-ViewModel proposal, followed by explicit agreement to visible collection binding, all-ancestor visibility with preserved expansion flags, nullable report check state and arrow-only expansion.
- Selected a ListView-based flattened hierarchy with application-owned row templates and visibility projection. Recorded mutually exclusive group/element rows, identity/parent association, aligned columns and retained existing tree interactions.
- User's item 3 says: "If we can make intention via monospace font, it's ok for me". Keep the exact indentation method open; do not infer approval of layout padding or a required monospace font.
- Updated Windows UI integration and TQ-15; BRD 0.177 becomes Draft 0.178 for confirmed interaction behavior.
- TRD 0.167 becomes Draft 0.168. No full-document approval, schema/API change or implementation completion is inferred.

### 2026-10-06 — Layout spacer indentation confirmed

- Requesting user accepted an empty first-column layout spacer sized as IndentLevel multiplied by a shared step. Normal fonts work; other columns retain their alignment. The illustrative 16-unit step is not a fixed requirement.
- TRD 0.168 becomes Draft 0.169. No full-document approval or application implementation is inferred.

### 2026-10-06 — Selection after collapse confirmed

- Requesting user agreed: when collapsing a group hides the selected row, select that group; selection elsewhere stays unchanged.
- Resume discussion in batches of three related questions at the user's request.
- TRD 0.169 becomes Draft 0.170. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Tree/table headers and overflow confirmed

- User accepted stationary column headers without sorting and single-line text with ellipsis/full-text tooltips. User rejected horizontal scrolling: users should make columns narrower.
- Width-configuration details remain open; no automatic resizing policy is inferred.
- TRD 0.170 becomes Draft 0.171. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Column setup and local preferences confirmed

- User accepted all three proposals: Columns popup with editable widths and Apply/Cancel, local per-view width persistence separate for main screens and selection popups, and Restore defaults.
- TRD 0.171 becomes Draft 0.172. No full-document approval or application implementation is inferred.

### 2026-10-06 — Percentage column widths and limits confirmed

- User accepted widths expressed as percentages totaling 100%, proportional resizing with the window, minimum widths protecting essential controls, and fixed column order with all columns visible in version one.
- The minimum-viewport fallback is still open; no horizontal scrolling is introduced.
- TRD 0.172 becomes Draft 0.173. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Minimum window width and cell alignment confirmed

- User accepted the minimum usable window/popup width and the proposed text/control alignment, adding that the currency-name string in the Account tree must be centered.
- User requested an explanation of the proposed deep-nesting indentation cap. It is not approved; finish this clarification before the next question batch.
- TRD 0.173 becomes Draft 0.174. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Fixed eight-level visual indentation cap confirmed

- User specified a constant limit of 8 indentation steps. Every deeper row is displayed at level-8 indentation; the underlying hierarchy is unchanged. This replaces the pending proposal for a width-dependent cap.
- TRD 0.174 becomes Draft 0.175. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Tree arrow-key navigation confirmed

- User accepted all three keyboard proposals: Up/Down moves through visible rows; Right expands or enters an expanded group; Left collapses or navigates to its parent, with no parent navigation at the root.
- TRD 0.175 becomes Draft 0.176. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Tree activation and checkbox shortcuts confirmed

- User accepted Enter matching double-click, Home/End selecting the first/last visible row, and Space toggling a report-tree checkbox with partial becoming fully checked.
- TRD 0.176 becomes Draft 0.177. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Tree refresh and failure behavior confirmed

- User accepted refresh on reactivation after saved catalog changes, retaining tree state subject to existing explicit reveal rules, and keeping old rows with Retry after refresh failure while disabling editing and selection confirmation. Cancel/Close remains available.
- TRD 0.177 becomes Draft 0.178. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Synchronous ordinary local loading and saving confirmed

- User rejected additional local loading UI and the proposed save-time busy controls, requesting synchronous execution because SQLite operations are expected to be fast. Do not infer removal of existing validation/error or refresh-failure rules; remote synchronization/setup keeps its distinct flows.
- TRD 0.178 becomes Draft 0.179. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Decimal editing and validation boundaries confirmed

- User accepted unfinished text during typing, validation/precision/rounding on focus loss or Save, and preserving invalid text with a field-level error while preventing Save until corrected.
- TRD 0.179 becomes Draft 0.180. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Plain numeric editing and focus behavior confirmed

- User accepted select-all on entering an amount/rate field and focusing the first invalid field on Save while preserving other entered values. User corrected editing to plain unformatted text, without regional formatting; formatting occurs after focus loss.
- Editing decimal separator remains to be clarified before closing this question batch.
- TRD 0.180 becomes Draft 0.181. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Dot decimal separator for editing confirmed

- User chose a dot as the decimal separator in plain amount/rate editing text, independent of Windows regional settings. Regional display formatting still applies after leaving a valid field.
- TRD 0.181 becomes Draft 0.182. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Numeric paste, keypad and syntax confirmed

- User accepted plain dot-decimal paste with surrounding whitespace ignored and currency/grouping symbols rejected, numeric keypad decimal normalized to dot, and simple decimal numbers without expressions or scientific notation.
- TRD 0.182 becomes Draft 0.183. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Popup placement, resizing and saved dimensions confirmed

- User accepted all three proposals: center over parent within the monitor work area; resize tree pickers, transaction/template editors and report results while keeping simple forms fixed; remember dimensions locally by popup type and catalog/mode for tree pickers, reopening centered.
- TRD 0.183 becomes Draft 0.184. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Main-window-only minimize and manual monitor adjustment

- User rejected stack minimization/restoration and automatic accommodation for a smaller monitor. Only the main window has a minimize button; users close all popups before adjusting the main window. This supersedes the prior automatic work-area position adjustment.
- User questioned the need for a taskbar. Clarify Windows taskbar terminology before selecting its application representation.
- TRD 0.184 becomes Draft 0.185. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Main-window Windows taskbar icon confirmed

- After clarification of Windows taskbar terminology, user accepted a normal taskbar icon for the main window and no separate popup icons. This does not introduce popup minimization or a stack-minimize command.
- TRD 0.185 becomes Draft 0.186. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Local JSON UI preferences confirmed

- User accepted local JSON storage under the Windows user profile without synchronization; save column widths on Apply, popup dimensions on closing, main-window geometry on exit; fall back to defaults for missing/invalid UI settings without affecting bookkeeping data.
- TRD 0.186 becomes Draft 0.187. No full-document approval or implementation completion is inferred.

### 2026-10-06 — Drag reorder, invalid-drop feedback and edge scrolling confirmed

- User accepted sibling reordering with an insertion line, keeping separate group/element sequences; invalid destinations show a not-allowed cursor with no mutation; dragging near viewport edges scrolls the tree. Existing move/merge gestures and mode restrictions remain binding.
- TRD 0.187 becomes Draft 0.188. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Favorites reordering allowed; placement specified

- User rejected disabling reorder under Favorites only. Insert immediately after the preceding visible sibling in the full sequence, or first when dropped at the start. The stated initial Order of 1 needs reconciliation with the existing zero-based Order contract.
- The other two questions in this batch, Escape canceling only a drag and retaining hover-expanded groups, remain unanswered; no approval is inferred.
- TRD 0.188 becomes Draft 0.189. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Drag cancellation and start-position clarification

- User answered yes to the first-position clarification, interpreted as retaining stored Order = 0 rather than changing numbering, and agreed to Escape canceling only the drag and preserving hover-expanded groups.
- TRD 0.189 becomes Draft 0.190. No full-document approval or implementation completion is inferred.

### 2026-10-07 — CSV structure and invariant values confirmed

- User accepted comma-separated CSV with quoting of commas, quotes and line breaks, and numbers with a decimal dot/no thousands grouping/configured precision plus yyyy-MM-dd dates, independent of Windows settings.
- User answered English to the UTF-8 with BOM proposal. Encoding and intended label language require clarification; no encoding approval is inferred.
- Affects report CSV under BR-028, UC-007-01 and TQ-16. TRD 0.190 becomes Draft 0.191. No full-document approval or implementation completion is inferred.

### 2026-10-07 — English technical text and original business language

- User clarified that all technical information in reports, logs and similar outputs is English; business information is in the user language. Preserve stored business text without translation. This resolves the language question, not CSV byte encoding.
- TRD 0.191 becomes Draft 0.192. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Report CSV UTF-8 with BOM confirmed

- User answered totally agree to using UTF-8 with BOM for CSV exports so business text supports any language. Previously confirmed English technical labels and unmodified business text remain binding.
- Affects report CSV under BR-028, UC-007-01 and TQ-16. TRD 0.192 becomes Draft 0.193. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Export layout, filename and summary encoding confirmed

- User accepted all three proposals: CSV header first with the complete report table and no title/date-range preamble; suggested ReportName_yyyyMMdd_HHmmss.csv using export time with forbidden filename characters replaced and user override; synchronization summary TXT in UTF-8 with BOM containing the displayed summary.
- Affects BR-028/043, UC-007-01, UC-012-01 and TQ-16. TRD 0.193 becomes Draft 0.194. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Empty exports, zero totals and report row ordering

- User accepted all three proposals: empty results remain exportable as headers only; include groups with matching transactions even when totals cancel to zero, excluding groups with no matches; date groups chronological and classification groups in catalog tree order, with CSV retaining displayed order.
- Affects BR-023/026/027/028 and UC-007-01. TRD 0.194 becomes Draft 0.195. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Report currency order, Unassigned and full paths

- User accepted currency catalog order and Unassigned first when matching data exists, then accepted full classification paths after clarification that these appear in report result grouping labels and CSV, not catalog trees or editors.
- Affects BR-026/027/028 and UC-007-01. TRD 0.195 becomes Draft 0.196. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Report sections, totals and negative amount presentation

- User accepted currency headings followed by rows with no expand/collapse, bold currency subtotals after sections and grand total at the bottom under existing calculation rules, and minus signs for negative amounts rather than parentheses.
- Affects BR-026/027/028 and UC-007-01. TRD 0.196 becomes Draft 0.197. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Executable-adjacent daily diagnostic logs

- User accepted daily UTF-8 yyyy-MM-dd.log files and seven-day cleanup at startup and daily, correcting the location to an application-created Logs directory beside the executable.
- File location is accepted; deployment compatibility with read-only MSIX installation directories needs clarification. No fallback or packaging change is inferred.
- Affects BR-043, OP-051/052 and TQ-08/09. TRD 0.197 becomes Draft 0.198. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Unpackaged first Windows release confirmed

- User accepted running the first Windows version unpackaged from a writable folder, resolving the executable-adjacent Logs location compatibility question. Runtime bundling and delivery format remain unselected.
- Affects Windows deployment and TQ-08/09. TRD 0.198 becomes Draft 0.199. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Diagnostic log clock, retention boundary and secret exclusion

- User accepted all three proposals: local-date filenames and local timestamps with UTC offsets; keep today plus the preceding six calendar dates; exclude passwords, authentication tokens and authorization headers while including operation names and error details.
- Affects BR-043, OP-051/052 and TQ-08. TRD 0.199 becomes Draft 0.200. No full-document approval or implementation completion is inferred.

### 2026-10-07 — ZIP delivery and bundled runtimes confirmed

- User accepted ZIP application-folder delivery and bundled .NET/Windows App SDK runtimes. On the x64 proposal, user replied that this is .NET and compiled libraries will not be distributed; clarify managed compilation versus native dependency architecture before selecting a target.
- Affects Windows deployment and TQ-09. TRD 0.200 becomes Draft 0.201. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Managed .NET compilation and initial x64 delivery

- User agreed to ordinary .NET compilation without Native AOT and an x64 ZIP initially, after clarification of IL versus architecture-specific runtime/native dependencies.
- Affects Windows deployment and TQ-09. TRD 0.201 becomes Draft 0.202. No full-document approval or implementation completion is inferred.

### 2026-10-07 — Local database location and manual update delivery

- User accepted all three proposals: local SQLite database under Data beside the executable; release ZIP excludes user databases/logs/personal settings; manual updates replace application files with the app closed while preserving data/logs/settings, without a version-one built-in updater.
- Affects Windows deployment and TQ-09. Schema compatibility/migration remains TQ-06. TRD 0.202 becomes Draft 0.203. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Database compatibility and automatic migration policy

- User accepted all three proposals: stored schema version checked before opening/accepting a downloaded copy; unsupported newer schema shows Please update the application without changing the database; older schema uses a provided migration automatically, fully applied or rolled back, with no mutation when no migration is available.
- Affects all persistent models, snapshot operations and TQ-06. Representation and synchronization upgrade coordination remain open. TRD 0.203 becomes Draft 0.204. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Schema-version integer, atomicity and pending changes

- User accepted all three proposals: an increasing schema-version integer separate from application version and changed only for compatibility changes; update that integer in the migration transaction; preserve pending changes and synchronization flags during schema-only migrations without marking all records modified.
- Affects all persistent models, migration contract and TQ-06. Physical version storage and synchronization coordination remain open. TRD 0.204 becomes Draft 0.205. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Schema compatibility at synchronization and Master ownership

- User accepted all three proposals: matching Local/Master schema versions before exchanging business changes; mismatch stops synchronization with pending work preserved and an explanation of which side needs updating, without automatic downgrade; only the Master service migrates Master.
- Affects migration/snapshot operations and TQ-06. TRD 0.205 becomes Draft 0.206. No full-document approval or implementation completion is inferred.

### 2026-10-08 — SQLite user_version and initial database handling

- User conditionally accepted all three proposals if SQLite has built-in user_version. SQLite documentation confirms that application-managed header integer. Record its use, explicit known-structure migration for existing version-0 databases, and current structure/version creation together before new database use.
- Affects migration contract and TQ-06. TRD 0.206 becomes Draft 0.207. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Initial schema version and multi-step migration failures

- User accepted version 1 for the first released schema, sequential migrations within one transaction for the entire upgrade chain, and source/target versions plus a short error with technical logging and no automatic retry on failure.
- Affects migration contract and TQ-06. TRD 0.207 becomes Draft 0.208. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Local startup and isolated Master migration

- User accepted all three proposals: check/migrate Local before Ledger or startup sync; serialize Master migration with synchronization using the same per-dataset lock; migrate a candidate Master copy and activate it only after success, preserving the current copy on failure.
- Affects migration/snapshot contracts and TQ-06. TRD 0.208 becomes Draft 0.209. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Master upgrade trigger, snapshot version and sync status

- User accepted migration on first dataset access after updating Master service, publication as a new snapshot with older snapshots unchanged, and no successful-sync/expiry-renewal/Recovery-required reset merely from migration.
- Affects migration/snapshot contracts and TQ-06. TRD 0.209 becomes Draft 0.210. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Non-expired Local2 upgrade with pending business changes

- User confirmed the explained sequence: update Local2 application, migrate its existing local database preserving pending changes/flags, then synchronize with matching Master schema. Never replace it from Master before merging those changes. The explanation explicitly assumes Local2 is not expired and retains Master-service migration ownership.
- Affects migration acceptance scenario and TQ-06. TRD 0.210 becomes Draft 0.211. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Windows 11 and stable runtime/package policy

- User answered No to needing Windows 10, Yes to using the backend .NET major version, and agreed to stable explicitly versioned Windows App SDK/CommunityToolkit.Mvvm packages without previews.
- Affects Windows deployment and TQ-09/15. Exact compatible versions remain to be selected. TRD 0.211 becomes Draft 0.212. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Sync-only remembered token authorization

- User clarified that authorization is for synchronization, not local work, then accepted a server-issued remembered token without saving the password, current-user Windows DPAPI protection, and login again on expired/rejected authorization while preserving local data and pending changes.
- Affects BR-005 and TQ-03. TRD 0.212 becomes Draft 0.213. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Per-Local tokens and same-account reauthorization

- User accepted all three proposals: independent authorization token per registered Local copy; read-only existing login and empty password for reauthorization to the same Master account; canceling login cancels sync but preserves local work and offline access subject to existing expiry/recovery rules.
- Affects BR-005 and TQ-03. TRD 0.213 becomes Draft 0.214. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Token file, missing-token recovery and network failures

- User accepted a separate DPAPI-protected token file under the Windows user profile outside database/UI JSON, reauthentication for missing/unreadable tokens retaining the current Local registration/database, and keeping tokens on connection failures rather than treating those failures as rejected authorization.
- Affects BR-005 and TQ-03. TRD 0.214 becomes Draft 0.215. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Random token, hashed Master storage and replacement

- User accepted all three proposals: cryptographically random 256-bit token with no login/business data; Master persists only its SHA-256 hash associated with owner and Local registration; successful reauthorization replaces and invalidates only that Local copy's previous token.
- Affects authentication contracts and TQ-03. Token encoding, exact schema and lifetime remain open. TRD 0.215 becomes Draft 0.216. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Token lifetime, authoritative clock and ownership scope

- User accepted all three proposals: fixed 90-day token lifetime from issue without per-sync extension, Master UTC clock determines expiration, and every synchronization request checks token ownership of the owner/Master dataset/Local registration. Token lifetime is distinct from Local-copy expiry.
- Affects BR-005 and TQ-03. TRD 0.216 becomes Draft 0.217. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Login case/space rules and exact passwords

- User accepted all three proposals: case-insensitive login matching; trim leading/trailing login spaces before account lookup/creation; case-sensitive password with no trimming or other normalization.
- Affects BR-003/005 and TQ-03. TRD 0.217 becomes Draft 0.218. No full-document approval or implementation completion is inferred.

### 2026-10-08 — PasswordHasher, rehash after login and temporary throttling

- User accepted ASP.NET Core PasswordHasher with salted hash storage, upgrading old hashes after successful verification when requested by the hasher without changing the password, and temporary throttling of repeated failed login attempts without permanent account lockout.
- Affects BR-005 and TQ-03. Exact work factors and throttle policy remain open. TRD 0.218 becomes Draft 0.219. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Login throttling removed from version one

- User rejected all three throttle-detail questions, specifying no limit in the first version. This supersedes the earlier acceptance of temporary throttling; remove attempt limits, cooldowns and failed-attempt lockout from active version-one requirements. Accepted password hashing and rehash behavior remain unchanged.
- Affects BR-005 and TQ-03. TRD 0.219 becomes Draft 0.220. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Master endpoint configuration and token address boundary

- User accepted all three proposals: local-file Master URL configuration without a UI editor, HTTPS for remote connections with HTTP only for localhost development, and reauthorization on address changes without sending the saved token to a different address or losing local data/pending changes.
- Affects BR-005, cloud connection/authentication contracts and TQ-03. TRD 0.220 becomes Draft 0.221. No full-document approval or implementation completion is inferred.

### 2026-10-08 — Endpoint reload, configuration errors and dataset identity

- User accepted all three proposals: read Master URL at startup with restart required for file changes; missing/invalid URL reports a synchronization configuration error without blocking otherwise permitted local work; reauthorization at a different server must match the existing MasterDatasetKey, not merely the login.
- Affects BR-005 and Master connection/authorization contracts. TRD 0.221 becomes Draft 0.222. No full-document approval or implementation completion is inferred.

## Approval

- Current decision: **Not submitted**.
- Exact TRD version reviewed: none.
- Intended approver: requesting user.
- Decision date: not applicable.
- Scope: consolidated technical requirements, detailed use cases, proposed schemas and API/BFF contracts.
- Next step: implement the scoped Business UI flow against an existing Local database when implementation starts; discuss only concrete integration blockers. Other subdomains remain deferred.


### 2026-10-03 — Initial cumulative implementation and validation

- Authorization: requesting user, "Cool. Can we start implementation?" Implement the agreed cumulative maintenance and selected-date transaction reads. This is scoped implementation authorization, not approval of all outstanding TRD proposals.
- Added ICumulativeService.Rebuild for outer callers and internal ICumulativeOperation full/ranged methods. IAppUnitOfWork exposes only repositories. ITransactionEntryRepository.GetPrevious supplies the nullable preceding amount through the EF repository implementation. ICumulativeAmountCommand is injected directly into CumulativeOperation; its implementation resides in DataAccess.EntityFramework.SqLite. Business supplies zero for an absent predecessor.
- TransactionService owns begin/save/recalculate/commit and rollback in the same function. Add, replacement Update, both Draft/Confirmed transitions, Delete and uncapped filtered deletion maintain affected account histories. Account combination rebuilds the destination within its service-owned transaction. Operation and command never inspect transaction context.
- The SQLite command uses one parameterized statement per account. Its window SUM includes an artificial opening-balance row, keeping arithmetic integral and raising overflow rather than promoting an overflowing seed-plus-sum expression to floating point. Deleted entry caches are left untouched; all live Draft caches in scope become zero. Cache writes do not alter synchronization flags.
- SQLite transaction IDs and their entry foreign keys use Guid.ToByteArray() BLOB storage; account/entry IDs retain their current representation. Amount and CumulativeAmount use signed int64 scaled by 10,000. Existing database files require migration; this change does not migrate or overwrite them. General schema migration remains separate work.
- Implemented full fresh DTO reads, constant MaxTransactionListCount = 300, 301-row overflow detection, newest-first ordering and refresh by displayed IDs without refill. Refresh returns removed IDs for the UI warning; it does not recompute navigation LimitExceeded. The UI should retain navigation metadata until the next navigation query. DateOnly boundaries use the local time zone and are translated to UTC; stored transaction timestamps must be UTC.
- Rebuild completes atomically for all live accounts. The synchronization download/publish workflow must invoke it before exposing a candidate database; that workflow and restart recovery are not implemented in this change. Frontend date-navigation buttons and warning presentation remain UI work.
- Validation: full solution builds with zero warnings/errors; 2,253 existing service tests, 45 existing business unit tests and 10 new local SQLite tests pass. SQLite tests cover binary ID/Position order, Draft/deleted exclusion, ranged seed behavior, old/new account/date changes, state transitions, deletion, uncapped bulk deletion, 300-row navigation/no-refill refresh, account combination, and atomic overflow rollback for add/update/combine.
- Historical statements above that describe these particular contracts or implementations as pending are superseded by this implementation record. Remaining TQ items outside this scope retain their prior status. Account base-currency totals remain calculated from historical entries; no CumulativeBaseAmount was introduced.

### 2026-10-03 — Repository lookup and independent command injection

- User correction: move GetPrevious to ITransactionEntryRepository and remove ICumulativeAmountQuery. The lookup retains its nullable result, scalar projection, ordering and filtering; Business still supplies zero when no predecessor exists.
- IAppUnitOfWork exposes repositories only, alongside its inherited unit-of-work lifecycle. ICumulativeAmountCommand is constructor-injected into CumulativeOperation independently. Service-owned transaction boundaries remain unchanged.


### 2026-10-03 — Dispose context after rollback

- User confirmed that rollback must dispose DbContext. UnitOfWork disposes the transaction and context in finally blocks, including when rollback fails. It does not clear the change tracker to permit context reuse.
- After rollback, callers must discard the service scope and resolve a new scope/context for further work or an explicit retry. SQLite overflow tests verify that the old context rejects use and that a fresh context sees the rolled-back database state.


### 2026-10-03 — Complete Business reads and numeric mappings; correct settings ownership

- User explicitly confirmed existing Business behavior: clearing persisted BalancingAccountId belongs to Setup, potentially through a remote API. Business can only read settings and correct its own detached read result. This supersedes historical decisions requiring Business deletion/combination to clear settings atomically.
- Implemented IAccountGroupService.GetAccountsTree with live groups/accounts, stable ordering, currency labels and detached results. Implemented and registered IConfigService.GetConfiguration through the existing ConfigOperation; missing singleton data fails, and unavailable balancing-account references produce null without writes.
- SQLite now maps TransactionEntry.Rate, CurrencyRate.Rate and TemplateEntry.Amount using exact signed int64 scaling by 10,000, in addition to TransactionEntry.Amount and CumulativeAmount. The same checked converter rejects excess fractional digits and overflow. Existing database schema/data migration remains required separately.


### 2026-10-03 — Remove public Business configuration service

- User correction: Business uses IConfigOperation internally; external configuration access belongs to Setup. Removed Business IConfigService, ConfigService, its DI registration and its two service-facing SQLite tests. IConfigOperation and its DTO remain available for internal use.
- Retired SVC-013 and OP-065 without reusing their IDs. Earlier decisions introducing the public Business service are superseded. No configuration endpoint or registration existed in Dehb.WebApi, so no Web API code required removal.


### 2026-10-03 — Expose transaction and account-tree Web API operations

- Added routes for every ITransactionService operation: aggregate add/update/delete, all five selected-date lists, all five range deletions, displayed-ID refresh, duplication, and both account-balance reads. Added the separate /account-groups/get-accounts-tree read with currency labels.
- Ordinary reads use GET; mutations use POST with JSON inputs. /transactions/refresh-transactions is a read-only POST accepting TransactionRefreshParam because up to 300 GUIDs can exceed common request-line limits. No recalculation or save occurs during refresh. HTTP handlers delegate Business validation and transaction ownership to the service.
- Verified route initialization and HTTP binding using an in-memory ASP.NET Core host, including populated entry lists, 300 refresh IDs, DateOnly account filtering, bulk-delete identity/date forwarding and account-tree service dispatch. No new decision about balancing-editor or favorites-filter ownership is inferred from this API change.


### 2026-10-03 — Frontend owns favorites filtering

- User confirmed that filtering by IsFavorite belongs to the frontend. Business supplies flags and hierarchy identities; the frontend retains favorite rows and necessary ancestor paths under BR-044. This is not an unfinished Business service.


### 2026-10-03 — Frontend owns the unsaved balancing-entry action

- User confirmed that Add balancing entry is a frontend responsibility. On explicit click, the frontend calculates the rounded base-currency total and appends its negative using the configured balancing account and Rate = 1. The existing missing/deleted-account warning and no-change rule remain binding.
- This only changes unsaved editor data. Business validates the complete submitted transaction and derives its state during the ordinary Add/Update operation. No additional Business service or Web API endpoint is required. Earlier unresolved ownership notes are superseded.


### 2026-10-03 — Consistent method naming without Async suffix

- Application-owned asynchronous methods omit the Async suffix, except Shared library wrappers described below. Existing synchronous counterparts use Sync where necessary to prevent overload ambiguity outside those wrappers; return types and asynchronous behavior are unchanged.
- Framework/library calls and required interface members retain their defined names, including IAsyncDisposable.DisposeAsync and IExceptionHandler.TryHandleAsync. The solution editor configuration no longer requires the Async suffix. HTTP route names are unchanged.


### 2026-10-03 — Shared file and directory wrapper naming exception

- User confirmed that IFileService and IDirectoryService wrap library functions and therefore retain Async suffixes on asynchronous methods, matching the wrapped library naming convention. Their synchronous methods keep the corresponding unsuffixed names.
- Apply this exception consistently to the contracts, implementations, mocks and callers. It overrides the general application-method naming rule for these two Shared wrappers. The user has already reverted their names; this change records the convention only and makes no code changes.

### 2026-10-03 — Consolidated API/BFF declaration notes

The former docs/reviews/api-service-contract-additions.md note, based on TRD 0.104, is consolidated here. The following records existing declaration choices, not completed implementations, dependency-injection registrations, an approved wire protocol or resolution of open requirements. Current TRD decisions take precedence over the historical note.

| Interface | TRD operations |
| --- | --- |
| IStartupService | OP-001–004 |
| ISynchronizationService | OP-043–049 |
| IDiagnosticsService | OP-050–052 |
| IReportsService | OP-037–040 |

- Setup: CreateBooks and OpenBooks use separate input records. Creation includes immutable AmountPrecision/RatePrecision, defaults 2/4 and accepted range 0–4. RequestId represents the proposed setup-attempt identity; retry semantics remain unresolved.
- Synchronization: Synchronize is the local application-facing BFF action. Its input supplies the trigger; implementation obtains owner/registration, known revision and priority from current context and durably captures pending changes. This contract does not define DTO-019 as a cloud upload payload or authorize exposing arbitrary objects/persistent models. TQ-04 still requires a typed change and ordering protocol before cloud synchronization implementation.
- SynchronizeInfo declares status and an optional transfer for the unresolved no-change case. Completion must follow the outcome state; null Transfer alone is not evidence of success.
- DownloadSnapshotInfo declares transfer metadata and a caller-owned Content stream. The transport adapter streams bytes outside JSON; activation and acknowledgement remain separate steps. ReplaceExpiredCopyInfo declares the replacement LocalDatasetKey alongside its transfer so the caller can retain the new registration.
- Diagnostics: GetLatestSyncReport currently returns a nullable result, with null representing no in-session report. WriteDiagnostic uses OperationKey for its string correlation key, rather than OperationId. These are current declaration choices; remaining diagnostic contracts and error details retain their open-question status.
- Reporting: declarations use typed selection, definition and row records. SaveReportDefinition.Id is nullable for create/update. The report JSON persistence format and rename-only selection preservation remain unresolved.
- Superseded timezone proposal: the historical note and current CalculateReport declaration contain TimeZoneKey. The later confirmed device-timezone decision already makes this parameter unnecessary. Remove it when Reporting contracts are aligned; do not restore the obsolete timezone proposal as a requirement.

Security, durable synchronization protocol, report serialization and other unresolved questions are not settled by this consolidation. The redundant review file is removed; no application code changes are included.

### 2026-10-03 — Temporary documentation artifacts and Git tracking

- /docs/reviews/ and /docs/validation/ are ignored by Git. They hold temporary review notes, validation output, test logs and one-off tooling, not authoritative project documentation.
- Preserve lasting technical decisions in docs/requirements/TRD.md and applicable business requirements in docs/requirements/BRD.md before removing useful temporary notes. Do not rely on ignored files as the only record of a requirement or decision.
- Temporary content may be removed when no longer needed. Source code and maintained test projects remain in the backend solution and are not covered by these ignore rules.


### 2026-10-04 — SQLite and Web API configuration

This section records the user-confirmed first-version configuration and the current implementation. It supersedes the 2026-10-03 initialization notes, including the old connection key, automatic initialization flags, SQL asset copying and request-scoped logger lookup. It does not approve unresolved requirements elsewhere in this Draft.

#### SQLite schema and manual initialization

- All explicit EF model configuration resides in SqLiteAppDbContext. AppDbContext retains entity sets and constructors; AppUnitOfWork does not configure the model.
- All 17 physical table names are singular, matching their entity names. EF mappings, raw SQL, foreign keys and indexes must agree. Quote SQL identifiers such as "Transaction", which is a SQL keyword.
- Transaction.Id and TransactionEntry.TransactionId use BLOB storage based on Guid.ToByteArray(), preserving binary ordering. Other identifiers retain their configured text representation. Monetary values and rates use signed integers scaled by 10,000; AmountPrecision and RatePrecision govern calculation and display, independently of that fixed storage scale.
- The user manually initializes and migrates databases for the first version. WebApi startup does not call SqliteDatabaseInitialization, EnsureCreated or EF migrations. Existing plural-table databases require an explicit manual migration or recreation; changing mappings does not rename existing tables.
- Maintained SQL scripts are in backend/DataAccess.EntityFramework.SqLite/Assets: 001-schema.sql, 002-initial-data.sql and optional 003-test-data.sql. Apply schema and seed scripts in that order to an empty database, with foreign keys enabled. Use a transaction around manual initialization. These scripts are not repeatable migrations.
- Initial data includes five canonical roots, UAH with initial rate 1, Rebalancing, SystemConfig and LocalConfig; AmountPrecision=2 and RatePrecision=4. Optional sample data adds USD/rate, child groups, classifications, accounts, a template and balanced confirmed transactions with cumulative amounts. Fixed dataset identities belong to the local demo.
- SQL assets are maintained for manual execution; automatic copying to host build/publish output is no longer part of initialization. SqliteAssetsTests is an isolated schema/seed validation test, not application initialization. Its output-asset dependency currently needs alignment with manual asset handling; the latest broad test run excluded it.

#### Connection configuration

- WebApi reads ConnectionStrings:DehbSqLite through GetConnectionString("DehbSqLite") and passes the value to UseSqlite. The former top-level SqLiteConnectionString and ConnectionStrings:AppDb keys are superseded.
- Supply a complete SQLite connection string, never just a file path. The current checked-in development value is:

~~~json
{
  "ConnectionStrings": {
    "DehbSqLite": "Data Source=bin/Debug/net10.0/Assets/dehb.db"
  }
}
~~~

- A relative Data Source is relative to the process working directory. The current DI code does not rebase it against the WebApi content root. For other launch locations, release builds or published deployments, configure the appropriate path or an absolute path.
- Standard configuration overrides apply, including environment variable ConnectionStrings__DehbSqLite and command-line key ConnectionStrings:DehbSqLite. Development settings can override appsettings.json.
- SQLite DI currently does not explicitly set Foreign Keys in the connection string; enforcement therefore follows provider/native defaults. Use Foreign Keys=True when explicit connection-level enforcement is required, and enable PRAGMA foreign_keys=ON in the manual SQL session before its transaction.
- The operator supplies the initialized database at the configured path. Do not interpret successful process startup as proof that the schema exists or matches the model.

#### Dependency injection and exception handling

- DbContext uses scoped registration. DI creates it when first resolved and disposes it at the end of the request scope; it is not eagerly created for every HTTP request. Repositories, UOW, cumulative command, Business operations and services remain scoped. Services/adapters that depend on these objects must not capture them in singletons.
- Service methods own transaction creation and commit through UOW. Request scope ownership does not make every request one database transaction. The established rollback behavior disposes the context; further database work then requires a new scope/context.
- Shared singleton registrations: IDateTimeService, IDirectoryService, IFileService, IZipService, ITlsService, IXmlService, IJsonService, ILogService and IMessageService.
- These implementations do not capture request-scoped dependencies. ZipService depends on singleton logging; TlsService uses AsyncLocal logical-flow storage; the in-process publisher uses IServiceScopeFactory and creates a scope per dispatch without retaining handlers. Singleton replacements must retain compatible dependencies and concurrency behavior.
- ISharedContext remains scoped so scoped replacements remain possible. A singleton dependent such as ZipService still requires a singleton-compatible logger replacement.
- AddExceptionHandler registers WebApiExceptionHandler as singleton. It constructor-injects singleton ILogService; request-time service lookup is no longer needed. The current NullLogService is a no-op, not a persistent logging sink.
- Program registers problem details and exception handling. Known business exceptions are mapped to their configured HTTP status; unexpected errors return a generic 500 response.

#### Console host and API clients

- Run from backend/Dehb.WebApi with dotnet run --launch-profile http after manually preparing the database. The http launch profile listens on http://localhost:5290. The https profile uses https://localhost:7250 and http://localhost:5290. Both profiles select Development.
- Program enables HTTPS redirection and exception handling, then maps endpoints. OpenAPI is mapped only in Development. Launch-profile ports are development settings, not fixed production endpoints.
- HTTP JSON uses camelCase names, case-insensitive input property matching, no default-value omission and Populate object creation handling. Shared IJsonService is a separate serializer configuration; its converters do not automatically configure HTTP JSON.
- The Dehb Postman collection is maintained at backend/Dehb.WebApi/Assets/Dehb.postman_collection.json, with service folders and function-named requests. Every URL uses {{host-name}}. Set that collection variable to the chosen API root including http:// or https:// and without a trailing slash.
- Earlier initialization commands in manual-test notes that use InitializeDatabase or SeedTestData are superseded by this manual workflow.
- Latest lifetime validation checked the production DI graph with scope validation enabled and singleton reuse across two scopes. The broad run passed 2,311 tests, excluding the SQL-assets test described above. This is historical validation evidence, not a guarantee for subsequent code changes.
