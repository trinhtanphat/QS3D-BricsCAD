# Quantity Description -> 3D Locate Hardening Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `docs/agent-work-claims/2026-08-11-chatgpt-web-gpt56sol-quantity-description-3d-locate.md`
Status: IMPLEMENTED_SOURCE_SIDE

## Goal

Complete and harden the QS3DBQ quantity-description-to-3D workflow without changing quantity/reporting semantics. A displayed BQ row must be revalidated against the current drawing/project, select every still-live CAD object represented by that row, and reveal the surviving selection in the current 3D view.

## Concurrent-work boundary

A concurrent agent already delivered the primary BQ detail UI/revalidation flow. This plan does not rewrite that feature and does not touch the active Core schedule/reporting identity lane. The residual work is intentionally limited to BQ row identity fallback and CAD locate orchestration.

## Verified source findings

1. `SnapshotQuantityAdapter.Build(...)` produces fallback rows from current CAD selection and stores CAD provenance in `QuantityReportRow.SourceHandles`; these rows do not receive semantic `ElementIds`.
2. `QuantitySummaryWindow.ResolveCurrentRow(...)` originally rejected every row whose `ElementIds` collection was empty before trying to revalidate it. Therefore snapshot/raw rows that had valid `SourceHandles` could not be located even though they contained stable CAD provenance.
3. The pre-existing `QS3DBQ` locate callback resolved handles only through semantic identity, while the row already carried revalidated `SourceHandles`.
4. `CadHandleService.Select(...)` already provides the desired low-level resilience: invalid/erased handles can be skipped while surviving objects remain selectable.
5. `QS3DZOOMSELECTED` already frames the implied selection by geometric extents in the current view's display coordinate system, preserves camera direction, and only reports failure when no valid selected extents exist. No viewport-core rewrite was required.
6. `QuantitySummaryWindow` already enforced document affinity and revalidated the displayed row before locate. This invariant remains fail-closed.

## Invariants

- Semantic BQ rows continue to revalidate by canonical semantic `ElementIds` first.
- Source-handle identity is a fallback only when a row has no semantic IDs.
- A row with neither semantic IDs nor source handles remains non-locatable.
- Revalidation must still produce exactly one current row and retain stale-data guards before CAD selection.
- Selection failure for one stale/erased handle must not discard other surviving matches.
- Zoom/reveal runs only when at least one CAD object was actually selected.
- No write/create project behavior is introduced by locate/revalidation.
- No quantity formulas, grouping, report identity rules, Excel semantics, or persistence state are changed.

## Implementation steps

### 1. Revalidation fallback in `QuantitySummaryWindow`

- Canonicalize both `ElementIds` and `SourceHandles` for the displayed row.
- Reject only when both identity sets are empty.
- If semantic IDs exist, keep the semantic-first matching behavior unchanged.
- Otherwise revalidate the stable source handles directly through `EntitySnapshotReader.ReadHandles(...)`, avoiding dependence on the mutable current PICKFIRST selection.
- Rebuild snapshot quantity rows, require a unique source-group match, validate drawing fingerprint, and retain the full stale-row guard when the complete expected handle set survives.
- Use a dedicated source-group identity helper rather than weakening `SameRow(...)`.

### 2. Resilient locate orchestration

- Prefer the revalidated row's live `SourceHandles` directly in `QuantitySummaryWindow`.
- Pass the full live handle set to `CadHandleService.Select(...)` so stale/deleted handles can be skipped individually.
- Report full, partial, or zero surviving selection explicitly (`selected / expected`).
- Fall back to the existing `_locate` callback only when no revalidated row handles are available.
- Call `QS3DZOOMSELECTED` only after a positive CAD selection count.
- Keep document-affinity checks and project reads non-creating.

### 3. Focused regression/preflight coverage

A dedicated gate now checks:

- snapshot/source-handle-only rows are eligible for safe revalidation;
- semantic identity remains preferred when present;
- rows with no stable identity fail closed;
- source-handle revalidation uses `ReadHandles(...)`, not `ReadCurrentSelection(...)`;
- direct handle selection supports partial stale/deleted handles;
- zoom remains ordered after selection and the zero-selection guard;
- source-handle revalidation remains read-only and non-creating.

Gate: `scripts/preflight-quantity-description-3d-locate.py`.

## Verification

Remote/source-verifiable evidence:

1. Implementation merged to `main` as `775a1098ec8e58d689ad099cfd658d78680e5bdd` through PR #470.
2. Focused gate committed as `a628fad8674715d3d2066c25fb06d2c30cf99bae`.
3. Re-fetch from `main` confirmed target blob `edb734b0a42ccb55147bbdc917a763250da5de46` and gate blob `e9bf3707b9bdbb7f5899532e6d0f2053ad0d6250`.
4. Python AST syntax check for the focused gate passed in the available environment.
5. GitHub exposed no combined status checks or workflow runs for the gate commit; that absence is recorded rather than treated as a CI pass.
6. Comparison from the gate commit to then-current `main` `a70ca7ad759ee13442ba98af3e3de473aaea0f23` showed nine later concurrent commits and none modified the target file or focused gate.
7. Implementation scope remained one BricsCAD UI file; no Core reporting identity, quantity formula/grouping, persistence, or Excel source was changed.

Local-only BricsCAD V25 qualification remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing `docs/LOCAL-AGENT-INBOX.md` BQ/modeless/full-interactive qualification matrix. A duplicate local queue item was intentionally not created because the inbox requires deduplication.

## Completion criteria

Source-side implementation criteria are satisfied: source-handle-only BQ rows can safely revalidate and locate; semantic rows retain semantic-first safety; stale individual CAD handles degrade to partial success; zoom only follows a non-empty surviving selection; focused regression coverage is present; and the work claim is `COMPLETE`. Licensed BricsCAD V25 interactive qualification remains explicitly local-only.
