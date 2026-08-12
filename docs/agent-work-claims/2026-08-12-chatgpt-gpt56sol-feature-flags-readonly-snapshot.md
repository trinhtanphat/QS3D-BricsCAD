# Work claim — feature flags read-only snapshot

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-feature-flags-readonly`
- Registered: `2026-08-12T08:52:00+07:00`
- Baseline main SHA: `6aa10c84eacb064ea6af76c5de32b2e874142296`
- Priority: `Core API integrity discovered during requested continue-all review; FeatureFlags.Snapshot advertises IReadOnlyDictionary while returning a mutable Dictionary instance.`

## Reserved scope

Make `FeatureFlags.Snapshot()` return a genuinely read-only detached snapshot while preserving case-insensitive key lookup and existing Set/IsEnabled semantics. Add focused regression coverage in the already-registered hardening smoke suite.

## Expected surfaces

- `src/QS3D.Core/Features/FeatureFlags.cs`
- `tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs`

## Exact contract

- A snapshot must remain detached from later live FeatureFlags changes.
- A caller that casts the returned object to a mutable dictionary interface must not be able to mutate the published snapshot.
- Snapshot key lookup remains `StringComparer.OrdinalIgnoreCase`-equivalent.

## Excluded scope

- Feature flag persistence or product feature policy
- Set/IsEnabled canonicalization behavior already completed by the prior lookup-normalization lane
- Threading/concurrency semantics beyond what is required for this snapshot mutability defect
- UI, BricsCAD adapter/runtime, licensing
- GitHub Actions

## Validation plan

- Re-read latest main and both target blobs immediately before writes.
- Wrap a detached case-insensitive dictionary copy in a read-only dictionary implementation.
- Extend `HardeningRegressionSmoke` to prove case-insensitive snapshot lookup, detached snapshot semantics, and mutation rejection through `IDictionary<string,bool>`.
- No GitHub Actions dispatch; no licensed BricsCAD runtime PASS claimed.

## Completion condition

Claim must be on main before implementation; source and regression commits must be pushed without overwriting concurrent work; claim closes `COMPLETED` with exact SHAs and evidence.
