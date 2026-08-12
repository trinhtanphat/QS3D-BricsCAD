# Work claim — Revision list delta encoding collision safety

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-list-delta-encoding-20260812-1106`
- Registered: `2026-08-12T11:06:00+07:00`
- Priority: P1 revision review fidelity

## Confirmed defect

`RevisionService.Compare(...)` correctly compares `SourceHandles` and `Dependencies` as canonical string sequences, but once it detects a difference it serializes each side with `string.Join(",", ...)`. Revision list values may themselves contain commas. Distinct lists such as `["A,B", "C"]` and `["A", "B,C"]` therefore produce the same rendered `Before`/`After` text (`A,B,C`) even though the service reports the field as changed. That makes the emitted revision delta internally contradictory and can hide the actual list difference from downstream review/UI consumers.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionService.cs`
- one focused new Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

Use an injective escaped list representation for revision `SourceHandles` and `Dependencies`: escape backslash and comma in each token, then join with comma. Preserve current output byte-for-byte for ordinary tokens that contain neither comma nor backslash, preserve case-insensitive canonical list comparison/sorting, and do not alter revision snapshot persistence format.

## Excluded scope

Do not touch the local-owned `RevisionCaptureXmlTextIntegritySmoke.cs`, RevisionSnapshotStore persistence, quantity/property diff semantics, CAD/UI, or build/release workflows.

## Validation boundary

Focused source-safe regression + exact readback only. No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed without execution.
