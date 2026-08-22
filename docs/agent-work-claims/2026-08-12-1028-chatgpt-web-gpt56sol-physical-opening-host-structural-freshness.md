# Work claim — Physical opening host structural freshness

- Status: `COMPLETE`
- State: `COMPLETE`
- Agent: `chatgpt-web-gpt56sol-physical-opening-host-structural-freshness-20260812-1028`
- Registered: `2026-08-12T10:28:00+07:00`
- Completed: `2026-08-12T10:41:00+07:00`
- Baseline main SHA observed: `bb50e290d890ec2f5b147f24445ca59d3b4baba4`
- Priority: P1 semantic ownership integrity
- Task Key: `CORE-PHYSICAL-OPENING-HOST-STRUCTURAL-FRESHNESS`

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` validated the supplied host against the current project before it normalized/enumerated caller-owned `openingIds`. `Normalize(openingIds)` can execute arbitrary lazy-enumerator code. Because `ProjectState.Elements` is a publicly mutable list, that enumeration could directly remove or replace the host without calling `project.Touch()`. The resolver then continued using the pre-enumeration `canonicalHost` object while opening targets themselves were resolved from the current project after enumeration.

## Implemented

- Product fix: `b63a932ad306601694ef548309ab91237e2c3cf4` (`fix(opening): revalidate host after target enumeration`).
- Regression: `ef92fa1259e0fbc942d04431bcd7355daf4cf70e` (`test(opening): guard host structural freshness`).
- After `Normalize(openingIds)` and the existing non-empty check, `Resolve(...)` now revalidates global current element integrity and requires the current host lookup to resolve to the same object preflighted before enumeration.
- Existing opening category, HostWallId canonicality, target ordering and target-state encoding semantics are unchanged.

## Validation evidence

Product commit readback confirms the source diff is limited to post-enumeration host revalidation before target resolution. Regression readback confirms a lazy opening-id iterator yields a valid target and then directly removes the host from `project.Elements` without calling `Touch()`. The test requires the structural-freshness exception, unchanged `ProjectState.ChangeVersion`, unchanged HostWallId, dirty flags and timestamps, aside from the deliberate external list removal itself.

At completion refresh, current `main` was `e5ec0a381629dbb1f676afab354dd8074038584c`; the product and regression commits had already been read back successfully from repository history. No GitHub Actions/full build/release was dispatched from this lane, and no licensed BricsCAD V25/V26 runtime PASS is claimed.

## Excluded scope

No changes to opening category policy, HostWallId canonicality, target-state encoding, native boolean execution, CAD/UI/runtime, global ProjectState collection tracking, Actions/build/release.
