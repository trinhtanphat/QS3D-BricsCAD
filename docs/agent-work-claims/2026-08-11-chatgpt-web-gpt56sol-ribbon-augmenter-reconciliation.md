# Work claim — legacy Ribbon augmenter button reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-ribbon-augmenter-reconcile`
- Registered: `2026-08-11T21:08:00+07:00`
- Completed: `2026-08-11T21:17:00+07:00`
- Baseline main SHA: `4de307dd4fb8afa914bdaccf44bc9ac43452de45`
- Priority: make completed Reference Wall and Project Tools Ribbon augmenters converge existing buttons to current text/command/handler state after plugin reload instead of treating an existing ID as permanently correct.

## Confirmed defect

The completed grouped-panel compatibility work made panel placement deterministic, but `ReferenceWallRibbonAugmenter` and `ProjectRibbonAugmenter` still used create-only button logic. Reference Wall returned immediately when the current ID or command was found; Project Tools skipped any existing ID. If a prior plugin version had already created those stable buttons with stale text, command parameter or handler, `Reset()` + reinitialize could not repair the in-memory button.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Ribbon/ReferenceWallRibbonAugmenter.cs`
- `src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs`
- `scripts/preflight-ribbon-augmenter-panel-targets.py`
- this claim file for close-out

## Completion record

- Reservation: `237e6ebbcfaa90b2465e172208bcc812ff95ccab` — `chore(agent): claim ribbon augmenter reconciliation`.
- Reference Wall: `aa8b4a067b18f0752a6c097993c0771c1b356516` — `fix(ribbon): reconcile reference wall button state`.
  - finds the stable ID first and preserves the prior command-based dedupe path as a fallback;
  - creates/adds only if neither existing identity is found;
  - then reconciles canonical ID, Name, Text, ShowText/ShowImage, CommandParameter and CommandHandler for both existing and newly created buttons;
  - deterministic `QS3D_AUTHOR_ARCHITECTURE_PANEL_SOURCE` targeting and click-time active-document dispatch remain unchanged.
- Project Tools: `60870ab437993849d27f6ded1dadf98766ab16f8` — `fix(ribbon): reconcile project tool button state`.
  - each stable Project Tools button is now find-or-create by ID;
  - presentation, command and handler state is reconciled on every initialization instead of skipped when an ID already exists;
  - dedicated `QS3D_PROJECT_TOOLS_PANEL_SOURCE` creation/reuse and unrelated-button preservation remain unchanged.
- Regression guard: `1efc2d329e3f30ddf6869f55095c8c631863370e` — `test(ribbon): guard augmenter button reconciliation`.
  - rejects the previous Reference Wall early-return and Project Tools `continue` patterns;
  - requires find-or-create followed by state reconciliation order;
  - preserves exact grouped/dedicated panel IDs and click-time `MdiActiveDocument` dispatch checks;
  - also repaired a stale bootstrap assertion in this gate so it follows the current `PanelSourceId(...)` helper introduced by the completed Ribbon reconciliation work rather than requiring the superseded inline ID expression.

## Integration verification

Exact commit diffs were inspected after merge. Current-main ancestry comparisons reported `behind_by: 0` for the Reference Wall implementation and for the final preflight commit, with each implementation commit as its comparison merge base. Concurrent Quantity/BQ/Workspace/Core work remained intact; no force push, reset or overwrite was used.

## Validation boundary

The source and static gate are committed and were re-fetched/reviewed, but the Python preflight was **not executed in this connector-only lane**. No GitHub Actions, local checkout/build, BricsCAD V25 launch, installer, signing or release was dispatched.

Exact V25 hot-reload convergence, native Ribbon object mutation behavior, render/DPI/Unicode and button click behavior remain `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`; no `LOCAL_PASS` is inferred from source review.

## Coordination

`QuickWorkflowRibbonAugmenter.cs` remained untouched because it is still reserved by the existing Create Similar claim. `RibbonBootstrapper.cs`, PluginEntry, Start Center, updater, Quantity/BQ, Workspace and Core were also not modified by this lane.

## Completion condition

Satisfied for remote/source scope: Reference Wall and Project Tools existing stable buttons now converge to current source state after reinitialization without duplication or unrelated deletion, focused static regression coverage is merged, and exact native V25 proof remains local-only.