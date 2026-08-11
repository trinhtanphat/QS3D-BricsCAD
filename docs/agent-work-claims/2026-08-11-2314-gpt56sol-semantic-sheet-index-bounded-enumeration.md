# Work claim — Semantic sheet index bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-sheet-index-bounded-20260811-2314`
- Registered: `2026-08-11T23:14:00+07:00`
- Baseline main SHA: `c607ee3b73ba6091d39c45ad5f69d8c05829c1bd`
- Priority: P2 source-proven bounded-input regression hardening

## Reserved scope

Fix `SemanticSheetIndexBuilder.Build` so its declared `MaxSheets = 10000` bound is enforced while enumerating the input instead of after `sheets.ToList()` has already materialized the entire enumerable. The current implementation defeats its own resource bound for very large or non-terminating inputs.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetIndexBuilder.cs`
- `tests/QS3D.Core.SmokeTests/SemanticViewSheetPlannerSmoke.cs`
- this claim file for close-out

## Explicit exclusions

- No semantic sheet/view planning geometry or placement behavior changes.
- No documentation catalog persistence/schema changes.
- No BricsCAD/WPF/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Validation plan

- Verify claim reachability from current `main`, then re-fetch exact source/test blobs before implementation.
- Replace unbounded `ToList()` source materialization with one-pass bounded enumeration that throws as soon as the 10001st sheet is observed.
- Preserve null-row, duplicate-ID/number, ordering, immutability and valid <=10000-sheet behavior.
- Add a sentinel enumerable regression that yields 10001 sheets and would throw if the builder asks for another item; expected failure must be the builder's own bound exception, proving it does not over-enumerate.
- Source/static readback plus committed smoke coverage only; no local .NET/BricsCAD/Actions PASS claim.

## Coordination

The original semantic sheet index feature claim is completed. No recent active sheet-index claim appears in commit history. This is a narrow Core documentation bounded-enumeration hardening lane.

## Completion condition

The 10000-sheet bound is enforced during enumeration, focused regression coverage is committed on `main`, current source is re-read, and this claim is marked `COMPLETED` with exact SHAs and actual validation scope.
