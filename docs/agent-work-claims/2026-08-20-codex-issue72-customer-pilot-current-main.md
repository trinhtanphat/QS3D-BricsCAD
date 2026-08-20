# Work claim — issue #72 exact-main customer-style V25 pilot

- Status: `ACTIVE / PENDING_LOCAL`
- Lane-Key: `issue-72`
- Canonical owner/session: `codex-root-20260820`
- Canonical carrier: `agent/codex/issue72-customer-pilot-20260820`
- Baseline main SHA: `2f1da84d4a93ba54eafb37832bd5b1c7e2bdd48b`
- Runtime: licensed BricsCAD V25.2.10 x64 on disposable local copies only

## Qualification gap

Historical local evidence proves many bounded LOCAL-001 rows and an older
single-floor quantity workbook, but it does not prove the complete requested
customer-style pilot on one current exact SHA. In particular, the same pilot
still needs bounded evidence for BQ Locate, cross-DWG isolation, a real native
edit followed by reconcile/Undo/Redo/rebuild, and stale or unsupported
fail-closed behavior.

## Reserved scope

- Re-run one one-floor disposable pilot on the exact committed candidate using
  production Project/Floor/Family and Wall/Beam/Column/Slab/StructuralWall/
  Foundation/Door commands.
- Require generated ownership, repeated regeneration, gross/net BQ,
  spreadsheet export, BQ Locate, explicit save/close, fresh-process reopen and
  recalculation to remain coherent in the same project.
- In that same qualification, exercise one supported top-level native source
  edit followed by production `QS3DSYNCSOURCE`, Undo/Redo and rebuild; prove a
  second DWG is not bound or mutated.
- Exercise bounded unsupported/stale input against disposable state and require
  refusal without semantic, CAD, audit or persistence mutation.
- Keep scripts, drawings, workbooks, screenshots and raw logs under ignored
  `artifacts/`; publish only allowlisted aggregate evidence.
- Update this claim and the existing `LOCAL-001` handoff/status documentation
  only after exact runtime evidence exists.

## Initial implementation boundary

This is a runtime-qualification lane. No production, adapter, Core, shared
runner, probe, workflow or release source is reserved initially. Test-owned
scripts may remain ignored/local. If licensed execution exposes a product or
reusable-runner defect, stop, publish the smallest sanitized evidence and
register a separate concrete Issue/Lane-Key before any source edit.

## Exclusions

- Do not edit or supersede the open issue #3289 / PR #3290 Beam-dependent MOVE
  carrier; its exact licensed PASS is independent evidence only.
- Do not overlap the historical ACTIVE NETLOAD-startup or LOCAL-003 Level
  source claims; this lane may observe their current-main behavior but does not
  own their implementation surfaces.
- No private/customer DWG, proprietary BricsCAD DLL, machine path, raw Handle,
  ProjectId, drawing fingerprint, screenshot or unsanitized runtime capture is
  committed.
- No 4D/5D feature work, custom DrawJig/grip implementation, package/signing,
  release publication, manual GitHub Actions dispatch/rerun/cancel or write to
  `main`.
- Issue #72 and the broader customer-release qualification remain open unless
  every applicable exact-SHA acceptance row is actually proven.

## Validation plan

1. Push this claim on the canonical branch before licensed execution.
2. Require a clean exact branch SHA, zero pre-existing BricsCAD process, Core
   Release build/smoke, generic and aggregate preflights, installed-reference
   V25 `Release|x64` build and matching adapter/Core ProductVersion.
3. Create a fresh disposable pilot directory without overwriting historical
   evidence; launch and clean up only test-owned BricsCAD processes.
4. Validate the exported workbook programmatically and render it for visual QA
   while retaining the raw workbook outside Git.
5. Record only sanitized booleans/counts/totals and exact SHA/build identity.
6. Commit/push the bounded handoff, observe the automatic branch evidence when
   applicable, open the single canonical PR, and stop before merge.
