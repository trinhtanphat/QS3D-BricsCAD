# Work claim — Physical opening global semantic element integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-physical-opening-global-element-integrity`
- Registered: `2026-08-12T09:35:00+07:00`
- Completed: `2026-08-12T09:39:00+07:00`
- Last Updated: `2026-08-12T09:39:00+07:00`
- Baseline main SHA: `4e311927d63c01b0e77227de79073823fafad979`
- Claim commit: `ec9e169a4c0974616be451034da991aa8dd6245c`
- Branch source commit: `f5f9a021dc9563113029e5d5183159f4f65c4f25`
- Branch regression commit: `b6ca44e1378fc6d91d6aceefc30fc1d6aa60ab4e`
- Pull request: `#704`
- Main merge commit: `ffa4cfeced771f4365f5bf66875f89b35e5a8c83`
- Priority: P1 — prevent physical opening ownership resolution from succeeding against a globally ambiguous semantic element identity set
- Task Key: `CORE-PHYSICAL-OPENING-GLOBAL-ELEMENT-INTEGRITY`

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Resolve(...)` resolved the supplied host and each opening with `ProjectState.FindElement(...)`. `FindElement` fails on null entries and duplicate matches for the requested id, but an unrelated duplicate pair could remain elsewhere in `project.Elements` while a unique host/opening pair resolved successfully. This allowed physical-cut ownership validation to return canonical-looking targets from a project whose semantic element identity set was globally invalid.

## Implemented

- `Resolve(...)` now preflights the complete `project.Elements` collection before host/opening lookup.
- Null semantic entries, blank semantic ids and case-insensitive duplicate ids fail closed.
- Existing host object-identity checks, target normalization/order, Door/WallOpening category validation, canonical `HostWallId` validation and target-state encoding remain unchanged.
- Native boolean commands, HostLinkService, persistence/interchange and unrelated opening geometry were not changed.

## Regression coverage

`PhysicalOpeningGlobalElementIntegritySmoke` is auto-registered through `ModuleInitializer` and proves:

- unique `HOST`/`OPENING` plus unrelated `DUP`/`dup` causes `Resolve(...)` to fail closed;
- a valid project resolves exactly one target and returns the canonical `ProjectElement` instance by reference.

## Merge/readback evidence

- PR `#704` contained exactly two changed files: the codec source and focused smoke.
- Squash merge succeeded at `ffa4cfeced771f4365f5bf66875f89b35e5a8c83` with expected PR head `b6ca44e1378fc6d91d6aceefc30fc1d6aa60ab4e`.
- Direct readback from `main` confirmed source blob `cca2ada0e315bb427584098919e137b8176dda6d` and smoke blob `7887a8cd6faec497fcbf536b8b0652897b61881b`.
- Comparison from merge SHA to later `main` reported `behind_by=0`; concurrent commits touched unrelated surfaces.

## Validation boundary

No full build, executable smoke, GitHub Actions or licensed BricsCAD V25/V26 runtime PASS was claimed or performed in this hosted lane.

## Outcome

Physical opening target resolution now fails closed on globally ambiguous semantic element identity while preserving valid canonical target resolution. The claim is released as completed.
