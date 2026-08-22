# Interchange source-handle provenance

`QS3D.SemanticSnapshot` v1 declares every element source reference with `sourceRefScope = "drawing-local"`. A handle exported from drawing A is therefore not authority to claim the same handle in drawing B.

`QS3DINTERCHANGEPROVENANCE` implements the safe `PreserveAsProvenanceOnly` slice without changing that rule.

## What is stored

After strict snapshot validation, the command stores source-project and per-source-element records under `ProjectState.Metadata` using the prefix:

`Interchange.Provenance.Source.*`

The record contains the source project identity/fingerprint and, for elements that have them, their drawing-local source handles plus element fingerprint/scope. Identity tokens and record fields are encoded so metadata key/value delimiters cannot turn a source handle into a generated-owner slot.

Re-importing provenance for the same source project replaces that source project's previous provenance records, so removed handles do not remain as stale provenance. Records from other source projects are preserved.

## What is deliberately not changed

The provenance operation does **not**:

- add anything to `ProjectElement.SourceHandles`;
- write `Generated*Handle(s)` or `PhysicalOpeningCut*` ownership properties;
- touch native DWG entities;
- import/replace Zone, Floor, Family or Element semantic identities;
- regenerate 3D/rebar/curtain/grid geometry;
- cut openings;
- save `.qsdb` automatically.

It records only counts in audit/status text; raw source handles are not copied into the audit message.

## Re-export boundary

`ProjectInterchangeJsonExporter` exports the project/family/element semantic snapshot, but does not serialize `ProjectState.Metadata`. Consequently, provenance imported from another project is not emitted later as the active drawing's `sourceHandles`.

The active drawing's exported `sourceHandles` still come only from each local `ProjectElement.SourceHandles` collection.

## Persistence and rollback

The metadata write uses `ProjectStateSnapshot` rollback. If storing/encoding/audit fails before completion, the previous project state is restored.

The provenance is part of project state and is expected to persist when the user explicitly saves the QS3D project. Exact save/reopen behavior remains subject to the normal BricsCAD V25/runtime qualification matrix.

## Relationship to semantic import

This command is intentionally separate from `QS3DINTERCHANGEIMPORT`, `QS3DINTERCHANGEAPPEND` and `QS3DINTERCHANGEUSESOURCE` in the first implementation. Semantic import and provenance retention therefore cannot be confused as one authorization decision.

A future combined import UX may offer provenance retention as an explicit option after its atomicity and save/reopen behavior are V25-qualified. It must still never adopt imported handles as native target ownership merely because provenance was retained.
