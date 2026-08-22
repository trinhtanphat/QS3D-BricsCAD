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

### Focused LOCAL-003 Level Z probe

After the aggregate build gates are green, run the guarded Level Z-chain probe on the same clean exact SHA before the wider interactive matrix:

```powershell
.\scripts\test-bricscad-v25-level-z.ps1 `
  -BricsCadDir "C:\Program Files\Bricsys\BricsCAD V25 en_US" `
  -PluginDll ".\src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll" `
  -DrawingCopy ".\artifacts\local-v25-level-z\QS3D-Sample.level-z-probe-copy.dwg" `
  -Profile "QS3D-V25-TEST" `
  -ArtifactDir ".\artifacts\local-v25-level-z\run" `
  -ExpectedSourceSha (git rev-parse HEAD) `
  -ConfirmDisposableCopy
```

Read `docs/LOCAL-LEVEL-Z-QUALIFICATION-2026-08-11.md` for preparation, exact expected values, evidence and the required follow-on matrix. A PASS from this focused automation is representative native evidence only; it does not by itself close `LOCAL-003` or qualify a customer release.

## 3. Required interactive scenario matrix

The automated probe proves load/runtime wiring, not every CAD operation. After it passes, run the following scenarios against the **same SHA and built DLL**.

### A. Plugin shell / UI

- DemandLoad from a clean user registration: run `QS3D` without manual `NETLOAD`.
- After installing the exact locally built package in `OnCommand` mode, run `scripts/test-bricscad-v25-runtime.ps1` with `-DemandLoadOnly -SkipScreenshot` and `-PluginDll` set to the registered installed loader. The marker assembly path must equal that loader; a stale or already-loaded build must fail the check.
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

#### C.1 Generated host ownership Health — **PENDING local V25 runtime proof**

This scenario covers only the primary host `GeneratedSolidHandle`. Rebar, mesh and curtain-frame handles keep their own ownership/Health contracts and must not be judged by the host-solid XData marker.

1. In a disposable test DWG, create/rebuild a supported host solid through Direct Draw or `QS3DBUILD3D` and verify both `QS3DHEALTH` and `QS3DHEALTHALL` do **not** report `GENERATED_SOLID_OWNERSHIP_MISMATCH` for a correctly owned generated `Solid3d`.
2. Using a disposable test fixture/harness, make one semantic element's `GeneratedSolidHandle` resolve to a live `Solid3d` whose QS3D XData owner belongs to a different project, element, category or unsupported ownership version. Do not use customer DWGs for this corruption test.
3. Run `QS3DHEALTH` and `QS3DHEALTHALL`. Both must report `GENERATED_SOLID_OWNERSHIP_MISMATCH` against the affected semantic element instead of accepting “live Solid3d” as sufficient ownership proof.
4. Before and after each Health command, verify the foreign/mis-owned `Solid3d` still exists unchanged. Health is diagnostic-only: it must not erase, replace, write XData, upgrade the object for write or silently claim it.
5. Save/close/reopen the disposable DWG and repeat the checks so marker parsing is proven against persisted V25 XData, not only the in-memory transaction state.
6. Repeat one normal rebuild after repairing the semantic/ownership state and verify replacement succeeds only for the correctly owned host output.

Record this scenario as **PASS / FAIL / NOT TESTED**. Never convert it to PASS from static source/preflight alone.

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

- Run `scripts/test-bricscad-v25-project-lifecycle.ps1` first against the exact clean SHA and the repository-generated `samples/generated/QS3D-Sample.dwg`. Its four disposable copies provide a repeatable baseline for DWG `SaveComplete` sidecar persistence, cold-cache canonical binding, A/B project isolation, absent-sidecar non-creation and corrupt-sidecar fail-closed behavior. This automation is only the baseline below; it does not replace the interactive/modeless scenarios.
- save `.qsdb`;
- close/reopen DWG;
- Save As and verify drawing identity synchronization;
- open two DWGs and switch repeatedly;
- verify project context does not bleed across documents;
- test millimeter and metre drawings;
- exercise `QS3DUNTRACK` / `QS3DUNTRACKFINISH` on source and generated selections;
- verify untrack blocks a target while external semantic dependents remain, allows a complete dependency batch, and does not erase CAD geometry.

#### H.1 Modeless multi-DWG lifetime — **PENDING local V25 runtime proof**

Run this scenario only on licensed BricsCAD V25 against the same exact SHA/package. Source preflight verifies the ownership contract, but only the real V25 document-destroy event/dispatcher can prove runtime behavior.

1. Open DWG **A** and open several source-DWG-bound modeless surfaces: BQ, BBS, Model Health, Door/Opening Schedule, HT_Phòng Schedule, Family Manager, Level Picker, Zone Manager, Material Catalog, Project Tools, Schedule Hub, Curtain Hub and Audit Log.
2. Also open **Domain Hub** and **Rebar 3D Hub**. These two are intentionally **active-document dynamic**, not bound to A.
3. Open/switch to DWG **B** while A remains open.
4. From the A-bound windows, attempt representative Locate/Refresh/Export/mutation actions. They must fail closed or tell the user to reactivate A; no B project/CAD state may change. Audit Log must continue displaying A's audit state, never B's.
5. From Domain Hub and Rebar 3D Hub, launch harmless representative commands while B is active. They must target B because those hubs resolve the active document at click time.
6. Close DWG **A** while all A-bound windows remain open. Every A-bound window must unregister and close without blocking the DWG close or throwing an unhandled exception.
7. Domain Hub and Rebar 3D Hub must remain usable after A closes and must continue targeting B.
8. Reopen A or open a new third drawing and repeat switch/close cycles several times. Verify there are no disposed-document callbacks, stale project selection, duplicate lifetime subscriptions, `eNotOpenForWrite`/disposed-object errors, crash, or visible stale-A data retained by a supposedly closed bound window.
9. Exit BricsCAD with bound and dynamic hubs open. Shutdown must complete without an unhandled UI/document lifecycle exception.

Record this scenario as **PASS / FAIL / NOT TESTED** in the sanitized local result. Never convert it to PASS from source review alone.

#### H.2 Modeless project-editor rollback and post-commit isolation — **PENDING local V25 runtime proof**

Use disposable projects/DWGs and a controlled test hook, debugger break/fault injection, or other local harness that can force an exception at a known boundary. Do not corrupt customer data merely to exercise failure handling.

1. In **Material Catalog**, apply one material to several semantic elements and force failure after at least one target has been mutated but before the semantic batch completes. Verify material properties, dirty flags, `UpdatedUtc`, custom-catalog metadata and audit events all return to the exact pre-operation state; no partial material assignment may remain.
2. Repeat an equivalent mid-batch failure for **Level Picker Assign**, **Zone Manager Assign**, and **Family Manager Assign**. Family testing must also cover `SetProperty` or `RemoveProperty` propagation across several inherited instances. Every operation must be all-or-nothing at project level, including its audit events.
3. Exercise Family create/rename/duplicate/delete/activate plus Floor create/update/delete/activate and Zone create/update/delete/activate with a forced exception in the semantic/audit boundary. A failed operation must restore the prior project state instead of leaving a successful semantic mutation with a failed audit entry or vice versa.
4. Separately force only a **Palette/UI refresh failure after semantic commit** for Material, Floor, Zone, Family, Curtain Family and Rebar Mesh Setup. The semantic edit must remain committed. The UI/editor must surface an `UI sync warning` or equivalent post-commit warning and must not report that the semantic mutation itself rolled back.
5. For a newly created Floor, Zone and Family, force the post-commit refresh failure, then attempt Save again. Verify the editor targets/re-resolves the already committed ID or fails closed; it must not create an accidental duplicate solely because the first UI refresh failed.
6. Keep **Audit Log** open for DWG A, perform a project reload/replacement for A through a supported local test flow, then reactivate Audit Log. It must re-resolve A's current `ProjectState` and display the new audit events rather than retaining the old in-memory project object.
7. While the same windows are open for A, switch to DWG B and repeat mutation attempts. A-bound editors must fail closed before touching B. Then return to A and verify the intended operation still works against A's current project object.
8. Save/close/reopen the disposable DWG after successful operations and verify committed material/floor/zone/family state and audit trail persist consistently.

Record **Modeless editor rollback/post-commit isolation: PASS / FAIL / NOT TESTED**. Static preflight proves source structure only; only this local scenario can qualify real WPF dispatcher, BricsCAD document lifetime and failure timing behavior.

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
Build3D/generated host ownership Health: PASS/FAIL/NOT TESTED
Opening booleans: PASS/FAIL
Room/HT_PHÒNG: PASS/FAIL
Curtain: PASS/FAIL
Rebar: PASS/FAIL
Project/save-reopen/multi-DWG: PASS/FAIL
Modeless multi-DWG close lifecycle: PASS/FAIL/NOT TESTED
Modeless editor rollback/post-commit isolation: PASS/FAIL/NOT TESTED
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
