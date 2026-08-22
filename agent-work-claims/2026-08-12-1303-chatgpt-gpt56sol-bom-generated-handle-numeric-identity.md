# Work claim — BOM generated handle numeric identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-bom-generated-handle-numeric-identity-20260812`
- Registered: `2026-08-12T13:03:00+07:00`
- Baseline main SHA: `705682b5833af9a631b97a56de6656d21e483ab2`
- Task Key: `CORE-BOM-GENERATED-HANDLE-NUMERIC-IDENTITY`
- Integration: `d9e3f56e5f29fbee38f943bab967a21470d20d29` (PR #916)
- Original PR: #913 closed unmerged after `main` movement made it non-mergeable; exact patch recovered on #916.

## Defect

`BomReleaseGuardService.Inspect(...)` canonicalized caller-supplied live CAD handles only with `Trim()` and then compared them directly to generated owner handles. The canonical generated-handle ownership contract uses `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`, so numerically equivalent CAD handles such as `000A`, `0xA`, and `A` could be treated as different by the BOM release guard and emit a false `BOM_GENERATED_HANDLE_MISSING` release blocker.

## Repair

- live generated handles are normalized with `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` before the BOM membership check;
- owner handles remain sourced through `EnumerateLogicalOwnerHandles(...)`, whose split path already applies the same normalization policy;
- empty-handle filtering and case-insensitive set semantics are preserved;
- `BomReleaseGuardGeneratedHandleNumericIdentitySmoke` covers a numeric-equivalent live handle and a truly missing handle, and verifies inspection does not mutate `ProjectState.ChangeVersion`.

## Validation

- exact PR diff reviewed: 2 files, +70/-1;
- source and regression read back from `main` after merge;
- `compare_commits(d9e3f56e5f29fbee38f943bab967a21470d20d29, main)` returned identical / `behind_by=0` at verification time;
- no GitHub Actions/full build/executable smoke/BricsCAD V25/V26 runtime PASS claimed.
