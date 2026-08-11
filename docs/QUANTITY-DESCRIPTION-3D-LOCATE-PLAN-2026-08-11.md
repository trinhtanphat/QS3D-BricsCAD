# Quantity Description -> 3D Locate Hardening Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-chatgpt-web-gpt56sol-quantity-description-3d-locate.md`
Status: IMPLEMENTATION_PENDING

## Goal

Complete and harden the QS3DBQ quantity-description-to-3D workflow without changing quantity/reporting semantics. A displayed BQ row must be revalidated against the current drawing/project, select every still-live CAD object represented by that row, and reveal the surviving selection in the current 3D view.

## Concurrent-work boundary

A concurrent agent already delivered the primary BQ detail UI/revalidation flow. This plan does not rewrite that feature and does not touch the active Core schedule/reporting identity lane. The residual work is intentionally limited to BQ row identity fallback and CAD locate orchestration.

## Verified source findings

1. `SnapshotQuantityAdapter.Build(...)` produces fallback rows from current CAD selection and stores CAD provenance in `QuantityReportRow.SourceHandles`; these rows do not receive semantic `ElementIds`.
2. `QuantitySummaryWindow.ResolveCurrentRow(...)` currently rejects every row whose `ElementIds` collection is empty before trying to revalidate it. Therefore snapshot/raw rows that have valid `SourceHandles` cannot be located even though they contain stable CAD provenance.
3. The `QS3DBQ` locate callback currently resolves handles only through `SourceHandleResolver.Resolve(project, row.ElementIds)` and therefore discards the row's already-revalidated `SourceHandles`.
4. `CadHandleService.Select(...)` already provides the desired low-level resilience: invalid/erased handles can be skipped while surviving objects remain selectable.
5. `QS3DZOOMSELECTED` already frames the implied selection by geometric extents in the current view's display coordinate system, preserves camera direction, and only reports failure when no valid selected extents exist. No viewport-core rewrite is required.
6. `QuantitySummaryWindow` already enforces document affinity and revalidates the displayed row before invoking the locate callback. This invariant must remain fail-closed.

## Invariants

- Semantic BQ rows continue to revalidate by canonical semantic `ElementIds` first.
- Source-handle identity is a fallback only when a row has no semantic IDs.
- A row with neither semantic IDs nor source handles remains non-locatable.
- Revalidation must still produce exactly one current row and `SameRow(...)` must still match all quantity/provenance fields before any CAD selection occurs.
- The callback must defend against an inactive/different MDI document even if the window already checked document affinity.
- Candidate handles are the case-insensitive, trimmed union of semantic-resolved handles and the revalidated row's `SourceHandles`.
- Selection failure for one stale/erased handle must not discard other surviving matches.
- Zoom/reveal runs only when at least one CAD object was actually selected.
- No write/create project behavior is introduced by locate/revalidation.
- No quantity formulas, grouping, report identity rules, Excel semantics, or persistence state are changed.

## Implementation steps

### 1. Revalidation fallback in `QuantitySummaryWindow`

- Canonicalize both `ElementIds` and `SourceHandles` for the displayed row.
- Reject only when both identity sets are empty.
- If semantic IDs exist, keep the current semantic-first matching behavior unchanged.
- Otherwise revalidate the freshly recalculated row set using canonical `SourceHandles`.
- Require a unique match and retain the existing full `SameRow(...)` stale-data guard.
- Add a dedicated source-handle identity helper rather than weakening `SameRow(...)`.

### 2. Resilient locate callback in `Commands.cs`

- Recheck that the callback's bound document is still the active MDI document.
- Read the existing project only; never bootstrap a replacement project during locate.
- Resolve semantic handles as today.
- Union semantic-resolved handles with `row.SourceHandles`, trimming and deduplicating case-insensitively.
- If there are no candidate handles, report a clear non-locatable status and stop.
- Pass all candidates to `CadHandleService.Select(...)` so stale/deleted handles are skipped individually.
- Report full, partial, or zero surviving selection explicitly (`selected / candidate`), making stale-handle behavior understandable to the user.
- Call `QS3DZOOMSELECTED` only when selection count is positive.

### 3. Focused regression/preflight coverage

Add or extend focused source/invariant coverage for:

- snapshot/source-handle-only rows are eligible for safe revalidation;
- semantic identity remains preferred when present;
- rows with no stable identity fail closed;
- callback consumes both semantic handles and row `SourceHandles`;
- handle union is canonical/deduplicated;
- active-document defense remains present;
- partial selection status is represented;
- zoom is gated by `selectedCount > 0`;
- existing BQ detail/reveal invariants remain intact.

Prefer a dedicated BQ locate-resilience preflight if modifying shared tests would collide with concurrent agents.

## Verification

Remote/source-verifiable gates:

1. Re-fetch each modified file from `main` after commit and inspect exact merged content.
2. Run repository CI/preflight workflows available through GitHub where supported.
3. Inspect commit status/workflow checks for the implementation commit.
4. Confirm the implementation diff contains no Core reporting-identity or quantity-formula changes.

Local-only BricsCAD V25 qualification (if not available remotely):

- Open a DWG with semantic QS3D elements; verify summary/detail rows locate all expected objects and preserve current camera orientation.
- Exercise snapshot fallback with no semantic elements and a current CAD selection; verify a source-handle-only row can locate/reveal its objects.
- Delete one object after a row is established, refresh/reopen as required by stale-row guard, and verify surviving valid handles still select while stale handles are reported rather than causing total failure.
- Switch active DWG while the BQ window remains modeless and verify locate fails closed instead of selecting in the wrong document.

Any qualification that truly requires the local BricsCAD runtime must be recorded in `docs/LOCAL-AGENT-INBOX.md` rather than represented as remotely executed.

## Completion criteria

Implementation is complete when source-handle-only BQ rows can safely revalidate and locate, semantic rows retain their existing safety guarantees, stale individual CAD handles degrade to partial success, zoom only follows a non-empty surviving selection, focused regression/preflight coverage is present, and the work claim is updated to `COMPLETE` with verified commit references.
