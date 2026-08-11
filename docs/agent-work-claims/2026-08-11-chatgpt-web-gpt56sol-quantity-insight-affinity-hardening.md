# Work claim — Quantity Insight modeless document/row affinity hardening

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-affinity-hardening`
- Registered: `2026-08-11T20:56:00+07:00`
- Reopened: `2026-08-11T21:06:00+07:00`
- Baseline main SHA: `12159590d88afd2127f49404d254184883e4f0b5`
- Priority: P1

## Reserved scope

- Audit and harden the newly added docked `QuantityInsightPanel` so modeless rows cannot locate/select CAD objects after the active DWG/project or live quantity row has changed.
- Preserve the existing read-only quantity tree, selection highlight, native Handle selection + `QS3DZOOMSELECTED`, and the completed `QS3DBQ` detail/reveal lane.
- Add deterministic source/preflight coverage for the document-affinity and stale-row fail-closed contract.
- Reconcile the affinity preflight with the concurrent detached preview-regeneration implementation without weakening either contract.

## Implemented before reopen

- `a13ff0bbc79d3c6bd6a4e6d8f1bdd33f45564a3e` — binds each quantity insight refresh to its exact BricsCAD `Document`, ProjectId and drawing fingerprint; stores the displayed grouped `QuantityReportRow` snapshot per tree item; rejects cross-DWG/project locate; rebuilds current grouped rows read-only and requires one exact semantic identity plus unchanged quantity/provenance before resolving Handles.
- `39259a39e4bba3c2fe2e11ae72d3bebc4912aa19` — added `scripts/preflight-quantity-insight-affinity.py`.
- Concurrent `1c5513b403cef7fd1463960f4714018f2ac2e666` changed current-row rebuilding from direct `ProjectQuantityReportBuilder.Group(project)` to detached `BuildPreviewRows(project, out _)`; source affinity guards remain intact but the original static token is now stale and must be reconciled.

## Validation plan after reopen

- Keep detached `ProjectStateSnapshot.CreateDetachedCopy(...)` + `RegenerateDirty(...)` preview semantics.
- Keep DWG -> project -> current preview row -> Handle -> CAD selection ordering.
- Update the affinity gate so it accepts/requires the detached preview path rather than the superseded direct live grouping token.
- Re-fetch both source and preflight after the reconciliation commit; no force push.

## LOCAL_ONLY disposition

- Native BricsCAD V25 modeless mouse selection, implied-selection highlight and viewport zoom remain covered by the existing local interactive qualification; no remote runtime PASS is claimed.

## Completion condition

- Aggregate static preflight remains green-compatible with the concurrent preview-regeneration source while preserving stale/cross-DWG fail-closed behavior.
- Claim is closed again with the reconciliation SHA.
