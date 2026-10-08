# TreeGrid first increment

Implemented in backend/WinUi.Controls/TreeGridControl. The library uses the same
WinUI package and Windows target as Dehb.WinUi, with no Business or DAL reference.

Run Dehb.WinUi in Debug/x64. The main window shows sample Accounts and
Correspondents. Switch catalogs and toggle Checkboxes. Expand More accounts to
check scrolling with 200 additional rows. This demo does not open the Local DB.
Double-click/Enter reports activation in the footer; no editor is opened yet.

## Public surface

- TreeGrid.ItemsSource: observable root nodes; populate each node's Children.
- TreeGridNode: Name, IsGroup, Data, IsExpanded, IsStarred, nullable IsChecked.
- TreeGrid.Columns: extra columns between Name and Star, with Header,
  BindingPath and optional CellTemplate. Bindings and cell
  templates receive the node; Data can contain an application view model.
  Replace a column in the collection to apply changed column settings.
- TreeGrid.ShowCheckboxes: changes visibility without clearing state.
- TreeGrid.SelectedNode: separate from checkbox selection.
- RowActivated, StarChanged, CheckStateChanged, SelectedNodeChanged:
  host integration events. Star and checkbox events follow the local state
  change. TreeGrid propagates checkbox changes through descendants and recalculates ancestors; the host owns saving and rollback.
- Mutate the tree and its collections on the UI thread. A node may occur only
  once; only groups may have children.

Rows are projected in supplied preorder, with stable node instances and preserved
nested expansion. Collapsing the selected descendant selects its collapsed
ancestor. Actual depth is retained; visual indentation is capped at eight.
ListView provides virtualization and Up/Down navigation; the control adds
Left/Right, Home/End, Enter and checkbox Space handling.

## Search

Search names with Next/Previous, or press Enter in the search box for Next.
Matching is case-insensitive and checks any part of group and element names
throughout ItemsSource, including collapsed branches. Navigation starts after
(or before) the current selection and wraps; with no selection it starts at the
first/last match. Blank searches are disabled. Typing alone does not navigate.
A match expands its ancestors and scrolls into view without activating the row
or changing stars or checkboxes. No matches keeps selection and shows a message.
Search reads current names and hierarchy on every request; it does not filter.

StarredOnly (off by default) shows starred rows and their ancestor paths, while
keeping all nodes in the model. Expansion still applies. Star changes update the
view immediately; starred groups do not include unstarred descendants automatically.
Disabling the filter restores the previous whole-tree selection when it still exists.
Typing a nonblank search or invoking Next/Previous clears StarredOnly first.

IsSearchEnabled defaults to true. Hosts can set it to false to disable the text
box and navigation, including FindNext/FindPrevious calls. Star filtering does
not disable search. Search covers the supplied applicable tree;
groups-only hosts must supply their groups-only hierarchy.

## Column widths

Columns opens a dialog for Name, data columns and Star, in their fixed order.
Enter percentages with up to two decimal places. If the total differs from 100%,
Apply sets the main column to 100 minus the other columns and updates its field.
An out-of-range result keeps the dialog open with an error. Apply validates
minimum usable widths before changing the layout. Cancel leaves it unchanged;
Restore defaults only changes pending dialog values until Apply is clicked.
The same percentages drive header and row sizing as the viewport changes.

ColumnWidths returns the current percentages. SetColumnWidths restores validated
values without raising a save event. Replacing the Columns collection resets
widths to defaults; restore settings after configuring columns. The former
TreeGridColumn.Width weight is replaced by this complete percentage layout.

ColumnWidthsApplying lets the host save before the new layout is committed.
Set ErrorMessage on failure to retain the old widths and keep the dialog open.
The demo stores Accounts.Main and Correspondents.Main separately under
%LOCALAPPDATA%/DoubleEntryHomeBookkeeping/column-widths.json. Future pickers must
use their own keys. Settings errors are shown, and no database writes occur.

## DecimalBox

WinUi.Controls.DecimalBoxControl.DecimalBox contains a TextBox and inline spin buttons and exposes
nullable decimal Value, Precision (0–28, default 2), property-change notifications,
and TryCommit(). Value is the last successfully committed value; call TryCommit
before saving and apply any required-value or domain range checks in the host.
The Columns dialog uses Precision = 2 for every width field.
Spin buttons remain visible; holding a button repeats the step. Up/Down keys
also spin. Step is exactly 10^-Precision (precision 4 gives 0.0001). Empty input
starts from zero. Invalid input, overflow and inexact steps do not change the
value. IsReadOnly disables editing and both buttons. Focus forwards to the editor.

While focused, text is plain dot-decimal without grouping; entry selects the full
value. The numeric keypad decimal key inserts a dot. Input accepts only ASCII digits, dot, minus and plus; other characters are
rejected for typing and paste. Unfinished numeric text remains editable. Focus loss and TryCommit parse decimal directly and round
midpoint-to-even to Precision. Valid unfocused values use regional formatting.
Invalid text remains with a field description; Apply focuses the first invalid
field. Empty input commits null. Regional display formatting bypasses the input filter.
Grouped input, symbols, exponents, expressions,
overflow and values not exactly representable as decimal are rejected.

## Context menus

Right-click selects the row before ContextMenuRequested is raised. The host
receives TreeGridContextMenuEventArgs.Node and a fresh MenuFlyout in Menu.
Populate Menu.Items synchronously with host-owned labels, enabled states and
command handlers. Empty menus are not shown. The control supplies no business
commands. The menu key and Shift+F10 use the selected, focused tree row; text
editors retain their own menus. Context cancellation and unloading close the menu.

The sample host supplies Activate (demo), Expand/Collapse for groups, and
Add/Remove star. Catalog editors, deletion, moves and merge commands remain host
integration work. These demo commands do not save to the database.

## Drag and drop

AllowDragDrop and AllowMerge both default to false. The main-window demo enables
both. Editable popups enable AllowDragDrop only; read-only pickers enable neither.

The top/bottom quarters of a row mean Before/After. The center means Inside.
Only same-parent, same-kind sibling boundaries permit reordering. Group centers
accept moves; Shift over a group requests a merge for a group source. Shift-merge
is rejected when AllowMerge is false. Roots, self drops, descendant destinations,
unchanged positions and cross-tree drags are rejected.

An insertion line marks reordering; a rectangle marks a destination group.
Hover expands a collapsed group after 700 ms. Edge scrolling operates while the
pointer remains near the top or bottom. Native cancellation and Escape clear
drag feedback; hover-expanded groups stay open. Buttons and text inputs do not
initiate row dragging.

DropValidating receives Request, Cancel and Reason. Set Cancel for domain or
mode restrictions. It runs repeatedly, including immediately before dropping:
do not show dialogs, mutate models or call write services there.

DropRequested receives Source, Target, DestinationParent, Operation and
InsertIndex. InsertIndex is a final zero-based index among siblings of the same
kind, excluding Source and including hidden siblings. The control never applies
the requested mutation.

The host flow is:
1. Confirm a merge; return immediately on Cancel.
2. Call the appropriate Business reorder, move or merge service using identities
   from the nodes' Data objects. Handle errors without changing tree collections.
3. After a successful save, use TreeGrid.UpdateRows(() => { ... }) to batch UI
   collection changes into one refresh. The callback must be synchronous and run
   on the UI thread; parent/depth links are refreshed when it returns. This does
   not provide rollback if the callback throws.
4. Call RevealNode for the moved/reordered source or the merge destination.

The current demo uses sample data only. It applies collection changes in
DropRequested and shows a merge confirmation. It rejects conflicting move names
and appends _1 for merge collisions. These demo handlers are not database saves.
Production hosts remain responsible for transactional persistence, business
validation and rollback/error presentation.

## Current scope

This is a control demonstration, not the completed Business editing UI.
Database integration, modal editors,
other saved view settings remain later work.
Reporting, Synchronization and Help are not implemented here.

## Verification

- Debug/x64 application build.
- Eighty-four local NUnit cases cover hierarchy projection, checkbox propagation
  and drag/drop planning, including hidden siblings, no-op drops and merge permissions.
  Large-merge regressions cover expanded/collapsed sources and batched-update recovery.
- Tests target .NET 8, matching the WinUI host. On the development VM, which only
  has .NET 10 installed, run with process environment DOTNET_ROLL_FORWARD=Major.
- Interactive visual verification remains to be done in the running WinUI app.
