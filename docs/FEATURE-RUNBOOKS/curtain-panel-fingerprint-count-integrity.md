# Curtain panel fingerprint indexed Count integrity

## Scope

`CurtainWallPanelFingerprint.Compute` accepts caller-provided `IReadOnlyList<CurtainWallPanelPiece>` input. The admitted `Pieces.Count` is integrity evidence for the canonical digest and must remain stable across each caller-controlled indexed read.

## Contract

- Read and validate the initial `Pieces.Count` against the negative and `MaxPieces` bounds.
- Before each `inputPieces[index]` read, revalidate Count against the admitted value.
- Read each source index exactly once.
- Immediately after each indexed read, revalidate Count again before the returned mutable DTO is snapshotted/accepted.
- Preserve the existing final Count rebound, detached scalar snapshot, canonical ordering and SHA-256 digest semantics.
- Fail closed if Count temporarily grows/shrinks between index reads even if it later returns to the original value.

## Deterministic regression

`CurtainWallPanelFingerprintCountSmoke` supplies an admitted two-piece `IReadOnlyList` whose index 0 getter changes Count from 2 to 3 and whose index 1 getter would restore it to 2. The old end-of-loop-only check could miss that transient drift. The strengthened contract rejects immediately after index 0, before index 1 is read or the returned piece is semantically accepted. A stable counted control verifies canonical fingerprint parity and exactly one source indexer read per admitted piece.

## Validation

```text
python scripts/preflight-curtain-panel-fingerprint-count-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Runtime classification: **NOT_APPLICABLE**. This is deterministic Core identity/integrity validation and does not claim licensed BricsCAD runtime evidence.
