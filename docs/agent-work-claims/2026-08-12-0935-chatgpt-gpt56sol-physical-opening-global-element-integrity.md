# Work claim — Physical opening global semantic element integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-physical-opening-global-element-integrity`
- Registered: `2026-08-12T09:35:00+07:00`
- Last Updated: `2026-08-12T09:35:00+07:00`
- Baseline main SHA: `4e311927d63c01b0e77227de79073823fafad979`
- Priority: P1 — prevent physical opening ownership resolution from succeeding against a globally ambiguous semantic element identity set
- Task Key: `CORE-PHYSICAL-OPENING-GLOBAL-ELEMENT-INTEGRITY`

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` resolves the supplied host and each opening with `ProjectState.FindElement(...)`. `FindElement` fails on null entries and duplicate matches for the requested id, but an unrelated duplicate pair can remain elsewhere in `project.Elements` while a unique host/opening pair resolves successfully. This allows physical-cut ownership validation to return canonical-looking targets from a project whose semantic element identity set is globally invalid. `DependencyGraph`, `BulkEditService`, snapshot capture, HostLinkService and persistence boundaries fail closed on global duplicate element identities.

## Reserved scope

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
- one focused auto-registered Core smoke for unrelated duplicate semantic ids plus valid control
- this claim file

## Intended contract

- `Resolve(...)` preflights the complete `project.Elements` collection for non-null, nonblank, case-insensitively unique semantic ids before resolving the requested host/openings.
- Unrelated duplicate semantic ids fail before a valid host/opening result can be returned.
- Preserve existing host reference identity checks, target ordering/normalization, Door/WallOpening category validation, canonical `HostWallId` validation and target-state codec behavior.
- Do not change native boolean commands, HostLinkService, persistence/interchange or unrelated opening geometry.

## Validation plan

Add a focused smoke with unique `HOST`/`OPENING` plus unrelated `DUP`/`dup`; prove `Resolve(...)` throws before returning physical-cut targets. Remove the duplicate and prove the same host/opening resolves by object identity. Use module-initializer registration to avoid shared registry contention. Re-read current source immediately before product write; no force push and no GitHub Actions dispatch.

## Validation boundary

No full build, executable smoke, GitHub Actions or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

## Completion condition

Physical opening target resolution fails closed on globally ambiguous semantic element identity with focused regression evidence merged to current `main`, then this claim is closed with exact commit/PR evidence.
