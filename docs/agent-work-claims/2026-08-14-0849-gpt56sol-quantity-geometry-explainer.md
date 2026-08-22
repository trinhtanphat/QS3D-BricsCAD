# Work claim — Geometry-backed quantity explanation

- Status: `ACTIVE`
- Agent: `gpt56sol-quantity-geometry-explainer-20260814-0849`
- Registered: `2026-08-14T08:49:00+07:00`
- Baseline main SHA: `01215685c429e54f657735be7bfc79f526aee53c`
- Priority: User-requested implementation of the attached “Diễn giải khối lượng chi tiết” specification; the existing quantity-detail panel explains report arithmetic but does not yet reserve an exact-geometry intersection/contact lane.

## Reserved scope

Implement the V25 quantity explanation lane for a selected QS3D element: exact 3D solid-intersection deductions after BoundingBox candidate filtering, union-before-total-deduction semantics, per-face formwork gross/deduction/net explanation including face-contact/coverage classification, stable explanation data contracts, panel projection, and click-through 3D locating/highlighting/zoom hooks. Add dirty/stale invalidation integration only where required for these explanation results.

## Expected surfaces

- `src/QS3D.Core/Reporting/QuantityGeometryExplanation*.cs` (new CAD-agnostic contracts/planning helpers as needed)
- `src/QS3D.BricsCAD.V25/Reporting/QuantityGeometryExplanation*.cs` (new exact-host geometry service)
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.*.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.Runtime.cs` and narrowly required quantity-panel wiring
- existing V25 selection/highlight/viewport helpers only if reusable without changing their neighboring contracts; otherwise a quantity-owned helper under the new reporting/UI surface
- focused quantity-geometry regression/probe files that do not collide with an ACTIVE shared smoke-registration claim

## Excluded scope

- General quantity-report Family identity/canonicalization, BQ/Material Usage, estimate/procurement, IFC, curtain/door/room schedules, rebar, QSDB publication, release/version automation, and LOCAL-003 native-startup diagnosis.
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` while the visible Curtain schedule Family-integrity claim reserves that shared file; this lane will use an independently registered/standalone guard first and will only expand after a fresh claim collision check if shared registration becomes necessary.
- Changing model geometry, authoring tools, or global NETLOAD/document lifecycle behavior unrelated to quantity-result invalidation.

## Validation plan

- Core tests/guards for tolerance normalization, individual deduction rows, union-total semantics, net-volume/formwork invariants, contact-vs-volume classification, and dirty-state transitions.
- V25 project compile/preflight where remote-safe; source guard must prove BoundingBox is candidate-only and final volume comes from exact boolean geometry.
- Native BricsCAD V25 probe remains explicit for Solid3d/BRep runtime behavior, row click highlighting/zoom, and contact-area acceptance if cloud CI cannot host BricsCAD.

## Coordination

The earlier quantity detail-explainer claim is completed and its panel shell is treated as an extension point. The recent quantity-report FamilyId claim is completed. Current Curtain schedule Family-integrity work owns its schedule files and `SmokeTestRegistration.cs`; this claim deliberately excludes those surfaces. QSDB stale-backup and LOCAL-003 host lanes are unrelated. Recheck all ACTIVE/BLOCKED claims immediately before every write.

## Completion condition

Geometry-backed explanation contracts/service and quantity-panel integration are pushed on a feature branch with focused regression evidence and a PR; claim is then updated with exact SHAs, CI/preflight evidence, and any remaining BricsCAD-native acceptance gate.