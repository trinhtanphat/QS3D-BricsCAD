# Work claim — Curtain Frame generated handle canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-handle-canonicality`
- Registered: `2026-08-12T10:45:00+07:00`
- Baseline main SHA: `84df2060da5d1eb4b5cd7e4c180146cd3937cc8b`
- Priority: P1 — generated Curtain Frame handle metadata must preserve the writer-owned delimiter/spacing contract.
- Task Key: `CORE-CURTAIN-FRAME-HANDLE-CANONICALITY`

## Confirmed defect

`CurtainWallFrameSolidBuilder` records each generated solid with `solid.Handle.ToString()` and persists `GeneratedCurtainFrameHandles` as `string.Join(";", update.Handles)`. `GeneratedCurtainFrameHealthService` currently splits this metadata and trims every token before validating it, so a persisted alias such as `"A; B"` can pass handle validity without health evidence even though the writer never emits surrounding whitespace.

## Non-overlap check

Recent claim/commit search found no Curtain Frame handle canonicality lane. The active/completed Curtain Frame health-command error-redaction lane only owns the BricsCAD command wrapper and does not modify this Core provider. Curtain Panel, Slab, Foundation and Wall Mesh handle canonicality are separate lanes.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- one focused Core smoke regression for Curtain Frame handle token spacing
- this claim file

Do not modify `CurtainWallFrameSolidBuilder`, health-command wrapper, native ownership/XData, empty-token validity, duplicate/count/source/live-solid semantics, hex-letter casing, fingerprint/geometry/mode logic, persistence format, or BricsCAD runtime code.

## Intended contract

- A non-empty generated Curtain Frame handle token with leading/trailing whitespace emits a dedicated `HealthSeverity.Error` canonicality diagnostic.
- Existing invalid/duplicate/count/source-overlap/live-solid/ownership checks continue to operate on the trimmed token.
- Empty tokens retain `INVALID_CURTAIN_FRAME_GENERATED_HANDLE` precedence without canonicality noise.
- Lower/upper hex spelling remains accepted; this lane only owns writer-proven whitespace/delimiter canonicality.
- Inspection remains read-only and deterministic.

## Completion condition

Padded Curtain Frame handle tokens are fail-visible without changing existing downstream validation semantics, focused smoke coverage pins padded/canonical/empty/duplicate/lowercase behavior, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
