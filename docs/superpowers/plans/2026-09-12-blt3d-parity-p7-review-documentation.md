# BLT3D Parity P7 Review & Documentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bind source-proven View, Quantity and Revision workflows into the existing fail-closed parity registry while keeping Drawing Manager reference-only until QS3D exposes one canonical equivalent workflow.

**Architecture:** Reuse the TSV manifest and host-neutral `ParityWorkflowRegistry` pattern from P2-P6. P7 adds read-only UI bindings only for workflows with explicit current QS3D commands; Project Browser and semantic sheet machinery remain evidence for Drawing Manager inventory but do not justify a `CommandWired` claim by themselves.

**Tech Stack:** C#/.NET Standard Core, console smoke tests, Python preflight, TSV manifest, Markdown runbook.

**Spec:** `docs/FEATURE-RUNBOOKS/blt3d-parity-p7-review-documentation.md`

## Global Constraints

- Keep `# catalog-complete=false`.
- No V25/V26 production source mutation in this carrier.
- No copied BLT3D implementation, binaries, proprietary assets, private APIs, license behavior, keys, or runtime dependencies.
- Do not claim `SemanticBehaviorPass`, `SaveReopenPass`, or `V25V26ParityPass`.
- `drawing-manager` remains `ReferenceCaptured` unless a canonical current QS3D user workflow is proven.
- Merge only after fresh protected `preflight` and `core` checks are green.

---
### Task 1: Fail-closed P7 contract

**Files:**
- Create: `scripts/preflight-blt3d-parity-p7-review-documentation.py`
- Modify: `tests/QS3D.Core.SmokeTests/ParityManifestSmoke.cs`
- Modify: `tests/QS3D.Core.SmokeTests/ParityWorkflowRegistrySmoke.cs`

**Interfaces:**
- Consumes: current parity manifest/parser/registry patterns.
- Produces: static contract requiring `view`, `quantity`, `revision` at `CommandWired`, `drawing-manager` at `ReferenceCaptured`, and exactly three host-neutral read-only UI bindings.

- [x] **Step 1: Write failing P7 preflight**
- [x] **Step 2: Run it and observe failure on current `ReferenceCaptured` evidence**

### Task 2: Minimal P7 catalog and evidence

**Files:**
- Create: `src/QS3D.Core/Features/ParityReviewDocumentationCatalog.cs`
- Modify: `docs/BLT3D-PARITY-MANIFEST.tsv`

**Interfaces:**
- Produces: `ParityReviewDocumentationCatalog.CreateRegistry()` with three read-only UI bindings.
- Leaves Drawing Manager unbound and fail-closed.

- [x] **Step 1: Add minimal catalog + manifest stage changes**
- [x] **Step 2: Add deterministic manifest/registry smoke assertions**
- [x] **Step 3: Run focused P7 preflight until GREEN**
### Task 3: Runbook, verification and integration

**Files:**
- Create: `docs/FEATURE-RUNBOOKS/blt3d-parity-p7-review-documentation.md`
- Modify: `docs/superpowers/plans/2026-09-12-blt3d-parity-p7-review-documentation.md`

- [x] **Step 1: Document current-source evidence and clean-room ceiling**
- [x] **Step 2: Run full deterministic Core smoke**

```powershell
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Expected: `ALL PASS`.

- [x] **Step 3: Run `git diff --check`, reservation and exact-path checks**
- [x] **Step 4: Fetch/rebase latest `origin/main` while branch is unpushed; rerun all gates**
- [ ] **Step 5: Commit/push/open PR and require fresh protected `preflight` + `core`**
- [ ] **Step 6: Merge exact current head only when fresh, mergeable and collision-clean**
