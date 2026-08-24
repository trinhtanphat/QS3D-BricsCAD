# ChatGPT session review — QS3D / BLT3D reconstruction and delivery state

Date: 2026-08-24
Issue: #3689
Canonical branch: `agent/chatgpt-gpt56sol/issue-3689-session-review`
Document type: owner-requested session consolidation / engineering handoff

## 1. Executive summary

This session converged on one product goal: keep `QS3D-BricsCAD` as the clean-room implementation target and recreate the observable BLT/BLT3D BIM/QS workflow without copying proprietary source. The target experience can be reduced to one acceptance sentence:

> Vẽ nhanh -> sửa nhanh -> tính đúng BT/VK -> giải thích được -> click định vị -> xuất đúng mẫu Excel.

The repository already had a substantial BIM/QTO foundation before this session: semantic Project/Zone/Floor/Family/Element modeling, native BricsCAD geometry, quantity/BQ, rebar, openings, room/finish, persistence, WPF/Ribbon UI, XLSX/CSV and model locating. The highest-value gaps were therefore not a new geometry kernel, but workflow completeness, formwork fidelity, customer Excel/report fidelity, editing UX and bidirectional traceability.

The session also established a strict delivery discipline for the repository: issue -> canonical branch -> CI -> PR -> merge to `main`; no direct writes to protected `main`; no bypassing a failing source guard; no claiming licensed BricsCAD runtime PASS from hosted/source-only CI; and LOCAL_ONLY evidence remains exact-SHA and separate from source-complete status.

## 2. Scope, evidence classes and clean-room boundary

Throughout the session, findings were separated into four evidence classes:

- **[ĐÃ XÁC NHẬN]**: directly observed in source, repository state, static binary metadata, screenshots, command output or CI.
- **[SUY LUẬN]**: strong engineering inference from verified evidence, but not a direct runtime fact.
- **[CHƯA RÕ]**: unresolved because the required runtime, file, environment or exact behavior was not available.
- **[ĐỀ XUẤT]**: recommended implementation/product direction.

### [ĐÃ XÁC NHẬN] Clean-room reconstruction boundary

The old BLT/BLT3D application is used as behavioral/product evidence only. The work in `QS3D-BricsCAD` must not copy proprietary BLT source or commit vendor/private binaries, license/activation material, customer-private DWGs, credentials or unsanitized runtime artifacts.

The correct reconstruction boundary is:

- reproduce observable business behavior, command flow, UI intent and exported results;
- derive independent domain logic in QS3D;
- keep BricsCAD-host-specific behavior in adapter/UI layers;
- keep reusable geometry/quantity/export/persistence logic in vendor-neutral/shared layers where practical.

## 3. Static analysis of the old BLT3D package

### [ĐÃ XÁC NHẬN]

`BLT3D V25.6.exe` was analyzed statically, without executing unknown code. It is a WinRAR SFX container rather than one monolithic executable. The archive evidence showed a broad application bundle including components such as:

- `blt3D.dll`;
- `blt_library.dll`;
- `BLT_QS_V2021.dll`;
- `BLT_QSR.dll`;
- `PHC_QS.dll`;
- `BLT_Fuzor.dll`;
- `model_from_cad.dll`;
- BricsCAD-related assemblies;
- DWG/LSP resources;
- Excel templates and quantity-related assets.

Metadata and resources strongly indicated business domains for:

- structural walls, beams, slabs, columns and foundations;
- doors/openings, levels, grids, project/family concepts;
- concrete and formwork calculation;
- rebar/BBS/shop-drawing workflows;
- quantity review, WBS-style grouping and Excel output;
- room/finish quantities;
- earthwork/model quantity tooling;
- model exchange/bridge behavior around Revit/Fuzor/model-from-CAD style workflows.

A BLT setup file contained formwork-oriented settings including:

- `coppha_đáy_dầm_tính_vào_đáy_sàn`;
- `dien_tich_ck_lay_khi`;
- `goc_min_lay_coppha_mat_tren`;
- `offset_coppha`;
- `the_tich_ck_lay_khi`;
- `tinhTongChieuDai`.

### [SUY LUẬN]

Those settings demonstrate that BLT formwork was not a naive total-surface-area calculation. It applied category-specific eligibility, minimum thresholds, top-face angle rules, beam/slab ownership choices and offsets. QS3D therefore needs an explainable formwork subsystem rather than a single generic `Area` projection.

## 4. BLT-style UI/workflow reconstructed from screenshots

### [ĐÃ XÁC NHẬN]

The screenshots discussed in the session showed a Ribbon and left-side object palette with a workflow that is materially different from a generic CAD add-in.

Observed Ribbon concepts included:

- Level/floor-oriented drawing setup;
- point/line/rectangle/circle helpers;
- create door and lintel;
- beam recognition;
- blinding concrete creation;
- wall recognition;
- position update;
- BLT-Solid3d conversion;
- copy floor;
- settings;
- clear highlight;
- bulk property edit;
- volume/area deductions;
- quantity calculation;
- quantity review;
- show all model;
- show formwork;
- login/register/information.

Observed left-palette categories included:

- Room, FloorFinish, WaterProof, Skirting, WallFinish, CeilingFinish;
- Beam;
- Slab and Slab Opening;
- Column;
- Structural Wall and Wall Opening;
- architectural wall and door;
- pile foundation, pile cap, foundation beam, strip foundation, raft foundation and blinding concrete.

### [ĐỀ XUẤT]

QS3D should pursue functional/familiar parity rather than pixel-perfect reproduction. Exact pixel parity is both unnecessary and unstable across BricsCAD version, theme, DPI, Windows scaling and host runtime.

## 5. BLT runtime/debug observations from the session

The session also contained direct debugging of an installed/built BLT environment, which provided valuable behavioral evidence.

### [ĐÃ XÁC NHẬN]

Observed behaviors included:

- BricsCAD APPLOAD/NETLOAD experimentation with BLT assemblies and diagnostic wrappers;
- a wall workflow in which two picked points did not create the wall, while three collinear points followed by Enter did create a wall;
- generated wall objects appearing as `Solid3d`;
- B4D quantity output returning zero for both concrete and formwork in the tested broken state;
- diagnostic output showing a valid solid volume and exploded Regions;
- a Handle lookup succeeding in one path but a later diagnostic reporting `link_CShap not found`;
- `s_listSemilerName` initially being null in BLT family state;
- `BLTFAMILYFIX` initializing a `Dictionary<String, blt_family>` in RAM only, with restart intentionally rolling the change back;
- recurring errors around resetting/setting the current family list.

### [SUY LUẬN]

These observations show that geometry existence alone did not guarantee semantic/family linkage or quantity eligibility. They reinforce the QS3D architecture decision to make semantic identity, source/generated Handle ownership, quantity evidence and health diagnostics first-class concepts rather than implicit global state.

## 6. Existing QS3D architecture verified during the session

### [ĐÃ XÁC NHẬN]

The repository already implements a broad base that should be extended rather than replaced:

- `QS3D.Core` for CAD-independent domain, quantity, geometry, export and persistence logic;
- BricsCAD V25/V26 adapter/UI projects;
- Project/Zone/Floor/Level/Family/Type/semantic Element concepts;
- source/generated CAD Handle ownership and drawing/project persistence;
- dirty/regeneration/revision concepts;
- semantic capture/recognition and native `Solid3d` generation;
- wall/beam/slab/column and extended-family direct draw flows;
- physical opening/door cuts;
- room and finish workflows;
- curtain-wall workflows;
- 3D rebar/BBS support for multiple categories;
- quantity/BQ review, filters, recalculation, locate/reveal;
- Quick Takeoff/B4D-style recognition;
- schedule/reporting support;
- XLSX/CSV export with identity provenance;
- WPF/Ribbon review tools, focus/isolate/highlight and model-health concepts;
- persistence and sidecar/project identity safeguards.

### [SUY LUẬN]

The project is no longer in the phase of “build basic BIM objects from scratch”. The remaining product risk is concentrated in workflow polish and correctness at subsystem boundaries: geometry <-> semantic identity, semantic identity <-> quantity rules, quantity <-> evidence/explanation, and model <-> Excel/report traceability.

## 7. Excel/export and reverse-trace analysis

### 7.1 Existing capability before the customer-workbook work

The repo already had `QS3DED2` and `QS3DEXCELLOCATE` behavior. ED2 exported detailed and summary data with provenance such as:

- QS3D Element ID;
- CAD Handle;
- Drawing Fingerprint.

The existing Excel locator was intentionally strict: workbook provenance had to match the active drawing/project and all required Handles had to resolve before CAD selection changed. Legacy BLT decimal-Handle behavior was kept as a separate compatibility path.

### 7.2 Gap identified in the session

The missing customer-facing behavior was a clearer workbook and a row-level business trace contract that also supported grouped/aggregate rows. The session therefore designed a workbook with:

- `DGKL`;
- `COP_PHA`;
- `CHI_TIET`;
- `TRACE_MODEL`.

`TRACE_MODEL` is the explicit row-to-model provenance map, allowing visible business sheets to stay clean while preserving deterministic traceability.

### 7.3 [ĐÃ XÁC NHẬN] Landed customer Excel -> CAD implementation

PR #3299, `feat(BIM3D-QS): customer Excel workbook + reverse model trace`, was ultimately merged. Its final scope included:

- customer workbook projection with `DGKL`, `COP_PHA`, `CHI_TIET`, `TRACE_MODEL`;
- evidence-aware quantity cells where unsupported values remain blank and real measured zero remains numeric zero;
- hardened `TRACE_MODEL` reading;
- detail and aggregate Excel -> CAD trace validation;
- Drawing Fingerprint + Element ID + Handle verification before changing selection;
- `QS3DEXCEL` and `QS3DEXCELTRACE` commands;
- compact Ribbon actions `Xuất Excel` and `Excel -> CAD`;
- preservation of ED2/legacy compatibility instead of replacing it;
- deterministic smoke tests and focused preflight guards.

PR #3299 merged with merge commit:

`99dc024faafa4becc1a89fa61a894f69fba8aa49`

### 7.4 [ĐÃ XÁC NHẬN] Current opposite-direction follow-up

At the report snapshot, PR #3685 is open for CAD/model -> existing Excel detail-row activation. Its stated design is fail-closed:

1. select exactly one semantic element;
2. attach only to an already-running Excel instance;
3. require a saved `.xlsx` workbook;
4. use Excel COM only for bounded discovery;
5. re-read the saved workbook with hardened XLSX readers;
6. revalidate Element ID + Drawing Fingerprint + Handle provenance through the existing resolution service;
7. recheck active DWG/project revision;
8. activate/select the exact `CHI_TIET` row only after validation.

This completes the bidirectional UX without making Excel COM the source of truth.

## 8. Excel template fidelity

### [ĐÃ XÁC NHẬN]

Template fidelity was identified as a P0 product requirement: preserve formatting, merged cells, borders, formulas and customer-specific mapping instead of forcing every export into one hard-coded layout.

Current `main` at report creation includes PR #3678:

`feat(qs): add reusable XLSX template mapping engine (#3673)`

Current-main merge commit at that snapshot:

`38d467c17cf85374f4b38323aa8056bebbdba49b`

### [SUY LUẬN]

This is a direct implementation of an earlier session recommendation: workbook generation should become template-driven while provenance remains deterministic and machine-readable.

## 9. Formwork subsystem analysis

A major business concern throughout the session was formwork correctness, especially wall openings and contact/deduction behavior.

### 9.1 Wall-opening reveal defect

The recurring target defect was: a wall has an opening but formwork on the horizontal reveal/head/soffit face above the opening is missing.

### [SUY LUẬN]

A common source of this bug is blanket exclusion of all horizontal wall faces when trying to remove the wall's exterior top/bottom surfaces. That rule also removes legitimate internal opening-reveal faces.

### [ĐÃ XÁC NHẬN] Current source lane

PR #3677 targets this exact distinction: preserve internal wall-opening reveal formwork while excluding only exterior wall top/bottom planes.

### 9.2 Wall-contact deduction runtime evidence

PR #3688 records licensed BricsCAD V25 runtime evidence where:

- baseline structural-wall gross formwork matched expectation;
- full end-face contact after a production column capture incorrectly deducted to zero instead of the expected contact area;
- small and larger live overlap cases still produced zero contact quantity;
- an independent native BricsCAD boolean on the overlap succeeded with the expected overlap volume.

### [SUY LUẬN]

That combination strongly points to source-side contact/deduction classification or provenance logic rather than a fundamental BricsCAD boolean failure.

### [ĐỀ XUẤT]

The formwork subsystem should remain explainable and category-aware, with explicit rules for:

- wall exterior versus opening-reveal faces;
- beam/slab ownership of shared formwork;
- structural contacts/intersections;
- opening deductions;
- minimum-area/volume thresholds;
- offset/tolerance behavior;
- top-face angle policy;
- per-face evidence/explanation.

## 10. CI failures encountered and how the session handled them

A large part of the session was repeated “CI đỏ -> sửa cho xanh” work. The failures were not all the same bug; they covered a range of source-contract and regression checks.

Examples discussed included:

- `preflight-plan-to-3d-finish-workflow.py`;
- `preflight-document-bound-modeless-lifetime.py`;
- `preflight-document-bound-window-attach-atomicity.py`;
- `preflight-modeless-window-lifetime-idempotence.py`;
- `preflight-modeless-project-affinity.py`;
- `preflight-curtain-path-frames.py`;
- `preflight-structural-wall-contact-stale-clear.py`;
- Lane-Key collision/admission failures;
- nullable compiler warnings promoted to errors (`CS8600`, `CS8602` families);
- smoke-test registration drift after branch reconciliation;
- customer workbook reader/exporter strictness and XLSX cell-length regressions.

### Engineering lessons consolidated from those failures

1. Fix the exact failing source guard instead of weakening/disabling the gate.
2. Keep corrections on the canonical branch for the issue/PR; do not create duplicate carriers when a lane already exists.
3. Reconcile with current `main` when parallel agents land changes, then validate the exact new head.
4. Treat protected `preflight` and `core` as authoritative hosted acceptance gates.
5. Do not equate hosted CI/source PASS with licensed BricsCAD runtime PASS.
6. Preserve smoke-test registration from both sides during merges; a careless reconciliation can silently drop another lane's tests.
7. Use expected-head protection when merging so a moving PR cannot be merged accidentally.
8. Respect HOLD and LOCAL_ONLY boundaries even under a broad “merge all” request.
9. Branch freshness matters: a previously-green commit is not sufficient after `main` moves materially.
10. CI errors that look mechanical (nullable, registration, metadata) can still block a real feature and should be fixed rather than bypassed.

## 11. Governance/rule interpretation established in this session

### [ĐÃ XÁC NHẬN]

The repo's working rules require a controlled integration flow. During this session the relevant rule files were read/re-read, including governance around:

- main-write authorization;
- issue/branch registration;
- branch CI and Actions lookup;
- prompt-to-release flow;
- duplicate-prompt/race handling;
- remote-agent boundaries;
- CI policy.

### Practical consequences

- Do not push directly to protected `main`; owner authorization to “merge main” means use the approved PR/integration workflow.
- Use a canonical `agent/...` branch tied to the issue/lane.
- Do not hijack another agent's active lane.
- Do not mark LOCAL_ONLY runtime work PASS from source CI.
- Do not merge a held runtime/evidence lane merely to make the backlog look clean.
- If a PR head changes, re-evaluate exact-head CI/freshness before merge.

## 12. Important integration history tracked in this session

### [ĐÃ XÁC NHẬN]

- PR #3295: owner-authorized integration batch, landed to `main` as `db7cc6f15a828d166731cee8011dd5289e948422` after exact-current `preflight` + `core` success. The then-held template lane #2842 was intentionally excluded.
- PR #3299: customer Excel workbook + reverse model trace, later merged as `99dc024faafa4becc1a89fa61a894f69fba8aa49`.
- PR #3678: reusable XLSX template mapping engine, merged to the current-main snapshot `38d467c17cf85374f4b38323aa8056bebbdba49b` used to start this documentation lane.

The historical #3299 carrier required multiple CI fixes and reconciliations. Its intermediate red/cancelled states are not its final state; it is merged.

## 13. Current repository snapshot when this document lane was created

### [ĐÃ XÁC NHẬN]

Current `main` at the start of issue #3689:

- SHA: `38d467c17cf85374f4b38323aa8056bebbdba49b`;
- latest merge: PR #3678 reusable XLSX template mapping engine.

Open PRs discovered at that snapshot:

- **#3688** — LOCAL runtime evidence for V25 wall-contact failure; not a production source fix and must not be interpreted as LOCAL_PASS.
- **#3685** — CAD -> Excel detail-row activation.
- **#3684** — consolidated LOCAL_ONLY source-ready index/handoff; its PR body explicitly states it is a handoff carrier and is not to be merged as part of that request.
- **#3677** — V25/shared wall-opening reveal formwork source fix.

This is a point-in-time snapshot, not a permanent backlog inventory. Other agents may merge or create work after the timestamp.

## 14. What is complete versus what still needs proof

### [ĐÃ XÁC NHẬN] Source/merge complete from this session's major Excel lane

- customer workbook structure;
- Excel -> CAD trace;
- strict provenance validation;
- aggregate and detail business-row handling;
- visible customer Excel actions;
- XLSX template mapping engine has since landed.

### [CHƯA RÕ / RUNTIME-DEPENDENT]

The following cannot be upgraded to full product PASS from hosted CI alone:

- exact Ribbon/WPF appearance under licensed V25/V26;
- modeless window/document lifecycle behavior under real BricsCAD wrapper changes;
- native BREP/contact behavior across real drawings;
- live PICKFIRST/highlight/zoom UX;
- Excel + BricsCAD combined interactive automation;
- real customer workbook templates with edge formatting/macros/external links;
- installer/license/environment-specific behavior.

### [ĐÃ XÁC NHẬN] Current defect evidence needing continued remediation

- wall-contact quantity/deduction runtime has a recorded V25 failure in the current LOCAL evidence lane;
- wall-opening reveal formwork has a dedicated source-fix PR still open at the report snapshot;
- CAD -> Excel is still an open feature PR at the report snapshot.

## 15. Recommended product roadmap after this session

### P0 — complete the customer QS golden path

1. Finish CAD -> Excel activation (#3685), then validate both directions end-to-end:
   - CAD/model -> Excel row;
   - Excel row -> CAD model.
2. Land the wall-opening reveal formwork fix after current CI/freshness validation.
3. Correct wall-contact deduction source logic based on the exact licensed failure evidence, then rerun the full exact-SHA local matrix.
4. Complete customer-template UX around the now-landed XLSX template engine.
5. Make every BT/VK number explainable to source geometry/rules.

### P0/P1 — authoring and editing UX

- Direct Draw V2 with transient preview and repeated authoring;
- native property/geometry editing instead of delete/redraw loops;
- Grid/Level refinement;
- faster floor/project navigation;
- stronger bulk-edit and visual feedback.

### P1 — specialist quantity/report workflows

- visual rebar shape library;
- shop-drawing/BBS refinement;
- report packs by floor/zone/family/material;
- detailed concrete/formwork/rebar reports;
- earthwork quantity workflows;
- richer customer template/report configuration.

### Interoperability

Prefer:

1. IFC as the first broad neutral interchange target;
2. Revit bridge where concrete business value justifies it;
3. Tekla-specific integration later.

Interoperability should not block the core QS golden path.

## 16. Runtime qualification boundary

Remote/source CI can verify source guards, deterministic Core tests and compile-level adapter compatibility, but it cannot prove the entire interactive BricsCAD workflow.

Runtime-only concerns include:

- NETLOAD/host startup behavior;
- WPF/Ribbon visual behavior;
- document-wrapper/modeless lifetime behavior;
- native BricsCAD BREP/boolean/contact behavior;
- PICKFIRST/selection/zoom behavior;
- Excel + BricsCAD interaction;
- license/install/environment-specific behavior.

Only exact-SHA licensed runtime evidence should be recorded as `LOCAL_PASS` / `LOCAL_FAIL`. `SOURCE_COMPLETE`, `CI_GREEN` and `LOCAL_PASS` are deliberately different states.

## 17. Architectural conclusions

### [ĐÃ XÁC NHẬN]

QS3D already has most of the structural architecture needed for a serious BricsCAD-native BIM/QS workflow.

### [SUY LUẬN]

The highest product risk is now correctness and workflow polish at subsystem intersections, not the existence of basic object types. A visually correct `Solid3d` is not enough if:

- semantic ownership is wrong;
- quantity evidence is stale;
- opening/contact deductions are wrong;
- the user cannot explain the number;
- exported Excel cannot trace back to the object;
- edited geometry silently diverges from persistence.

### [ĐỀ XUẤT]

Use the golden-path sentence as the acceptance north star for subsequent issues and local qualification:

> Vẽ nhanh -> sửa nhanh -> tính đúng BT/VK -> giải thích được -> click định vị -> xuất đúng mẫu Excel.

Every feature should improve one of those steps or provide exact evidence that the step works.

## 18. Suggested acceptance matrix for future work

For each meaningful feature/bug fix, record separate evidence columns:

| Gate | Meaning | Can hosted CI prove it? |
| --- | --- | --- |
| Source guard | Required code/path/contract exists | Yes |
| Core deterministic tests | CAD-independent behavior is correct | Yes |
| V25/V26 compile | Adapter compiles against trusted references | Yes |
| PR freshness | Candidate is based on/reconciled with current main | Yes |
| Merge landed | Exact PR head reached main | Yes |
| Licensed runtime | Real BricsCAD behavior on exact SHA | No, LOCAL_ONLY |
| Customer acceptance | Real project/template/workflow works | No, separate evidence |

This prevents repeated ambiguity around “xong chưa?” when source, CI, merge and runtime are at different states.

## 19. Recommended local-agent contract

For LOCAL_ONLY lanes, remote work should prepare everything possible in Git:

- exact carrier branch and SHA;
- source fix already committed;
- automated source guards;
- deterministic Core tests;
- build/run scripts where safe;
- precise manual steps and expected result;
- sanitized evidence schema.

The local agent should ideally only:

1. fetch/checkout exact SHA;
2. build/load under licensed BricsCAD;
3. run the prescribed test;
4. report sanitized PASS/FAIL/NO_RESULT tied to that exact SHA.

This was repeatedly requested in the session and is the right separation between remote coding and licensed-host qualification.

## 20. Documentation provenance and limitations

This file consolidates:

- the current ChatGPT session's requirements and decisions;
- prior static-analysis notes from the supplied BLT3D package;
- screenshot-derived workflow observations;
- BLT runtime/diagnostic outputs discussed in the session;
- GitHub CI/PR/branch work performed during the session;
- a fresh repository-state check performed on 2026-08-24 before creating issue #3689.

Where hosted source evidence and licensed runtime evidence differ, this document intentionally uses the more conservative state. It does not convert source/CI evidence into runtime proof.

## 21. Final status statement

The major Excel reverse-trace lane discussed earlier in the session is now merged. The reusable XLSX template engine is also on `main`. Remaining near-term work at this snapshot is concentrated in CAD -> Excel activation and formwork correctness/runtime qualification, especially wall-opening reveals and wall-contact deductions.

The engineering direction remains stable: do not replace the existing QS3D architecture; finish the user-facing golden path, make quantity results explainable, and prove host-sensitive behavior on exact licensed runtime candidates.
