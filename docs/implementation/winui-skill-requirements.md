# WinUI application skill — reusable requirements brief

## Purpose and authority

This document is input for an agent that will create a Codex skill for developing WinUI desktop applications. It extracts the user's project-independent preferences from an implemented application and its requirements. It is not a SKILL.md, an installed skill, or a request to modify application code.

Source: the user's confirmed decisions in this conversation, the [TRD](../requirements/TRD.md) revision 0.233, and the implementation used to resolve details. Current decisions supersede historical requirements for percentage-based columns, inline column settings, resizable editors and input-field tooltips.

These are this user's application conventions, not universal Microsoft recommendations. A future project's explicit requirements take precedence. Do not copy the source application's business model, names, service APIs or database architecture.

Use the established spelling **uisettings.json**. The request's `uasetting.json` is interpreted as referring to that existing file, not as a requested rename.

## How to turn this brief into a skill

- Make the core interaction rules below the default for data-entry WinUI applications.
- Apply conditional patterns only when the target application has the corresponding feature.
- Keep project-specific values and command bindings in project configuration/documentation rather than embedding them into the skill.
- Separate required observable behavior from an implementation technique. Reuse a target project's suitable infrastructure rather than replacing it merely to match the source implementation.
- Include the verification scenarios at the end. A successful build does not prove focus, modality, scrollbar or tooltip behavior.
- Do not require confirmation for routine implementation already authorized by the user. Surface a genuine conflict with a target project's requirements instead of silently overriding it.

## 1. Editing and modal windows

### Core requirements

- Prefer dedicated Add/Edit popup editors over inline editing in management grids and trees. This does not prohibit editable detail rows inside a dedicated editor.
- Reuse one editor for Add and Edit when their fields and layout are the same. Use one settings object for their shared size. Separate settings are appropriate when layouts materially differ.
- Editor and selection popups are fixed-size modal windows. Show only the Close title-bar button: no minimize, maximize or user resizing.
- Read width and height from UI settings. Treat dimensions as logical pixels and apply DPI scaling consistently.
- Use owned windows for nested editors and selectors. Simple confirmations may use ContentDialog.
- Only the top modal accepts interaction. Its parent stays blocked until the child closes. Restore the parent's focus on close.
- Keep one application taskbar entry; do not expose each popup as a separate taskbar window.
- Returning from another application through the taskbar must reveal the active top modal above its owners. A blocked main window with an inaccessible hidden popup is unacceptable.
- Center the popup over the main window, accounting for the monitor work area. Do not silently change configured dimensions or compress fields.
- Keep unsaved edits in the editor until Save. Cancel must not commit them; closing a dirty editor uses the application's discard-confirmation policy.
- Validation failure preserves entered data, keeps the editor open, reports the error and focuses the first invalid field where applicable.

### Layout

- No scrollbar around the whole editor. Keep its footer visible.
- Use 24 logical-pixel left/right content margins and a 12-pixel label-to-control gap as the preferred form baseline. Keep inter-field spacing consistent, including controls with different native templates.
- Put each ordinary field on its own row. Explicit detail-row layouts are an exception.
- Stretch controls between the content margins; avoid independent hardcoded widths that cause clipping or uneven alignment.
- Align Save and Cancel together at the bottom left. Put auxiliary editor commands at the bottom right on the same footer line.
- Size Save/Cancel from shared appearance settings. Ensure chosen font and button sizes remain usable.
- Use shared layout and popup infrastructure so all editors inherit these rules, including less frequently opened editors.

Fixed sizing does not justify clipped controls: validate intended font sizes, DPI scales and minimum supported display area. The target project must define any behavior for a display smaller than the configured popup; do not invent an automatic resizing policy.

## 2. Portable UI configuration

### Storage and lifecycle

- Keep appearance configuration separate from business/application configuration. Continue using appsettings.json for application configuration.
- Store `Assets/uisettings.json` and `Assets/uisettings.default.json` in the project and in build/publish output.
- Resolve both relative to the executable directory, not the current working directory or a hardcoded machine/user path.
- The active uisettings.json is the single source of runtime appearance values. The default file is a distribution copy for manual restoration, not an automatic fallback.
- Load once at startup before constructing the normal UI. Changes take effect after restart. No settings editor or live reload is required by this baseline.
- Use a plain `UiSettings` data model and a separate `UiSettingsLoader` for loading/validation. The loader may be static.
- Do not automatically rewrite active settings. Fresh output receives both files; rebuilding/publishing/updating must preserve an existing user-edited active file.
- Do not maintain competing column-width stores, profile-based appearance files or database copies of the same values. Runtime window position/geometry, when persisted, is separate from user-authored appearance settings.

### Validation and failure behavior

- Use a versioned schema and required typed properties.
- Reject malformed JSON, duplicate/unknown properties, missing required properties, null required objects, unsupported versions and incorrect value types.
- Require finite positive dimensions and valid RGB colors in #RRGGBB format. Popup dimensions are positive integers. Row height must accommodate the configured font.
- Show a startup error with the active file path and useful parser/property detail. Do not open the normal workspace with partially loaded or silently substituted settings.
- The initial recovery experience is an error message explaining manual correction/restoration. Do not introduce automatic repair, silent defaults or recovery buttons unless requested.
- Do not overwrite user settings to resolve a new required-property mismatch during an update. Configuration migration/version compatibility must be decided for that project's release policy.

### Configuration categories

| Category | Reusable fields/purpose |
| --- | --- |
| Root | version |
| appearance | fontSize, rowHeight, buttonWidth, buttonHeight |
| Appearance colors | backgroundStartColor, backgroundEndColor, selectionBorderColor, focusBorderColor, readonlyColor, errorColor |
| Appearance outlines | selectionBorderThickness, focusBorderThickness |
| mainWindow | Sidebar color/font/item height and applicable window dimensions |
| Named grids | Named logical-pixel column widths; optional filter-control widths |
| Named popups | width and height; extra field widths only where the popup requires them |

Use the JSON key `appearance`; type names and project namespaces must follow the target repository's conventions. Choose semantic names such as `itemEditor`, `itemPicker` and `itemsGrid`, not names copied from the accounting application.

Exact colors, dimensions and font sizes are project defaults. They are not mandatory values of the skill. The baseline readonly/error color examples are #E7F1FF and #FF0000.

## 3. Consistent appearance and readonly data

- Apply shared font size throughout views, editors, inputs, menus, row text and headers. Give sidebar text its own configurable font size. Icon sizes may remain independent.
- Apply configured row height to every list/tree row, not only selected views.
- Use the same selection appearance in every grid/list/tree. Configure keyboard-focus color/thickness separately from selection.
- Make focus borders consistent for textboxes, comboboxes, decimal inputs and date/time controls. Remove conflicting red underlines, thick default focus rectangles and bottom-only dark borders where they violate the configured style.
- Checkbox focus outlines must include suitable padding and must not clip the checkbox glyph or label.
- Use readonly styling instead of disabled styling when the value must remain readable, focusable, selectable and copyable. Prevent typing without preventing selection/copy.
- Apply readonly background in normal, hovered and focused states, including composite decimal controls.
- For an immutable choice/date whose native control cannot provide the desired readonly interaction, display its value in an enabled readonly textbox rather than a disabled combobox/date picker.
- Use errorColor for editor errors and field-validation messages. Do not replace the user's input with a default on error.
- When a gradient background is supported, equal start/end colors must produce a solid background.

## 4. Reference-selection controls and picker behavior

Apply when a field references another record or group:

- Show the chosen value in an enabled readonly textbox.
- Place a square choose button (`...` or the supplied Select icon) to its right. Match the textbox height.
- If clearing is allowed, add a square clear button (`x` or the supplied Clear icon).
- Choosing opens a modal list/tree picker; editing the textbox directly must not change the reference.
- On opening, focus the picker grid immediately. Select the current reference, expand all required ancestors and scroll it into view.
- If there is no valid current reference, select the first available row when nonempty. Disable acceptance for ineligible rows. An empty picker has no selection.
- Preserve the distinction between selection and editing: Enter selects; Shift+Enter edits only where that picker permits editing.
- Closing the picker returns focus to the invoking editor control/row. Cancellation preserves the prior reference.

## 5. Grids, trees and stable scrolling

- Keep management rows read-only; edit them in popups.
- Use named fixed pixel column widths from configuration. Do not implement an interactive Columns dialog, percentage widths or a second persistence mechanism for widths.
- Share column definitions between header and rows. Center headers; left-align names/descriptions, right-align numeric data and center compact indicators where appropriate.
- Initially select the top row when a grid is nonempty. Refresh should preserve a valid selection when possible.
- Keep headers stationary during vertical scrolling.
- Reserve scrollbar space so adding/removing overflow never changes element widths or horizontal positions. Enabling/disabling a persistent scrollbar is preferred; showing/hiding is acceptable only with the same reserved space.
- No scrollbar may overlap search, headers, inputs or footer buttons.
- For fixed-width layouts, calculate a flexible text column from the remaining width. Include margins, gaps, fixed columns, button widths and scrollbar space in the calculation.
- When a view omits a column, explicitly redistribute its configured width to the designated flexible column when that layout calls for it. Do not invent unrelated dimensions.
- Avoid horizontal scrolling in these fixed-layout data-entry views. Plan compatible popup and column widths instead.
- Use single-line row text with ellipsis when needed, without text-cell tooltips under this user's tooltip policy.

For hierarchical data, use the following navigation behavior: Up/Down moves through visible rows; Right expands a group or enters its first visible child; Left collapses a group or selects its parent; Home/End selects the first/last visible row. Preserve actual hierarchy even when visual indentation is limited. The underlying tree-control implementation is not prescribed by this skill.

## 6. Toolbars, command buttons and tooltips

- Use compact square icon buttons for view toolbars. Group related commands with separators.
- Reuse supplied icons and consistent enabled/disabled presentation. Keep a meaningful accessible command name independent of the icon.
- Put command names and assigned keyboard shortcuts in tooltips and context menus.
- Tooltips belong only on command buttons and sidebar navigation items. Do not add them to textboxes, decimal controls, date/time pickers, comboboxes, checkboxes, labels or grid/tree text cells.
- A tooltip is not another modal window. Showing it must not steal focus, alter ownership, block the editor or change an input's pointer behavior.
- Hints must work repeatedly after leaving/returning to a button, including Save and Cancel in every popup. No app shutdown, permanent disappearance or pointer oscillation is acceptable.
- Do not preserve temporary diagnostic tracing in the completed feature. Keep functional tooltip code separate from debugging code.

## 7. Keyboard access and focus

### Routing and traversal

- Scope shortcuts to the active view or top modal. Hidden views, blocked owners and disabled commands must not react.
- Route view-level commands independently of whether search or another input currently has focus. Do not override native editing shortcuts accidentally.
- Keep row-level commands scoped to the focused row. Preserve current-row context when a command button is clicked.
- Exclude command buttons and sidebar items from Tab traversal under this user's keyboard-driven workflow. Tab/Shift+Tab cycle through inputs, checkboxes, lists and trees within the active main view.
- Provide keyboard command access for buttons excluded from Tab; do not leave an essential command mouse-only. Retain accessible names and activation support.
- Changing views through a shortcut must move keyboard focus into the new view and remove stale focus visuals from the previous navigation item.
- Keep selection and keyboard focus distinct. A selected row is not proof that the grid has keyboard focus.
- Let an open input flyout or confirmation handle its own keyboard input; do not run background commands through it.

### Reusable shortcut conventions

Apply these only where the corresponding command exists. They are personal conventions, not claims that the combinations are universally unassigned.

| Command | Preferred shortcut | Scope |
| --- | --- | --- |
| Save | Ctrl+Enter | Editor |
| Cancel/Close | Esc | Active popup; respect active child flyouts/confirmations |
| Select current row | Enter | Selection picker grid |
| Edit current row | Shift+Enter | Focused grid/tree row |
| Next/previous search match | Enter / Shift+Enter | Search textbox |
| Refresh | Ctrl+R | Active view |
| Focus search | Ctrl+F | View with search |
| Focus first input | F5 | Active view/editor |
| Focus grid/first detail input | F8 | View/editor; switch grids where appropriate |
| Collapse to roots and immediate children | F6 | Tree view; collapse hidden descendants too |
| Expand all | F7 | Tree view |
| Add item/detail row | Alt+Shift+Insert | Applicable view/editor |
| Delete item/detail row | Alt+Shift+Delete | Applicable selection/focused row |
| Add group | Ctrl+Shift+Insert | Hierarchical management view |
| Move | Ctrl+M | View supporting moves |
| Merge | Ctrl+U | View supporting merges |
| Toggle Favorite | Ctrl+S | Feature with Favorite; never interpret as Save here |
| Toggle favorites-only filter | Ctrl+Shift+S | Feature with that filter |
| Choose group/parent | Ctrl+G | Editor with group/parent reference |
| Context menu | Shift+F10 or Menu key | Grid/tree supporting a context menu |

Assign view-navigation and domain-specific reference shortcuts per application. Do not copy the source application's numbered views/references. Avoid adopting Ctrl+Shift+0: it failed in the source environment because of an OS/input-setting conflict. Check candidate shortcuts on the target keyboard layouts and environment rather than claiming a combination is always free.

## 8. Editors with repeated detail rows

Apply only to editors with a collection of child rows:

- Place rows in an independently scrolling panel filling the body between ordinary fields and the fixed footer.
- Put Add row at the footer's right side. Keep auxiliary collection commands beside it.
- Keep narrow data columns configurable; calculate the main reference/text field's remaining width after padding, gaps, scrollbar and buttons.
- Put the remove action at the row's right edge with a recognizable icon. Do not give an enabled destructive action a disabled-looking appearance.
- Center column headers and align headers with their corresponding controls.
- Adding focuses the new row's first suitable input.
- Removing focuses the previous row; if removing the first, focus the first remaining row. If no rows remain, focus the editor's designated primary field.
- Choose/remove/reset commands act on the row containing focus, including focus in its numeric input.
- Do not carry over balancing, currency, rate or accounting-state behavior from the source application.

## 9. Conditional application patterns

These are reusable preferences, but are not required features of every WinUI application:

- **Fixed-width main window:** if chosen, configure Width rather than MinWidth, allow height resizing, and make maximize fill only the monitor work-area height while retaining width. Never cover content with the taskbar.
- **Mutually exclusive command buttons:** when the user wants a button row rather than radio controls or a combobox, allow exactly one pressed state and use the configured sidebar color for its selected background. State names and count are project-specific.
- **Help:** a small fixed modal can show centered application/version information, a User guide button and Close. Keep its dimensions configurable.
- **Guide:** ship an editable Markdown guide in Assets, grouped by view/popup, describing commands and shortcuts. Keep it synchronized with hints and actual bindings. Markdown is sufficient; PDF conversion is not a baseline requirement. Notepad was the source application's viewer, not a requirement for every future application.
- **Portable distribution:** when requested, publish an unpackaged folder with all assets and both .NET and Windows App SDK runtimes bundled. Preserve user data/settings across updates. Verify on a clean target machine; do not assume a developer-machine build proves deployability.
- **Exact decimal input:** when the domain requires exact decimal arithmetic, use a reusable decimal-aware input with precision/validation distinct from business rules. Do not copy financial precision, rounding or date semantics into unrelated applications.

## 10. Implementation guidance, not mandatory architecture

- Share popup ownership, settings loading, styling, command metadata and focus behavior instead of implementing separate variations per window.
- A shared command definition can feed execution, shortcut, tooltip, context-menu text and enabled state. The target project's command framework remains its choice.
- Reusable controls belong in an appropriate controls library; application-specific editors and settings consumers remain in the application.
- Prefer a shared Button style for focus behavior when sufficient. A custom Button subclass is not a requirement.
- An overlay-based tooltip was used to address modal-specific bugs in the source application. The skill should require reliable behavior, not blindly mandate that workaround.
- Keep settings data classes free of file access and UI side effects. Keep business operations out of shared controls.
- When replacing a UI feature, remove its obsolete files, event wiring, settings stores and dedicated tests. Do not delete helpers still needed by active layout paths without replacing those paths first. Do not delete unused supplied assets without authorization.
- Follow the target repository's C# conventions and dependency rules. This brief does not prescribe namespaces, DI scope design, MVVM toolkit, database technology, exact SDK versions or a business-layer arrangement.

## 11. Verification checklist for the future skill

1. Build and run applicable existing tests; state any unavailable runtime or environment limitation.
2. Check settings failures: invalid JSON, unknown/missing properties, invalid dimensions and colors. Verify the normal workspace never starts with partial settings.
3. Check build/publish Assets contents, executable-relative loading, and preservation of an edited active settings file.
4. Open every editor/picker, including rarely used ones: fixed DPI-scaled dimensions, close-only title bar, footer visibility and no outer scrollbar.
5. Open nested popups, switch applications and return through the taskbar. Verify the top modal is visible and owners stay blocked.
6. Exercise Tab traversal and navigation shortcuts from different focused controls. Verify no stale focus border or commands reaching hidden views.
7. Verify each shortcut from its allowed scope and verify that disabled/ineligible commands cannot execute.
8. Open pickers for existing references, including deeply nested/off-screen items. Verify selection, expansion, scrolling and initial grid focus.
9. Add/remove detail rows and invoke row commands from every row control. Verify the affected row and subsequent focus.
10. Add/remove enough data to toggle scrolling. Verify no horizontal shifts or width changes and no overlaps.
11. Verify copy/selection from readonly controls, inability to edit, and consistent normal/hover/focus appearance.
12. Hover every command repeatedly in main views and modal windows; verify hints and shortcut text. Hover ordinary inputs; verify no hints or cursor oscillation caused by hints.
13. Verify configured font/row sizes at representative DPI scales. Check date/time content, checkbox focus padding and button labels for clipping.
14. Confirm the shipped guide, hints, context menus and actual command bindings agree.
15. Inspect for obsolete feature code and temporary diagnostic output after changes.

Do not report visual behavior as verified unless the application was actually exercised. Distinguish a static review, a successful build, unit tests and interactive checks.

## 12. Explicit exclusions

Do not import the following into a general WinUI skill:

- Accounting entities, currency/rate rules, transaction states, balancing rules, cumulative totals or account-name generation.
- Particular sidebar pages, account-filter slots or their replacement algorithm.
- Domain command bindings such as choosing specific classifications with numbered keys.
- Exact popup sizes, column widths, colors or SDK package versions from the source project.
- Business merge/duplicate/delete rules, API call sequencing, transaction boundaries, synchronization or database initialization.
- Hardcoded user/machine paths, sample databases, application-specific icons or diagnostic log names.
- Obsolete column-editing dialogs and percentage-width persistence, input/grid-cell tooltips, or resizable-editor requirements from historical documents.

The handoff deliverable is a reusable skill expressing the conventions above, with conditional patterns clearly separated from core defaults and project values supplied by the target application.
