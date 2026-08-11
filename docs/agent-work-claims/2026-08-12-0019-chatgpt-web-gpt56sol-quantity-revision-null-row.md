# Work claim — Quantity revision summary null-row integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-revision-null-row`
- Registered: `2026-08-12T00:19:00+07:00`
- Baseline main SHA: `fa73d76c8d76de5c53ebaa458a492d4b1716f0f0`
- Priority: P2 — malformed public revision report input must not be silently dropped from summary processing.

## Confirmed defect

`QuantityRevisionReport.Summarize(IEnumerable<QuantityRevisionRow>)` currently begins with `rows.Where(x => x != null && !string.IsNullOrWhiteSpace(x.QuantityName))`. A null row is therefore silently ignored. This differs from other current Core collection boundaries, which fail closed on null entries, and can hide corruption in caller-supplied review data.

Rows with a blank `QuantityName` are intentionally retained as ignorable element-only add/remove rows and must continue to be skipped. Only null collection entries become invalid.

## Reserved scope

- `src/QS3D.Core/Revisions/QuantityRevisionReport.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRevisionSummaryNullRowSmoke.cs` (new auto-registered focused smoke)
- this claim file

## Intended contract

- `Summarize(...)` rejects a null row with `ArgumentException` and identifies its input index.
- Blank-quantity rows remain ignored exactly as before.
- Valid grouping, case-insensitive quantity names, finite/overflow math, ordering, and report build behavior remain unchanged.

## Coordination

Recent revision claims cover capture IDs, snapshot payloads, dependencies and canonical identities. No current claim was found for `QuantityRevisionReport.Summarize` null-entry handling.

## Validation plan

- Add focused auto-registered smoke for null-row rejection, continued blank-name skipping, and normal case-insensitive aggregation.
- Re-fetch source before update, SHA-guard write, inspect exact diffs, and close this claim.
- No GitHub Actions dispatch; no executable .NET or BricsCAD V25 runtime PASS claim from this hosted environment.

## Completion condition

Null summary rows can no longer disappear silently, valid summary semantics are preserved, regression is on `main`, and this claim is closed.
