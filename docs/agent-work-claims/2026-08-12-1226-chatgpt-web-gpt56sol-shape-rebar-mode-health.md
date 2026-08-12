# Work claim — Shape Rebar generated mode health

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-shape-rebar-mode-health`
- Registered: `2026-08-12T12:26:00+07:00`
- Completed: `2026-08-12T12:30:00+07:00`
- Baseline main SHA: `83b3f93274a60e8de3744cb8ae668ca7de381e5b`
- Priority: P1 — writer-owned Shape Rebar mode metadata must participate in generated-rebar mode health.
- Task Key: `CORE-SHAPE-REBAR-MODE-HEALTH`

## Confirmed defect

`ShapeRebarSolidBuilder.CommitSemanticUpdate(...)` always persists `GeneratedShapeRebarMode = "BBS.ShapePath.SegmentedCylinder"` whenever it persists `GeneratedShapeRebarHandles`. `GeneratedRebarModeHealthService` inspected longitudinal rebar plus slab/wall/foundation mesh modes but did not inspect Shape Rebar at all.

As a result, missing, unsupported, or alias Shape Rebar mode metadata could pass generated-rebar mode diagnostics without any mode-specific evidence.

## Completed implementation

- Claim commit: `5c462b3a79eb58db3c9caba6f9950058ce3d9532`.
- Source commit: `f1d2638149f3169f3b11b4131b674451312cc0cc`.
- Smoke commit: `efbc095ac0f799e1f96d566a8ab26792210185d3`.
- PR #878 squash merge: `52639d13ab883bbdb5c76ffc4d1680c84d298878`.
- Merged source blob read back from `main`: `a9f7d037a8d48e2ea323e41dbff923108347f288`.
- Merged smoke blob read back from `main`: `eeb85d84018c2475173bd1f633ff9b3d0d2c7257`.
- Ancestry verified: merge `52639d13ab883bbdb5c76ffc4d1680c84d298878` is an ancestor of `main@d7a8a823e5915ddc28bbf2148ced40cf3a2abd28`; subsequent commits in that compare did not touch the service or smoke.

## Final contract

- If `GeneratedShapeRebarHandles` exists, missing/blank or unsupported `GeneratedShapeRebarMode` emits `GENERATED_REBAR_MODE_METADATA_INVALID` as Warning.
- A stored Shape mode that normalizes case/outer whitespace to `BBS.ShapePath.SegmentedCylinder` but is not exactly that writer-owned token emits `GENERATED_REBAR_MODE_METADATA_NON_CANONICAL` as Error.
- Exact writer-owned Shape mode preserves existing behavior.
- Elements without Shape Rebar handles remain unaffected.
- Longitudinal and mesh mode semantics remain unchanged.

No GitHub Actions were dispatched. No full local .NET build PASS, executable smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed for this lane.
