# Work claim — ProjectState persisted scalar versioning owner takeover

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-state-scalar-versioning-owner-takeover-20260812`
- Registered: `2026-08-12T14:37:00+07:00`
- Completed: `2026-08-12T14:53:00+07:00`
- Baseline main SHA: `f2bff143d6dd199b10d38044574d6a296018d314`
- Priority: repository-owner coordinated continuation of the persisted-scalar freshness lane
- Predecessor: `docs/agent-work-claims/2026-08-12-1436-chatgpt-web-gpt56sol-project-state-persisted-scalar-versioning.md`

## Coordination

Repository owner `trinhtanphat` explicitly requested that the remaining work be finished. The predecessor reservation was released to this successor before implementation completion, preserving claim-first ownership.

## Completed contract

- `DrawingPath`, `DrawingFingerprint`, `ActiveZoneId`, and `ActiveFloorId` now use exact ordinal equality; assigning the current value is a no-op.
- A real value change advances `ChangeVersion` exactly once and refreshes `UpdatedUtc`.
- Exact string storage semantics are preserved; no trimming, casing normalization, Floor/Zone validation, schema, XML, or serialization changes were added.
- `ProjectState.Touch()` remains unchanged.
- Snapshot hydration remains clean: `ProjectStateSnapshot.CopyInto()` may invoke the setters while copying values, then restores the captured `UpdatedUtc` and `ChangeVersion` through `RestorePersistenceState(...)`.

## Regression coverage

`tests/QS3D.Core.SmokeTests/ProjectStatePersistedScalarVersioningSmoke.cs` is auto-registered with `[ModuleInitializer]` and covers:

- each of the four persisted scalars advancing `ChangeVersion` exactly once on a real change;
- `UpdatedUtc` refreshing on a real change;
- exact string value preservation;
- same-value assignments preserving both `ChangeVersion` and `UpdatedUtc`;
- four post-capture scalar mutations followed by `ProjectStateSnapshot.Restore(...)` restoring all four captured values plus the captured persistence version/timestamp.

## Landing evidence

- Successor claim registration: `b51a0c062c7e46a1870ce723260777fe9033cba7`
- Source fix: `0c7dbe5612c0db6e5252f3ec1db6385b06771e0e`
- Source blob read back from `main`: `0c9a1dee11d48083605ee8d1e200acebe997ae9a`
- Focused smoke: `a69f93136a88c730834cc494b8d5f2e19bf9de5f`
- Smoke blob read back from `main`: `17c12094477857241c1ed9070ddfea5a09350d67`
- Ancestry check against `a69f93136a88c730834cc494b8d5f2e19bf9de5f`: source fix is the exact merge base; `behind_by=0`.

## Validation boundary

Source and focused auto-registered smoke were read back from `main`. No GitHub Actions were dispatched. No full .NET build, executable smoke process, or licensed BricsCAD V25/V26 runtime was executed or claimed as PASS.
