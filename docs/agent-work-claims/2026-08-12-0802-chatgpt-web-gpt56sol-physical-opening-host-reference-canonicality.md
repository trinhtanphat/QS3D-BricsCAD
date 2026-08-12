# Work claim — physical opening host reference canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-physical-opening-host-reference-canonicality-20260812-0802`
- Registered: `2026-08-12T08:02:00+07:00`
- Baseline main SHA: `0d8585b10d8de98b6a54929b6c38a4ff0d9d3ad6`
- Priority: P1 — keep physical opening-cut ownership verification fail-closed on canonical semantic relations.

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` verified each persisted cut target by reading mutable `HostWallId` and comparing `linkedHostId?.Trim()` to the canonical host ID. A padded relation such as `" HOST "` is rejected by QSDB persistence yet was accepted as proof that the opening belonged to the host.

## Implemented fix

- `Resolve(...)` now requires `HostWallId` to be present/non-blank and canonical without leading/trailing whitespace before identity comparison.
- Case-insensitive canonical host IDs remain valid.
- Target-list normalization/encoding/order/size rules, opening-category checks, project ownership checks, and native BricsCAD cutting code remain unchanged.
- Focused smoke verifies lowercase canonical host relation resolves the exact opening instance, while padded and whitespace-only relations fail closed.

## Reserved surfaces

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningHostReferenceCanonicalitySmoke.cs`
- this claim file

## Integration evidence

- Claim registration: `aa99cc9cc25eef0ebf2d6ddd013ea9a67ff55f4b`.
- Branch source commit: `2056a3590bb92e4c6e9aa119b66d56022cc640cb`.
- Branch smoke commit: `9ec3d9e56ca3989e69263953d0ac1bc2d646f9b6`.
- Branch diff was exactly the reserved codec plus new focused smoke (+5/-2 source lines).
- Comparison from claim registration to then-current `main` `103b7bc98455f126bb68b3db816b9c1402dbee22` showed 13 intervening commits and no modification of either reserved path.
- PR `#635` squash-merged cleanly at `eb79fa6d083c7babe6686f840e1acc240f145f53`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.
