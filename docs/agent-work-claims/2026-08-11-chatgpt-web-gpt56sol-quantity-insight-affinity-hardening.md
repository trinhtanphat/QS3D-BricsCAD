# Work claim — Quantity Insight modeless document/row affinity hardening

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-affinity-hardening`
- Registered: `2026-08-11T20:56:00+07:00`
- Reopened: `2026-08-11T21:06:00+07:00`
- Completed: `2026-08-11T21:08:00+07:00`
- Baseline main SHA: `12159590d88afd2127f49404d254184883e4f0b5`
- Priority: P1

## Reserved scope

- Audit and harden the newly added docked `QuantityInsightPanel` so modeless rows cannot locate/select CAD objects after the active DWG/project or live quantity row has changed.
- Preserve the existing read-only quantity tree, selection highlight, native Handle selection + `QS3DZOOMSELECTED`, and the completed `QS3DBQ` detail/reveal lane.
- Add deterministic source/preflight coverage for the document-affinity and stale-row fail-closed contract.
- Reconcile the affinity preflight with the concurrent detached preview-regeneration implementation without weakening either contract.

## Implemented

- `a13ff0bbc79d3c6bd6a4e6d8f1bdd33f45564a3e` — binds each quantity insight refresh to its exact BricsCAD `Document`, ProjectId and drawing fingerprint; stores the displayed grouped `QuantityReportRow` snapshot per tree item; rejects cross-DWG/project locate; requires one exact semantic identity plus unchanged quantity/provenance before resolving Handles.
- `39259a39e4bba3c2fe2e11ae72d3bebc4912aa19` — introduced the document/row-affinity source gate.
- Concurrent `1c5513b403cef7fd1463960f4714018f2ac2e666` upgraded the read model to detached preview-regeneration. Current source keeps the affinity guards while rebuilding both displayed and validation rows from `ProjectStateSnapshot.CreateDetachedCopy(...)` -> `RegenerateDirty(...)` -> `ProjectQuantityReportBuilder.Group(previewProject)`.
- `bdbc501bb377d64da47d41554c4c0cfbf680f6b4` — reconciled and strengthened `scripts/preflight-quantity-insight-affinity.py` so detached preview regeneration is now a mandatory part of the stale-row protection contract rather than a conflicting token.

## Validation

- Re-fetched current `QuantityInsightPanel.xaml.cs` and confirmed the source retains exact-document/project binding, full row/provenance equality, detached preview-regeneration, current-row Handle resolution and `QS3DZOOMSELECTED` only after validation.
- Re-fetched the affinity preflight after `bdbc501...`; it requires DWG -> project -> current preview row -> Handle -> native CAD selection ordering, detached snapshot -> regenerate -> group ordering, and forbids project creation/mutation binds, stale item-ID Handle resolution and direct live-project grouping.
- The repository aggregate preflight auto-discovers `scripts/preflight-*.py`, so the reconciled affinity gate participates automatically in aggregate runs.
- No force push or Actions dispatch was used.

## LOCAL_ONLY disposition

- Native BricsCAD V25 modeless mouse selection, implied-selection highlight and viewport zoom remain covered by the existing local interactive qualification; no remote runtime PASS is claimed.

## Completion evidence

- Quantity Insight fails closed after DWG/project changes or stale/ambiguous/changed quantity rows while preserving detached read-only regeneration.
- Only a current revalidated preview row reaches semantic Handle resolution, native CAD selection and zoom.
- Current reconciliation/test tip: `bdbc501bb377d64da47d41554c4c0cfbf680f6b4`; concurrent main winners were preserved.
