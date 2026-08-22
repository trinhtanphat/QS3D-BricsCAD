# Work claim — SE closed-polyline to 3D Solid

- Status: `COMPLETED (SOURCE) / PENDING_LOCAL_NATIVE`
- Agent: `chatgpt-gpt56sol-se-20260813`
- Registered: `2026-08-13T17:47:00+07:00`
- Baseline main SHA: `455759887d9a34ac5f91a7aff3914abc47f2009c`
- Priority: owner-requested continuation of the supplied SE workflow reference: active Family/Type -> `SE` -> select closed 2D polylines -> native 3D Solids.

## Final source contract

`SE` now implements an **all-or-nothing batch** workflow rather than partial-success mutation:

1. observe the existing project and canonical active Family/Type before selection;
2. capture the current/pick-first selection and reject stale drawing/project/Family context;
3. de-duplicate source handles;
4. validate **every** selected source before semantic mutation: unique live handle, `POLYLINE`, closed, XY-parallel planar 2D source, Model Space ownership;
5. capture one whole-project rollback snapshot;
6. semantic-capture the complete source batch against the same active Family;
7. set the complete source batch as implied selection and invoke the existing `StructuralSolidBuilder` exactly once so its CAD transaction owns the full native batch;
8. require the native builder output count to equal the requested source count;
9. on any batch failure, restore the QS3D semantic project snapshot; the builder transaction aborts its uncommitted native output;
10. retain the original source polylines and restore their selection after the attempt.

This source lane deliberately supports only categories with the currently verified closed-footprint extrusion path: Slab, Foundation, Stair, Earthwork, and Column. Categories whose canonical builders are centerline/host/opening based are not coerced into this footprint workflow.

## Integration evidence

- Initial command/source implementation existed on `main` before closeout.
- Atomic whole-batch implementation: `e860b38b171edf284d7b6e457311ef8be6eabcc8`
- Focused atomic regression guard: `2ac289098c73e9873d466349701f1d6264c589d7`

Primary surfaces:

- `src/QS3D.BricsCAD.V25/SeClosedPolylineSolidCommands.cs`
- existing `src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs`
- existing `src/QS3D.BricsCAD.V25/Services/SemanticCaptureActiveFamilyAdapter.cs`
- `scripts/preflight-se-closed-polyline-solid.py`

## Regression contract

The focused preflight locks:

- unique `SE` command registration;
- read-only observation followed by existing-project mutation context;
- project/drawing/active-Family freshness checks;
- whole-selection validation before semantic mutation;
- closed/live/Model Space/XY-plane POLYLINE gates;
- one whole-batch rollback snapshot;
- active-Family semantic capture;
- exactly one `StructuralSolidBuilder.BuildSelected(...)` invocation for the complete batch;
- exact output-count requirement;
- source retention and selection restoration;
- no direct source erase and no automatic project save.

## Coordination / excluded scope

- No replacement native builder or parallel persistence system was introduced.
- No changes to the completed Line/Rectangle/Circle direct-drawing lane.
- No Curtain, Source Reconcile, Quantity, Family Manager, release/versioning, signing, or unrelated UI work is part of this claim.

## Validation boundary

Remote/source completion is closed. Exact native Solid3d shape, transaction behavior inside the licensed BricsCAD host, selection restoration, and visual/semantic result must still be executed with an artifact built from the exact resulting SHA on licensed BricsCAD V25/V26. Those checks remain `PENDING_LOCAL_NATIVE`; this claim does not misreport them as remote PASS.
