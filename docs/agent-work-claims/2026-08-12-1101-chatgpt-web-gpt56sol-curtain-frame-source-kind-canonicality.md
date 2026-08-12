# Work claim — Curtain Frame source-kind canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-source-kind-canonicality`
- Registered: `2026-08-12T11:01:00+07:00`
- Baseline main SHA: `e4515b9ad9c46b4e1f4e325028db9809eb2ef645`
- Priority: P1 — generated path Curtain Frame source-kind metadata must preserve the exact writer-owned token.
- Task Key: `CORE-CURTAIN-FRAME-SOURCE-KIND-CANONICALITY`

## Confirmed defect

`CurtainWallPathFrameSolidBuilder.CommitSemanticUpdate(...)` always persists `GeneratedCurtainFrameSourceKind = "OpenPolyline"`. `GeneratedCurtainFrameHealthService` previously trimmed the stored source-kind value and compared it case-insensitively while validating path modes, allowing padded/case-varied aliases to pass path-source health even though the writer never emits those spellings.

## Completed implementation

- Claim commit: `91c537f918e6d3d6cbd748ed53d09ae73f9bd6e2`.
- Branch source commit: `6b1eea06991aca84d6383bc0db29eb6be284df64`.
- Branch smoke commit: `94986eec51fb2294b4767a6a10b2a2f3778a2d49`.
- PR: `#792` (`chatgpt-curtain-frame-source-kind-canon-20260812`).
- Squash merge on `main`: `99e28cf15909948f06a95255229b5b1d814da60c`.
- Merged source and `GeneratedCurtainFrameSourceKindCanonicalitySmoke.cs` were read back from `main`.
- Ancestry was verified from squash merge `99e28cf15909948f06a95255229b5b1d814da60c` to `main` snapshot `8162f77e16d4aed27281738a972fac9ee023848b`; the intervening commit did not touch this lane.

## Resulting contract

- In path mode, case-varied or outer-whitespace aliases of `OpenPolyline` emit `CURTAIN_FRAME_PATH_SOURCE_KIND_NON_CANONICAL` as `HealthSeverity.Error`.
- Existing `CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID` remains the diagnostic for missing or genuinely unsupported normalized values.
- Line modes remain unaffected by path source-kind metadata.
- Exact writer-owned `OpenPolyline` preserves existing behavior.
- Inspection remains read-only and deterministic.

## Verification boundary

No GitHub Actions were dispatched. No full local .NET build PASS and no BricsCAD V25/V26 runtime PASS are claimed by this lane.
