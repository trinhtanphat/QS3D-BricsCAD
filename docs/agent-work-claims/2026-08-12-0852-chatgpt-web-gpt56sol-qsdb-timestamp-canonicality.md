# Work claim — QSDB timestamp canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-timestamp-canonicality`
- Registered: `2026-08-12T08:52:00+07:00`
- Baseline main SHA: `a9e4bd4bdd4ad6dfdd99eaa88cd3d9953a87d0db`
- Priority: P1 persistence canonicality / deterministic round-trip integrity found during owner-requested `continue all` audit.

## Confirmed defect

`QsdbProjectStore.Serialize(...)` always emits project, element and audit timestamps using UTC `DateTime.ToString("O", InvariantCulture)`. `ProjectSchemaMigrator` also seeds missing legacy timestamps with the same canonical `1970-01-01T00:00:00.0000000Z` representation. However, `QsdbProjectStore.Date(...)` currently accepts any parseable timestamp with an explicit offset and normalizes it through `DateTimeOffset.UtcDateTime`.

As a result, a current-schema `.qsdb` can contain semantically equivalent but noncanonical timestamp tokens such as `+00:00`, lowercase `z`, padded text or alternate fractional precision; `Load()` accepts them and the next save silently rewrites their representation. This violates the repository's strict persisted-token / deterministic-roundtrip policy and makes malformed state indistinguishable from canonical state at load time.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- one focused Core smoke registration/source under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, current store and claim before source writes.
2. Preserve existing explicit-offset parsing and UTC semantics, but after parsing require the original token to equal the exact canonical UTC round-trip string emitted by the serializer.
3. Apply the same helper to root `updatedUtc`, element `updatedUtc` and audit `utc` without changing migration defaults or valid canonical files.
4. Add focused smoke coverage that writes valid QSDB, mutates each timestamp surface to an equivalent `+00:00` token, and verifies `Load()` rejects it while a canonical file still round-trips.
5. Read back source/test on current `main`; no GitHub Actions and no BricsCAD runtime PASS.
6. Close this claim only after source/regression commits are visible on current `main`.

## Excluded

- No schema-version bump or migration rewrite.
- No change-version, numeric, category, map/list or relation canonicality changes.
- No BricsCAD adapter/UI changes.
- No installer/signing/release changes.
