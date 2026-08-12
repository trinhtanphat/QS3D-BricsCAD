# Work claim — ProjectState persisted scalar versioning

- Status: `RELEASED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T14:36:00+07:00`
- Released: `2026-08-12T14:37:00+07:00`
- Baseline main SHA: `553cc7e411b39f413c380fa123b6c0f4f6940dc1`
- Priority: continue-all persistence false-clean correctness

## Reserved scope

Fix `ProjectState` persisted scalar mutation freshness so changes to `DrawingPath`, `DrawingFingerprint`, `ActiveZoneId`, and `ActiveFloorId` advance persistence state and cannot remain false-clean after a save stamp.

## Planned implementation

- Replace the four persisted scalar auto-properties with equality-guarded setters that advance `ChangeVersion`/`UpdatedUtc` exactly once on a real value change.
- Preserve current exact string value semantics; do not add trimming, casing normalization, Floor/Zone validation, or schema/serialization changes.
- Preserve snapshot hydration semantics: `ProjectStateSnapshot.CopyInto()` restores the persisted timestamp/version after assigning snapshot values.
- Add deterministic Core smoke coverage for all four persisted scalars plus same-value no-op behavior and snapshot restore cleanliness.

## Reserved files

- `src/QS3D.Core/Domain/ProjectState.cs`
- Existing Core smoke-test persistence/project-state regression file(s) needed for the deterministic regression.
- This claim file.

## Excluded scope

- No changes to `ProjectState.Touch()` contract.
- No QSDB schema/XML/serialization format changes.
- No Floor/Zone canonicalization or validation changes.
- No GitHub Actions dispatch/rerun and no BricsCAD runtime changes.

## Owner-coordinated handoff

Repository owner `trinhtanphat` explicitly requested that the remaining work be finished. At the handoff point, current `main` still had the four persisted scalar auto-properties and no implementation/regression commit had landed under this reservation. The scope is released to successor claim `docs/agent-work-claims/2026-08-12-1437-chatgpt-web-gpt56sol-project-state-scalar-versioning-takeover.md`, which was published first as `b51a0c062c7e46a1870ce723260777fe9033cba7`.

No source/test changes are attributed to this released claim.
