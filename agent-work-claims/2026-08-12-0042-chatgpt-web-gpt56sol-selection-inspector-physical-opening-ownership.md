# Work claim — Semantic selection inspector physical-opening ownership filtering

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-selection-inspector-physical-opening-ownership`
- Registered: `2026-08-12T00:42:00+07:00`
- Completed: `2026-08-12T00:49:00+07:00`
- Baseline main SHA: `4fce6a653e5438fe21bb18a8841b6d619284f0d5`
- Reservation commit: `5796db50d20478f9a5195b967e8f7a3a42113ace`
- Priority: P1 — keep drawing-local physical-opening ownership metadata out of semantic property inspection.

## Defect fixed

`SemanticSelectionInspector.IsInternalOwnershipProperty(...)` hid handle-bearing, `QS3D.Generated...` and legacy `PhysicalOpeningCut...` keys, but did not hide the actual namespaced physical-opening state used by the cut-target codec: `QS3D.PhysicalOpeningCutOpeningIds`. Effective selection properties could therefore surface internal native/drawing-local cut ownership metadata in the semantic Workspace property inspection layer.

The inspector now filters the `QS3D.PhysicalOpeningCut...` namespace in addition to the legacy prefix. The focused smoke injects the real namespaced ownership-key shape and asserts it is absent from the inspected property list.

## Published commits

- `b8075871e6ebd406f2ca7e64c42c5bff4aeed6ac` — `fix(selection): hide namespaced opening ownership metadata`.
- `0987d61f49c4c96821b795483a110cecd2265ac0` — `test(selection): hide namespaced opening ownership metadata`.
- `4793c94545bbfeb1f3504d21099a8e1a9730f0cb` — `test(selection): pin opening ownership inspector filter`.

## Preserved contract

- Existing handle, `QS3D.Generated...` and legacy `PhysicalOpeningCut...` filtering remains unchanged.
- Ordinary semantic property aggregation, Family defaults, quantities and selection/reference validation remain unchanged.
- This lane remained disjoint from the concurrent generic `SemanticPropertyEditPolicy` protection lane; no edit-policy or codec/native file was modified.

## Validation notes

Current inspector source and focused smoke were re-read around publication, and the dedicated static gate is committed under the repository's auto-discovered preflight naming convention. This connector-only lane did not execute Core smoke or Python preflights, so no executable PASS is claimed. A prior close-write attempt received HTTP 409 as `main` advanced; current `main` and the claim blob were re-fetched before this retry, with no force-push or overwrite. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.

## Excluded scope

No `SemanticPropertyEditPolicy`, target-state codec/boolean/native, Workspace WPF or release workflow changes.

## Completion condition

Satisfied for the remote-safe source/static contract: namespaced physical-opening ownership is absent from semantic selection inspection, focused smoke/static coverage is on `main`, and this reservation is released.
