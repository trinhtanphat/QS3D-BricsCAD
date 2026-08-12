# Work claim — Foundation Mesh generated-count canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:07:00+07:00`
- Completed: `2026-08-12T11:29:00+07:00`
- Baseline main SHA: `bef7b5a543d3ec799076de3a2510a89329d161c8`
- Claim commit: `474d91b38601a7ea5b87089bf8d034847bab301b`
- Plan commit: `1a3a625b44d76326df04658b44b23b55f35718a8`
- Source commit: `df5f2fba7abfe20a5800d971cdd73ff125043875`
- Regression source commit: `944ea7eefd309fefb5795c37b192213497de6928`
- Priority: P2 — generated metadata health integrity

## Confirmed defect

`GeneratedFoundationMeshHealthService.Inspect(...)` parsed `GeneratedFoundationMeshCount` with `NumberStyles.Integer` and only reported a mismatch when parsing failed, the value was negative, or the numeric value differed from the valid handle count. Writer-owned Foundation Mesh metadata is emitted as `update.Handles.Count.ToString(CultureInfo.InvariantCulture)`, so aliases such as `+2`, `02`, or surrounding whitespace could be accepted as healthy even though production never writes them.

## Implemented contract

1. Canonical non-negative invariant integer count text remains accepted.
2. Invalid/missing/count-mismatch behavior is unchanged.
3. A numerically matching but noncanonical count token now emits `FOUNDATION_MESH_GENERATED_COUNT_NON_CANONICAL` at `HealthSeverity.Warning`.
4. Health inspection does not normalize or rewrite persisted metadata.
5. Handles, ownership, liveness, dimensions, spacing, cover, faces, mode, footprint, category and stale semantics are unchanged.

## Regression coverage

`GeneratedFoundationMeshCountCanonicalitySmoke` is auto-registered and covers:

- canonical `2` with two handles remains free of count-integrity issues;
- `+2`, `02`, and padded ` 2 ` each surface the noncanonical-count issue without being misreported as numeric mismatch;
- canonical count `1` against two handles retains the existing mismatch diagnostic.

## Validation boundary

Exact source diff and regression-source readback were verified on `main`; the regression source was current `main` at verification time. No GitHub Actions, full build, executable smoke, release, or licensed BricsCAD V25/V26 runtime PASS is claimed.
