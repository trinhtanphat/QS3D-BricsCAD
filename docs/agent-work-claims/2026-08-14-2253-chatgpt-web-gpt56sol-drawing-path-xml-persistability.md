# Agent work claim — DrawingPath XML persistability

- Agent: `chatgpt-web-gpt56sol-drawing-path-xml-persistability`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `af791a6b89264fdb0042faecf29888184586d945`
- Claim commit: `cce32ee36a045fc81af43157428e29fd5149016d`
- Implementation branch: `agent/chatgpt-web-gpt56sol/drawing-path-xml-persistability-20260814`
- Source commit: `76403b171cfdad8fb11d8e3228e5945019c919cc`
- Regression commit / implementation head: `2b8b46ba342128af52cf1cb30d62f5dfa6e74200`
- Planned integration branch: `integration/chatgpt-web-gpt56sol-drawing-path-xml-persistability-20260814`
- Priority: Core P1 persistence integrity

## Reserved scope

Fix one confirmed Core public-mutation persistability defect in `ProjectState.DrawingPath`. The current setter delegates raw text directly to the generic persisted-scalar mutation path, so XML-illegal control characters can enter canonical in-memory project state and increment `ChangeVersion`; QSDB later serializes `drawingPath` directly as an XML attribute and rejects that same state during serialized XML validation.

This lane preserves the established exact DrawingPath round-trip contract, including surrounding whitespace. It only rejects control characters before mutation; it does not trim, canonicalize, resolve, normalize, or reinterpret file paths.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectState.cs` — validate raw DrawingPath control characters before delegating to the existing exact-once persisted-scalar revision path.
- `tests/QS3D.Core.SmokeTests/QsdbDrawingIdentityRoundTripSmoke.cs` — retain the existing exact whitespace round-trip fixture and add focused rejection atomicity coverage proving invalid DrawingPath assignment leaves value, `ChangeVersion`, and `UpdatedUtc` unchanged.
- this claim file for coordination/closeout evidence.

## Excluded scope

- DrawingFingerprint semantics (completed separately), ActiveZone/ActiveFloor ids, ProjectId/Name, element identities, generic metadata, measurement mapping metadata, QSDB schema/version/migration, save-path argument policy, native drawing identity capture, BricsCAD adapters, Source Reconcile, UI/DPI work, FieldMerge, release/signing/CI-package lanes, and LOCAL_ONLY runtime qualification.
- No change to DrawingPath whitespace/case/path-separator semantics; the historical exact Save→Load behavior remains authoritative.
- No manual GitHub Actions dispatch/rerun/cancel under `CI_POLICY.md`.

## Evidence before registration

At baseline `af791a6b89264fdb0042faecf29888184586d945`, `ProjectState.DrawingPath` calls `SetPersistedScalar(ref _drawingPath, value)` without text validation. `QsdbProjectStore.Serialize(...)` writes `project.DrawingPath` directly to the root `drawingPath` XML attribute, while `ValidateSerializedXmlText(...)` calls `XmlConvert.VerifyXmlChars(...)`; therefore a value such as `"drawing\u0001path.dwg"` is accepted by the public mutation API but rejected by the canonical persistence boundary. The existing drawing-identity smoke intentionally verifies exact whitespace preservation and does not cover invalid control-character mutation.

The previously completed drawing-fingerprint public-mutation claim explicitly excluded DrawingPath semantics, so this is a separate scalar and does not reopen/duplicate that lane.

## Implementation evidence before integration

- Source commit `76403b171cfdad8fb11d8e3228e5945019c919cc` adds only the pre-mutation control-character guard and continues to pass accepted raw text unchanged to `SetPersistedScalar`.
- Regression commit `2b8b46ba342128af52cf1cb30d62f5dfa6e74200` adds the `U+0001` rejection/atomicity assertions while retaining the existing whitespace-preserving Save→Load fixture.
- Compare from claim commit to implementation head reports exactly two modified files: `ProjectState.cs` and `QsdbDrawingIdentityRoundTripSmoke.cs`.
- Source/test were read back from the branch; the test contains the C# `\u0001` runtime escape. No managed or native test PASS is claimed from the branch because CI remains manual-only.

## Validation plan

- verify this claim is visible on refreshed `main` and re-check new ACTIVE/BLOCKED claims before implementation;
- make the smallest source change: a DrawingPath-specific pre-mutation control-character guard that preserves raw accepted text;
- add deterministic smoke regression for valid exact whitespace round-trip plus invalid assignment failure atomicity;
- inspect implementation diff/readback, reconcile against fresh current `main`, land source/test once through the integration branch with no force push, and observe only automatically triggered CI evidence;
- do not report managed/cloud/native PASS unless actually observed on the relevant source ancestry.

## Completion condition

The focused source/test fix is reachable from current `main` through the required agent/integration flow, regression semantics are preserved, no unrelated source is modified, ancestry/readback and available CI evidence are recorded, and this claim is then closed `COMPLETED` with exact SHAs.
