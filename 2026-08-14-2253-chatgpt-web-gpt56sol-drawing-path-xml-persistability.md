# Agent work claim — DrawingPath XML persistability

- Agent: `chatgpt-web-gpt56sol-drawing-path-xml-persistability`
- Date: 2026-08-14
- Status: `COMPLETED`
- Baseline main SHA: `af791a6b89264fdb0042faecf29888184586d945`
- Claim commit: `cce32ee36a045fc81af43157428e29fd5149016d`
- Implementation branch: `agent/chatgpt-web-gpt56sol/drawing-path-xml-persistability-20260814`
- Source commit: `76403b171cfdad8fb11d8e3228e5945019c919cc`
- Regression commit / implementation head: `2b8b46ba342128af52cf1cb30d62f5dfa6e74200`
- Integration branch: `integration/chatgpt-web-gpt56sol-drawing-path-xml-persistability-20260814`
- Initial integration candidate: `a692abf354f7f287bad5ac74fe9ddf59f88af579`
- Reconciled integration / final source landing: `6fe3ca39b9974341d9b4414acd24410b4b44456f`
- Priority: Core P1 persistence integrity

## Reserved scope

Fixed one confirmed Core public-mutation persistability defect in `ProjectState.DrawingPath`. The previous setter delegated raw text directly to the generic persisted-scalar mutation path, so XML-illegal control characters could enter canonical in-memory project state and increment `ChangeVersion`; QSDB later serialized `drawingPath` directly as an XML attribute and rejected that same state during serialized XML validation.

This lane preserves the established exact DrawingPath round-trip contract, including surrounding whitespace. It only rejects control characters before mutation; it does not trim, canonicalize, resolve, normalize, or reinterpret file paths.

## Changed surfaces

- `src/QS3D.Core/Domain/ProjectState.cs` — `DrawingPath` now rejects control characters before delegating accepted raw text unchanged to the existing persisted-scalar revision path.
- `tests/QS3D.Core.SmokeTests/QsdbDrawingIdentityRoundTripSmoke.cs` — retains exact whitespace-preserving Save→Load coverage and adds `U+0001` rejection atomicity assertions for value, `ChangeVersion`, and `UpdatedUtc`.

## Excluded scope preserved

- DrawingFingerprint semantics (completed separately), ActiveZone/ActiveFloor ids, ProjectId/Name, element identities, generic metadata, measurement mapping metadata, QSDB schema/version/migration, save-path argument policy, native drawing identity capture, BricsCAD adapters, Source Reconcile, UI/DPI work, FieldMerge, release/signing/CI-package lanes, and LOCAL_ONLY runtime qualification.
- DrawingPath whitespace/case/path-separator semantics were not changed.
- No manual GitHub Actions dispatch/rerun/cancel was performed.

## Evidence and integration

- At baseline `af791a6b89264fdb0042faecf29888184586d945`, `ProjectState.DrawingPath` called `SetPersistedScalar(ref _drawingPath, value)` without text validation. `QsdbProjectStore.Serialize(...)` writes `project.DrawingPath` directly to the root `drawingPath` XML attribute, while `ValidateSerializedXmlText(...)` verifies XML characters; therefore the public API admitted state the persistence boundary rejected.
- The previously completed drawing-fingerprint public-mutation claim explicitly excluded DrawingPath semantics, so this lane did not reopen/duplicate that scope.
- Claim-only coordination landed at `cce32ee36a045fc81af43157428e29fd5149016d` before source work and was read back as current `main`.
- Source commit `76403b171cfdad8fb11d8e3228e5945019c919cc` and regression head `2b8b46ba342128af52cf1cb30d62f5dfa6e74200` were read back; compare from claim commit reports exactly two modified reserved files.
- The regression contains the C# `\u0001` runtime escape and proves rejected assignment leaves the accepted drawing path, project `ChangeVersion`, and `UpdatedUtc` unchanged while the original whitespace-preserving Save→Load fixture remains intact.
- Initial integration candidate `a692abf354f7f287bad5ac74fe9ddf59f88af579` was built from refreshed `main`. During freeze, `main` advanced through concurrent docs/claim commits only; compare showed no overlap with the two reserved files.
- Reconciliation commit `6fe3ca39b9974341d9b4414acd24410b4b44456f` used the refreshed `main` as primary parent and the prior integration candidate as additional parent, preserving concurrent FieldMerge/family-category claim work. Final `main` update succeeded with `force:false`, and immediate readback showed `main` exactly at `6fe3ca39b9974341d9b4414acd24410b4b44456f`.
- No agent-branch managed test PASS is claimed because Actions are manual-only by default. The standing automatic post-integration dispatcher created run `31816672755` (`Dispatch V25 cloud CI after main integration`) for exact source SHA `6fe3ca39b9974341d9b4414acd24410b4b44456f`; it was `in_progress` at close, so this claim does not report cloud CI PASS.
- Licensed/native BricsCAD V25/V26 NETLOAD acceptance was not executed by this remote lane and remains LOCAL_ONLY.

## Completion

The focused DrawingPath persistence fix and deterministic regression are reachable from `main` at `6fe3ca39b9974341d9b4414acd24410b4b44456f`. Source/integration protocol is complete, concurrent ownership was preserved, no force push or manual CI dispatch was used, and validation limitations are explicit.
