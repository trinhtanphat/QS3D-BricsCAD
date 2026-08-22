# Work claim — Wall-junction fingerprint signed-zero canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:23:00+07:00`
- Completed: `2026-08-12T09:29:00+07:00`
- Baseline main SHA: `44b1fec832766d04530c235b7ba7185d9c111477`
- Priority: evidence-driven remote-safe generated ownership fingerprint integrity

## Reason

`WallJunctionOwnershipPlanner.BuildFingerprint` serialized junction coordinates and owner/profile elevations with `double.ToString("R", InvariantCulture)`. IEEE-754 `+0.0` and `-0.0` compare equal throughout physical matching/ordering but can retain different textual representations, so semantically identical physical junction inputs could produce different `WJF1:` rebuild fingerprints solely from the sign bit of zero.

## Changed scope

Wall-junction fingerprint numeric serialization now canonicalizes every zero value to positive zero before round-trip formatting. All non-zero values, group/owner identity (`WJP1`/`WJX1`), occurrence assignment, geometric tolerances, physical validation, packed-key layout and SHA-256 format remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Geometry/WallJunctionOwnershipPlanner.cs`
- `tests/QS3D.Core.SmokeTests/WallJunctionOwnershipSignedZeroSmoke.cs`
- this claim file

## Completion

- Claim commit: `52cc7ca78284aa7f0fa6b33af175caff7b9bb26a`.
- Implementation commit: `152cae3c5e2989b8116da35e3f94065801380fe9` — route all `WJF1` numeric fingerprint fields through signed-zero canonical round-trip formatting.
- Regression commit: `d030e1655075b4055cdf46fc451f27b9c58b5a7a` — prove `+0/-0` junction/profile inputs retain identical group, owner and fingerprint tokens while a real non-zero point movement still changes only the rebuild fingerprint.
- Validation actually performed:
  - fetched the implementation commit diff and confirmed only seven fingerprint call-sites plus the `CanonicalDouble` helper changed;
  - re-fetched current ownership source and confirmed the helper/call-sites remain present;
  - re-fetched the dedicated smoke source and checked signed-zero equivalence plus non-zero sensitivity coverage;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25/V26 runtime PASS is claimed.

## Coordination

No newer wall-junction fingerprint claim was present when this lane was reserved. Concurrent signed-zero work was limited to separate Floor and Curtain fingerprint files.

## Completion condition

Satisfied: current `main` no longer changes `WJF1` solely because a semantically zero coordinate/elevation carries the IEEE-754 negative-zero sign bit, focused regression coverage is present, and this claim is released as `COMPLETED`.
