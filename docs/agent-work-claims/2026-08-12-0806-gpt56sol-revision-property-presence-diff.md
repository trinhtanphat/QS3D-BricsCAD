# Work claim — revision property presence diff fidelity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-property-presence-diff-20260812-0806`
- Registered: `2026-08-12T08:06:00+07:00`
- Baseline main SHA: `e394d708125977bc1f41e5fea908e8ee263eeba3`
- Priority: evidence-driven remote-safe revision fidelity during owner-requested `continue all`

## Confirmed defect

`RevisionService.CompareProperties()` unions property keys but collapses both an absent property and a present property whose value is `""` to the same empty string before calling `Add(...)`. As a result, adding or removing an explicit empty property produces no revision delta even though Core property-map semantics treat absent→empty as a real mutation.

## Reserved scope

Preserve property-key presence in revision comparison so absent↔present-empty changes are reported while present-empty↔present-empty remains a no-op.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionService.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- Revision snapshot persistence/store/backup handling and its recent backup-preservation lane.
- Quantity tolerance/comparison, identities, dependencies, source handles, category validation, capture semantics, Revision UI/native runtime, or report formatting.
- No new sentinel string is introduced into `Before`/`After`; the existence of a `RevisionFieldDelta` records the presence transition while values remain the actual empty text.
- No GitHub Actions or LOCAL_ONLY qualification.

## Validation plan

- Before absent / after `Properties["Note"] = ""` produces one Changed delta with field `Property:Note`.
- Before explicit empty / after absent likewise produces a Changed delta.
- Explicit empty on both sides remains no-op.
- Ordinary non-empty property changes remain unchanged.
- Re-read exact branch diff and moving `main` before integration; do not dispatch Actions.

## Coordination

Recent revision snapshot backup work is confined to snapshot persistence/store behavior. No recent claim/history search found a revision property-presence comparison lane. A concurrent Quantity Settings schema-badge closeout landed immediately before this claim and was verified non-overlapping; the baseline above records that actual parent.

## Completion condition

Focused source and regression are merged to current `main`, remote source is re-read, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
