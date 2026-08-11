# Work claim — Bulk empty-property presence semantics

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-bulk-empty-property-presence`
- Registered: `2026-08-12T01:05:00+07:00`
- Last Updated: `2026-08-12T01:09:00+07:00`
- Baseline main SHA: `4091bf5df09ef07ff5f609104ac9f580234e3265`
- Priority: deterministic Core property-presence mismatch found during owner-requested continue-all audit
- Task Key: `CORE-BULK-EMPTY-PROPERTY-PRESENCE`
- Implementation PR: `#598`
- Implementation commit on `main`: `a5937bae2b324edbcbc4040bf7d63d5320a46944`

## Confirmed defect

`BulkEditService.SetProperty(...)` read a property with `TryGetValue(...)` but compared `(before ?? string.Empty)` to the requested value without considering whether the key existed. For an absent property and requested empty string, the comparison succeeded and the bulk API returned a no-op without creating the property.

Canonical `ProjectElement.SetProperty(...)` only returns a no-op when the key already exists and its value matches, so absent -> explicit empty is a real semantic map mutation.

## Implemented scope

`BulkEditService.SetProperty(...)` now keeps the `TryGetValue(...)` presence bit and only treats equal values as a no-op when the property already existed. This preserves explicit empty-property presence while leaving all existing non-empty/update policies unchanged.

Focused isolated smoke coverage verifies:

- absent -> empty creates the key, reports one changed element, marks `Properties | Quantity`, and touches project freshness once;
- existing empty -> empty remains a complete no-op and keeps the explicit key;
- existing non-empty -> empty remains a real mutation with the established dirty policy.

## Surfaces changed

- `src/QS3D.Core/Services/BulkEditService.cs`
- `tests/QS3D.Core.SmokeTests/BulkEmptyPropertyPresenceSmoke.cs`
- `tests/QS3D.Core.SmokeTests/BulkEmptyPropertyPresenceSmokeRegistration.cs`
- this claim file

## Coordination / exclusions preserved

- The completed Bulk Family relation-dirty code path and smoke were not modified beyond retaining their already-merged source.
- `ProjectElement.SetProperty(...)` was not changed; it remained the canonical reference behavior.
- No property-removal API, UI behavior, persistence format, quantity engine, BricsCAD adapter/runtime or Family semantics changed.
- No GitHub Actions/build/release workflow was dispatched and no licensed BricsCAD runtime PASS is claimed.

## Validation evidence

- Claim was published on `main` before implementation at `c2c6c30af9ac876fd40fdbf3bc8f8f082161e0e0`.
- Post-claim readback confirmed `BulkEditService.cs` blob `1189bc3f544dc0928119337df839a4b0805ad445` still collapsed property absence into empty-string equality.
- PR `#598` diff was reviewed before merge and contained exactly three intended files with `+107/-2`; production behavior changed only the no-op condition.
- Server-side squash merge with exact expected head produced `a5937bae2b324edbcbc4040bf7d63d5320a46944`.
- Local build/smoke execution is **not** claimed because this connector-only environment does not provide the project checkout/build runner.

## Completion

`COMPLETED`: current `main` preserves explicit empty-property presence through bulk edits consistently with the canonical element property API while preserving existing no-op and dirty behavior.
