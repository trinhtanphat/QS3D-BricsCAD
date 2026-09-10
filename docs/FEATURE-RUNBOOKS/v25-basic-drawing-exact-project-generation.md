# V25 Basic Drawing cached project generation affinity

## Scope

Lane C03 guards `QS3DDRAWLINE`, `QS3DDRAWRECT`, and `QS3DDRAWCIRCLE` against committing native geometry after the authoritative cached QS3D project generation backing the source DWG changes during an interactive prompt.

The command already captures one exact BricsCAD `Document`, checks that it remains the active MDI document, requires Model Space, and freezes the prompt UCS. This carrier adds the missing cached project-generation fence without changing authoring semantics or XData format.

## Root cause and compatibility constraint

Before this fix, `BasicDrawingContext` retained only semantic values (`ProjectId`, `ChangeVersion`, active Family, Floor, Zone). `RequireFreshContext` reacquired the current project after point/distance prompts and accepted a replacement authoritative cached `ProjectState` when those values happened to match. A reload/rebind can therefore replace project generation A with generation B while preserving identifiers and version values; the old command could then hand geometry captured under A into a commit performed while B was authoritative.

A naïve unconditional `ReferenceEquals` fence is not compatible with the existing read-only path. `ProjectContextCoordinator.TryGetReadOnly` may deserialize the sidecar into a new `ProjectState` wrapper on each call when no project is cached. Distinct uncached wrappers do not by themselves mean an authority transition. The fence therefore distinguishes authoritative cached generations from uncached sidecar rehydration.

## Required behavior

1. `CaptureContext` retains the `ProjectState` returned by `TryGetReadOnly` and snapshots whether that exact object is also the cached project authority. If a cache exists but disagrees with the read-only object, capture fails closed.
2. Immediately before native commit, `RequireFreshContext` reacquires read-only state and cache state.
3. A cached↔uncached transition fails closed. When both snapshots are cached, both the cache and read-only result must be the exact captured `ProjectState` generation.
4. When both snapshots are uncached, wrapper reference inequality is permitted and the existing ProjectId/version/Family/Floor/Zone semantic checks remain the freshness authority. This preserves normal sidecar-only read behavior.
5. Any authoritative cached project replacement fails before `AppendEntity` starts a native transaction. The user reruns the command against the new generation.
6. Existing active-document, Model Space and UCS fences remain in force. Prompt cancel remains a no-op. Native transaction ownership, XData identity and post-commit UI-warning truth are unchanged.

## REMOTE_SAFE verification

Run the auto-discovered guard:

```text
python scripts/preflight-v25-basic-drawing-exact-project-generation.py
```

Protected Shared CI must also pass aggregate feature guards, deterministic smoke, trusted/admitted BricsCAD V25 reference validation, and the V25 plugin compile on the exact PR head.

These checks prove source ordering/containment and compile compatibility. They do not prove native host timing.

## LOCAL_ONLY licensed qualification

A native runtime check requires licensed BricsCAD V25 and has two important cases.

**Cached replacement:** start a Basic Drawing command in DWG A while project generation A is cached, replace/reload it with cached generation B while an interactive point/distance prompt is outstanding while preserving the same ProjectId/ChangeVersion/Family/Floor/Zone values, then complete the prompt. Expected result: no geometry is committed and the command reports a retry/fresh-context failure. A new command started after replacement should work normally.

**Uncached sidecar read:** exercise Basic Drawing while no project is cached and `TryGetReadOnly` rehydrates the same sidecar state into distinct wrappers across capture and pre-commit freshness checks. Expected result: wrapper inequality alone does not abort; semantic freshness checks still govern the commit.

Do not mark `LOCAL_PASS` from hosted CI, static inspection, admitted-reference compile, or a synthetic managed-only simulation. Until these licensed timing scenarios are executed, classification is `LOCAL_ONLY / NO_RESULT`.
