# Work claim — ProjectFamilyService.Assign lazy-input freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-assign-input-freshness-20260812-1002`
- Registered: `2026-08-12T10:02:00+07:00`
- Completed: `2026-08-12T10:07:00+07:00`
- Baseline main SHA: `790af584a2b356c04303913cfd750991a0f13961`
- Pull Request: `#735`
- Reviewed head: `1d5a6a8c67ca059dce1ee159807e51ccd583f892`
- Merge SHA: `165ffee90a5b0df10165e5c43575eb44e0c70aa5`
- Priority: P1 — prevent Family assignment from applying a stale target/property plan after lazy target enumeration mutates the project.

## Confirmed defect

`ProjectFamilyService.Assign(project, familyId, IEnumerable<ProjectElement>)` resolved the target Family and snapshotted its properties before enumerating the caller-supplied target sequence. A lazy target enumerable could mutate/touch the project while being consumed, after which assignment continued using the pre-enumeration plan or returned a false no-op.

## Completed implementation

- Capture `project.ChangeVersion` immediately before target enumeration.
- Require the same version immediately after `ResolveOwnedElements(...)` fully materializes/validates the sequence.
- Mutating lazy inputs fail before assignment planning, including inputs that mutate then yield no targets.
- Stable lazy input, target/property/category/ownership validation, duplicate collapsing and normal one-revision assignment semantics remain unchanged.
- Focused ModuleInitializer smoke covers stable lazy input, touch-then-yield and touch-then-stop cases.

## Evidence

- PR #735 exact patch reviewed.
- Moving-main comparison showed no overlap with `ProjectFamilyService.cs` or the new smoke before merge.
- Squash merge: `165ffee90a5b0df10165e5c43575eb44e0c70aa5`.

## Validation boundary

No GitHub Actions were dispatched. No local/full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed.
