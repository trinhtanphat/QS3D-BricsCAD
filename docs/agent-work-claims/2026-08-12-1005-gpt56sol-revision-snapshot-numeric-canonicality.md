# Work claim — Revision snapshot numeric canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-snapshot-numeric-canonicality-20260812-1005`
- Registered: `2026-08-12T10:05:00+07:00`
- Baseline main SHA: `2b42086f53c87111c40566e7f30858248ebbec7a`

## Confirmed defect

`RevisionSnapshotStore.Serialize` emits every persisted quantity value with `ToString("R", CultureInfo.InvariantCulture)`, but `Load` delegates to a permissive `Number(...)` parser that accepts alternate spellings such as `1.0`, `+1`, padded values, or equivalent exponent forms. A revision snapshot can therefore load successfully and then round-trip to a different persisted numeric token even though the surrounding revision format already enforces canonical IDs, categories, XML structure and finite values.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs`
- one focused persisted-file smoke for revision quantity numeric token canonicality
- this claim file

Require loaded quantity tokens to exactly match the serializer-owned round-trip representation for the parsed finite value. Preserve accepted numeric values, finite checks, serializer output, timestamp compatibility, revision compare semantics, backup behavior and XML schema.

## Completion

Complete only after source + focused regression are on current `main`, exact SHAs are recorded here, and this claim is marked `COMPLETED`. No GitHub Actions, local .NET build/smoke execution, or BricsCAD V25/V26 runtime qualification is claimed by this remote lane.