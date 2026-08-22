# Work claim — Physical opening semantic target freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-physical-opening-semantic-target-freshness`
- Registered: `2026-08-12T11:49:00+07:00`
- Completed: `2026-08-12T11:52:00+07:00`
- Baseline main SHA: `9a1b6d914dff68c3546743cf4205f29c8ea14491`
- Priority: P2 — fail closed when caller-controlled lazy physical-opening target enumeration changes the project semantic version.

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` validated the project and canonical host, then enumerated caller-controlled `openingIds` through `Normalize(...)`. After enumeration it revalidated global element identity and host instance freshness, but did not capture/re-check `ProjectState.ChangeVersion`. A lazy enumerable could call `project.Touch()` while preserving the same host and element instances, and resolution then continued across a semantic-version boundary.

## Delivered contract

- Capture `project.ChangeVersion` immediately before caller-controlled `Normalize(openingIds)`.
- Fail immediately after enumeration when the version changed, before empty-target handling or relation resolution.
- Preserve the existing global element integrity and canonical-host structural freshness checks from the completed structural lane `c40f3bf91fa107a1244612c1e0dc053b222b727d`.
- Preserve opening category and canonical `HostWallId` ownership checks.
- A mutating empty lazy target sequence fails closed on semantic freshness.
- Stable target resolution remains unchanged.
- No public API signature changes.

## Evidence

- Claim: `a6e89584974c7ece6a62502cb89e9f2dcf78c22d`
- Plan: `ef093faa058b39d9c246d90ca55c0b22591f5f76`
- Source fix: `a4e4d2241048df987e5504e5ce97cc1aee527bb1`
- Focused smoke: `aabe19dc5eebba962f1715212f7310946a2c7bfc`
- Smoke registration: `7c160de66de68c811282f4cd460e927370e454cd`
- Static preflight: `68b2994a57a25b9df60c79f5701562f02179998a`

Readback on current `main` confirmed capture/enumerate/version-check ordering, semantic check before empty-target handling, stable/mutate-yield/mutate-empty smoke coverage, and preservation of global element integrity, canonical host identity, and `HostWallId` ownership checks after concurrent writes.

## Validation limits

The GitHub connector session did not execute the Core smoke executable, Python preflight, GitHub Actions, or licensed BricsCAD runtime. No PASS is claimed for those execution environments.

## Excluded scope

- Existing physical opening structural/global identity/canonical relation lanes.
- Physical boolean execution, native geometry, and persisted target-state encoding.
- GitHub Actions or licensed BricsCAD runtime qualification.
