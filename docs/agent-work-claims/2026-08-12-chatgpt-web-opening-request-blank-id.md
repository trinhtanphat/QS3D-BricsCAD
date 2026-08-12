# Work claim — targeted opening request blank-id fail-closed

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-opening-request-blank-id`
- Registered: `2026-08-12T13:49:00+07:00`
- Baseline main SHA: `7493794b079e188fa1ac9ab04411eb5bb0b3f359`
- Priority: destructive targeted opening cuts must reject malformed explicit target collections before any CAD boolean mutation instead of silently dropping blank/null ids and executing the valid subset.

## Reserved scope

Harden only explicit requested-opening normalization in `src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs`, plus focused source regression/preflight coverage and this claim close-out.

## Canonical evidence

- Before this fix, `NormalizeRequestedOpenings(...)` constructed a set from `openingIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())`, so an explicit request such as `[validOpeningId, "   "]` silently became `[validOpeningId]`.
- The returned set is the authorization boundary for the subsequent targeted physical cut. Silently narrowing malformed destructive input could therefore execute a partial request instead of failing before CAD mutation.
- `PhysicalOpeningCutTargetStateCodec.Normalize(...)` already rejects blank opening ids (commit `044c84903fab09428533bf526bb9e6e99bb3437b`), so the change aligns explicit requested-cut normalization with persisted target-state fail-closed handling.

## Implemented

- Source fix: `2860296bf1c6805b6dcf5d101b2fa24d3c8c25a8` — `NormalizeRequestedOpenings(...)` now enumerates every explicit requested id and throws on null/blank before semantic lookup or CAD work; valid ids retain trim + case-insensitive set semantics; `openingIds == null` remains the all-linked path.
- Regression/preflight: `c45a77b442ce3abe530ff9fa8c609e3409b30313` — `scripts/preflight-opening-request-blank-id.py` rejects reintroduction of subset filtering and locks validation ordering before `FindElement`, project rollback scope and `BoolSubtract`.
- Main ancestry was verified after concurrent writes: source fix is the merge base and six commits behind then-current `main` `c4f8259fdfe4b80b1ddf8c99cd22d87171872bdc`; regression is the merge base and four commits behind that same `main`.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs`
- `scripts/preflight-opening-request-blank-id.py`
- this claim file for close-out

## Excluded scope

- No changes to physical cut geometry, target-state codec encoding, Direct Draw, UI/ribbon, health logic or unrelated opening ownership rules.
- No GitHub Actions dispatch, build/release publication or BricsCAD V25 runtime PASS claim.

## Validation status

- Exact source readback and Git ancestry verified through GitHub.
- Static regression is committed; no GitHub Actions were dispatched in this lane.
- BricsCAD V25 runtime qualification remains local-only and is not marked PASS here.

## Coordination

Recent main claims reserve Grid Annotation handle identity, semantic schedule canonical id, formula underflow, Preview Review CDATA, interchange name canonicality, Room Finish XLSX, QSDB drawing identity round-trip and related independent lanes. This lane changed only opening request normalization, its dedicated preflight and this claim.

## Completion condition

Completed: malformed explicit opening-id collections now fail closed before any physical cut can run, focused regression is on `main`, and ancestry has been verified after concurrent writes.
