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

## Current scope

This is a control demonstration, not the completed Business editing UI.
Database integration, modal editors, drag/drop, search, favorites filtering,
the column-width popup and saved view settings remain later work.
DecimalBox, Reporting, Synchronization and Help are not implemented here.

## Verification

- Debug/x64 application build.
- Five local NUnit tests: nested expansion/check state, hidden child changes and
  ordering, subscription lifetime, deep hierarchy, invalid tree structures.
- Tests target .NET 8, matching the WinUI host. On the development VM, which only
  has .NET 10 installed, run with process environment DOTNET_ROLL_FORWARD=Major.
- Interactive visual verification remains to be done in the running WinUI app.
