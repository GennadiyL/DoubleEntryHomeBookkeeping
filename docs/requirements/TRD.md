# Technical Requirements Document (TRD)

Status: Draft 0.1 — initial technical requirements; incomplete.
Updated: 2026-09-25.

## Synchronization discovery pointer — 2026-09-27

[Synchronization discovery v0.21](002-synchronization/discovery.md) records the current, unapproved technical direction: immutable versioned SQLite MasterDb files in Azure, first-release SQLite Admin.db, copy-and-publish followed by full local database replacement, SyncId/publication tracking, acknowledgements, and latest-version recovery. Earlier exact-result replay is superseded. Incomplete downloads restart; publication and local replacement require safe atomic switching. Registry hosting, metadata placement, local snapshot preparation, and cleanup details remain unresolved.

This is a provenance pointer, not a new approved TRD or implementation contract. Keep the existing core technical requirements and rework the TRD only at the later agreed phase.

## Discovery continuation — 2026-09-25

This is a legacy, unapproved technical draft. [Discovery Draft 0.1](001-personal-bookkeeping/discovery.md) preserves the new technical input: personal SQLite database, on-demand balance/report calculation, UTC timestamps, decimal rounding, independent stored entry rates, and ID-based saved-report JSON. It also retains the parent-reference requirement below. These notes have not been promoted into an approved technical baseline. Rework this TRD using GL Analysis after BRD approval; do not derive implementation authority from discovery.

## Purpose and scope

This document records agreed technical requirements. It starts with the existing parent-reference rules and will grow as the [BRD](BRD.md) becomes stable. Open business questions remain in the BRD.

## Parent references for aggregate children

`TemplateEntry` and `TransactionEntry` intentionally keep both their parent navigation property and parent foreign-key property.

The application needs to navigate from either child entity to its parent without resolving the parent separately. This is a deliberate performance optimization and overrides the general rule that aggregate-owned children omit parent references.

This exception applies only to `TemplateEntry` and `TransactionEntry`. Their parent navigation and foreign-key values must always identify the same parent.

