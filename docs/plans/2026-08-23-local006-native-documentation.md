# LOCAL-006 Native Documentation Completion Plan

> Carrier: #3642 / `issue-3642`
> Branch: `agent/chatgpt/local006-source-completion-20260823`
> Scope boundary: source-safe work only. Licensed BricsCAD V25/V26 runtime evidence remains LOCAL_ONLY and must not be fabricated by remote CI.

## Goal

Close the remaining source gaps explicitly recorded by LOCAL-006 without reimplementing the already-owned MText tag and native table lifecycles:

1. Add native MLeader semantic tags with the same generated-ownership, refresh/remove, rollback, health, and audit guarantees as existing MText tags.
2. Materialize Core `SemanticSheetPlan` / `SemanticViewPlan` into owned BricsCAD Layout/PaperSpace artifacts: paper-space Viewport(s), locked scale, and title-block BlockReference attribute binding.
3. Add refresh/remove/health commands for owned sheet artifacts, fail closed on ownership or live-object drift, and require explicit user confirmation before destructive refresh/remove.
4. Keep existing generic/authoritative/custom schedule Table lifecycle intact; only reuse its conventions where applicable.
5. Add static contract coverage to remote CI and update LOCAL-006 with the exact source-ready SHA for V25/V26 licensed qualification.

## Design constraints

- Core planners remain the source of truth. Native code materializes plans; it does not duplicate semantic planning or auto-layout logic.
- `GeneratedSemanticTagHandles` remains the canonical semantic-tag owner slot. Existing MText remains valid; MLeader becomes a second supported runtime artifact kind rather than a parallel ownership system.
- MLeader owns a deterministic target handle and WCS leader/text placement metadata so health/refresh can validate drift.
- Sheet artifacts get one explicit project-owned metadata contract. Refresh validates the complete currently-owned set before erasing anything, then rebuilds atomically inside one CAD transaction; project state is rolled back when CAD does not commit.
- Never erase or mutate a live CAD object merely because a Handle resolves: generated XData/project ownership must match first.
- V25 and V26 source parity must use the repo's existing source-sharing/build pattern; no copy-paste fork unless the project structure requires it.

## Task 1 — RED contract test for MLeader lifecycle

**Files:**
- Create: `scripts/preflight-local006-native-documentation.py`
- Modify: branch-CI script/workflow entry point that enumerates repo preflights (exact file discovered before edit)

**RED assertions:**
- A native semantic MLeader builder/service exists and creates `MLeader` rather than raw line + MText geometry.
- It records tag-kind plus leader target/text WCS metadata under the existing semantic-tag ownership slot.
- Existing replace/remove/runtime-health code accepts both MText and MLeader and validates ownership before destructive operations.
- Batch command/service exists and processes a bounded deterministic selection without silently partially replacing ownership metadata.

**Verify RED:** push the test-only commit and confirm feature-branch CI fails specifically because the implementation symbols/files are absent.

## Task 2 — GREEN MLeader implementation

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/Cad/SemanticTagBuilder.cs`
- Modify: `src/QS3D.BricsCAD.V25/Cad/SemanticTagRemovalService.cs`
- Modify: `src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs`
- Modify: `src/QS3D.BricsCAD.V25/SemanticTagCommands.cs`
- Create native helper/service files under `src/QS3D.BricsCAD.V25/Cad/` as required by the RED contract.
- Modify command discovery/ribbon files only if command registration is explicit rather than attribute-driven.

**Implementation:**
- Reuse existing renderer, text-height guard, source-handle uniqueness, rollback snapshot, generated XData, owner index, and audit trail.
- Build a native MLeader with validated finite target/text points and bounded semantic text height.
- Store artifact kind and leader placement/target metadata.
- Generalize validation/removal/health by supported semantic-tag entity kind; preserve existing MText-specific drift checks and add corresponding MLeader checks.
- Add create/refresh/remove-safe batch path with deterministic ordering and fail-closed validation before mutation.

**Verify GREEN:** feature-branch CI static contract passes; no existing semantic-tag preflight regresses.

## Task 3 — RED contract test for native sheets/layouts

**Files:**
- Extend: `scripts/preflight-local006-native-documentation.py`

**RED assertions:**
- Native sheet artifact service consumes `SemanticSheetPlan` and `SemanticViewPlan`.
- It creates/finds a Layout and PaperSpace BTR, creates `Viewport`, applies deterministic center/size/view target/height/custom scale and locks it after configuration.
- It inserts the requested title-block BlockReference and binds attributes from `SemanticTitleBlockParameterMapBuilder` output.
- Generated viewport/title-block artifacts carry project/sheet/view ownership metadata.
- Refresh/remove validate the complete owned set and fail closed before erase; health is read-only.
- Commands prompt before mutation.

**Verify RED:** run/push contract and confirm failure is for missing sheet implementation.

## Task 4 — GREEN native sheet lifecycle

**Files:**
- Create: `src/QS3D.BricsCAD.V25/Cad/SemanticSheetArtifactService.cs`
- Create: `src/QS3D.BricsCAD.V25/Cad/SemanticSheetRuntimeHealthService.cs`
- Create: `src/QS3D.BricsCAD.V25/SemanticSheetCommands.cs`
- Modify shared command/health aggregation files only where integration is required.

**Implementation:**
- Validate active-document + existing-project mutation context before any CAD mutation.
- Materialize `SemanticSheetPlan` placements to PaperSpace viewports using Core placement values; lock viewports after scale/view configuration.
- Resolve title-block definition fail-closed; bind existing AttributeReferences by destination tag from `SemanticTitleBlockParameterMapBuilder` output.
- Mark all generated entities with project/sheet/view ownership metadata; persist canonical handle set in the established project metadata location chosen from existing repo conventions.
- Refresh validates every old handle and ownership first, then replaces atomically. Remove validates all then erases all. Health never mutates.
- Audit create/refresh/remove.

**Verify GREEN:** new contract + existing documentation/table/tag preflights all pass in branch CI.

## Task 5 — V25/V26 build/static parity and docs handoff

**Files:**
- Inspect/modify V25/V26 project files only if new source files are not automatically/shared-included.
- Modify: `docs/LOCAL-AGENT-INBOX.md`
- Modify any LOCAL-006 runbook/checklist that names the old source gap.

**Verification:**
- Branch CI green on exact head SHA.
- Search for stale statements saying MLeader or Sheet/Layout/Viewport/title-block/PaperSpace are unimplemented; replace only where #3642 makes them source-ready.
- Record exact source-ready branch/SHA in LOCAL-006, leaving status LOCAL_ONLY/OPEN until licensed V25/V26 evidence is produced.

## Task 6 — PR protected checks, review, merge

1. Re-check `main` and collision state; sync only if required by repo policy.
2. Open one PR for #3642.
3. Obtain fresh protected `preflight` and `core` green on PR head.
4. Review diff against issue acceptance and race policy.
5. Merge through PR under standing owner authorization.
6. Verify `main` contains the merge, comment final evidence on #3642, and close source carrier when appropriate while keeping licensed LOCAL-006 evidence boundary explicit.
