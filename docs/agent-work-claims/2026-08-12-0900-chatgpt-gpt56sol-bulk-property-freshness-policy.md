# Agent Work Claim — BulkEdit property freshness policy parity

- Agent: `chatgpt-gpt56sol-bulk-property-freshness-policy`
- Owner: OpenAI ChatGPT
- Status: `ACTIVE`
- Registered: 2026-08-12 09:00 +07:00
- Baseline main SHA observed: `ecf86e34009ffd962ed75752db02fa85af8926af`
- Task key: `CORE-BULK-PROPERTY-FRESHNESS-POLICY`

## Confirmed defect

`BulkEditService.SetProperty(...)` and `MultiplyNumericProperty(...)` bypass the canonical `ProjectElement.SetProperty(...)` mutation policy: they assign into `Properties` directly and then call `MarkDirty(Properties | Quantity | optional Geometry)`. `ProjectElement.MarkDirty(...)` treats any Properties dirty bit as a reason to mark generated output stale, so a real bulk edit of an ordinary property such as `Scale` stales generated output even though `ElementGeometryPolicy.AffectsGeneratedOutput(...)` is false. This diverges from the current canonical property-setting contract, where ordinary properties dirty Properties/Quantity without staling generated output; output-only properties such as `Material` stale generated output without Geometry dirty; geometry properties stale output and dirty Geometry.

Historical bulk behavior intentionally staled every bulk edit, but the newer `ElementGeometryPolicy` contract and the completed Family property-removal freshness lane establish selective generated-output freshness as the current source of truth.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs`
- one focused Core smoke source for bulk property freshness parity
- this claim file

## Excluded scope

- Bulk target enumeration, ownership, bounds, numeric no-op, Family assignment and relation semantics
- `ProjectElement.cs` / `ElementGeometryPolicy.cs` policy changes
- selection/UI/BricsCAD adapters
- Actions/build/release/runtime qualification

## Plan

1. Re-fetch moving `main` and confirm the two bulk property mutation paths still bypass `ProjectElement.SetProperty(...)`.
2. Route committed string and numeric property updates through the canonical element property setter without changing preflight/no-op/atomicity behavior.
3. Preserve exact numeric no-op semantics and changed-element reporting.
4. Add focused smoke coverage proving ordinary, generated-output-only and geometry properties receive the same dirty/stale semantics as direct property setting.
5. Review exact diff against moving `main`, squash-merge with expected head SHA, read back source/test, then close claim with immutable evidence.

No GitHub Actions/build/release is authorized by this lane. No BricsCAD V25/V26 runtime PASS will be claimed remotely.
