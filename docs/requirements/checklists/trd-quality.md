# TRD quality review

## Review metadata

- Review date: 2026-10-08. Reviewer: Codex.
- Scope: consolidated requirements in docs/requirements; current rules, UI sections, schemas, operations, decision registers, traceability and approval, with relevant later supersession records. Historical discussion is provenance, not an independent current requirement.
- Reviewed BRD: [BRD.md](../BRD.md), Draft **0.212** (2026-10-08).
- Reviewed TRD: [TRD.md](../TRD.md), Draft **0.222** (2026-10-08).
- Approval: BRD **0.32** only, requesting user, 2026-09-27, entire version; BRD:2245–2255. Current TRD is Not submitted (TRD:2740–2745).
- References: repository AGENTS.md; GL Analysis review skill and its artifact/type/checklist references; consolidated documents supersede their three Draft discovery sources.
- This is a document review, not a fresh backend implementation or test audit. Source documents, code and staged changes are not modified.
- Evidence uses one-based lines in these exact reviewed versions. Relative links target the source files.

## Summary

- Verdict: **Fail**.
- Findings: 12 total; 9 Blocker, 3 Important, 0 Minor.
- Review coverage: 18/18 mandatory checks evaluated.
- Quality pass rate: 4/17 applicable checks pass; 1 explicitly not applicable.
- Core traceability: 8/67 = 11.940299%. User-selected capability-inclusive traceability: 8/79 = 10.126582%.
- Development handoff: **Not ready** for the complete package.
- Cross-artifact policy contradictions are recorded once in brd-quality.md; they also require TRD/BRD alignment.

The user's explicit WinUI, MVVM, DI, SQLite and implementation choices override the skill's default solution-neutral restriction. They are not architecture-leakage defects. Exact package pins, indentation step, popup dimensions and filenames remain ordinary implementation choices unless a contract requires a specific value. No new capacity limit, local login, local progress UI, theme or HTTP dependency is proposed.

## Mandatory quality checks

| Check ID | Category | Expected condition | Status | Evidence location | Finding IDs |
| --- | --- | --- | --- | --- | --- |
| TRD-C-001 | Exact BRD reference | Exact current BRD is validly Approved for full handoff. | Fail | TRD:5–12,1138; BRD:2245–2255 | TRD-F-001 |
| TRD-C-002 | Role and use-case mapping | Roles/capabilities retain meaning and map to UC/operations. | Pass | TRD:198–271; BRD:571 (user-selected BC/UC convention) | None |
| TRD-C-003 | Persistent schemas | Models define properties, relationships, constraints and lifecycle. | Fail | TRD:388–405,486–492,582–584,637–670 | TRD-F-003/004/007 |
| TRD-C-004 | DTO separation | Persistent models never cross operation boundaries. | Pass | TRD:691–693,762,766,789,1139–1140; Changes explicitly inactive, not PM exposure | None |
| TRD-C-005 | Canonical types | All active schema values have defined canonical shapes. | Fail | TRD:693,715 (unclosed Changes),744–754; named record/enum aliases resolved via tables | TRD-F-005/007 |
| TRD-C-006 | Nullability | Every operation payload field has explicit requiredness. | Fail | TRD:697,712,715,770,773; incomplete setup/change/naming shapes prevent exhaustive field validation | TRD-F-004/005/008 |
| TRD-C-007 | Validation | Operations have complete source-backed validation. | Fail | TRD:783,823–825,953–974,1034–1041 | TRD-F-004/005/007 |
| TRD-C-008 | Operations | Inputs, outputs, links and effect boundaries are complete. | Fail | TRD:697,772–783,820–892,928–936,1034–1048 | TRD-F-004/005/007/008/009/010 |
| TRD-C-009 | Authorization | Protected operations define ownership and enforcement. | Fail | TRD:794–795,980–991,1034; bootstrap and reauthorization remain open | TRD-F-004/005 |
| TRD-C-010 | Errors | Failure triggers and observable results are sufficiently closed. | Fail | TRD:783,955,968–974,1035,1038; setup/sync/report error results unresolved | TRD-F-004/005/007 |
| TRD-C-011 | Side effects | Required side effects and their observable outcomes are complete. | Fail | TRD:355,670,795,862–868,998–1001 | TRD-F-005 |
| TRD-C-012 | Transactions and idempotency | Every mutation has complete atomic/retry boundaries. | Fail | TRD:792–800,772,861,951,998–1003 | TRD-F-005/006/008 |
| TRD-C-013 | Cross-cutting requirements | Security, reliability, integration and observability are consistent and testable. | Fail | TRD:991,998–1005,1014–1024,1035–1039 | TRD-F-004/005/006/012 |
| TRD-C-014 | Unresolved questions | Questions have accurate status, owner, affected contracts and impact. | Fail | TRD:1028–1048 compared with108–110,126–161,685,928,2750–2758,2825–2829 | TRD-F-002 |
| TRD-C-015 | Full traceability | Every applicable requirement maps to complete applicable technical contracts. | Fail | TRD:1052–1141; favorite/reorder operation mappings absent at820–892; refresh unassigned at697/1036 | TRD-F-009/010; affected contract findings |
| TRD-C-016 | Architecture leakage | Apply solution-neutral default unless user explicitly selects implementation content. | Not applicable | TRD:19,26–30,100–116,137–161,178–192; user explicitly requested framework, MVVM, DI, controls and implementation choices | None; authorized scope overrides skill default |
| TRD-C-017 | Source readiness claim | Source readiness accurately reflects its recorded conditions. | Pass | TRD:1138–1143 correctly says Not ready and distinguishes drafting from handoff | None |
| TRD-C-018 | Approval provenance | No unsupported current approval claim. | Pass | TRD:5–12,2740–2745 explicitly Not submitted; BRD:2245–2255 prior scope only | None |

## Findings

| Finding ID | Severity | Evidence location | Affected IDs | Expected condition | Observed condition | Required disposition |
| --- | --- | --- | --- | --- | --- | --- |
| TRD-F-001 | Important | TRD.md:5–12,1138–1143,2740–2745; BRD.md:2245–2255 | Document approval/handoff | Exact reviewed BRD/TRD are approved before a complete-package handoff. | Both current documents are explicitly Draft; only BRD 0.32 was fully approved. Drafting and earlier limited implementation were separately authorized. | After correction and re-review, obtain exact-version approval. This is a handoff prerequisite, not a request to undo authorized Business work. |
| TRD-F-002 | Important | TRD.md:40–42,118,162,642–643,698,1018,1036,1045,1047–1048; contrasted with 108–110,126–161,523–525,685,928,991,2750–2758,2825–2829 | TQ-03/05/08/14/15/16; DTO-002; OP-026/057–060; PM-007/013 | Open items distinguish genuinely unresolved contracts from settled decisions and dated observations. | Window storage, numeric editing, columns, time handling, list wrapper/tie-break, cumulative property/lookup and some setup/diagnostic declarations are already recorded, but current registers still list them as unresolved. Historical implementation snapshots also remain adjacent to current requirements. | Consolidate current contracts and prune only settled portions of questions; keep dated history clearly historical. Do not claim a fresh code audit or close sync/recovery gaps from old build results. |
| TRD-F-003 | Blocker | TRD.md:486,490,492; BRD.md:140,143,625–628 | PM-006; OP-027; BR-016/019; FR-005 | Draft may persist zero or one valid entry; Confirmed requires at least two entries. | PM-006 lifecycle still says it owns 2+ entries on any persisted save, contradicting the adjacent confirmed Draft rules. | Change the lifecycle cardinality to the already accepted state-dependent rule; no new business decision. |
| TRD-F-004 | Blocker | TRD.md:637–670,712–720,794–795,823–825,983–991,1034 | PM-013/014/015; DTO-016–023; OP-002–004/043–049; BR-003/005 | Executable setup, reauthorization and token ownership/lifecycle contracts. | Token policy is settled, but persistent hash/issue/expiry associations, token encoding and reauthorization of an existing registration are not closed operation/schema contracts. CheckMasterExists still has None input while routing is credential scoped; safe bootstrap/races remain open. | Specify only missing schemas and setup/reauthorization contracts, including same-registration rotation and failure outcomes, under accepted sync-only authorization policy. |
| TRD-F-005 | Blocker | TRD.md:345–369,660–681,715–719,795,862–868,998–1001,1035 | PM-014–016; DTO-018–023; OP-003/004/043–049; BR-029–042; NFR-003/004 | Typed changes and durable transitions for capture, publication, download, activation, receipt and recovery. | Changes is an explicitly unclosed object; durable storage/atomic publication/outcome, installation/queue removal, wait liveness, leases and incomplete registration handling remain unresolved. Prose names the required guarantees but does not close the protocol. | Complete TQ-04 and the ordering transport part of TQ-12. Define stage inputs/outputs, durable transitions, repeat/cancel/failure behavior and ownership enforcement without changing accepted policies. |
| TRD-F-006 | Blocker | TRD.md:512,687,1003–1005,1037; historical 2851–2854 | TQ-06; all PMs; OP-043–049; FR-010; NFR-004 | Schema upgrades preserve pending operations and only compatible rebuilt snapshots become usable. | Normal Local2 upgrade is settled, but already-captured old-schema batches and pending receipts/recovery across Master migration have no complete coordination contract. Required supported legacy mappings are also not specified; the dated manual-initialization record must not be mistaken for the new runtime policy. | Specify supported schema/batch/recovery compatibility and activation order; distinguish developer demo initialization from automatic released-app migration. Preserve pending changes and original SyncKey outcomes. |
| TRD-F-007 | Blocker | TRD.md:388–405,574–586,708–710,744–754,768,775–783,855–859,1048,2830–2831 | PM-001/010; DTO-012–014/036; OP-037–040/070; BR-006/023–028/044 | Complete six-catalog report persistence, reads and run/rename contracts. | ReportGroup root and contract extensions remain proposed; JSON serialization and rename-versus-selection preservation are unresolved. Result-row Key/Level and grouping/total semantics are not fully closed; period-only mutation still has unresolved errors/shape. | Complete Reporting extensions and migration, typed result semantics and exact JSON/read/save/date-only behavior. Preserve accepted CSV and read-only result rules. |
| TRD-F-008 | Blocker | TRD.md:75,94,615,704,711,768–773,844,861,950–951; BRD.md:261,265–267 | PM-012; DTO-008/015/035; OP-025/042/056; BR-012; FR-004/009 | Settings Save is atomic across Local/System; name generation has its required currency and bool input. | The two existing save actions commit separately; no combined transaction contract is selected. Account naming still documents only classification IDs while the accepted suffix needs currency and the explicit bool. | Define the Setup-owned atomic save boundary and currency-aware naming inputs/output/validation. Keep one scoped graph for the logical atomic action. |
| TRD-F-009 | Blocker | TRD.md:63,146–147,283–306,792,820–892; BRD.md:362–363,478–480 | BR-044; FR-003/004/006/008; catalog mutation contracts | Every independently persisted favorite/reorder UI action maps to a defined service operation. | SetFavorite and reorder are described as separate API actions with atomic flags, but the operation catalog has no explicit operation IDs/input-output mapping for these actions. Report catalog support is additionally incomplete. | Document existing service contracts where already implemented and add the required stable operation mappings; reuse their accepted atomic/rollback rules. Do not invent a frontend-only favorite flag. |
| TRD-F-010 | Important | TRD.md:246,697–698,845,876,928–936,1036; BRD.md:506–510 | UC-005-03; DTO-001/002; OP-026/057–060; FR-005 | Ledger navigation and refresh have one definitive contract and complete date-step behavior. | RefreshTransactions and its fields are recorded but have no stable OP assignment; old wrapper/overflow questions contradict later settled declarations. Month/quarter/year navigation at month ends and leap days remains genuinely open. | Assign/map the existing refresh operation, consolidate the settled wrapper and metadata-retention rule, and specify calendar-step edge behavior. Do not re-ask already settled cap or refresh policy. |
| TRD-F-011 | Blocker | TRD.md:64,1047; BRD.md:436–437; TRD.md:843,902 | OP-023; FR-005/006; TQ-15 | Nested account deletion respects existing-editor unsaved references while allowing new-editor entry removal. | Business deletion covers persisted references; TQ-15 still leaves the frontend draft-reference guard contract unresolved. No defined source/lifetime/handoff conveys current unsaved existing-editor references to all nested deletion paths. | Specify the coordinator/editor guard ownership and lifetime and enforce it for every enabled deletion gesture; preserve independent catalog commits and per-call DI scopes. |
| TRD-F-012 | Blocker | TRD.md:683–685,720,755–758,869,1024,1039,2829; BRD.md:188,702 | DTO-024; OP-050; BR-043; FR-012 | Full-snapshot replacement yields received business-change counts, excluding uploads and unchanged downloaded rows. | Latest-report lifetime, absent result, TXT and log policy are settled; how full replacement derives created/updated/deleted counts remains explicitly unresolved. | Define count comparison/baseline and correction/deletion/restoration classification; retain the accepted one-message currency-reordering rule. |

## Development handoff gate

- Both exact authoritative versions Approved: **No**, TRD:5–12/1138 and BRD:2245–2255.
- All mandatory checks pass: **No**. Failed TRD checks: TRD-C-001, TRD-C-003, TRD-C-005, TRD-C-006, TRD-C-007, TRD-C-008, TRD-C-009, TRD-C-010, TRD-C-011, TRD-C-012, TRD-C-013, TRD-C-014, TRD-C-015.
- Traceability complete: **No**, 8/67 = 11.940299%; capability-inclusive 8/79 = 10.126582%.
- Unique package findings: 17 total; 11 Blocker, 6 Important, 0 Minor. Trace findings are dependent occurrences of these same defects, not additional remediation items.
- Source readiness statement: correctly **Not ready**; no false current approval was found.
- Earlier authorized Business work remains authorized. This full-package gate is not a claim that Business implementation must be undone or that every Windows UI component needs further business discussion.

## Remediation order

1. Documentation editor: align the settled business corrections in BRD-F-001–005, repair Draft cardinality (TRD-F-003), and consolidate settled/current technical declarations (TRD-F-002/010).
2. Technical design with the requesting user: close setup/token, sync state/transport and pending-recovery migration contracts (TRD-F-004/005/006). Preserve accepted no-local-login and pending-data rules.
3. Technical design: close Reporting, atomic Settings/name generation, favorite/reorder mappings, editor-reference guards and received-change counts (TRD-F-007/008/009/011/012). Existing business behavior is the constraint, not a new question list.
4. Re-review the revised exact versions and seek their full approval only when substantive gaps are closed (TRD-F-001).

No source correction or implementation is performed by this review.
