# BLT3D Parity P6 Special Categories Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the existing fail-closed BLT3D parity program with P6 rooms, finishes, earthwork, and special-category evidence without overclaiming recovery-only capabilities.

**Architecture:** Reuse the existing manifest + host-neutral `ParityWorkflowRegistry` pattern. Add one P6 catalog for only current QS3D command-wired workflows; keep recovery-only capabilities as manifest evidence with no runtime binding.

**Tech Stack:** C#/.NET Standard Core, console smoke tests, Python repository preflights, TSV manifest, Markdown runbook.

**Spec:** `docs/FEATURE-RUNBOOKS/blt3d-parity-p6-special-categories.md`

## Global Constraints

- Keep `# catalog-complete=false`.
- No V25/V26 production source mutation in this carrier.
- No copied BLT3D implementation, assets, private APIs, license code, keys, or runtime dependencies.
- Do not claim `SemanticBehaviorPass`, `SaveReopenPass`, or `V25V26ParityPass`.
- Merge only after fresh protected `preflight` and `core` checks are green.

---

### Task 1: Fail-closed P6 contract

**Files:**
- Create: `scripts/preflight-blt3d-parity-p6-special-categories.py`
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`
- Modify: `tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs`

**Interfaces:**
- Consumes: existing parity parser/registry patterns from P2–P5.
- Produces: P6 static contract requiring eight wired rows, seven reference-only rows, and eight host-neutral bindings.

- [x] **Step 1: Write the failing P6 preflight**

Run:
```powershell
python scripts/preflight-blt3d-parity-p6-special-categories.py
```
Expected RED: missing P6 manifest feature such as `room`.

- [x] **Step 2: Verify RED before implementation**

Observed: `ERROR: manifest missing P6 wired feature: room`.

### Task 2: Minimal P6 catalog and manifest

**Files:**
- Create: `src/QS3D.Core/Features/ParitySpecialCategoriesCatalog.cs`
- Modify: `docs/BLT3D-PARITY-MANIFEST.tsv`

**Interfaces:**
- Produces: `ParitySpecialCategoriesCatalog.CreateRegistry()` returning exactly eight UI semantic-mutation bindings.
- Produces manifest rows for `room`, `room.finish`, `earthwork`, `stair`, `railing`, `curtain`, `door-opening`, and `grid` at `CommandWired`.
- Produces recovery-only rows for pile-cap, steel-detail, copy-to-level, blinding-concrete, and opening-to-slab families at `ReferenceCaptured`.

- [x] **Step 1: Add minimal catalog and manifest rows**
- [x] **Step 2: Add deterministic P6 smoke assertions**
- [ ] **Step 3: Run focused preflight until GREEN**

### Task 3: Evidence runbook and verification

**Files:**
- Create: `docs/FEATURE-RUNBOOKS/blt3d-parity-p6-special-categories.md`
- Modify: `docs/superpowers/plans/2026-09-12-blt3d-parity-p6-special-categories.md`

**Interfaces:**
- Documents the 61 XAML recovery inventory as clean-room evidence only.
- Records the evidence ceiling separating current QS3D command wiring from recovery-only references.

- [x] **Step 1: Write the P6 runbook**
- [ ] **Step 2: Run full deterministic Core smoke**

Run:
```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```
Expected: `ALL PASS`.

- [ ] **Step 3: Run repository hygiene checks**

Run:
```powershell
git diff --check
git status --short --branch
```
Expected: no whitespace errors; only reserved P6 paths changed.

- [ ] **Step 4: Commit, push, open PR, and wait for protected checks**

Commit message:
```text
feat(parity): bind P6 special-category workflows
```

Merge only when the current PR head has fresh protected `preflight` and `core` GREEN and no Reservation-v2 collision.
