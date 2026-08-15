# Google Sheet native acceptance handoff — 2026-08-15

Status: `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`

This document is the explicit handoff for Google Sheet items whose source/cloud work is already integrated or guarded but whose final acceptance requires an interactive Windows desktop with licensed BricsCAD V25 x64. Remote/web agents must not keep re-reporting these items to the owner as work the owner should perform. Local agents own the native execution and must publish only sanitized evidence.

## Exact candidate rule

- Latest cloud-qualified anchor observed when this handoff was written: `v0.1.0-preview.10043`, exact release source `f25ef50bcc66f992ee3691d058e559f7658f9070`.
- Published V25 ZIP SHA-256: `1c1f784357f1cc9396e95d983d7abb8ad375a087043a24dd23752d7971d1a0e3`.
- Current `main` at handoff creation: `220da6a1a5986f945411f3e4858961d3531765f6`; it is a descendant of the green release and the post-release delta observed here is LOCAL-004 documentation only.
- Before any licensed run, refetch `main` and the latest successful V25 cloud prerelease. Use one exact committed/pushed descendant containing the reviewed Sheet implementation. Record exact Git SHA, plugin/Core `ProductVersion`, plugin SHA-256, BricsCAD build, drawing unit and the runner/interactive scenario used.
- Do not mix evidence from different SHAs or binaries. Do not promote source/static/cloud evidence to `LOCAL_PASS`.

## Priority and ownership

All rows below belong in the canonical local workflow (`docs/LOCAL-AGENT-INBOX.md`, issue #72). Claim the exact row/surface before implementation or execution. P0 rows are 6, 7 and 8 because they exercise native geometry/selection behavior. Rows 1–5 are acceptance/UX confirmation unless a real runtime defect appears.

If a licensed run exposes a production defect, stop the local qualification at the smallest sanitized reproduction, create/hand off a non-local source-fix issue/claim, and do not patch unrelated production code from the local execution lane unless the repository rules explicitly allow that scope.

## Row 1 — smoke executable Windows Application Error

Remote status: source wrapper and cloud Windows smoke are green.

Local acceptance:
- Build the exact candidate in Release and launch `QS3D.Core.SmokeTests.exe` normally on Windows.
- Confirm it exits cleanly with the expected test status and does not surface a Windows `Application Error` / CLR `0xe0434352` popup.
- Intentionally exercise one controlled managed failure path only if the existing harness supports it; the failure must become stderr/non-zero exit rather than an unhandled Windows popup.

PASS evidence: exact SHA/ProductVersion, process exit code, sanitized stdout/stderr summary, confirmation of no Windows Application Error dialog/process residue.

## Row 2 — Direct Draw preserves current view and continues drawing

Remote status: current-view preservation source guard is integrated.

Local acceptance:
- In a disposable DWG, set a non-default view/zoom.
- Run the affected Direct Draw beam workflow and create at least two consecutive elements.
- Verify the command does not force an unexpected `VPOINT`/`ZOOM` reset, the new object is visibly identifiable/highlighted as designed, and the command can continue to the next draw without destructive view jumps.
- Repeat cancel/finish and confirm view and semantic/CAD state remain coherent.

PASS evidence: exact SHA, BricsCAD build, before/after view identifiers or sanitized viewport measurements, command-result summary, no private screenshot required unless repository policy explicitly permits sanitized UI evidence.

## Row 3 — Family Manager quick property/template workflow

Remote status: quick workflow source and guards are integrated.

Local acceptance:
- Open the Family workflow in licensed V25 from an ordinary disposable project.
- Create/select a Family, use the quick template/property path, edit representative property values and apply them to a member.
- Confirm the number of user interactions is reduced as intended, values persist across close/reopen, and cancel paths do not partially mutate the project.
- Include Unicode property text and one invalid/rejected value path if supported by the current UI.

PASS evidence: exact SHA, Family/member counts before/after, sanitized property names/values, persistence result, cancel/no-partial-mutation result.

## Row 4 — Tool Diễn giải / Quantity Insight native UI and Locate

Remote status: detached/read-only detail, metrics and Locate source guards are integrated.

Local acceptance:
- Open Quantity Insight / Diễn giải on a project containing representative structural quantities.
- Verify detail/metrics render from the intended detached/read-only snapshot and opening/closing/refreshing the window does not change live project dirty state, `ChangeVersion`, timestamps or audit state.
- Use `Locate 3D` on at least one valid generated/native object and verify the expected entity is selected/located in the active DWG.
- Exercise stale/deleted/foreign object refusal and multi-DWG active-document mismatch; these must fail closed without cross-document selection or semantic mutation.

PASS evidence: exact SHA, before/after live-state invariants, located entity category/ownership summary, stale/foreign/multi-DWG refusal results.

## Row 5 — `QS3DSETUP` native modal host

Remote status: command uses BricsCAD modal hosting and source guards for close/rule-management behavior are integrated.

Local acceptance:
- Run `QS3DSETUP` in licensed V25 with an active disposable project.
- Confirm the WPF window is correctly owned by BricsCAD, remains responsive, blocks/returns focus as a proper modal host and does not create a second orphan window.
- Edit one representative quantity/rule setting, Save/Apply, reopen and verify persistence.
- Exercise dirty-close Cancel/Discard/Save paths and one invalid rule; no crash, orphan process/window or partial mutation is allowed.

PASS evidence: exact SHA, BricsCAD build, modal/focus result, persistence result, dirty-close matrix and cleanup result.

## Row 6 — `slabOpen` negative-Z and automatic native subtraction

Remote status: negative-Z extrusion, host ownership/first-use auto-build and Boolean-subtract source contracts are guarded; native Boolean execution remains local-only.

Priority: P0.

Local acceptance:
- Use a disposable Millimeter DWG and repeat in Meter if the runner/fixture supports it.
- Create a host Slab, then create the `slabOpen`/`QS3DDRAWSLABOPEN` source through the production path.
- Verify the opening extrusion is oriented through the host using the negative-Z contract where required and that first-use host build occurs automatically when the Slab has not yet been materialized.
- Verify native `Solid3d` subtraction actually reduces the host volume and preserves correct ownership/fingerprint metadata.
- Modify the opening and rebuild; the old generated result must retire exactly once and the replacement must be disjoint/coherent.
- Exercise stale/deleted/ambiguous host, Undo/Redo, save/reopen and a second DWG. All invalid cases must fail closed without partial host replacement.

PASS evidence: exact SHA/ProductVersion/plugin hash, drawing unit, host volume before/after, opening/host ownership summary, first-use auto-build result, rebuild replacement result, Undo/Redo, save/reopen, multi-DWG and cleanup.

## Row 7 — curved/round Beam + CIRCLE Slab/Column

Remote status: implementation and dedicated runtime harness are integrated; cloud build/package is green. This remains the main #1443 `LOCAL-003` gap.

Priority: P0.

Required runner:
- `scripts/test-bricscad-v25-curved-structural.ps1`

Required licensed matrix:
- Run on interactive Windows + licensed BricsCAD V25 x64 for both Millimeter and Meter.
- Beam control LINE.
- WCS-XY ARC Beam.
- WCS-XY CIRCLE Beam.
- straight open POLYLINE Beam.
- curved/bulged open POLYLINE Beam.
- CIRCLE Slab.
- CIRCLE Column.
- Verify finite captured Length/Area, owned `Solid3d` bounds/volume and production `QS3DBEAM/QS3DSLAB/QS3DCOLUMN -> capture -> eligibility -> snapshot -> QS3DBUILD3D -> StructuralSolidBuilder` behavior.
- Verify stale rebuild retirement/exact replacement.
- Verify fail-closed closed Beam POLYLINE, non-WCS/non-planar/invalid CIRCLE and any configured object/segment budget limits.
- Complete applicable Undo/Redo, save/reopen and multi-DWG isolation evidence.

PASS evidence: exact Git SHA + matching plugin/Core ProductVersion + plugin SHA-256, mm+m markers, shape-by-shape native measurements, ownership/rebuild, negative/refusal matrix, lifecycle and cleanup. Post sanitized evidence to issue #72 and parent #1443. Only this licensed evidence may promote the curved/round row from `PENDING_LOCAL`.

## Row 8 — `QS3DWALLQTY` Locate 3D native selection

Remote status: source fix for generated-solid Locate/read-only behavior is integrated and cloud-qualified; native host interaction still needs confirmation.

Priority: P0.

Local acceptance:
- Build a disposable wall/vách model that produces the quantity rows and generated 3D ownership expected by `QS3DWALLQTY`.
- Open the quantity UI and invoke `Locate 3D` for a valid row. Confirm the actual expected native/generated entity is selected/located in the active document rather than returning `no object selected`.
- Verify the quantity/read-only snapshot path does not mutate live project dirty/change-version/timestamp/audit state.
- Delete or stale the target and repeat; it must refuse clearly without selecting an unrelated object.
- Repeat with another active DWG to prove no cross-document selection.

PASS evidence: exact SHA, target semantic category/owner slot, located native entity summary, before/after project-state invariants, stale/deleted/multi-DWG refusal results and cleanup.

## Additional #1443 LOCAL-001 acceptance — automatic update discovery must stay non-modal

This is not a separate Sheet row but is part of the integrated #1443 source contract and cannot be proven by cloud execution.

Local acceptance:
- Start licensed BricsCAD V25 with the exact candidate and a configuration that allows the existing automatic update-discovery path to detect an available update without modifying production endpoints/secrets.
- Automatic discovery may notify through the non-modal/editor path but must not open/block on Update Center, steal input, duplicate windows or mutate project/CAD state.
- Then explicitly run `QS3DUPDATE`; the user-invoked command must open the intended Update Center modal UI and return focus/cleanup correctly.
- Repeat document activation/switching while discovery callbacks are pending and verify no stale cross-DWG callback/window appears.

PASS evidence: exact SHA, sanitized automatic-discovery event/result, explicit `QS3DUPDATE` modal result, no startup blockage/duplicate windows/cross-DWG mutation and clean process/UI state.

## Closeout rules for local agents

1. Start from a clean exact candidate; zero pre-existing BricsCAD processes unless a scenario explicitly requires an operator-owned session (none above do).
2. Run focused preflights, full Core smoke and installed-reference V25 `Release|x64` build before licensed execution.
3. Use repository-generated disposable DWGs where possible. Never commit private/customer DWGs, raw machine paths, ProjectIds, Handles, fingerprints, proprietary DLLs, license data or secrets.
4. Restore/remove disposable DWGs, sidecars, backups, lock files, scripts, probe environment and all test-owned BricsCAD processes after every run.
5. Update the matching `LOCAL-001`/`LOCAL-003` item in `docs/LOCAL-AGENT-INBOX.md` with exact sanitized evidence. Post the same bounded summary to issue #72. For rows 6–8 and the updater boundary, also update parent issue #1443.
6. If all eight rows pass on an applicable exact descendant and the #1443 updater/native boundaries pass, #1443 may be considered for licensed closeout. Do not close it from source/cloud evidence alone.
