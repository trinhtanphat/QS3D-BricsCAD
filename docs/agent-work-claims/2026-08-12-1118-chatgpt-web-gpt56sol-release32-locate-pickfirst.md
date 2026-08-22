# Work claim — release #32 QS3DLOCATE PICKFIRST preservation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release32-locate-pickfirst`
- Registered: `2026-08-12T11:18:00+07:00`
- Completed: `2026-08-12T11:22:00+07:00`
- Baseline main SHA: `591e1a8916cc79e61c882371b4b3415b5449a214`
- Claim commit: `f8493b172acf8ab7e7fa70495c3a3d5528ed94f2`
- Scope-expansion commit: `6c0240503d8f24572524c579c3d57a20a308be70`
- Priority: release #32 `scripts/preflight-locate-selection.py` failure plus confirmed zero-match PICKFIRST contradiction.

## Confirmed defect

`QS3DLOCATE` reported that a zero-match Locate kept the current selection, but normal `CadHandleService.Select(...)` always called `SetImpliedSelection(...)`, including with an empty resolved id set. A zero-match Locate therefore cleared PICKFIRST before claiming it was preserved. Quantity validation-failure guards meanwhile intentionally needed a real pre-clear, overloading the same normal selection API with incompatible semantics.

## Completed implementation

- `0ac5d4b7e0d6b6ed865c7f9e595d1ff57946f08c` — normal `CadHandleService.Select` now delegates to `SelectIfAny`, preserving PICKFIRST on zero live handles; added explicit `ClearSelection(Document)` which directly sets an empty implied selection.
- `c807ae00e444fb9383229077ecfa6700e20da673` — Quantity Summary validation-failure pre-clear migrated to `ClearSelection` after its existing active-DWG/trigger guards.
- `3df6fd6bb2265a5b48798e0c87d95c2b387f3023` — Quantity Insight validation-failure pre-clear migrated to `ClearSelection` after its existing active-DWG/trigger guards.
- `408740551d5dcbaf213da7225ae196b39c700911` — quantity validation-failure preflight now requires the explicit clear API, pins its direct empty `SetImpliedSelection`, rejects re-use of normal `Select(empty)` in those guards, and preserves all existing Follow3D/wrong-DWG/locate/zoom checks.
- `scripts/preflight-locate-selection.py` required no source edit after the production API fix: its existing `Select => SelectIfAny`, zero-before-SetImpliedSelection and QS3DLOCATE positive-zoom/zero-feedback assertions now match the corrected implementation.

## Resulting contract

- Normal Locate/select operations do not destroy current PICKFIRST when no live target resolves.
- Positive live selection still sets implied selection and zoom remains positive-count-only.
- Explicit validation-failure pre-clear remains available only through `ClearSelection` and stays guarded to the same active DWG.
- Source/boundary/generated owner fallback and existing resolver smoke coverage are unchanged.

## Readback evidence

Current `main` readback confirms `Select => SelectIfAny`, the zero-count return before `SetImpliedSelection`, explicit `ClearSelection(Array.Empty<ObjectId>())`, both Quantity guards calling `ClearSelection`, and the updated quantity preflight pinning those semantics.

## Validation boundary

Remote/static source and regression-gate verification only. No GitHub Actions were dispatched. No executable .NET smoke/build, signing/package/release, or licensed BricsCAD V25/V26 runtime PASS is claimed.