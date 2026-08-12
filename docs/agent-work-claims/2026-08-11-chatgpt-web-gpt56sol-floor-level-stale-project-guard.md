# Work claim — Floor/Level stale modeless project guard

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-floor-level-stale-project-guard`
- Registered: `2026-08-11`
- Completed: `2026-08-11`
- Baseline main SHA: `7b289bd9a63100eb36d5b3405b7b0dcaa58b66f4`
- Registration commit: `841b462765c6fa4621f08d8cf587309e0a9ebf3b`
- Priority: P0 modeless mutation lifecycle correctness

## Confirmed defect

`FloorLevelWindow` was document-bound but not project-bound. Its mutation callbacks validated only that the source DWG was active and then bound whatever canonical `ProjectState` currently existed. If the same DWG reloaded/replaced its QS3D project while the Level Picker remained open, stale UI state could therefore write into the replacement project. Family Manager and Zone Manager already rejected this lifecycle with a bound `ProjectState` reference; Floor/Level was the remaining manager gap.

## Implemented repair

- `13c5d959a835626efb5b41d55c728425c0d5e9e7` — `FloorLevelWindow` now stores the exact `ProjectState` from the latest successful `RefreshAll`, clears the binding when refresh cannot resolve a project or fails, and revalidates that project before writes.
- Save, delete and activate use `RequireBoundProjectForMutation`, which first validates source-DWG + read-only bound-project identity and then verifies the canonical mutation bind is still the same project instance.
- Assignment preserves the existing selection-before-canonical-bind workflow: it first validates the bound read-only project and previews the exact semantic selection, then after canonical binding verifies bound/preview/canonical project identity plus existing ProjectId and selection ownership checks before mutation.
- Inspect Selection intentionally remains read-only/document-bound and can inspect the newly-current project without creating or binding a mutation context.
- Existing rollback, audit, Floor service semantics, stale generated-output behavior and post-commit refresh remain unchanged.
- `5d3f1086fbb65e90cd865410df2a2c309b21d8dd` — added `scripts/preflight-floor-level-stale-project-guard.py`, requiring bound-project capture/clear/revalidation, rejecting the old document-only mutation pattern, preserving selection-before-bind assignment and keeping inspection read-only.

## Integration

- Branch: `agent/floor-level-stale-project-guard-20260811`.
- Moving-main reconciliation commit: `6faa312346e20c24fc07413d2310bd477ec7c122`; this merged current `main` into the lane without force push while preserving exactly the two reserved product/test paths.
- PR: `#494` — `fix(level): reject stale modeless project mutations`.
- PR changed exactly two paths: `FloorLevelWindow.xaml.cs` and the focused preflight.
- Squash merge into `main`: `b7198497d1858467c4a7c59849285fdf9daa75b4`.
- After integration, moving `main` reached `69478a0e1e9f8371746647a137c700718ec68226`; compare from the squash merge reported `ahead`, `behind_by=0`, with only unrelated later changes, so this repair remained in current ancestry.

## Validation evidence

- Re-fetched the merged `FloorLevelWindow.xaml.cs` from `main`; blob `5499ae52ca17df26df5d7008393bae2e3f888fde` contains `_boundProject` and the new fail-closed binding helpers.
- Re-fetched the merged focused preflight from `main`; blob `0a0dae68584f14784eca006e2c158a534791cfc8` guards the intended lifecycle contract.
- Reviewed PR #494 changed-file list and confirmed only the two reserved paths were present.
- The connector lane source-reviewed the committed preflight but did not execute a repository checkout/runtime test, so no executed preflight/build PASS is claimed.
- No GitHub Actions were dispatched, consistent with repository policy.
- No licensed BricsCAD V25 build/NETLOAD/WPF runtime PASS is claimed; native qualification remains LOCAL_ONLY.

## Completion

The Level Picker now fails closed when its source DWG has replaced/reloaded the QS3D project since the latest successful Refresh, matching the established Family/Zone modeless project-replacement boundary without broadening this lane into ChangeVersion policy or Core semantics.
