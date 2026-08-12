# Work claim — revision property presence diff fidelity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-revision-property-presence-diff-20260812-0806`
- Registered: `2026-08-12T08:06:00+07:00`
- Baseline main SHA: `e394d708125977bc1f41e5fea908e8ee263eeba3`
- Priority: evidence-driven remote-safe revision fidelity during owner-requested `continue all`

## Confirmed defect

`RevisionService.CompareProperties()` unions property keys but previously collapsed both an absent property and a present property whose value is `""` to the same empty string before calling `Add(...)`. As a result, adding or removing an explicit empty property produced no revision delta even though Core property-map semantics treat absent→empty as a real mutation.

## Implemented fix

- Preserve `TryGetValue(...)` presence on each side while comparing property maps.
- Emit a `RevisionFieldDelta` when key presence differs, keeping the actual text values unchanged.
- Preserve explicit-empty↔explicit-empty no-op behavior and ordinary equal/non-equal value comparison semantics.
- Add focused Core smoke coverage for add-empty, remove-empty, unchanged-empty and ordinary non-empty changes.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `tests/QS3D.Core.SmokeTests/RevisionPropertyPresenceDiffSmoke.cs`
- this claim file

## Excluded scope

- Revision snapshot persistence/store/backup handling and its recent backup-preservation lane.
- Quantity tolerance/comparison, identities, dependencies, source handles, category validation, capture semantics, Revision UI/native runtime, or report formatting.
- No new sentinel string is introduced into `Before`/`After`; the existence of a `RevisionFieldDelta` records the presence transition while values remain the actual empty text.

## Integration evidence

- PR `#647` was reviewed as a two-file focused diff and was mergeable against moving `main`.
- PR head: `8bb1373480665142e4b7b4e64225ab672129426f`.
- Squash integration commit on `main`: `dab8a9b89a4d44aa6080921351acfd6ee09d0f5b`.
- Remote `main` was re-read after integration and confirms the presence-aware `CompareProperties(...)` path plus the focused smoke source.

## Validation boundary

Exact remote source/test readback and focused committed smoke coverage only. No GitHub Actions were dispatched, no local .NET build PASS is claimed, and no licensed BricsCAD V25 runtime PASS is claimed.
