# Session review — BLT3D clean-room analysis, QS3D-BricsCAD status, Excel round-trip, CI and next priorities

Date: 2026-08-21 (UTC+7)  
Issue / Lane-Key: #3348 / `issue-session-review-20260821`  
Canonical owner/session: `chatgpt / gpt56sol-20260821-session-review-doc`  
Baseline current `main` at registration: `519ed8ae53df03f9366de44194c8c45b2706d130`

## 1. Purpose of this document

This document consolidates the requirements, evidence, product conclusions, repository analysis, implementation results, CI/integration history, open risks, and recommended follow-up work discussed throughout the current ChatGPT session about `trinhtanphat/QS3D-BricsCAD`.

It is an engineering/session record, not a replacement for current source or repository policy. When this document conflicts with current source, `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/PRODUCT-BOUNDARY.md`, or `CI_POLICY.md`, the current repository truth wins.

Evidence labels used here:

- **[ĐÃ XÁC NHẬN]** — directly observed from repository/source, GitHub state, supplied binary metadata, supplied screenshots, or completed CI/integration evidence.
- **[SUY LUẬN]** — engineering inference consistent with the evidence but not a runtime-qualified fact.
- **[CHƯA RÕ]** — not proven by the available source/static/remote evidence.
- **[ĐỀ XUẤT]** — recommended future implementation direction.

## 2. Owner intent reconstructed from the session

### [ĐÃ XÁC NHẬN]

The owner repeatedly asked to continue the complete repository lifecycle rather than stop at analysis: review requirements, create planning, implement, commit/push, inspect CI failures, remediate them on the canonical carrier, open/update PRs, and merge to `main` when the repository-required gates are green.

The product objective discussed in this session can be summarized as:

> **Vẽ nhanh → sửa nhanh → tính đúng BT/VK → giải thích được → click định vị → xuất đúng mẫu Excel.**

The owner also wants QS3D workflows to feel familiar to the prior BLT/BLT3D workflow without requiring or copying the original BLT implementation. The strongest immediate business requirement in this session was Excel quantity export plus reverse trace from workbook rows back to live BricsCAD model objects.

The broader requested parity themes included:

- BLT-style Ribbon/palette workflow familiarity;
- semantic model authoring for structural/architectural categories;
- concrete and formwork quantity computation;
- quantity review and traceability;
- Excel output resembling the historical QS workflow;
- model ↔ quantity / Excel bidirectional navigation;
- later refinement for rebar, reports, earthwork, and interoperability.

## 3. Locked product boundary

### [ĐÃ XÁC NHẬN]

`QS3D-BricsCAD` is a **BricsCAD-hosted Windows x64 plugin**, not a standalone CAD executable.

Current host-major targets:

- BricsCAD V25 → `QS3D.BricsCAD.V25.dll`, `.NET Framework 4.8` / `net48`;
- BricsCAD V26 → `QS3D.BricsCAD.V26.dll`, `.NET 8` / `net8.0-windows`;
- shared `QS3D.Core` → `netstandard2.0` while shared/vendor-neutral work may progressively move to sibling `QS3D-Platform`.

BricsCAD remains responsible for the native DWG database, document/editor lifecycle, viewport, selection, transactions, and host CAD APIs. QS3D contributes commands, Ribbon/palette/modeless UI, semantic/project data, generated geometry workflows, quantity/reporting, recognition, persistence, and traceability inside that host.

The sibling product split is important:

```text
QS3D-Platform  -> shared/vendor-neutral contracts and logic
QS3D-BricsCAD  -> BricsCAD V25/V26 hosted plugin (this repository)
QS3D-CAD       -> separate future/parallel standalone desktop CAD/BIM/QS product
```

### Clean-room boundary

**[ĐÃ XÁC NHẬN]** BLT/BLT3D is a clean-room workflow/UX reference only. The repository must not depend on BLT source, proprietary BLT assets, BLT license material, or bundled proprietary BricsCAD SDK/runtime binaries.

**[SUY LUẬN]** Functional familiarity can be reproduced from observable behavior, UI screenshots, input/output structure, command semantics, quantity rules, and independently designed implementations without copying proprietary implementation details.

## 4. Legacy BLT3D executable analysis from the session

The session previously performed static inspection of the owner-supplied `BLT3D V25.6.exe`. It was not executed as part of the analysis.

### [ĐÃ XÁC NHẬN] Packaging and contents

The file behaved as a PE32 WinRAR self-extracting archive rather than a monolithic native application. The archive payload exposed a large BLT installation tree containing many managed DLLs/resources.

Notable observed assemblies/resources included:

- `blt3D.dll`
- `blt_library.dll`
- `BLT_QS_V2021.dll`
- `BLT_QSR.dll`
- `PHC_QS.dll`
- `BLT_Fuzor.dll`
- `model_from_cad.dll`
- `BRC/BLT-BIM.dll`
- BricsCAD-related managed dependencies/resources
- Excel templates, DWG/LSP resources, and related support files

A compatibility/settings file observed in the package contained quantity/formwork-oriented parameters equivalent to:

```json
{
  "coppha_đáy_dầm_tính_vào_đáy_sàn": false,
  "custom1": false,
  "custom2": false,
  "custom3": false,
  "custom4": false,
  "dien_tich_ck_lay_khi": 0.001,
  "goc_min_lay_coppha_mat_tren": 45,
  "offset_coppha": 6,
  "the_tich_ck_lay_khi": 0.0001,
  "tinhTongChieuDai": false
}
```

### [ĐÃ XÁC NHẬN] Functional signals from managed metadata

Static metadata inspection indicated broad BIM/QS capabilities across the BLT assemblies, including recognition, floor/level/project handling, wall/beam/slab/column/foundation workflows, formwork, quantity takeoff, rebar/shopdrawing, Excel integration, 2D→3D/model conversion, WBS-like classification, rendering/grid support, and interoperability-oriented modules.

Examples from the inspected assembly roles:

- `blt_library.dll` — recognition, foundation selection, formwork, 2D→3D, wall/beam edit, level/project handling, Excel interop signals;
- `BLT_QS_V2021.dll` — rebar/shopdrawing/Excel/export-oriented signals;
- `BLT_QSR.dll` — earthwork/model quantity-oriented signals;
- `model_from_cad.dll` and `BLT_Fuzor.dll` — model bridge/interoperability-oriented signals.

### [SUY LUẬN]

The old system was not merely an Excel exporter. Its observable packaging suggests a broad BIM/QS workflow family. Reproducing every historical feature in one change would be unsafe; QS3D should continue using vertical slices with explicit source/runtime acceptance rather than attempting one giant parity merge.

## 5. BLT-style UI evidence from the supplied screenshot

### [ĐÃ XÁC NHẬN]

The supplied historical screenshot showed a BLT-BIM-style Ribbon with groups broadly equivalent to:

- **BẢN VẼ:** Level
- **VẼ:** Điểm, Vẽ line, Rectangle, Circle, Tạo cửa, Tạo lanh tô, Chọn đối tượng
- **TỰ ĐỘNG NHẬN DẠNG:** Nhận dạng dầm, Tạo bê tông lót, Nhận dạng tường, Cập nhật vị trí, Chuyển đổi BLT-Solid3d, Copy tầng
- **HỆ THỐNG:** Cài đặt, Xoá Highlight, Sửa thuộc tính hàng loạt
- **KHỐI LƯỢNG:** Khấu trừ thể tích, Khấu trừ diện tích, Tính toán khối lượng, Xem khối lượng
- **HIỂN THỊ:** Hiện tất cả mô hình, Hiện coppha
- **HỆ THỐNG VÀ BẢN QUYỀN:** Đăng nhập, Đăng ký, Thông tin

A left-side floor/category palette was also visible with categories such as:

- `HT_Phong`: Room, FloorFinish, WaterProof, Skirting, WallFinish, CeilingFinish
- Dầm
- Sàn: Sàn, Lỗ Mở
- Cột
- Vách: Vách, Lỗ Mở Vách
- TườngKT: TườngKT, Cửa_đi
- Móng: MóngCọc, Đài_Cọc, Dầm_Móng, Móng_Băng, Móng_Bè, Bê_Tông_Lót

### [CHƯA RÕ]

Exact pixel parity is not a source-level contract because BricsCAD theme, DPI, host version, Windows scaling, Ribbon hosting, and runtime WPF/native behavior affect appearance. Licensed BricsCAD runtime evidence is required for visual qualification.

## 6. Current QS3D architecture and capability baseline

### [ĐÃ XÁC NHẬN]

The current repository already contains substantial BIM/QS infrastructure rather than a blank reconstruction project. Across the session, source/docs inspection established existing capabilities in areas such as:

- Project / Zone / Floor / Level / Family / Type / semantic Element modeling;
- source/generated CAD Handle ownership and provenance;
- dirty/regeneration/persistence lifecycle;
- Workspace / Project Browser;
- Model Health / release-readiness checks;
- semantic capture for architecture/structure/rooms/openings;
- direct drawing of common structural families;
- native/generated 3D solid workflows;
- door/opening cuts and room/finish workflows;
- curtain-wall and rebar/BBS-related features;
- Quantity/BQ review, filters, grouping, recalculation, locate/reveal;
- Quick Takeoff / recognition-oriented workflows;
- Schedule/reporting infrastructure;
- XLSX/CSV export paths with semantic/CAD provenance;
- Ribbon, WPF palette, highlight/focus/isolate/review utilities.

**Important evidence boundary:** implementation in source or a passing remote Core/build workflow does not prove licensed BricsCAD interactive runtime behavior.

## 7. Excel / quantity state before the new customer workbook lane

### [ĐÃ XÁC NHẬN]

Before the customer-workbook work completed in this session, QS3D already had a strong quantity/export foundation:

- `QS3DBQ` — quantity summary/review/filter/group/locate/XLSX workflow;
- `QS3DSETUPBLT` — BLT3D compatibility quantity settings preset;
- `QS3DED2` — scoped export (`Selection`, active `Floor`, active `Zone`, or `All`) using regenerated semantic data and provenance;
- `QS3DEXCELLOCATE` — reverse locate for the existing ED2/legacy workbook contract.

The existing ED2 workbook already preserved provenance using the canonical identity concepts:

- QS3D Element ID;
- CAD Handle;
- DWG / Drawing Fingerprint.

Its modern locate path intentionally fails closed when workbook provenance does not match the active project/drawing or live handles cannot be resolved completely.

### [SUY LUẬN]

Because Excel→CAD provenance logic already existed, the correct next step was not to add a second quantity engine. The safer architecture was to project customer-facing workbook sheets from existing canonical quantity/reporting output while reusing the same identity/provenance authority.

## 8. Customer workbook + aggregate/detail reverse trace delivered in this session

### [ĐÃ XÁC NHẬN] Canonical GitHub lane

- Issue: **#3296** — `[BIM3D-QS][P0][Excel] customer workbook + aggregate/detail reverse locate`
- Lane-Key: `issue-3296`
- Canonical branch: `agent/chatgpt-gpt56sol/customer-excel-trace-3296`
- Canonical PR: **#3299**
- Final task head: `9053af77d7dc3b6d6d76dd4bc6eeb46ee18439e4`
- Protected CI run: `32434789970`
- Required checks: `preflight SUCCESS` + `core SUCCESS`
- Landed main commit: `99dc024faafa4becc1a89fa61a894f69fba8aa49`
- Issue state: `MERGED_MAIN / SOURCE_SAFE`, closed completed

### [ĐÃ XÁC NHẬN] Delivered workbook contract

The new customer-facing workbook lane uses exactly these business sheets:

1. `DGKL` — grouped quantity view;
2. `COP_PHA` — formwork-oriented grouped view;
3. `CHI_TIET` — one semantic element per detail row;
4. `TRACE_MODEL` — explicit business-row-to-model provenance map.

The implementation keeps the existing ED2 path for compatibility rather than replacing it.

### [ĐÃ XÁC NHẬN] Delivered commands / UI routing

The lane added/finished a customer-facing export/trace path around:

- `QS3DEXCEL` — export customer workbook;
- `QS3DEXCELTRACE` — reverse locate from customer workbook row to CAD;
- visible quantity Ribbon title/action behavior using **QS3D**, **Xuất Excel**, and **Excel → CAD**, while preserving the internal `QS3D_QTY` identity where required.

### [ĐÃ XÁC NHẬN] Reverse-trace safety contract

The new path validates provenance before changing PICKFIRST/selection. The effective safety model is:

- validate Drawing Fingerprint;
- validate semantic Element ID(s);
- validate the exact/canonical project Handle set;
- require complete live CAD Handle resolution;
- reject malformed, unsupported, stale, wrong-DWG, missing, partial, or ambiguous provenance;
- for `CHI_TIET`, resolve exactly one semantic element;
- for aggregate `DGKL` / `COP_PHA` rows, resolve all underlying elements.

Critical identity/trace cells are hardened against unsupported/formula-backed/ambiguous states. `TRACE_KEY` is deterministic and tied to sheet plus model provenance.

### [ĐÃ XÁC NHẬN] Evidence semantics

Unsupported quantity metrics remain blank rather than being fabricated as zero. A genuine measured zero remains numeric zero. This prevents an exported workbook from falsely claiming measurement evidence that does not exist.

### CI remediation performed on the same canonical carrier

**[ĐÃ XÁC NHẬN]** The lane was not merged after its first CI attempt. Failures were remediated on the same canonical branch, including issues around nullable analysis, workbook reader/exporter robustness, smoke-helper typing, canonical trace/provenance handling, high-bit unsigned CAD Handle support, and Excel's 32,767-character cell limit. The final exact candidate passed the protected `preflight` and `core` checks before merge.

## 9. Earlier integration batch handled during the session

### [ĐÃ XÁC NHẬN]

PR **#3295** (`integration: land reconciled open-PR batch to main`) was an owner-authorized integration carrier for a large set of previously open task PRs.

Important evidence:

- exact integration head: `122cc799a87555e7b4117cba8f90e9c297d4d809`;
- merge commit: `db7cc6f15a828d166731cee8011dd5289e948422`;
- the candidate required CI remediation of stale smoke fixtures after stricter active Floor/Zone canonical-ID behavior landed;
- the explicitly held lane **#2857 / issue #2842** was removed from the combined candidate and not silently merged;
- protected merge occurred through the PR path rather than a direct `main` contents/ref write.

This is important because the session repeatedly had to distinguish “integration candidate exists” from “candidate is green/current/mergeable” and from “actually landed on current main”.

## 10. Repository governance conclusions reinforced by the session

### [ĐÃ XÁC NHẬN]

The repository enforces a lifecycle roughly equivalent to:

```text
read current main
→ check collision / Issue / Lane-Key
→ one canonical task branch
→ implement + commit/push branch
→ exact-head branch CI where applicable
→ refresh/reconcile main
→ one canonical PR
→ protected current-candidate preflight + core SUCCESS
→ re-check current/fresh/mergeable
→ merge same task PR
→ refresh and record landed main SHA
```

Direct task writes to `main` are forbidden, including docs-only work. Current policy gives standing authorization to merge the **same current task PR** after all required gates are green/current/mergeable; it is not permission to bulk-merge unrelated PRs or bypass protection.

The session also reinforced these state distinctions:

```text
edited
!= committed
!= pushed
!= branch CI green
!= PR ready
!= protected PR green
!= merged main
!= licensed runtime qualified
```

## 11. Current repository snapshot when this document lane started

### [ĐÃ XÁC NHẬN]

At registration of this documentation lane, current `main` was:

`519ed8ae53df03f9366de44194c8c45b2706d130`

That current main was already later than the Excel lane's merge commit `99dc024faafa4becc1a89fa61a894f69fba8aa49`, so this document records the Excel work as historical landed functionality rather than pending work.

The current main head at that snapshot was the merge of PR #3347 (`fix(persistence): make MarkSaved failure-atomic (#3343)`), illustrating that concurrent repository activity continued after the Excel work landed.

## 12. Gap analysis after the Excel round-trip milestone

### P0 — highest business value

#### A. Excel template fidelity / customer template engine

**[ĐỀ XUẤT]** Continue toward a template-preserving customer workbook layer with familiar visible columns such as:

`STT | Cấu kiện | Loại | Tầng | Dài | Rộng | Cao | BT m³ | VK m²`

while retaining technical provenance in hidden/technical columns or the dedicated trace sheet.

Desired capabilities:

- preserve formatting, borders, merges and formulas;
- support user/company templates without weakening provenance;
- avoid COM dependency in shared Core;
- keep deterministic export and fail-closed validation.

#### B. Dedicated formwork subsystem

**[ĐỀ XUẤT]** Treat formwork as a first-class quantity subsystem instead of only a presentation view.

Priority rules include:

- column / beam / slab / wall / foundation surface rules;
- deductions at intersections/openings;
- top/bottom/side face inclusion policy;
- minimum area/volume thresholds;
- `coppha_đáy_dầm_tính_vào_đáy_sàn`;
- `offset_coppha`;
- `goc_min_lay_coppha_mat_tren`;
- explainable per-face/per-rule quantity trace.

This is the largest remaining functional area if the business target is “BT/VK đúng và giải thích được”.

#### C. Direct Draw V2

**[ĐỀ XUẤT]** Improve repeated authoring with transient preview, predictable command continuation, family/type-aware defaults, and fewer modal interruptions.

#### D. Native geometry/property edit

**[ĐỀ XUẤT]** Strengthen edit-after-create workflows so users can modify semantic/native geometry and properties without needing destructive rebuild patterns.

#### E. Grid / Level refinement

**[ĐỀ XUẤT]** Continue Grid/Level workflow parity where it directly supports structural authoring, floor copy, and quantity grouping.

### P1 — important follow-up

- visual rebar shape library and shopdrawing workflow;
- richer reports: detail/summary/floor/zone/family/material/formwork/concrete/rebar;
- earthwork workflows;
- model-to-report and report-to-model navigation across more report types;
- IFC-first interoperability; Revit bridge second; Tekla-oriented export later when requirements are explicit.

## 13. Recommended architecture principles for future work

### [ĐỀ XUẤT]

1. **One semantic authority.** Do not build a second quantity/model identity engine for Excel, Ribbon, reports, or integrations.
2. **Trace everything important.** Quantity/report rows should retain enough provenance to answer “this number came from which semantic/CAD objects and which drawing?”
3. **Fail closed on provenance drift.** Never partially select or silently guess when drawing fingerprint, Element IDs, Handles, or workbook trace disagree.
4. **Separate calculation from presentation.** Business workbook formatting/templates should project canonical quantity evidence rather than own the quantity math.
5. **Keep shared Core host-neutral.** BricsCAD-specific UI/editor/ObjectId operations stay in adapters; deterministic parsing/calculation/export logic stays testable without the host where practical.
6. **Do not equate CI with runtime qualification.** Remote source/build evidence and licensed BricsCAD interactive evidence are separate acceptance classes.
7. **Prefer vertical slices.** Each lane should deliver an end-to-end usable behavior with tests/guards rather than broad speculative rewrites.

## 14. Runtime / LOCAL_ONLY boundary

### [CHƯA RÕ]

The following cannot be truthfully marked PASS from this session's remote/source evidence alone:

- licensed BricsCAD V25/V26 `NETLOAD` / DemandLoad behavior on the exact release SHA;
- interactive Ribbon placement, theme/DPI visual fidelity, WPF/palette lifecycle;
- real DWG selection/zoom/highlight behavior under representative customer drawings;
- multi-DWG lifecycle edge cases that depend on the native host;
- installer/updater behavior on a clean Windows + licensed BricsCAD machine;
- performance with large real customer DWGs;
- exact visual comparison with historical BLT screenshots.

Those items require the repository's LOCAL_ONLY qualification runbooks and evidence tied to the exact candidate SHA.

## 15. Definition of success for the broader BLT-familiar QS workflow

### [ĐỀ XUẤT]

A practical broader acceptance target is reached when a user can perform this sequence reliably inside BricsCAD:

1. create/open a QS3D project and floors/levels;
2. author or recognize structural/architectural elements;
3. inspect/edit semantic properties and generated 3D geometry;
4. calculate concrete/formwork quantities with explainable rules;
5. review/filter/group quantities and click back to the model;
6. export a customer template workbook without losing provenance;
7. choose a detail or aggregate Excel row and locate the complete corresponding live model selection;
8. save/reopen/multi-DWG without semantic/provenance drift;
9. pass host-specific local qualification for the supported BricsCAD major.

The Excel customer workbook + reverse trace lane completed in #3296/#3299 is a major part of steps 5–7, but it is not by itself proof that the entire historical BLT business surface has been reproduced.

## 16. Final session assessment

### [ĐÃ XÁC NHẬN]

The session moved the repository beyond analysis-only discussion in two important ways:

- it integrated a broad authorized PR batch through protected `main` while intentionally excluding a held lane;
- it delivered and merged the customer Excel workbook + aggregate/detail Excel→CAD reverse-trace feature through a canonical Issue/branch/PR with protected CI remediation.

### [SUY LUẬN]

The project is now better positioned to reproduce the *business workflow* that made the historical BLT system useful without copying its implementation: semantic authoring, explainable QS data, provenance-safe reports, and direct navigation between model and quantity artifacts.

### [ĐỀ XUẤT]

The next highest-value product lane should be **dedicated, explainable formwork calculation** coupled to the now-landed workbook/trace workflow. That turns the current export/trace infrastructure into a stronger end-to-end concrete/formwork QS workflow instead of treating Excel as the endpoint by itself.

## 17. Traceability references

Repository artifacts directly relevant to this session record:

- `AGENTS.md`
- `docs/MAIN-WRITE-AUTHORIZATION.md`
- `docs/PRODUCT-BOUNDARY.md`
- `CI_POLICY.md`
- `docs/BIM3D-QS-CUSTOMER-EXCEL-TRACE-PLAN-3296.md`
- Issue #3294 — integration batch tracking
- PR #3295 — reconciled open-PR integration batch
- Issue #3296 — customer Excel + aggregate/detail reverse locate
- PR #3299 — customer workbook + reverse model trace
- Issue #72 — licensed/local runtime qualification boundary referenced by the Excel lane
- Issue #3348 — this consolidated session-review documentation lane

This document intentionally records analysis and delivery state without claiming private implementation knowledge of BLT or unexecuted BricsCAD runtime evidence.
