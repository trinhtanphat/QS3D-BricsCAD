# Work claim — Curtain Frame integer snapshot canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-integer-snapshot-canonicality`
- Registered: `2026-08-12T11:10:00+07:00`
- Baseline main SHA: `ad62c1648569c5ae792378bdaefc7325b3778f8e`
- Priority: P1 — generated Curtain Frame integer snapshots must preserve exact writer-owned invariant decimal spelling.
- Task Key: `CORE-CURTAIN-FRAME-INTEGER-SNAPSHOT-CANONICALITY`

## Confirmed defect

The line/path Curtain Frame writers persist generated integer metadata with `int.ToString(CultureInfo.InvariantCulture)`, including `GeneratedCurtainFrameCount`, `GeneratedCurtainFrameBaseCount`, `GeneratedCurtainFrameOpeningCount`, `GeneratedCurtainFrameColumns`, `GeneratedCurtainFrameRows`, and for path mode `GeneratedCurtainFramePathSegmentCount` / `GeneratedCurtainFrameMappedFrameCount`. `GeneratedCurtainFrameHealthService` previously accepted these values through `int.TryParse(...)` only, allowing alternate spellings such as padded, signed-positive or leading-zero text to pass health even though the writers never emit them.

## Completed implementation

- Claim commit: `e03d2a1777b6d2ff3b9c974acb96a40db4223ba9`.
- Branch source commit: `881d71881e9fdbec84f01ffc37954f06de2fe4f0`.
- Branch smoke commit: `ac9b9bab4b6e464b6d3ba7e58a5a61221e811729`.
- PR: `#802` (`chatgpt-curtain-frame-integer-canon-20260812`).
- Squash merge on `main`: `e9aaab613fc57dad6655730a677dbef181498c12`.
- Merged source and `GeneratedCurtainFrameIntegerSnapshotCanonicalitySmoke.cs` were read back from `main`.
- Ancestry was verified from squash merge `e9aaab613fc57dad6655730a677dbef181498c12` to `main` snapshot `0fda3dfc5da53cdb7be739dbd6900faef21d7b74`; the intervening commits did not touch this lane.

## Resulting contract

- After generated integer snapshots parse and satisfy their existing positive/nonnegative domain rules, their raw text must equal `value.ToString(CultureInfo.InvariantCulture)` or emit `CURTAIN_FRAME_INTEGER_METADATA_NON_CANONICAL` as `HealthSeverity.Error`.
- Existing missing/invalid/range warnings retain precedence and invalid values do not receive canonicality noise.
- Existing count/grid/opening/path mismatch calculations continue to use parsed integer values.
- Exact writer-owned decimal strings preserve existing behavior.
- Inspection remains read-only and deterministic.

## Verification boundary

No GitHub Actions were dispatched. No full local .NET build PASS and no BricsCAD V25/V26 runtime PASS are claimed by this lane.
