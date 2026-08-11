# Work claim — Quantity XLSX structural limits

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-xlsx-limits-20260812-0127`
- Registered: `2026-08-12T01:27:00+07:00`
- Baseline main SHA: `fe4aadce282748ee6b13cf19bc96c9465905771d`
- Priority: P2 evidence-driven remote-safe XLSX integrity hardening

## Confirmed defects

`XlsxQuantityExporter` has no Excel worksheet row-cap preflight for either the standard report or ED2 detail/summary worksheets, and no 32,767-character inline-string cell preflight. `QuantityReportRow` also exposes computed ElementIdText/SourceHandleText/FloorZoneText values that join raw lists/fields, so oversized cell payloads can be allocated only while worksheet XML is being built.

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

## Validation plan

- Standard export rejects 1,048,576 data rows before indexing/enumeration/filesystem mutation.
- ED2 independently rejects oversized detail or summary worksheets before semantic traversal/filesystem mutation.
- Exactly 32,767 characters remain accepted in a standard scalar text cell; 32,768 is rejected.
- Oversized separator-inclusive ElementIds/SourceHandles and combined FloorZoneText are rejected before computed joins/concatenation and filesystem mutation.
- Current null-row and ED2 parity behaviors remain intact.
- SHA-guard writes and re-read current `main` after integration.

## Coordination

The prior Quantity XLSX null-row claim is completed. Active quantity-report grouping work owns reporting builders, not this exporter. This claim is limited to export structural bounds.

## Completion condition

Completed only when both standard/ED2 worksheet capacities and all inline-string cell capacities are enforced before filesystem mutation, focused regression source is present on current `main`, exact SHAs are recorded and the claim is marked `COMPLETED`.
