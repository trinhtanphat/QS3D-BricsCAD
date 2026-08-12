# Work claim — QSDB timestamp canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-timestamp-canonicality`
- Registered: `2026-08-12T08:52:00+07:00`
- Baseline main SHA: `a9e4bd4bdd4ad6dfdd99eaa88cd3d9953a87d0db`
- Regression commit: `b789a8d4127470f0941c4d70163ddb187128557c`
- Completed source commit: `da64ef09c8b3f90c23bf38933e74952ef3d76f97`
- Readback main SHA before close-out: `f9f6332bde8bc958cdeda748b586a59b15ae8b5e`
- Priority: P1 persistence canonicality / deterministic round-trip integrity found during owner-requested `continue all` audit.

## Confirmed defect

`QsdbProjectStore.Serialize(...)` always emits project, element and audit timestamps using UTC `DateTime.ToString("O", InvariantCulture)`. `ProjectSchemaMigrator` also seeds missing legacy timestamps with the same canonical `1970-01-01T00:00:00.0000000Z` representation. However, `QsdbProjectStore.Date(...)` accepted any parseable timestamp with an explicit offset and normalized it through `DateTimeOffset.UtcDateTime`.

A current-schema `.qsdb` could therefore contain semantically equivalent but noncanonical timestamp tokens such as `+00:00`, lowercase `z`, padded text or alternate fractional precision; `Load()` accepted them and the next save silently rewrote their representation.

## Implemented contract

1. Existing explicit-offset parsing remains the semantic timestamp validity check.
2. After parsing, `Date()` derives `result.UtcDateTime` and the exact canonical UTC round-trip token with `ToString("O", InvariantCulture)`.
3. The original persisted value must exactly equal that canonical token using `StringComparison.Ordinal`; otherwise `Load()` fails closed with `InvalidDataException`.
4. The same `Date()` helper covers root `updatedUtc`, element `updatedUtc` and audit `utc`.
5. Legacy migration defaults remain unchanged and canonical valid QSDB files continue to round-trip.
6. Focused module-initializer smoke coverage creates a valid QSDB, verifies the canonical UTC round-trip, then independently rewrites root/element/audit timestamps to equivalent `+00:00` tokens and requires `Load()` to reject each.

## Verification

- Current-main source readback confirmed the parse -> UTC -> canonical `"O"` -> exact-original comparison in `Date()`.
- Current-main smoke readback confirmed coverage of root, element and audit timestamp surfaces plus canonical round-trip preservation.
- `da64ef09c8b3f90c23bf38933e74952ef3d76f97...main` compared as `ahead` with the source commit as merge base; subsequent concurrent changes touched unrelated Zone/claim files.
- The smoke source is committed but was not executed from this remote connector session. Full Core smoke execution, build and GitHub Actions were not run; no PASS is fabricated.
- This is Core persistence work and makes no licensed BricsCAD runtime claim.

## Excluded

- No schema-version bump or migration rewrite.
- No change-version, numeric, category, map/list or relation canonicality changes.
- No BricsCAD adapter/UI changes.
- No installer/signing/release changes.
