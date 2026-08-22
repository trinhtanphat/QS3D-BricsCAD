# Work claim — Curtain Frame source-kind canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-source-kind-canonicality`
- Registered: `2026-08-12T11:01:00+07:00`
- Baseline main SHA: `e4515b9ad9c46b4e1f4e325028db9809eb2ef645`
- Priority: P1 — generated path Curtain Frame source-kind metadata must preserve the exact writer-owned token.
- Task Key: `CORE-CURTAIN-FRAME-SOURCE-KIND-CANONICALITY`

## Confirmed defect

`CurtainWallPathFrameSolidBuilder.CommitSemanticUpdate(...)` always persists `GeneratedCurtainFrameSourceKind = "OpenPolyline"`. `GeneratedCurtainFrameHealthService` currently trims the stored source-kind value and compares it case-insensitively while validating path modes. Persisted aliases such as `" openpolyline "` can therefore pass path-source health even though the writer never emits those spellings.

## Non-overlap check

Recent claim/commit search found no Curtain Frame source-kind canonicality lane. Completed Curtain Frame handle and mode canonicality lanes own different metadata. Other active Curtain/Reporting/Revision/XLSX lanes do not reserve this source-kind validation.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- one focused Core smoke regression for source-kind canonicality
- this claim file

Do not modify Curtain Frame builders, handles, mode/opening semantics, fingerprint/geometry/count metadata, native ownership/XData, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- In path mode, a stored source-kind that normalizes case/outer whitespace to `OpenPolyline` but is not exactly `OpenPolyline` emits `CURTAIN_FRAME_PATH_SOURCE_KIND_NON_CANONICAL` as `HealthSeverity.Error`.
- Existing `CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID` remains the diagnostic for missing or genuinely unsupported normalized values.
- Line modes remain unaffected by source-kind metadata.
- Exact writer-owned `OpenPolyline` preserves existing behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Case-varied/padded path source-kind aliases are fail-visible without changing invalid or line-mode semantics, focused smoke coverage pins aliases plus invalid/canonical/line controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
