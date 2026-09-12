# BLT3D Parity P8 Drawing Manager + IFC Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bind source-proven Drawing Manager/Xref and IFC workflows into the fail-closed BLT3D parity evidence without claiming native runtime or round-trip qualification.

**Architecture:** Reuse the manifest + host-neutral `ParityWorkflowRegistry` pattern from P2-P7. P8 adds two `Infrastructure`/`Ui` bindings requiring only an active document because both workflows delegate to existing BricsCAD-host operations rather than QS3D semantic mutation logic.

**Tech Stack:** C#/.NET Core contracts, BricsCAD V25 source evidence, Python preflight, TSV manifest, console smoke tests, Markdown runbook.

**Spec:** `docs/superpowers/specs/2026-09-10-blt3d-full-parity-design.md`

## Global Constraints

- Keep `# catalog-complete=false`.
- Advance only `drawing-manager` and `ifc`, and only to `CommandWired`.
- No production V25/V26 behavior mutation in this carrier.
- No copied BLT3D implementation, binaries, assets, private APIs, keys, license behavior, or runtime dependencies.
- Do not claim `SemanticBehaviorPass`, `SaveReopenPass`, `V25V26ParityPass`, IFC round-trip/schema fidelity, or full Drawing Manager rename/clip parity.
- Merge only after exact-head protected `preflight` + `core` are green and Reservation-v2 remains collision-clean.

---

### Task 1: Fail-closed P8 contract

**Files:**
- Create: `scripts/preflight-blt3d-parity-p8-drawing-ifc.py`
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`
- Modify: `tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs`

**Interfaces:**
- Consumes: existing `ParityManifestParser`, `ParityWorkflowRegistry`, current V25 Drawing Manager/Xref and IFC command source.
- Produces: a static contract requiring exactly two P8 `Infrastructure`/`Ui`/`ActiveDocument` bindings and canonical `CommandWired` manifest rows.

- [x] **Step 1: Write failing focused P8 preflight**
- [x] **Step 2: Run it and observe RED because both manifest rows are still `ReferenceCaptured`**

### Task 2: Minimal host-neutral P8 catalog

**Files:**
- Create: `src/QS3D.Core/Features/ParityDrawingInteroperabilityCatalog.cs`
- Modify: `docs/BLT3D-PARITY-MANIFEST.tsv`
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`
- Modify: `tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs`

**Interfaces:**
- Produces: `ParityDrawingInteroperabilityCatalog.CreateRegistry()`.
- Binding IDs: `drawing-manager`, `ifc`.
- Kind/surface/requirements: `Infrastructure`, `Ui`, `ActiveDocument`.

- [x] **Step 1: Add deterministic P8 manifest and registry smoke assertions**
- [x] **Step 2: Add minimal catalog and advance only the two manifest rows to `CommandWired`**
- [x] **Step 3: Run focused P8 preflight until GREEN**

### Task 3: Runbook and integration verification

**Files:**
- Create: `docs/FEATURE-RUNBOOKS/blt3d-parity-p8-drawing-ifc.md`
- Modify: `docs/superpowers/plans/2026-09-12-blt3d-parity-p8-drawing-ifc.md`
- Modify: scripts/preflight-blt3d-parity-p7-review-documentation.py (forward-compatible historical evidence guard)

- [x] **Step 1: Document current-source evidence and clean-room/evidence ceiling**
- [x] **Step 2: Run full deterministic Core smoke**

```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Expected: `ALL PASS`.

- [x] **Step 3: Run `git diff --check`, exact-path and Reservation-v2 collision checks**
- [x] **Step 4: Fetch/rebase latest `origin/main` while branch is unpushed; rerun focused + full Core gates if base changes**
- [ ] **Step 5: Commit/push/open PR and require fresh protected `preflight` + `core` on the exact head**
- [ ] **Step 6: Merge exact current head only when fresh, mergeable and collision-clean; verify resulting `main` and close #6526**

## TDD RED expectation

Before adding the catalog or changing the manifest, run:

```powershell
python scripts/preflight-blt3d-parity-p8-drawing-ifc.py
```

Expected failure: `drawing-manager` (or `ifc`) is not yet canonical `Applicable/CommandWired`. This proves the focused gate detects the missing P8 evidence rather than merely passing existing source.
