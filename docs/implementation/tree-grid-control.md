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
  BindingPath, proportional Width and optional CellTemplate. Bindings and cell
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
Database integration, modal editors, search, favorites filtering,
the column-width popup and saved view settings remain later work.
DecimalBox, Reporting, Synchronization and Help are not implemented here.

## Verification

- Debug/x64 application build.
- Twenty-seven local NUnit cases cover hierarchy projection, checkbox propagation
  and drag/drop planning, including hidden siblings, no-op drops and merge permissions.
  Large-merge regressions cover expanded/collapsed sources and batched-update recovery.
- Tests target .NET 8, matching the WinUI host. On the development VM, which only
  has .NET 10 installed, run with process environment DOTNET_ROLL_FORWARD=Major.
- Interactive visual verification remains to be done in the running WinUI app.
