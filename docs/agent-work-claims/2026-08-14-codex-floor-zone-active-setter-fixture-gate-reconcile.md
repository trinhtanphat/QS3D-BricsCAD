# Work claim — Floor/Zone active-setter fixture and gate reconciliation

- Status: `COMPLETED`
- Agent: `/root/fix_curtain_method_gates`
- Registered: `2026-08-14T15:29:00+07:00`
- Baseline main SHA: `f16f97490bb5d18da676d92770d9b80df41dcec4`
- Priority: current-main Core smoke/static-gate drift after active-context persistability integration

## Ownership and diagnosis

`/root` explicitly delegated this bounded follow-up after PR `#1207`. The prior Floor/Zone canonical-reference and mutation-preflight claims are `COMPLETED`, there is no open PR for these exact artifacts, and the active LOCAL-003 claim reserves the distinct `ProjectFloorZoneMutationIntegritySmoke.cs` fixture rather than either artifact below.

Commit `0a06a9e61929d80866bd7f48021a46f0b1dde7fb` intentionally routed `ActiveFloorId` and `ActiveZoneId` through `SetActiveContextId`, which trims padding while preserving case before persisted-scalar mutation. `ProjectFloorZoneCanonicalReferenceSmoke` still correctly proves case-insensitive active-delete refusal, but its final assertions that padded raw values survive the setters are unreachable. The mutation-integrity preflight likewise still requires the superseded direct `SetPersistedScalar` property-setter tokens.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectFloorZoneCanonicalReferenceSmoke.cs`: update only the two active-id storage assertions to the trimmed, case-preserving values while retaining active-delete refusal.
- `scripts/preflight-project-floor-zone-mutation-integrity.py`: require the active-context helper route and its trim, control-character rejection, and persisted-scalar delegation contract instead of obsolete direct property delegation.
- this claim document for closeout only.

## Preserved coverage and exclusions

- Preserve Floor/Zone relation canonicality, active-delete protection, missing/invalid, ambiguity, and case-sensitive non-canonical health coverage.
- Production `ProjectState`, Floor/Zone services/setters, Level behavior, P10, #1005, #77, native behavior, private data, release/signing, and GitHub Actions remain untouched.
- Validate the Core `Release` build, the focused mutation-integrity and canonical-reference preflights, and the full Core smoke iteratively; report the next independent blocker without expanding this claim.

Completion means the bounded test/gate-only reconciliation is merged through a normal PR, this claim is closed, and exact merged-main evidence is returned to `/root`.

## Outcome

- Merged test/gate-only reconciliation: PR `#1214`, main SHA `0e0c8dbad6e0adea3681ad6345308d7b0dc96531`.
- Core `Release` build passed with 0 warnings and 0 errors.
- The mutation-integrity and canonical-reference preflights passed, as did the Floor/Zone name, editor atomicity, assign no-op audit, and active no-op audit gates.
- Full Core smoke reached the independent `ModelHealthElementRelationCanonicalitySmoke.PaddedFamilyFailsVisible` fixture after the relation setter began trimming padded Family IDs; that fixture was not expanded into this claim.
- Active-delete protection and all missing/invalid, ambiguity, and case-sensitive non-canonical health coverage remain intact. Production, Level, P10, #1005, #77, native, private-data, and Actions surfaces were untouched.
