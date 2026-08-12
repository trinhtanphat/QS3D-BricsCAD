# Work claim — BOM live generated-handle canonical whitespace

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bom-live-handle-trim-20260812-0759`
- Registered: `2026-08-12T07:59:00+07:00`
- Baseline main SHA: `c8181892b29fb3eb394fb008172747f5b337428c`
- Priority: evidence-driven release-guard correctness during owner-requested `continue all`

## Confirmed defect

`BomReleaseGuardService.Inspect(ProjectState, ISet<string>?)` rebuilds caller live handles with an ordinal-ignore-case comparer, but stores each raw string without trimming or removing whitespace-only entries. This is inconsistent with `ModelHealthService.NormalizeHandleSet`, which treats CAD handle identity as trimmed + case-insensitive. A caller set containing `" 2b "` therefore causes a false `BOM_GENERATED_HANDLE_MISSING` even though the canonical generated handle is `2B`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs`
- `tests/QS3D.Core.SmokeTests/BomReleaseGuardSmoke.cs`
- this claim file for close-out

## Contract

- BOM live generated-handle inputs are normalized by ignoring null/blank entries, trimming surrounding whitespace and comparing case-insensitively;
- the same normalized index continues to feed both curtain-panel health and generic generated-handle liveness checks;
- existing missing-handle detection, owner registry behavior, release diagnostics and quantity semantics remain unchanged;
- no CAD mutation, persistence, WPF/native BricsCAD, updater/release packaging or unrelated health-provider behavior changes.

## Validation plan

Extend the existing deterministic BOM smoke so a case-sensitive caller set containing padded lower-case handles satisfies canonical liveness, while an actually missing handle still produces `BOM_GENERATED_HANDLE_MISSING`.

No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim from this web session.

## Completion evidence

- Implementation PR: `#634` (`agent/gpt56sol-bom-live-handle-trim-20260812`).
- Reviewed PR head: `ddf7f387d4c56fa3657057adb80f18110402c22e`.
- Squash merge to `main`: `ff89905bb8a2b1fda922dd28cd7ac8c30b514851`.
- Moving-main comparison from PR base `ff0030d631379071b62d69ae2e238dcd5c5ce387` to `2c7fafcff8c9fa05dc258e72553b7309b225820e` showed only an unrelated Slab Mesh claim and `src/QS3D.Core/Rebar/RebarSchedule.cs`; neither reserved BOM file overlapped.
- Reviewed patch normalizes nonblank live generated handles with `Trim()` into an ordinal-ignore-case index and adds deterministic padded/lower-case smoke coverage while retaining missing-handle assertions.
- No GitHub Actions, build/release workflow, force-push, or BricsCAD runtime qualification was performed.
