# BIM3D-QS customer acceptance evidence matrix

Status: SOURCE-SAFE ACCEPTANCE CHECKPOINT  
Parent program: #3142  
Canonical lane: #3308 / `issue-3308`  
Baseline audited: `main@5fc42de65f270a5901f99f20d89af12822c8a81a`  

This document records what the repository can currently prove for the customer-first BIM3D-QS path. It deliberately separates deterministic source/CI evidence from licensed BricsCAD runtime evidence. Source presence, smoke coverage, V25 adapter compilation, or a merged PR must never be reported as an interactive licensed-runtime PASS.

The target path remains:

`Project/Floor/Family -> author/capture 3D -> regenerate -> quantity -> review/locate/explain -> export -> save/reopen/recalculate`

## Evidence classes

- **SOURCE_PROVEN** — current `main` contains deterministic implementation plus focused regression/guard evidence for the stated contract.
- **SOURCE_COMPOSED** — current `main` contains the required source slices and an integrated deterministic cross-slice smoke, but native UI/geometry behavior still needs local qualification.
- **ACTIVE_DEPENDENCY** — another current canonical carrier owns the remaining source-safe work; this lane must not duplicate it.
- **LOCAL_ONLY / PENDING** — exact-SHA interactive/licensed BricsCAD evidence is required under #72 and has not been established by this remote lane.

## Current acceptance matrix

| # | Customer acceptance row | Current source evidence on `main` | Classification | Remaining boundary |
|---|---|---|---|---|
| 1 | Create/open a drawing-bound QS3D project and establish a safe starter context | PR #3155 merged `ProjectOnboardingService` with fail-closed unit resolution, add-only starter Floor/Families, explicit material confirmation, idempotence and `ProjectOnboardingRegression` coverage. It reuses canonical project/Family services instead of a second store. | SOURCE_PROVEN | Interactive first-run click-through remains LOCAL_ONLY under #72. |
| 2 | Define/select Floor/Level and Family/Type without hidden unit/material guesses | PR #3155 requires unit confirmation when unresolved, creates `Tầng 1` only when no Floor exists, preserves existing data, and uses `ProjectFamilyQuickSchemaService` + `ProjectFamilyService`. | SOURCE_PROVEN | Richer Grid/Level constraints remain separate under #79; do not fold them into this P0 acceptance claim. |
| 3 | Author the P0 category envelope through canonical modeling routes | Program #3142 records canonical Direct Draw/capture routes for ArchitecturalWall, Beam, Column, Slab, StructuralWall, Foundation and Door/WallOpening. Modeling hardening PR #3159 merged a concrete rebuild-provenance fix in `src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs` with `scripts/preflight-generated-solid-opening-cut-provenance.py`. | SOURCE_COMPOSED | Native Direct Draw/host geometry behavior still requires exact-SHA licensed V25 qualification; #74/#80 remain separate interaction/edit scopes. |
| 4 | Regenerate stable native 3D ownership without stale opening-cut provenance | PR #3159 makes generated-solid replacement invalidate `PhysicalOpeningCut*` provenance before publishing clean replacement state; the guard verifies ordering against `OpeningBooleanService` and `GeneratedDependentGeometryInvalidator`. | SOURCE_PROVEN for the corrected ownership invariant | Real `Solid3d` replacement/boolean behavior is LOCAL_ONLY / PENDING. |
| 5 | Rebuild/recalculate without duplicate or stale quantity truth | Merged PR #3240 added `tests/QS3D.Core.SmokeTests/Bim3dGoldenProjectSmoke.cs`. It asserts repeated clean regeneration, unique semantic/source identity, dependency-aware regeneration, QSDB save/load, dirty re-regeneration and intended quantity equality. | SOURCE_PROVEN at CAD-independent boundary | Native generated-object uniqueness after interactive rebuild/save/reopen remains LOCAL_ONLY. |
| 6 | Calculate deterministic quantities for the P0 categories and fail closed on malformed hosted-opening identity | #3144 / PR #3154 landed the hardened canonical quantity path in `src/QS3D.Core/Services/SemanticRegenerators.cs`; `tests/QS3D.Core.SmokeTests/WallOpeningHostCanonicalitySmoke.cs` pins canonical host deduction and fail-closed malformed/null cases. PR #3240 additionally covers Wall+Door, Beam, Column, Slab, StructuralWall and Foundation quantities in one synthetic project. | SOURCE_PROVEN | Representative customer/private-DWG behavior remains LOCAL_ONLY. |
| 7 | Review quantity evidence and locate/explain without turning missing evidence into fabricated zero | #3145 / PR #3171 landed evidence-aware reporting/export through `QuantityReportRow`, `ProjectQuantityReportBuilder`, `XlsxQuantityExporter`, `QuantityExportEvidenceSmoke`, and `scripts/preflight-ed2-excel-roundtrip.py`. #3148 landed the canonical Quantity Review UX composition and single-instance lifecycle, including locate/recalculate/repair routing. | SOURCE_COMPOSED | Interactive selection/highlight/focus behavior remains LOCAL_ONLY. |
| 8 | Export a readable workbook/report with stable source provenance and support reverse trace requested by the customer | Existing ED2/XLSX evidence semantics and Element ID / CAD Handle / drawing-fingerprint provenance are merged through #3145. The requested customer workbook projection (`DGKL`, `COP_PHA`, `CHI_TIET`, `TRACE_MODEL`) plus aggregate/detail Excel -> CAD reverse locate is currently owned by ACTIVE carrier #3296. | ACTIVE_DEPENDENCY | Do not duplicate #3296. Until it lands, the new customer workbook/reverse-locate acceptance row is not source-complete. |
| 9 | Save, close/reopen, recalculate and preserve intended result | Merged PR #3240 exercises `QsdbProjectStore.Save/Load`, identity preservation, quantity preservation, dirty marking, regeneration and report revalidation in the synthetic P0 project. | SOURCE_PROVEN at Core/persistence boundary | Actual DWG + sidecar lifecycle in licensed BricsCAD remains LOCAL_ONLY. |
| 10 | Reject stale/missing/unsupported conditions instead of guessing | #3144 rejects malformed hosted-opening identity before publishing replacement quantities; #3145 preserves missing-vs-zero evidence; PR #3155 fails closed on unresolved units/materials; PR #3240 pins missing-element selection refusal. #3296 separately owns fail-closed wrong-DWG/stale/partial reverse-locate semantics for the requested customer workbook. | SOURCE_PROVEN for landed contracts; ACTIVE_DEPENDENCY for new workbook reverse-locate path | Runtime-only host failures remain LOCAL_ONLY. |

## Integrated synthetic evidence

Merged PR #3240 is the current repository-owned cross-slice source control. `Bim3dGoldenProjectSmoke` covers:

- ArchitecturalWall with linked Door;
- Beam;
- Column;
- Slab;
- StructuralWall;
- Foundation;
- independently asserted deterministic quantities;
- dependency-aware report provenance and drawing fingerprint;
- repeated clean regeneration;
- semantic/source identity uniqueness;
- meter/millimeter parity;
- fail-closed missing-element selection;
- QSDB save/load/recalculate.

The PR was merged as `5812ada1c2dac7edf411b69951750dbdb6c33758` after protected `preflight` + `core` success on its reconciled candidate. This is source/CI evidence, not licensed BricsCAD runtime qualification.

## Current metadata reconciliation notes

Some child Issue bodies are stale relative to merged PR truth and therefore must not be used alone as current acceptance evidence:

- #3146 still presents an ACTIVE source carrier, but integration PR #3240 is merged and its synthetic smoke is on `main`.
- #3147 still presents an unclaimed template in search results, but PR #3155 is merged as `c4e0de612d21a72ecfec5c66f750a61e9f1acf9b`.
- #3149 still presents an unclaimed template in search results, but PR #3159 is merged as `7cec9ebb8896c57dea6db92482df8aab6430b22b`.

This C0 lane does **not** mutate those historical carriers merely to normalize metadata. PR/main ancestry is used as the implementation truth; ownership remains untouched.

## Remaining P0 blockers/dependencies

### #3296 — customer workbook + Excel -> CAD

This is the only currently identified source-safe customer-path dependency that overlaps acceptance row 8. It is ACTIVE under another canonical owner and reserves the customer workbook projection, trace reader/resolver, quantity-ribbon routing and aggregate/detail reverse locate. #3308 records it only as a dependency and must not modify its files or branch.

### #72 — licensed V25 qualification

Native `Solid3d`, NETLOAD, modeless UI, selection/highlight, Undo, save/reopen DWG behavior, private/customer DWGs, Unicode/HiDPI and real interactive click-through remain `LOCAL_ONLY / PENDING` unless #72 records exact-SHA licensed evidence. This matrix must not convert cloud/source evidence into a runtime PASS.

## C0 closure rule

The source-safe matrix itself is complete when every P0 row has one of: current merged evidence, an explicit non-overlapping ACTIVE dependency, or a LOCAL_ONLY classification. Program #3142 must **not** be called fully customer-ready merely because this document is complete. In particular, row 8 remains dependent on #3296, and native customer acceptance remains dependent on #72.
