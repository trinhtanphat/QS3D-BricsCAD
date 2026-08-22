# Regeneration Preview Known Count Integrity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `RegenerationPreviewService.PreviewSubset` fail closed on malformed or dishonest known collection counts without changing existing target-ID, duplicate-ID, or project-cardinality precedence.

**Architecture:** Keep all behavior inside the existing `CanonicalPreviewTargets` boundary. Snapshot supported `ICollection<string>`, `IReadOnlyCollection<string>`, and non-generic `ICollection` Count evidence before enumeration; reject negative or conflicting evidence immediately; preserve the existing per-item validation/cardinality loop; after successful traversal, reject a legal known Count that does not equal the observed target count.

**Tech Stack:** C# / .NET Standard Core library, self-registering `QS3D.Core.SmokeTests`, GitHub protected `preflight` + `core` CI.

**Spec:** GitHub Issue #3255 (`issue-3255`).

## Global Constraints

- Preserve `PreviewSubset` project ownership/freshness checks and regeneration semantics.
- Preserve blank/canonical target diagnostics, duplicate-target diagnostics, and the existing streaming project-cardinality guard order.
- Negative or conflicting supported known Counts must fail before target enumeration.
- Legal known Count/traversal mismatches must fail before preview regeneration.
- Pure streaming `IEnumerable<string>` and honest arrays/collections must remain supported.
- No UI, host-runtime, release-workflow, or unrelated refactor changes.

---

### Task 1: Pin the regression first

**Files:**
- Modify: `tests/QS3D.Core.SmokeTests/RegenerationPreviewSmoke.cs`

**Interfaces:**
- Consumes: `RegenerationPreviewService.PreviewSubset(ProjectState, IEnumerable<string>)`.
- Produces: deterministic smoke coverage for negative Count, conflicting Counts, under-yield, over-yield, honest counted input, and pure streaming input.

- [ ] Add test-only counted enumerable helpers whose interface Counts can disagree and whose enumerator can prove whether enumeration occurred.
- [ ] Add smoke assertions that malformed Count metadata is rejected before enumeration.
- [ ] Add smoke assertions that legal Count/traversal mismatches are rejected after traversal but before preview execution.
- [ ] Keep controls proving ordinary counted and streaming sources still work.
- [ ] Push the test-only commit and verify exact-head CI is red for the intended missing production behavior.

### Task 2: Implement the minimal Core fix

**Files:**
- Modify: `src/QS3D.Core/Services/RegenerationPreviewService.cs`

**Interfaces:**
- Consumes: `IEnumerable<string>` and optional supported collection Count interfaces.
- Produces: one consistent optional known Count bound to the completed traversal.

- [ ] Add `System.Collections` support for non-generic `ICollection`.
- [ ] Snapshot all supported known Counts before enumeration.
- [ ] Reject negative Count evidence and conflicting Count evidence before enumeration.
- [ ] Leave the existing target validation and `maxCount` loop order unchanged.
- [ ] After successful enumeration, reject a snapshotted Count that differs from `result.Count`.
- [ ] Push and require exact-head branch CI `preflight` + `core` success.

### Task 3: Protected PR and merge

**Files:**
- No additional production scope.

**Interfaces:**
- Consumes: exact green canonical branch head.
- Produces: one canonical PR for `issue-3255`, then `MERGED_MAIN` under standing owner authorization.

- [ ] Refresh current `main`; if it moved, fast-forward/reconcile the same canonical branch and revalidate.
- [ ] Open the one canonical PR with Lane-Key, owner/session, branch, and acceptance metadata.
- [ ] Require protected current-candidate `preflight` + `core` success, clean review state, and mergeability.
- [ ] Merge with expected-head protection.
- [ ] Fetch `main` again and record the exact landed SHA; close #3255 only after the merge is verified.
