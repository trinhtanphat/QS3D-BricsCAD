# Work claim — Physical opening host structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-physical-opening-host-structural-freshness-20260812-1028`
- Registered: `2026-08-12T10:28:00+07:00`
- Baseline main SHA observed: `bb50e290d890ec2f5b147f24445ca59d3b4baba4`
- Priority: P1 semantic ownership integrity
- Task Key: `CORE-PHYSICAL-OPENING-HOST-STRUCTURAL-FRESHNESS`

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` validates the supplied host against the current project before it normalizes/enumerates caller-owned `openingIds`. `Normalize(openingIds)` can execute arbitrary lazy-enumerator code. Because `ProjectState.Elements` is a publicly mutable list, that enumeration can directly remove or replace the host without calling `project.Touch()`. The resolver then continues using the pre-enumeration `canonicalHost` object while opening targets themselves are resolved from the current project after enumeration.

## Reserved scope

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs` — post-normalization current host ownership revalidation only
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningGlobalElementIntegritySmoke.cs` — focused regression in the existing auto-discovered smoke
- this claim file

## Intended contract

Preserve existing global element integrity, host category/reference semantics, target ordering and target relation checks. After caller `openingIds` enumeration completes and the existing non-empty check passes, revalidate the host against the current project by semantic id and object identity before resolving any target opening. Direct structural host removal/replacement/duplicate identity during enumeration must fail closed.

## Excluded scope

No changes to opening category policy, HostWallId canonicality, target-state encoding, native boolean execution, CAD/UI/runtime, global ProjectState collection tracking, Actions/build/release.

## Validation plan

Extend the current physical-opening global element integrity smoke with a lazy opening-id sequence that yields a valid opening id and then directly removes the host from `project.Elements` without calling `Touch()`. Require `Resolve(...)` to throw before returning target authorization, while `ChangeVersion` remains unchanged and no resolver-side host/opening semantic state is mutated.

No GitHub Actions/full build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS claim from this lane.
