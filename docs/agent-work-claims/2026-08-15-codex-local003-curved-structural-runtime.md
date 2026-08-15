# LOCAL-003 curved and round structural runtime qualification

Status: COMPLETED
Agent: codex-local003-curved-structural-20260815
Issue: #72 (source capability lineage: #1443)
Branch: `agent/local003/local003-curved-structural-runtime-20260815`
Baseline: `d1e557bff248df77957d9096e1e4bc52f17242b0`

## Reserved scope

- Qualify the existing licensed BricsCAD V25 curved/round structural runtime harness through `scripts/test-bricscad-v25-curved-structural.ps1` on clean committed and pushed exact SHA candidates.
- Run fresh guarded disposable-copy matrices for both `Millimeter` and `Meter` native drawing units.
- Exercise the production capture-to-build route for Beam LINE, WCS-XY ARC, WCS-XY CIRCLE, open straight POLYLINE and open curved/bulged POLYLINE, plus CIRCLE Slab and CIRCLE Column.
- Verify sanitized snapshot Length/Area, generated `Solid3d` ownership, bounds/volume, stale rebuild retirement and the provided closed-Beam-polyline and non-WCS-CIRCLE fail-closed controls.
- Publish only sanitized exact-SHA evidence and the narrow matching LOCAL-003 queue/handoff update. This bounded harness result does not complete the broader lifecycle, private-DWG, multi-DWG or customer-release matrix.

Expected repository surfaces are limited to:

- `docs/agent-work-claims/2026-08-15-codex-local003-curved-structural-runtime.md`;
- `scripts/test-bricscad-v25-curved-structural.ps1` only if licensed execution proves a genuinely local-dependent harness defect;
- `src/QS3D.BricsCAD.V25/CurvedStructuralRuntimeProbeCommands.cs` only if licensed execution proves a minimum automation-only probe defect;
- `scripts/preflight-curved-structural-runtime.py` only when the same native-dependent harness/probe correction requires a focused static guard update;
- `docs/LOCAL-AGENT-INBOX.md` and the existing #1443 addendum/current handoff only for sanitized exact-SHA results.

Generated licensed-runtime evidence remains under ignored `artifacts/` or another explicitly local evidence directory.

## Exclusions

- No production structural builder, semantic capture, Core/domain, user-facing command or product behavior change.
- No general bug fixing, broad repository audit, unrelated documentation cleanup, private/customer DWG, proprietary binary, secret, screenshot or raw machine evidence.
- Any ordinary source defect exposed by licensed qualification is handed to a non-local agent with the smallest sanitized reproduction; this local worker stops at that defect boundary.
- Scope expansion for a native-dependent runner/probe correction must be recorded on issue #72 and in this claim before that surface is edited.
- No GitHub Actions dispatch/re-run/cancel, release operation, direct `main` write, force-push or merge.

## Validation plan

1. Fetch current `origin/main`, verify the claim baseline, and publish this claim on the task branch/draft PR before any build or licensed execution.
2. Reconcile current `main` safely if it moves, then commit and push one exact candidate SHA before running any gate/build/runtime step.
3. Confirm zero BricsCAD processes, clean worktree, matching adapter/Core `ProductVersion +SHA` and plugin SHA-256.
4. Run the focused curved-structural preflight, PowerShell AST parsing, strict manual-CI and local-handoff policy gates, full Core smoke, and installed-reference V25 `Release|x64` build sequentially for the weak local machine.
5. Create two fresh guarded disposable copies outside tracked source and run the licensed V25 harness once with `NativeDrawingUnit=Millimeter` and once with `NativeDrawingUnit=Meter`.
6. Verify exact-process cleanup, unchanged/restored disposable drawing bytes, no sidecar/backup/lock/script/private-state residue, no tracked artifacts, and sanitized markers only.
7. Publish exact evidence to issue #72 and the draft PR, update the canonical LOCAL-003 status narrowly, and stop before merge.

## Exact licensed evidence

- Tested candidate: committed and pushed SHA `f3640ebbb35ecdda7a68c85e12d57f4fa717612d` on draft PR #1614.
- Host: licensed BricsCAD V25.2.10 Windows x64; adapter/Core ProductVersion `0.1.0-preview.10041+f3640ebbb35ecdda7a68c85e12d57f4fa717612d`.
- Adapter SHA-256: `FE2201BEF17D08EC79FC791546E7AA8FB1C0E56256F285BADE65523FFA52D910`; Core SHA-256: `26024B4F7D4B839ED89A4742674C679029C0402871952DB0160FF55473352431`.
- The focused curved-structural gate, PowerShell AST parse, strict manual-CI/local-handoff guards, full Core smoke (`ALL PASS`) and installed-reference V25 `Release|x64` build (`0 warnings / 0 errors`) passed before licensed execution.
- Fresh Millimeter (`INSUNITS=4`) and Meter (`INSUNITS=6`) runs each passed all seven positive cases and seven stale-rebuild replacements. Beam LINE/ARC/CIRCLE/straight-open-POLYLINE/curved-open-POLYLINE reported the expected Length, positive owned `Solid3d` volume and `0..0.5 m` Z span; CIRCLE Slab/Column reported the expected Area, positive owned volume and `0..0.2 m` / `0..3 m` Z spans. Old generated handles were retired by the automation probe before replacement ownership was accepted.
- Closed Beam POLYLINE and non-WCS Beam CIRCLE failed closed in both units. Sixty-six common marker fields matched across mm/m within the guarded tolerance.
- Both hosts exited gracefully; exact process, script and private-state cleanup passed. Both guarded synthetic copies remained read-only through host exit, unwritten/restored, and retained SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`. Retained ignored artifacts contain only the synthetic copy plus sanitized marker/metadata; forbidden marker fields and `.qsdb/.bak/.dwl/.dwl2/.scr` residue are absent.
- No runner/probe or production source correction was needed. No GitHub Actions, release or merge was run.

This completes only the dedicated curved/round capture-to-build harness row. Representative Undo/Redo, explicit save/cold reopen, broader stale-rebuild lifecycle, multi-DWG isolation and authorized private-DWG coverage remain `PENDING_LOCAL`; overall LOCAL-003 remains `IN_PROGRESS` and customer-release qualification remains false.
