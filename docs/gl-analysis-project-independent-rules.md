# GL Analysis: project-independent rules and plugin-change handoff

Date: 2026-09-28.
Audience: agent updating the GL Analysis plugin.
Purpose: transfer the reusable requirements-documentation conventions established in the discussion. This file requests plugin changes; it does not implement or install them.

## 1. Evidence and scope

The user explicitly agreed to:

- Name the high-level BRD behaviors **Business Capabilities**, identified by `BC`.
- Reserve **Use Cases**, identified by `UC`, for detailed action-level scenarios in the TRD.
- Use concise main/alternative flows rather than long button-by-button UI descriptions.
- Link detailed use cases to API/BFF operations, making the scenarios useful for manual testing and traceability.
- Apply those naming changes to the existing BRD.

The user also repeatedly corrected the agent for asking already answered questions. Preserve decisions and consult them before asking again.

Sections 2–6 describe the agreed documentation conventions. Section 7 turns the observed decision-retention problem into reusable workflow requirements. Section 8 lists implementation recommendations for the receiving agent; these recommendations are not additional user-approved business policies.

Exclude application-specific rules. Do not add accounting, platform, cloud-provider, configuration-table, database-engine, precision, authentication, expiration or log-retention choices to this plugin's universal defaults.

## 2. Separate business capabilities from detailed use cases

### BRD: Business Capabilities

A Business Capability describes a high-level business ability and its intended outcome. It may encompass several distinct user actions and alternative paths.

- Section title: **Business Capabilities**.
- Identifier: `BC-###`, for example `BC-003`.
- Meaning of BC: **Business Capability**, not Business Case. A business case normally means an investment justification.
- Keep business scope, roles, policies, functional/nonfunctional requirements and success criteria in the BRD.
- Capabilities may have high-level flows and acceptance examples, but should not become exhaustive UI scripts or API specifications.

Example:

```text
BC-003 — Maintain groups and classifications
Outcome: Users can organize information while preserving hierarchy rules.
```

### TRD: Detailed Use Cases

A Detailed Use Case describes one specific action or user goal, its result and meaningful alternatives.

- Section title: **Detailed Use Cases**.
- Identifier: `UC-###-##`, for example `UC-003-01`.
- The middle number identifies the parent capability; the final number identifies a scenario within it.
- Each use case links explicitly to its parent `BC-###` and relevant requirements.
- A capability may have several use cases. Do not simply copy each capability into one equally broad use case.
- Detailed use cases belong in the TRD, alongside the technical contracts they use.

Example:

```text
UC-003-01 — Merge groups
Parent: BC-003 — Maintain groups and classifications
```

Preserve stable identifiers across revisions. If an existing identifier scheme must change, document the old-to-new mapping rather than silently breaking references.

## 3. Keep detailed use cases compact and outcome-oriented

The user prefers short scenarios over exhaustive click sequences. A detailed use case is specific in behavior, not necessarily long.

Recommended compact form:

```text
UC-003-01 — Merge groups
Parent: BC-003
Role: ROLE-001
Precondition: The user can access and modify the relevant hierarchy.

Main flow:
Merge the source group into the destination. Move the source's elements
and children, delete the source, and display the updated hierarchy.

Alternative flows:
A1: Reject a merge that would create a cycle; hierarchy remains unchanged.
A2: Reject a protected source; hierarchy remains unchanged.

Postcondition:
The destination contains the moved content and the source no longer exists.

API/BFF operations:
OP-005 — GroupService.GetTree
OP-007 — GroupService.MergeGroups
```

This is a generic format example, not a universal requirement that every application supports groups or uses these exact operation names or merge policies.

Rules:

- Describe the action and observable outcome.
- Include meaningful validation failures, cancellation, exceptional paths and resulting state when supported by requirements.
- Use source/destination or similarly explicit terms where selection order could be ambiguous.
- Include role, trigger and preconditions when they affect behavior. Shared preconditions may be declared once and referenced or inherited explicitly.
- Main flow and postcondition may share one concise statement if the result is unambiguous.
- Avoid describing every click, popup and screen unless that interaction is itself a requirement.
- A table is acceptable if it preserves the same meaning and links.
- Do not invent a UI action, selected-row state, error policy or operation merely to make the description look complete. Mark unsupported details proposed or unresolved.

## 4. Traceability chain

Use:

**Business Capability → Detailed Use Case → API/BFF Operation → Test Scenario**

The test scenario normally derives from a main or alternative use-case flow; the arrow is a traceability relationship, not a rule that every operation has exactly one test.

Required mappings:

1. Every in-scope system capability maps to detailed use cases.
2. Every detailed use case maps to the operations needed for that behavior, or explicitly identifies a local/UI-only or non-system action.
3. Each operation links back to the use cases and applicable business requirements.
4. Main and alternative flows provide identifiable manual test scenarios with expected results.
5. Shared operations can serve several use cases. A use case can need several operations.
6. Retain mappings for roles, rules, functional requirements, nonfunctional requirements and success criteria; BC/UC separation does not replace those mappings.

For API/BFF links, show the logical service and operation name as well as the stable `OP-###` ID. Example: `OP-007 — GroupService.MergeGroups`. If a compact table contains only operation IDs, its operation catalog must resolve each ID to a readable name.

An API/BFF operation is a logical contract. Do not assume every listed operation is HTTP, remote, public or cloud-hosted. Detailed use cases must not accidentally turn local/offline behavior into a network dependency.

## 5. Manual-test usefulness

Detailed use cases should be usable as manual test specifications:

- State enough preconditions to reproduce the behavior.
- State the action and expected observable outcome.
- Treat each meaningful alternative as a separately identifiable scenario.
- Identify whether data changes, remains unchanged, or awaits further action when that distinction is required.
- Reference shared business rules instead of duplicating them inconsistently.

A phrase such as “user merges two groups” alone is a useful name, but not a complete test specification. Add the result and relevant alternatives concisely.

Do not claim that a use-case catalog is executed test evidence, or that unspecified inputs and expected results are complete manual test procedures. Concrete fixtures and execution records can be produced later without changing the approved behavior.

## 6. Keep document responsibilities distinct

| Artifact/section | Responsibility |
| --- | --- |
| Discovery | Inputs, decisions, alternatives, assumptions and open questions |
| BRD | Business Capabilities, scope, rules, outcomes and business acceptance criteria |
| TRD — Detailed Use Cases | Action-level scenarios, alternatives, expected results, parent BC and operation links |
| TRD — API/BFF contracts | Inputs/outputs, validation, authorization, effects, transaction/concurrency and retry semantics |
| TRD — schemas | Persistent models and separate DTO contracts with explicit decision status |
| Verification artifacts | Concrete manual/automated tests and execution results linked to scenarios |

Preserve existing safeguards: proposed technical details are not approved business requirements; persistent models are not API DTOs; approving a BRD does not automatically approve a TRD or authorize implementation.

This conversation did not establish a project-independent change to approval identity requirements, formal approval gates or deployment architecture. Do not treat the particular workflow used by the agent as evidence that those policies should be changed.

## 7. Preserve decisions; do not repeat answered questions

The user explicitly objected to repeated clarification of settled behavior. The plugin should guide the agent to:

1. Read the decision register, current authoritative document and relevant conversation answers before asking.
2. Update the existing requirement/use case and close the corresponding question when answered.
3. Update related summaries, risk statements, acceptance scenarios and traceability so they do not continue calling the same item unresolved.
4. Separate a resolved business rule from an unresolved technical mechanism. Do not reopen the business decision just because implementation design remains.
5. Before an approval gate, remove stale generic questions already answered by concrete rules. Preserve a real unresolved decision with its precise impact.
6. Do not repeatedly ask “are there any other required fields?” when requirements already enumerate fields. Identify a concrete contradiction or missing behavior instead.
7. When genuinely new uncertainty remains, ask the smallest specific question, normally one at a time.
8. Do not infer more than the answer supports. A user accepting unique entries does not necessarily select the proposed tie-break rule as well.
9. Preserve provenance and superseded decisions, but clearly separate historical statements from current rules.
10. Do not delete a substantive unanswered question merely to claim approval readiness.

The goal is continuity: settled decisions remain settled unless the user changes them or new evidence exposes a concrete conflict.

## 8. Recommended plugin change coverage

Inspect the plugin before editing; filenames below refer to the current package layout, not a mandatory implementation plan.

- **Artifact contract:** add `BC-###`, define `UC-###-##`, update artifact responsibilities and identifier migration guidance.
- **BRD template/write skill:** rename high-level Use Cases to Business Capabilities; update examples, links and traceability columns.
- **TRD template/write skill:** add the compact Detailed Use Cases section and its BC/operation/test-scenario links. Preserve full API/BFF contract requirements.
- **Discovery/clarification skills:** add decision reconciliation and anti-repetition checks; distinguish business decisions from technical questions.
- **Review skill/checklists:** review capability coverage and detailed-use-case coverage separately; check every UC parent and operation reference; require observable outcomes and supported alternatives.
- **Traceability rules:** include BRD BC rows in the BRD-to-TRD matrix, and evaluate TRD UC-to-operation mapping separately. Do not inflate coverage by double-counting a capability and its children as independent covered BRD IDs.
- **Examples and plugin validation:** update old BRD `UC` examples and include a before/after migration example. Preserve historical identifiers through an explicit mapping.

A legacy high-level BRD `UC-003` may become `BC-003`; detailed scenarios then use `UC-003-01`, `UC-003-02`, etc. Do not relabel an already detailed legacy use case as a business capability without inspecting its meaning.

## 9. Acceptance checklist for the plugin changes

- [ ] New BRDs call high-level behavior Business Capabilities, not Business Cases or detailed Use Cases.
- [ ] New TRDs contain specific `UC-###-##` scenarios under explicit parent `BC-###` identifiers.
- [ ] Compact main/alternative flows include expected results without requiring button-by-button scripts.
- [ ] Every UC links to resolvable named API/BFF operations, or an explained local/UI-only/non-system action.
- [ ] Business-to-technical and use-case-to-operation traceability are both reviewed.
- [ ] Existing ROLE/BR/FR/NFR/SC and schema/operation coverage checks remain intact.
- [ ] Identifier migration preserves meanings and old-to-new references.
- [ ] Resolved questions are not asked again because stale summaries still list them as open.
- [ ] Unknown technical choices are labeled rather than invented or promoted to approved requirements.
- [ ] No application-specific rule from the originating project becomes a universal plugin default.
- [ ] No unrelated approval-policy changes are smuggled into this terminology/workflow update.
