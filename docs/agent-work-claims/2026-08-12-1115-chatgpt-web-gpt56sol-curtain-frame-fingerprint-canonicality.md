# Work claim — Curtain Frame config fingerprint canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-fingerprint-canonicality`
- Registered: `2026-08-12T11:15:00+07:00`
- Baseline main SHA: `41ec60f899c8aff4f73b9896299050c5579399a5`
- Priority: P1 — generated Curtain Frame config fingerprints must preserve the exact writer-owned SHA-256 spelling.
- Task Key: `CORE-CURTAIN-FRAME-FINGERPRINT-CANONICALITY`

## Confirmed defect

`CurtainWallFrameFingerprint.Compute(...)` returns SHA-256 as exactly 64 lowercase hex characters via `value.ToString("x2", CultureInfo.InvariantCulture)`. Both line and path Curtain Frame builders persist that exact returned string unchanged into `GeneratedCurtainFrameConfigFingerprint`. `GeneratedCurtainFrameHealthService.ValidateConfigFingerprint(...)` previously compared the recomputed fingerprint against `storedFingerprint.Trim()` with `StringComparison.OrdinalIgnoreCase`, allowing padded or uppercase aliases to pass stale/config health even though no writer emits those spellings.

## Completed implementation

- Claim commit: `c7f49481e2ab44278ea6b2ccebb2bc1ebb9d9461`.
- Branch source commit: `131cd969ce8bafb46d5e70aa0f9abfa23115d421`.
- Branch smoke commit: `43792568ac73d90cb8df598b299dfe17402eb4e1`.
- PR: `#809` (`chatgpt-curtain-frame-fingerprint-canon-20260812`).
- Squash merge on `main`: `8ed5b6d6679d8e92cedf1d626787d2f0431549f1`.
- Merged source and `GeneratedCurtainFrameFingerprintCanonicalitySmoke.cs` were read back from `main`.
- Ancestry was verified from squash merge `8ed5b6d6679d8e92cedf1d626787d2f0431549f1` to `main` snapshot `51d03fbd6ec244ead5895adeddcbb42a506aa06a`; intervening commits did not touch this lane.

## Resulting contract

- If a stored non-empty fingerprint is semantically equal to the recomputed fingerprint under trim/case-folding but not exactly the writer-owned lowercase digest, health emits `CURTAIN_FRAME_CONFIG_FINGERPRINT_NON_CANONICAL` as `HealthSeverity.Error`.
- Existing `CURTAIN_FRAME_CONFIG_STALE` remains the diagnostic when the normalized stored fingerprint differs from the recomputed value.
- Missing fingerprint retains `CURTAIN_FRAME_CONFIG_FINGERPRINT_MISSING` precedence.
- Existing config-invalid handling remains unchanged.
- Exact writer-owned lowercase fingerprint preserves existing behavior.

## Verification boundary

No GitHub Actions were dispatched. No full local .NET build PASS and no BricsCAD V25/V26 runtime PASS are claimed by this lane.
