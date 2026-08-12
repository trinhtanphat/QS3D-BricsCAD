# Work claim — Quantity XLSX structural limits

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-xlsx-limits-20260812-0127`
- Registered: `2026-08-12T01:27:00+07:00`
- Baseline main SHA: `fe4aadce282748ee6b13cf19bc96c9465905771d`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defects

`XlsxQuantityExporter` had no Excel worksheet row-cap preflight for either the standard report or ED2 detail/summary worksheets, and no 32,767-character inline-string cell preflight. `QuantityReportRow` also exposes computed ElementIdText/SourceHandleText/FloorZoneText values that join raw lists/fields, so oversized cell payloads could be allocated only while worksheet XML was being built.

## Reserved scope

Enforce the 1,048,575 data-row capacity independently for every worksheet and enforce the 32,767-character inline-string capacity before filesystem mutation and before oversized computed/joined cell strings are constructed. Preserve existing ED2 semantic/numeric parity rules and exception ordering where practical.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxQuantityExporter.cs`
- `tests/QS3D.Core.SmokeTests/XlsxQuantityStructuralLimitSmoke.cs`
- this claim file

## Excluded scope

- No XML sanitizer change in this claim.
- No reporting/grouping/business-rule changes.
- No Door/Material/Curtain/RoomFinish/Rebar exporters.
- No UI/native BricsCAD/runtime or GitHub Actions work.

## Validation implemented

- Standard export rejects 1,048,576 data rows before indexing/enumeration/filesystem mutation.
- ED2 independently rejects oversized detail or summary worksheets before semantic traversal/filesystem mutation.
- Exactly 32,767 characters remain accepted in a standard scalar text cell; 32,768 is rejected.
- Oversized separator-inclusive ElementIds/SourceHandles and combined FloorZoneText are rejected before computed joins/concatenation and filesystem mutation.
- Current null-row and ED2 semantic/numeric parity logic remains unchanged outside the new preflight checks.
- Current source and focused smoke were re-read from `main` after integration.

## Integration commits

- Claim: `11467b985a8aeddb17798f5de79774609b0c26ee`
- Source bounds: `0b3da551aece5c84dbd7d608ecf24bb505d2e77a`
- Focused smoke creation: `1ee9cd3d18c30a9549ee056e3ccff838bc4d8981`
- ED2 summary/FloorZone smoke completion: `d759f163109101bcdb148ee42bef8e84ba16b4f1`

## Validation boundary

Remote source/smoke review only. No .NET build, BricsCAD V25/V26 runtime qualification, private-DWG/native execution or GitHub Actions run is claimed by this session.

## Coordination

The prior Quantity XLSX null-row claim is completed. Active quantity-report grouping work owns reporting builders, not this exporter. This claim remained limited to export structural bounds.

## Completion condition

Completed: standard and ED2 worksheet capacities and inline-string cell capacities are enforced before filesystem mutation, focused regression source is present on current `main`, and exact integration SHAs are recorded above.
