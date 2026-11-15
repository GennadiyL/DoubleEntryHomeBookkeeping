# WinUI UI Conventions

Status: collected requirements for a future project-independent skill. This document is not an installed skill.

## Intended scope

Apply these conventions when implementing or reviewing WinUI dialogs, command buttons, keyboard interactions and inline validation layouts. Keep application-specific business rules, storage paths, deployment procedures and test datasets outside this skill.

## Informational dialogs

- In an informational dialog with a single Close button, center the button horizontally across the dialog footer.
- Support Escape to close the dialog and use the exact tooltip `Close (Escape)`.
- Keep long text readable and wrapped within the available content width. Text must not overlap a vertical scrollbar, including an overlay scrollbar.
- Allow long content to scroll while keeping the footer and Close button accessible.
- Do not generalize the centered single-button layout to multi-action editors such as Save/Cancel dialogs.

## Command hints and keyboard behavior

- Include the keyboard shortcut in a command's tooltip when that shortcut is supported. The hint must match the actual behavior.
- Scope keyboard actions to the active dialog. A shortcut must not activate a command in a disabled owner window or bypass a foreground modal interaction.
- For a running operation, treat Escape or the window close action according to the cancellation flow; do not interpret the informational-dialog Close convention as permission to dismiss an unfinished operation.

## Inline validation layout

- Showing a validation error beneath an input may increase the row height, but must not stretch adjacent input fields or picker buttons vertically.
- Clearing the error must release the space occupied by the error. No neighboring control may retain an enlarged height caused by validation.
- Preserve the entered text while showing an error so the user can correct it.

### Implementation guidance derived from the observed layout defect

- Align an adjacent account or item picker to the top of a row that contains an input with an inline error message. Keep the picker's text field at its natural input height.
- Avoid layout feedback loops: do not derive a button's explicit height from a sibling that stretches to the height of the entire validation row. Such a button can prevent the row from shrinking after the error disappears.
- If a picker button follows the text field's height, ensure the text field itself cannot grow with the error region.
- Reuse the correction in other editors that use the same affected picker layout.

## Focused verification scenarios

- Show short and long dialog messages. Confirm wrapping, scrollbar clearance and access to the footer.
- Confirm that a single Close button is centered, displays `Close (Escape)` and closes the active informational dialog with Escape.
- Enter an incomplete or invalid amount, move focus away, then correct the value. Confirm that adjacent fields and picker buttons retain their normal height and the error space disappears.
- Repeat the invalid/valid cycle to detect persistent size growth.

These scenarios verify the UI conventions; they do not define a deployment or database-testing workflow.
