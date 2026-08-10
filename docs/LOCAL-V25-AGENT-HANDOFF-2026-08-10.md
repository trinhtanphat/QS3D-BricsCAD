# Local BricsCAD V25 agent handoff — 2026-08-10

This document is the canonical handoff for work that **cannot be completed or truthfully certified from a remote/source-only agent**. It is intentionally execution-oriented: each item identifies the local-only dependency, the exact validation path, the expected evidence and the acceptance rule.

Always read `AGENTS.md`, `docs/PRODUCT-BOUNDARY.md`, `CI_POLICY.md`, `docs/AGENT-HANDOFF-CURRENT-2026-08-10.md` and the latest `main` before starting.

## Non-negotiable policy

QS3D is a BricsCAD V25 x64 plugin. Do not convert it into a standalone CAD executable and do not copy BLT proprietary source/assets.

Do **not** dispatch GitHub Actions merely because this handoff exists or because an agent is continuing work. Local validation should be performed directly on the Windows/BricsCAD machine unless the repository owner separately and explicitly requests a GitHub Actions run. Do not publish a release without a separate explicit release request.

Never commit BricsCAD-owned DLLs, private/customer DWGs, screenshots containing confidential drawing data, signing keys/certificates, or other proprietary fixtures.

## 1. Start from an exact clean source SHA

On the local Windows machine:

```powershell
git fetch origin
git checkout main
git pull --ff-only origin main
git status --short
git rev-parse HEAD
```

Acceptance:

- working tree is clean unless the agent is intentionally testing an uncommitted patch;
- the exact SHA is written into the local evidence/report;
- if another agent pushes while testing, finish and report the tested SHA before syncing again. Never describe an older test as proof of the newer head.

## 2. Local qualification runner

Set the BricsCAD installation path, then use the repository-local qualification runner:

```powershell
$env:BRICSCAD_V25_DIR = 'C:\Program Files\Bricsys\BricsCAD V25 en_US'
$env:BRICSCAD_V25_PROFILE = 'QS3D-V25-Test'   # optional dedicated profile

.\scripts\local-v25-qualification.ps1 `
  -BricsCadDir $env:BRICSCAD_V25_DIR `
  -Profile $env:BRICSCAD_V25_PROFILE `
  -RunRuntime
```

The script performs the manual-only policy guard, generic preflight, all feature preflights, PowerShell parse validation, Core Release build, Core smoke tests, V25 x64 adapter build, and optional real `NETLOAD`/`QS3DRUNTIMEPROBE` plus screenshot. It writes SHA/hash/version evidence under `artifacts/local-v25-qualification/` and never dispatches GitHub Actions.

Use `-BuildPackage` only when packaging itself is part of the requested local test. Packaging is not publication.

Acceptance:

- `qualification-metadata.json` reports `PASS` for the tested exact SHA;
- `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll` are built from that SHA and hashes are recorded;
- when `-RunRuntime` is used, `runtime/runtime-result.txt`, `runtime/runtime-metadata.json` and the screenshot exist and match the tested DLL;
- failures are fixed or documented; never replace a failed local result with a source-only assumption.

## 3. Interactive Direct Draw qualification — P0

Commands:

- `QS3DDRAWWALL`
- `QS3DDRAWBEAM`
- `QS3DDRAWCOLUMN`
- `QS3DDRAWSLAB`

Run each in a clean test drawing and a private representative drawing. Verify:

- World UCS, translated planar UCS, and planar UCS rotated 30°, 45° and 90°;
- tilted/3D UCS fails closed **before source creation**;
- millimeter and meter drawings;
- valid dimension prompts and inherited active Family values;
- ESC/cancel at every point/prompt stage leaves no new source/semantic/generated residue;
- forced native build failure rolls back operation-owned LINE/POLYLINE and generated output;
- successful operation creates exactly one semantic element, stable source handle and expected generated owner state;
- `QS3DBUILD3D` can rebuild after selecting semantic/generated aliases and resolves back to complete source handles;
- PaperSpace/Layout is rejected for P0 authoring.

Evidence:

- command transcript or concise checklist per command/UCS case;
- screenshots only with non-confidential/synthetic drawings;
- before/after handle counts for at least one forced-failure rollback case.

Do not weaken tilted-UCS rejection merely to make a test pass. Current builders still contain WCS-planar assumptions.

## 4. Interactive Direct Draw qualification — guarded P1

Commands:

- `QS3DDRAWGLASSWALL`
- `QS3DDRAWWALLPIER`
- `QS3DDRAWSTRUCTWALL`
- `QS3DDRAWFOUNDATION`

Verify active Family selection, prompts, source creation, semantic capture, nested `QS3DBUILD3D`, generated-owner verification and rollback when the nested build does not produce the required live generated host.

Specific boundaries:

- WallPier P1 is two-point LINE-only; do not claim arbitrary freeform/open-polyline specialized profile parity;
- StructuralWall uses the current supported LINE path;
- Foundation uses the current supported closed-POLYLINE path;
- GlassWall creates the backing host; Curtain frame behavior is a separate `QS3DCURTAIN3D` contract.

## 5. Door / Opening / host / physical-cut qualification

Commands:

- `QS3DDRAWDOOR`
- `QS3DDRAWOPENING`
- `QS3DAUTOLINKHOSTS`
- manual host link/unlink commands exposed by current UI/command catalog;
- `QS3DCUTSELECTEDOPENINGS`
- legacy `QS3DCUTOPENINGS`

Required cases:

- one clear host;
- no host;
- two ambiguous hosts;
- Floor mismatch;
- Zone mismatch;
- elevation/sill mismatch;
- gap beyond configured tolerance;
- same opening cut rerun with identical fingerprint;
- different cut state on an already-cut generated host must fail closed until rebuild;
- one selected opening, multiple selected openings, multiple hosts and mixed unrelated CAD selection.

Direct Draw must not silently become an automatic destructive cut. One-shot Door/Opening + physical cut UX is intentionally deferred until the targeted-cut transaction/rollback behavior is proven in real V25 and the product owner explicitly wants that destructive shortcut.

## 6. Room Auto and HT_Phòng lifecycle qualification

Use mixed LINE/POLYLINE/ARC/SPLINE boundaries and verify:

- `QS3DROOMAUTO` creates deterministic active Room elements;
- rerun/update reuses the correct Room identity where provenance is unambiguous;
- topology change marks obsolete auto Rooms stale instead of silently remapping ambiguous split/merge topology;
- re-activated/reused Rooms synchronize existing HT_Phòng Floor/Zone/fingerprint/provenance;
- exactly one Room dependency remains on each finish after legacy dependency repair;
- removed Room metrics such as `OpeningAreaM2` / `DoorWidthM` do not remain as stale finish deductions;
- Floor Finish, Waterproofing, Skirting, Wall Finish and Ceiling Finish regenerate together;
- a forced finish synchronization/regeneration failure rolls the complete batch back rather than leaving partial state;
- save/reopen preserves Room/finish identity and quantities.

If a local fixture exposes a Room-to-Door deduction rule that is not represented in current source semantics, document the required mapping instead of guessing which Door belongs to which Room.

## 7. Curtain Wall local qualification and remaining product work

Current source supports the backing GlassWall host and guarded Curtain frame paths. Test:

- LINE path;
- open POLYLINE path;
- bulged open POLYLINE path;
- opening-aware frame interruption;
- identical rerun idempotency/live fingerprint behavior;
- stale frame lifecycle after opening property/link changes;
- ownership/erase refusal when generated ownership is ambiguous or foreign.

Still local/product work after current source proof:

- richer curved/open-path visual/runtime qualification on representative geometry;
- panel-by-panel backing glass solids are not complete product parity;
- any further multi-segment product geometry must preserve generated ownership and fail-closed replacement semantics.

Do not merge a geometry implementation that only works on one sample without a deterministic planner/source contract and rollback/ownership tests.

## 8. Tường KT / WallPier / multi-owner wall reconciliation

These are intentionally not guessed remotely:

- physical multi-owner L/T/X/Multi wall-solid union/reconciliation;
- a safe unmerge/rebuild model when source centerlines change;
- richer WallPier open-POLYLINE specialized profile authoring.

Before implementation, a local agent must define ownership semantics for a generated solid that represents more than one semantic wall. The design must answer:

- which semantic element owns the solid handle;
- how all contributing element IDs are persisted;
- how selection/Locate/BQ resolve a shared solid;
- how a single source-wall edit invalidates and rebuilds the shared result;
- how opening cuts and Curtain/rebar dependents behave;
- how rollback restores both CAD and semantic ownership when Boolean union fails.

Acceptance for any implementation: no destructive erase of ambiguous/foreign CAD, deterministic rebuild from source geometry, explicit shared-owner health checks, and representative V25 Boolean testing.

## 9. DrawJig preview / repeated authoring mode

A BLT-familiar transient thickness/profile preview and repeated placement loop still needs the exact BricsCAD V25 managed Jig API and interactive validation.

Local agent task:

- prototype with native `DrawJig`/editor APIs from the installed V25 assemblies;
- no persistent source entity before the user commits;
- ESC/cancel must leave no CAD or semantic residue;
- preview must respect planar UCS and current unit conversion;
- commit must converge through the existing Direct Draw/capture/build ownership path rather than a parallel geometry stack;
- repeated mode must have a clear Finish/ESC state and must not trap the command line.

Do not implement a fake WPF canvas preview in place of the BricsCAD viewport.

## 10. UI / Ribbon / context menu / HiDPI qualification

Current source contains the native-hosted Ribbon, Workspace/right palettes, dense dark theme, context menus and keyboard shortcuts. Validate them in real V25 at 100%, 125%, 150% and 200% Windows scaling.

Check:

- no clipping/overlap in Vietnamese labels;
- Ribbon groups wrap/size acceptably;
- Workspace split panes remain usable at narrow widths;
- right-click selects the intended Family/CAD row before action;
- context menu text remains readable in the BricsCAD host theme;
- `Ctrl+S`, `Ctrl+F`, `Ctrl+B`, `F5` and Family `Delete` act only when the QS3D workspace owns focus and do not hijack normal BricsCAD command input;
- disabled/unsupported native-build state is visible before users attempt impossible operations;
- Layer/Xref palette reflects live data, real native color/lock state and no fake sample rows;
- native BricsCAD viewport remains the center CAD renderer.

If a keyboard shortcut conflicts with BricsCAD host behavior in practice, prefer removing/scoping that shortcut over intercepting global CAD input.

## 11. Private-DWG save/reopen and multi-document qualification

Use a **private local copy** of the owner-provided representative DWG(s). Never commit them.

Required scenarios:

- open DWG A, create/edit QS3D semantic state, save `.qsdb`, close and reopen;
- Save As / renamed DWG and project fingerprint behavior;
- DWG A + DWG B open together, switch MDI documents repeatedly;
- verify palettes/project editors never mutate the wrong document after focus switch;
- semantic Locate/selection only targets the bound document;
- stale/dangling project references are reported by Model Health rather than silently rewritten;
- `QS3DRELEASECHECK` on representative populated data.

Record only anonymized command/result evidence if the drawing is confidential.

## 12. Rebar / schedule / quantity local qualification

Exercise generated rebar families that source-only review cannot certify against real Solid3d behavior:

- column/beam longitudinal;
- BBS shape;
- beam stirrups;
- column ties;
- slab X/Y mesh;
- StructuralWall H/V mesh;
- Foundation X/Y mesh.

Verify generated ownership, stale/rebuild lifecycle, live-handle health, BBS/BQ/Material/Room Finish/Curtain/Door-Opening schedule export and semantic Locate. Do not infer fabrication-grade hooks, bend radii or anchorage when explicit configured data is absent.

## 13. Performance qualification

Use a representative large drawing and record approximate timings/memory for:

- palette refresh;
- large Layer list search/select;
- semantic tree/family search;
- BQ build/filter;
- Model Health / Full Health;
- regeneration after a localized edit;
- Room Auto on a bounded representative selection.

Investigate UI stalls or full-project recomputation that violates the intended incremental design. CAD database operations must remain on the appropriate BricsCAD document/thread context; do not move native DB work to arbitrary background threads just to improve perceived speed.

## 14. Signed package / install / update qualification

This cannot be production-certified without a real signing certificate and a controlled Windows test machine.

When credentials are available and the owner requests this phase:

- build the exact approved SHA;
- sign the expected executable payloads;
- verify Authenticode signer/version binding;
- fresh install through DemandLoad;
- upgrade from a known previous version;
- forced mid-install failure restores prior payload/registry;
- intentionally mismatched/relabelled manifest/package is rejected;
- update archive traversal/size/entry guards remain enforced;
- BricsCAD `SECURELOAD` is never weakened.

Never commit signing secrets or certificate private keys.

## 15. Local agent report format

At the end of a local batch, update this handoff or the current canonical handoff with:

```text
Tested commit: <SHA>
BricsCAD: V25.x.x / file version
Windows scaling: <100/125/150/200% cases>
Private fixture: <anonymized name only, not committed>
Static/Core/build: PASS|FAIL
NETLOAD/runtime probe: PASS|FAIL
Interactive matrix: <cases passed / failed>
Screenshots/evidence: <safe repo path or local-only note>
Source changes committed: <SHAs>
Still blocked: <exact local/API/product blocker>
```

Never write `runtime verified`, `release ready` or `complete` unless the corresponding exact-SHA local gates actually passed.
