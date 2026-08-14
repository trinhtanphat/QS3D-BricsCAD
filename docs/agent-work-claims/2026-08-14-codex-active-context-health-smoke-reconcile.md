# Work claim — Active Floor/Zone health smoke reconciliation

- Status: `COMPLETED`
- Agent: `/root/fix_curtain_method_gates`
- Registered: `2026-08-14T15:20:10+07:00`
- Baseline main SHA: `a9d7c4438b5bee468261006989ec12cc30199e5e`
- Priority: current-main Core smoke blocker after active-context persistability integration

## Verified stale fixture

`ModelHealthActiveFloorZoneCanonicalitySmoke.PaddedActiveFloorFailsVisible` assigns `" Floor-A "` and expects `ACTIVE_FLOOR_NON_CANONICAL`. The completed `0a06a9e61929d80866bd7f48021a46f0b1dde7fb` contract now trims supported `ActiveFloorId`/`ActiveZoneId` setter inputs before storage, so that assignment becomes canonical `"Floor-A"` and the diagnostic is correctly absent.

The fixture's case-variant Zone input remains non-canonical because setters do not rewrite casing. Missing IDs still exercise `INVALID_ACTIVE_FLOOR`/`INVALID_ACTIVE_ZONE`, and duplicate targets still exercise both ambiguity diagnostics.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ModelHealthActiveFloorZoneCanonicalitySmoke.cs`: replace only the unreachable padded-setter expectation with an assertion that the stored ID is trimmed and canonicality health remains clean.
- this claim and the completed active-context persistability claim for ownership/closeout only.

## Validation and exclusions

- Core `Release` build and full Core smoke executable.
- relevant health/active-context preflights and aggregate preflight when independent current-main gates allow.
- installed-reference V25 `Release|x64` build if the concurrently active Family Manager XAML lane has restored its handlers; otherwise record that independent blocker.
- No production/Core source edit; no P10, #1005, #77, LOCAL automation, native behavior, private data, release/signing or GitHub Actions.

Completion means the test-only reconciliation is merged through a normal PR, this claim is closed, and an exact merged-main SHA is returned to `/root`.

## Outcome

- Merged test-only reconciliation: PR `#1207`, main SHA `8cd9cc5c08c3fc040f7a52c5c07b3b63417eefc7`.
- Core `Release` build passed with 0 warnings and 0 errors.
- The full Core smoke advanced beyond this reconciled fixture. On the final merged-base validation it stopped at the independent `ModelHealthElementRelationCanonicalitySmoke.PaddedFamilyFailsVisible` stale expectation introduced by concurrent relation-setter normalization work.
- Relevant Floor/Zone name, editor atomicity, assign no-op audit, active no-op audit, and canonical-reference preflights passed. The mutation-integrity preflight remained independently stale against the `0a06` canonicalizing setters and was not changed under this exact claim.
- Missing/invalid, ambiguous, and case-sensitive non-canonical Active Floor/Zone health coverage remains intact. No production, P10, #1005, #77, native, private-data, or Actions changes were made.
