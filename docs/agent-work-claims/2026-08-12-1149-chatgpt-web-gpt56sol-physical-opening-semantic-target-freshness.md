# Work claim — Physical opening semantic target freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-physical-opening-semantic-target-freshness`
- Registered: `2026-08-12T11:49:00+07:00`
- Baseline main SHA: `9a1b6d914dff68c3546743cf4205f29c8ea14491`
- Priority: P2 — fail closed when caller-controlled lazy physical-opening target enumeration changes the project semantic version.

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` validates the project and canonical host, then enumerates caller-controlled `openingIds` through `Normalize(...)`. After enumeration it revalidates global element identity and host instance freshness, but does not capture/re-check `ProjectState.ChangeVersion`. A lazy enumerable can call `project.Touch()` while preserving the same host and element instances, and resolution then continues across a semantic-version boundary.

## Reserved scope

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`, limited to semantic-version freshness around target enumeration in `Resolve(...)`
- focused Core smoke regression + ModuleInitializer registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-physical-opening-semantic-target-freshness.md`
- this claim file

## Intended contract

- Capture `project.ChangeVersion` immediately before caller-controlled `Normalize(openingIds)`.
- Fail immediately after enumeration when the version changed, before empty-target handling or relation resolution.
- Preserve the existing global element integrity and canonical-host structural freshness checks from the completed structural lane `c40f3bf91fa107a1244612c1e0dc053b222b727d`.
- A mutating empty lazy target sequence must fail closed on semantic freshness.
- Stable target resolution remains unchanged.

## Excluded scope

- Existing physical opening structural/global identity/canonical relation lanes.
- Physical boolean execution, native geometry, and persisted target-state encoding.
- GitHub Actions or licensed BricsCAD runtime qualification.
