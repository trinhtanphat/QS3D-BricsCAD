# V25 Basic Drawing exact project generation affinity

## Scope

Lane C03 guards `QS3DDRAWLINE`, `QS3DDRAWRECT`, and `QS3DDRAWCIRCLE` against committing native geometry after the QS3D project object backing the source DWG has been replaced during an interactive prompt.

The command already captures one exact BricsCAD `Document`, checks that it remains the active MDI document, requires Model Space, and freezes the prompt UCS. This carrier adds the missing project-generation fence without changing authoring semantics or XData format.

## Root cause

Before this fix, `BasicDrawingContext` retained only semantic values (`ProjectId`, `ChangeVersion`, active Family, Floor, Zone). `RequireFreshContext` reacquired the current project after point/distance prompts and accepted a replacement `ProjectState` when those values happened to match. A reload/rebind can therefore replace project generation A with generation B while preserving identifiers and version values; the old command could then hand geometry captured under A into a commit performed while B was authoritative.

## Required behavior

1. `CaptureContext` retains the exact `ProjectState` object used to derive command context.
2. After `ProjectContextCoordinator.TryGetReadOnly` reacquires the project immediately before native commit, `RequireFreshContext` requires exact reference equality with the captured project before checking semantic values or active Family.
3. Any project replacement fails closed before `AppendEntity` starts a native transaction. The user reruns the command against the new project generation.
4. Existing active-document, Model Space, UCS, ProjectId/version, Family, Floor and Zone checks remain in force.
5. Prompt cancel remains a no-op. Native transaction ownership, XData identity and post-commit UI-warning truth are unchanged.

## REMOTE_SAFE verification

Run the auto-discovered guard:

```text
python scripts/preflight-v25-basic-drawing-exact-project-generation.py
```

Protected Shared CI must also pass aggregate feature guards, deterministic smoke, trusted/admitted BricsCAD V25 reference validation, and the V25 plugin compile on the exact PR head.

These checks prove source ordering/containment and compile compatibility. They do not prove native host timing.

## LOCAL_ONLY licensed qualification

A native runtime check requires licensed BricsCAD V25. Start a Basic Drawing command in DWG A, replace/reload the QS3D `ProjectState` for the same document while an interactive point/distance prompt is outstanding while preserving the same ProjectId/ChangeVersion/Family/Floor/Zone values, then complete the prompt. Expected result: no geometry is committed and the command reports a retry/fresh-context failure. A new command started after replacement should work normally.

Do not mark `LOCAL_PASS` from hosted CI, static inspection, admitted-reference compile, or a synthetic managed-only simulation. Until the licensed timing scenario is executed, its classification is `LOCAL_ONLY / NO_RESULT`.
