# Business Requirements Document (BRD)

Status: Draft 0.1 — brainstorming; not yet a stable baseline.
Updated: 2026-09-22.

## 1. Purpose and process

DoubleEntryHomeBookkeeping supports home accounting based on strict double-entry bookkeeping. Users organize accounts and reference data, record transactions in different currencies, and analyze money movement in a common base currency.

This document captures the initial requirements supplied by the user. Open questions are not approved requirements. Update this BRD during brainstorming. Maintain the [Technical Requirements Document (TRD)](TRD.md) alongside this BRD. It records existing technical requirements and will expand as the BRD becomes stable.

Implementation choices belong in the TRD. Explicit structural requirements supplied by the user are retained here so they are not lost.

## 2. Scope and terms

The initial scope covers group hierarchies, account classifications, currencies, transactions, balance validation, and reporting through account classifications. Template workflows, synchronization behavior, and report layouts are not yet defined.

| Group type | Element type | Business meaning of element |
| --- | --- | --- |
| AccountGroup | Account | Records money movement in one currency, with optional reporting classifications. |
| CategoryGroup | Category | An activity. |
| ProjectGroup | Project | A project into which the user invests money and/or from which the user earns money. |
| CorrespondentGroup | Correspondent | A physical or legal person. |
| TemplateGroup | Template | An element organized in a template group; further behavior remains to be defined. |

## 3. Stated requirements

### 3.1. Groups and elements

- **BRD-GRP-001:** All five group types have identical structure. Each type forms a hierarchical tree with exactly one root.
- **BRD-GRP-002:** Every group node has a non-null Parent reference and its foreign key. The root's Parent points to itself.
- **BRD-GRP-003:** Every group has a Children collection containing its child groups.
- **BRD-GRP-004:** Every group has an Elements collection containing elements of its corresponding type.
- **BRD-GRP-005:** Every element has a mandatory reference to its corresponding group and the group's foreign key. An element cannot exist without a group.
- **BRD-GRP-006:** Category, Project, and Correspondent are practically identical. They remain separate types to help users understand their different meanings.

The tree requirement implies that all nodes connect to their type's root and no parent cycles exist other than the root's required self-reference. Whether that self-reference also places the root in Children remains open.

### 3.2. Accounts and reporting

- **BRD-ACC-001:** Every Account has a mandatory Currency.
- **BRD-ACC-002:** Every Account may optionally reference a Category, a Project, and a Correspondent. Each of the three references is optional independently.
- **BRD-ACC-003:** Account Currency may be changed only before the account's first use or synchronization. Either event locks the currency against subsequent changes.
- **BRD-REP-001:** Users can group money movement across accounts to produce reports at different levels of detail using account classifications.

The meaning of first use and synchronization, and the effect of later classification changes on historical reports, require clarification.

### 3.3. Currencies and exchange rates

- **BRD-CUR-001:** The user selects a base Currency when creating the database.
- **BRD-CUR-002:** The database's base Currency cannot be changed afterwards.
- **BRD-CUR-003:** Currency has a collection of CurrencyRate values relative to the base currency.
- **BRD-CUR-004:** The rate for the base currency is always 1.

Rate direction, effective dates, selection rules, precision, and rounding are not yet defined.

### 3.4. Transactions and entries

- **BRD-TXN-001:** The user can enter a Transaction.
- **BRD-TXN-002:** Each Transaction records when it occurred and has an optional description.
- **BRD-TXN-003:** Each Transaction consists of two or more TransactionEntry items.
- **BRD-TXN-004:** Each TransactionEntry has a mandatory Account, an Amount expressed in that account's currency, and a Rate.
- **BRD-TXN-005:** Each TransactionEntry has a calculated BaseAmount representing its amount in the database's base currency.
- **BRD-TXN-006:** The sum of BaseAmount across all entries in a Transaction must equal 0.
- **BRD-TXN-007:** If that sum is not 0, the Transaction is marked invalid and does not participate in other activities.

The BaseAmount formula and the exact activities excluded for invalid transactions require clarification. A zero sum satisfies the balance rule; additional validity rules have not yet been agreed.

## 4. Acceptance examples

These examples illustrate the stated requirements without choosing a rate formula or rounding policy.

| Scenario | Expected result | Requirement |
| --- | --- | --- |
| Inspect any group hierarchy | Exactly one root; its Parent points to itself | BRD-GRP-001–002 |
| Create an element without a group | The element cannot exist without a corresponding group | BRD-GRP-005 |
| Create an Account without Category, Project, or Correspondent | Missing optional classifications are allowed; Currency remains mandatory | BRD-ACC-001–002 |
| Change Account Currency before any use or synchronization | Change is allowed | BRD-ACC-003 |
| Change Account Currency after first use or synchronization | Change is disallowed | BRD-ACC-003 |
| Change base Currency after database creation | Change is disallowed | BRD-CUR-001–002 |
| Use a rate for the base currency | Rate is 1 | BRD-CUR-004 |
| Enter a Transaction without a description | Missing description is allowed | BRD-TXN-002 |
| Create a Transaction with only one entry | Minimum entry requirement is not met; handling remains open | BRD-TXN-003 |
| Two entries have BaseAmount values +100 and -100 | Balance rule is satisfied | BRD-TXN-006 |
| Three entries have BaseAmount values +100, -60, and -40 | Balance rule is satisfied | BRD-TXN-003, BRD-TXN-006 |
| Two entries have BaseAmount values +100 and -99 | Transaction is invalid and excluded from other activities | BRD-TXN-007 |

## 5. Open questions

Resolve these during brainstorming. Possible answers below are not confirmed requirements.

| ID | Question |
| --- | --- |
| Q-01 | What counts as an Account's first use: any saved entry, only a valid transaction, or another event? What synchronization event locks currency? |
| Q-02 | Is Rate defined as base-currency units per one unit of account currency, so BaseAmount = Amount × Rate? Can users override an entry's Rate? |
| Q-03 | What dates/times identify CurrencyRate values? How is an entry's rate selected, and what happens when no rate exists? |
| Q-04 | What precision and rounding apply to Amount, Rate, and BaseAmount? How should users handle conversion rounding differences while keeping the sum exactly zero? |
| Q-05 | What activities exclude invalid transactions: balances, reports, synchronization, template creation, or others? Can invalid transactions be saved, edited, and corrected? |
| Q-06 | Can incomplete transactions be saved as drafts? What happens with fewer than two entries or missing required fields? |
| Q-07 | Does the root appear in its own Children collection? Can the root contain Elements directly? |
| Q-08 | What rules apply to creating, renaming, moving, deleting, or archiving groups and elements, especially when already used? |
| Q-09 | Which common properties do elements need, and what naming or uniqueness rules apply? |
| Q-10 | Do later changes to an Account's Category, Project, or Correspondent reclassify historical money movement? |
| Q-11 | What does a positive or negative Amount mean for each kind of account? Are zero amounts and repeated accounts within one transaction allowed? |
| Q-12 | Which reports and group rollups are needed? How should unclassified accounts appear? |
| Q-13 | What should Templates contain and how should users apply them? |
| Q-14 | How should transaction occurrence time handle time zones? |

## 6. Related documents

- [Repository instructions](../../AGENTS.md).
- [TRD](TRD.md): technical requirements, including child-to-parent references; these do not resolve the business questions above.
