# Double Entry Home Bookkeeping - Keyboard shortcuts

Shortcuts apply to the active view or popup. A command must be available and enabled.
Edit commands need a selected row and focus in the grid. Entry commands act on the row containing keyboard focus.

## Main window - navigation

- Ledger - Ctrl+F12
- Accounts - Ctrl+F5
- Correspondents - Ctrl+F6
- Categories - Ctrl+F7
- Projects - Ctrl+F8
- Templates - Ctrl+F9
- Currencies - Ctrl+F10
- Help - Ctrl+F1
- Exit - Ctrl+F4

Reserved for currently disabled views:

- Reports - Ctrl+F11
- Synchronization - Ctrl+F3
- Settings - Ctrl+F2

## Focus and common controls

- Next input control - Tab
- Previous input control - Shift+Tab
- Toggle a focused checkbox - Space
- Select all text in a textbox - Ctrl+A
- Copy selected text (including readonly textboxes) - Ctrl+C
- Increase/decrease a decimal value - Up/Down (in an editable decimal box)

Tab skips command buttons and the sidebar. In main views, Tab cycles within the view.

## Accounts, Correspondents, Categories, Projects and Templates - tree views

- Focus first input control - F5
- Focus treegrid - F8
- Collapse groups, leaving roots and their immediate children visible - F6
- Expand all groups - F7
- Refresh - Ctrl+R
- Add group - Ctrl+Shift+Insert
- Add element - Alt+Shift+Insert
- Edit selected group or element - Shift+Enter
- Delete selected group or element - Alt+Shift+Delete
- Move selected group or element - Ctrl+M
- Merge / combine into selected - Ctrl+U
- Add/remove star on selected group or element - Ctrl+S
- Toggle Starred only filter - Ctrl+Shift+S
- Focus search textbox - Ctrl+F
- Next search result - Enter (in search textbox)
- Previous search result - Shift+Enter (in search textbox)

With focus in the treegrid:

- Previous/next visible row - Up/Down
- Expand group or select its first visible child - Right
- Collapse group or select its parent - Left
- First/last visible row - Home/End
- Open context menu - Shift+F10 or Menu key
- Toggle row checkbox, where checkboxes are shown - Space
- Cancel an active drag operation - Esc

## Choose group / Choose element / Choose account popups

- Select the highlighted row - Enter (with focus in treegrid)
- Cancel - Esc
- Edit selected row, when editing is available - Shift+Enter

Tree navigation, search, focus, refresh and filter shortcuts are the same as in the tree views above.
Add, delete, move, merge and star commands work only when the popup exposes and enables those commands.
In the search textbox, Enter and Shift+Enter navigate search results instead of selecting or editing.

## Currencies and rates view

- Focus first input control - F5
- Focus/switch between currency and rate lists - F8
- Refresh - Ctrl+R
- Add currency - Ctrl+Shift+Insert
- Edit selected currency - Shift+Enter (currency list focused)
- Delete selected currency - Ctrl+Shift+Delete
- Add currency rate - Alt+Shift+Insert
- Edit selected currency rate - Shift+Enter (rate list focused)
- Delete currency rates - Alt+Shift+Delete

Commands unavailable for the base currency remain disabled.

## Ledger view

- Focus first input control - F5
- Focus transaction grid - F8
- Refresh - Ctrl+R
- Add transaction - Alt+Shift+Insert
- Edit selected transaction - Shift+Enter (grid focused)
- Delete selected transaction - Alt+Shift+Delete
- Duplicate transaction - Ctrl+D
- Create transaction from template - Ctrl+L
- Create template from transaction - Ctrl+Shift+L
- Today - Ctrl+T
- Previous period - Ctrl+PageUp
- Next period - Ctrl+PageDown
- Choose account filter - Ctrl+9
- Clear account filter - Ctrl+Shift+9
- Restore quick account filter 1 through 5 - Alt+1 through Alt+5

Empty quick-filter slots are disabled. Hover over a slot button to see its stored account.

## All editor popups

- Save / confirm the editor action - Ctrl+Enter
- Cancel / close - Esc

Unsaved changes may require confirmation before closing.
These shortcuts also apply to currency and currency-rate editors.

## Add/Edit group popup

- Choose parent - Ctrl+G
- Toggle Favorite - Ctrl+S
- Save - Ctrl+Enter
- Cancel - Esc

## Add/Edit catalog element popup

Applies to Correspondents, Categories and Projects.

- Choose group - Ctrl+G
- Toggle Favorite - Ctrl+S
- Save - Ctrl+Enter
- Cancel - Esc

## Add/Edit account popup

- Choose group - Ctrl+G
- Toggle Favorite - Ctrl+S
- Choose correspondent - Ctrl+6
- Clear correspondent - Ctrl+Shift+6
- Choose category - Ctrl+7
- Clear category - Ctrl+Shift+7
- Choose project - Ctrl+8
- Clear project - Ctrl+Shift+8
- Restore name - Ctrl+Shift+R
- Save - Ctrl+Enter
- Cancel - Esc

## Template editor popup

- Focus first input control - F5
- Focus first entry - F8
- Choose group - Ctrl+G
- Toggle Favorite - Ctrl+S
- Add entry - Alt+Shift+Insert
- Choose account for current entry - Ctrl+9
- Remove current entry - Alt+Shift+Delete
- Save - Ctrl+Enter
- Cancel - Esc

Focus an entry's account or amount control before using its row commands.
Adding an entry focuses the new row. Removing one focuses the previous row, or the first remaining row when deleting the first. If no rows remain, focus returns to Name.

## Transaction editor popup

- Focus first input control - F5
- Focus first entry - F8
- Add entry - Alt+Shift+Insert
- Add balancing entry - Ctrl+Shift+B
- Choose account for current entry - Ctrl+9
- Remove current entry - Alt+Shift+Delete
- Restore rate for current entry - Ctrl+Shift+R
- Select Draft state - Alt+1
- Select Planned state - Alt+2
- Select Confirmed state - Alt+3
- Save - Ctrl+Enter
- Cancel - Esc

Focus a control in the entry before using its row commands.
Adding an entry focuses the new row. Removing one focuses the previous row, or the first remaining row when deleting the first. If no rows remain, focus returns to Description.

## Help popup

- Close - Esc

Use the User guide button to open this file in Notepad.
