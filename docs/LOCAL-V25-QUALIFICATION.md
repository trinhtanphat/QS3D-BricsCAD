# QS3D — local BricsCAD V25 qualification handoff

Updated: 2026-08-10 (UTC+7)

This runbook is for an agent or engineer with **interactive Windows + licensed BricsCAD V25 x64 installed locally**. Remote/source-only agents must not mark these items complete.

The goal is to qualify one **exact Git SHA** without committing BricsCAD proprietary DLLs, private DWGs, screenshots, generated packages or machine-specific evidence into Git.

## 1. What the local agent owns

A local-capable agent should execute the items that remote GitHub agents cannot truthfully prove:

- build `QS3D.BricsCAD.V25` against the installed V25 `BrxMgd.dll` / `TD_Mgd.dll`;
- real `NETLOAD` and Registry DemandLoad behavior;
- Ribbon, palette, modeless windows and Vietnamese/HiDPI rendering;
- native `Solid3d` creation/replacement/boolean behavior;
- command cancel/UNDO/exception behavior inside the actual editor transaction model;
- save/reopen, Save As and multi-DWG context behavior;
- representative private DWG regression;
- install/upgrade/uninstall on a clean Windows user profile;
- production Authenticode only when the approved certificate is available.

Do not spend local-machine access re-reviewing ordinary source/docs unless a runtime failure requires it.

## 2. One-command automated exact-SHA gate

Close all BricsCAD processes, open an **interactive** PowerShell session in the repository, make sure the working tree is clean, then run:

```powershell
.\scripts\run-local-v25-qualification.ps1 `
  -BricsCadDir "C:\Program Files\Bricsys\BricsCAD V25 en_US" `
  -Profile "QS3D-V25-TEST"
```

Use the real local BricsCAD V25 directory; do not copy BricsCAD managed assemblies into the repository.

The runner performs, in order:

1. exact `git rev-parse HEAD` + clean-tree requirement;
2. manual-only CI policy preflight;
3. generic source preflight;
4. every auto-discovered feature preflight;
5. Core Release build;
6. deterministic Core smoke suite;
7. V25 adapter Release build against the installed BricsCAD assemblies;
8. licensed V25 `NETLOAD` runtime probe;
9. Ribbon + Palette validation and screenshot unless `-SkipScreenshot` is explicit;
10. a machine-readable `qualification.json` report.

Default evidence directory:

```text
artifacts/local-v25-qualification/
```

This directory is local evidence and must remain untracked.

### Optional local package check

Only when source `<Version>` already matches the intended tag exactly:

```powershell
.\scripts\run-local-v25-qualification.ps1 `
  -BricsCadDir "C:\Program Files\Bricsys\BricsCAD V25 en_US" `
  -Profile "QS3D-V25-TEST" `
  -Package `
  -ReleaseTag "v0.1.0-preview.2"
```

`package-v25.ps1` will fail closed when the full product SemVer does not match the requested tag.

`-SkipRuntime` is allowed only for diagnosing source/build problems. A report containing `runtimeSkipped=true` **cannot qualify a customer release**.

## 3. Required interactive scenario matrix

The automated probe proves load/runtime wiring, not every CAD operation. After it passes, run the following scenarios against the **same SHA and built DLL**.

### A. Plugin shell / UI

- DemandLoad from a clean user registration: run `QS3D` without manual `NETLOAD`.
- `QS3DRUNTIMECHECK` reports V25 + x64 + matching package/assembly state.
- Ribbon tabs/actions exist once, not duplicated after reopen/reset.
- Workspace/RightPanel/Family Manager/Hubs are modeless where designed.
- selection changes do not unexpectedly mutate PICKFIRST while a panel merely refreshes.
- Vietnamese Unicode is readable at 100%, 125%, 150% and a representative HiDPI setting.

### B. Direct Draw

For `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWCOLUMN`, `QS3DDRAWSLAB`:

- valid authoring from a blank Model Space;
- semantic element + real source + generated/native result all exist;
- generated result resolves back to the correct semantic owner;
- ESC before commit leaves no semantic/CAD residue;
- forced invalid dimensions fail closed;
- select generated result -> Family/Instance/Locate/Health workflows still resolve correctly;
- save, close, reopen, regenerate and verify ownership/quantity stability.

Repeat representative Wall/Beam/Column/Slab authoring in World UCS, translated UCS and planar 30°/45°/90° UCS. Verify tilted/3D UCS is rejected before source creation.

### C. Capture -> edit -> Build3D compatibility

- draw ordinary LINE/open POLYLINE source;
- capture Wall/Beam/Column/Slab where supported;
- edit supported Family/Instance values;
- run `QS3DBUILD3D`;
- modify the source and rebuild;
- verify old owned output replacement is atomic and foreign/ambiguous output is never erased.

### D. Door / Opening

- Direct Draw Door and WallOpening with one unique valid host;
- no-host case;
- ambiguous two-host case;
- Floor/Zone/elevation/gap mismatch cases;
- sill/bottom offset and boolean clearance persistence;
- `QS3DCUTSELECTEDOPENINGS` with one target, multiple targets, mixed unrelated CAD selection and multiple hosts;
- same-fingerprint rerun is idempotent;
- changed/different cut state fails closed when rebuild is required;
- legacy `QS3DCUTOPENINGS` still works for supported cases;
- force a boolean failure and verify semantic metadata + native host are not half-committed.

### E. Room / HT_PHÒNG

- closed simple room;
- supported LINE/POLYLINE/ARC/SPLINE boundary discovery cases;
- finish generation/synchronization;
- force one finish regeneration failure and verify the batch restores the previous project state;
- save/reopen and regenerate.

### F. Curtain / Glass Wall

- guarded LINE path;
- guarded open/bulged WCS-XY POLYLINE path;
- frame ownership and live-fingerprint Health state;
- replacement of old owned frames;
- forced failure before native commit restores project state;
- post-commit fingerprint stamp failure is surfaced as Health warning, not reported as a failed valid geometry commit.

Do not mark panel-by-panel backing glass, arbitrary freeform paths or broader clipping as supported unless separate source/runtime acceptance exists.

### G. Structural / rebar families

Exercise at least one valid and one forced-invalid case for:

- Column longitudinal bars;
- Column ties;
- Beam longitudinal bars;
- Beam stirrups;
- supported BBS shape geometry;
- Slab mesh;
- Structural Wall mesh;
- Foundation mesh.

For each family verify:

- old owned output replacement is atomic;
- generated-handle ownership is unique;
- invalid/foreign/ambiguous ownership fails closed;
- post-commit UI refresh failure cannot undo or misreport a valid CAD commit;
- `QS3DHEALTHALL`, Rebar Health and `QS3DRELEASECHECK` reflect the live result.

No agent may infer engineering reinforcement, hook, lap, anchorage or fabrication data that the semantic input does not explicitly provide.

### H. Project lifecycle

- save `.qsdb`;
- close/reopen DWG;
- Save As and verify drawing identity synchronization;
- open two DWGs and switch repeatedly;
- verify project context does not bleed across documents;
- test millimeter and metre drawings;
- exercise `QS3DUNTRACK` / `QS3DUNTRACKFINISH` on source and generated selections;
- verify untrack blocks a target while external semantic dependents remain, allows a complete dependency batch, and does not erase CAD geometry.

### I. Reporting

- BQ summary/recalculate/Locate;
- Excel export and Excel Handle locate with drawing fingerprint;
- legacy no-fingerprint confirmation path;
- BBS export only when explicit schedule semantics exist;
- Door/Opening and other current schedule surfaces;
- verify quantities remain stable after save/reopen/regenerate.

### J. Clean customer install lifecycle

On a Windows profile that did not build QS3D:

1. install BricsCAD V25 compatible edition and start it once;
2. extract the QS3D release candidate package;
3. run packaged `install-v25-autoload.ps1`;
4. start BricsCAD and run `QS3D` without manual NETLOAD;
5. run `QS3DRUNTIMECHECK`;
6. upgrade from the previous signed build using the supported `-Force`/updater path;
7. uninstall;
8. verify QS3D payload/registration is removed while unrelated BricsCAD settings remain unchanged.

For a stable customer build, repeat with `-RequireSigned` and the approved publisher thumbprint.

## 4. Evidence to retain locally

Keep under `artifacts/local-v25-qualification/` or another explicitly local folder:

- `qualification.json`;
- runtime marker/metadata;
- screenshots;
- command transcript or manual checklist notes;
- sanitized failure logs;
- optional package/hash produced for local checking.

Private/customer DWGs, proprietary BricsCAD DLLs, signing private keys/certificates and machine secrets must never be committed.

If evidence needs to be shared in GitHub, commit only a **sanitized text summary** containing:

- exact SHA;
- V25 edition/build number;
- PASS/FAIL per scenario family;
- issue IDs/commit IDs for failures fixed;
- no private file paths, customer names, DWG content or secrets.

## 5. Required handoff after local execution

When local qualification finishes, update a safe Markdown status note with this structure:

```text
Exact SHA: <40-char SHA>
Environment: BricsCAD V25 <edition/build>, Windows x64
Automated runner: PASS/FAIL
NETLOAD: PASS/FAIL
DemandLoad: PASS/FAIL
Direct Draw: PASS/FAIL
Opening booleans: PASS/FAIL
Room/HT_PHÒNG: PASS/FAIL
Curtain: PASS/FAIL
Rebar: PASS/FAIL
Project/save-reopen/multi-DWG: PASS/FAIL
BQ/BBS/Excel: PASS/FAIL
Unicode/HiDPI: PASS/FAIL
Clean install/upgrade/uninstall: PASS/FAIL
Known blockers: <sanitized list>
```

A runtime failure must produce a source fix/regression test where practical. Never edit a checklist from FAIL to PASS without rerunning the affected runtime scenario on the fixed exact SHA.

## 6. Release decision

Source review and static preflight alone cannot authorize a stable release.

A release candidate may advance only when the exact-SHA automated runner and the applicable interactive matrix are green. Stable additionally requires representative private-DWG regression, clean customer lifecycle proof and the repository's existing signing/tag/version gates.

GitHub Actions remain manual-only. Running this local script does not authorize a workflow dispatch or GitHub Release publication.
