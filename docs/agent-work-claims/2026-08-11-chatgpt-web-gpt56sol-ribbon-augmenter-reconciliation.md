# Work claim — legacy Ribbon augmenter button reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ribbon-augmenter-reconcile`
- Registered: `2026-08-11T21:08:00+07:00`
- Baseline main SHA: `4de307dd4fb8afa914bdaccf44bc9ac43452de45`
- Priority: make completed Reference Wall and Project Tools Ribbon augmenters converge existing buttons to current text/command/handler state after plugin reload instead of treating an existing ID as permanently correct.

## Confirmed defect

The completed grouped-panel compatibility work made panel placement deterministic, but `ReferenceWallRibbonAugmenter` and `ProjectRibbonAugmenter` still use create-only button logic. Reference Wall returns immediately when the current ID or command is found; Project Tools skips any existing ID. If a prior plugin version already created those stable button IDs with stale text, command parameter or handler, `Reset()` + reinitialize cannot repair the in-memory button. The main `RibbonBootstrapper` now reconciles known buttons, so these two legacy augmenters should follow the same hot-reload contract.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Ribbon/ReferenceWallRibbonAugmenter.cs`
- `src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs`
- `scripts/preflight-ribbon-augmenter-panel-targets.py`
- this claim file for close-out

## Intended contract

- Find existing augmenter buttons by their stable exact ID.
- Create/add only when the stable ID is absent.
- Whether new or existing, reconcile Name/Text/ShowText/ShowImage/CommandParameter/CommandHandler to the current source contract.
- Keep exact grouped/dedicated panel targeting, idempotence and click-time `MdiActiveDocument` dispatch.
- Do not delete or rewrite unknown buttons.

## Excluded scope

- `QuickWorkflowRibbonAugmenter.cs` remains reserved by the existing Create Similar claim and is read-only/out of scope here.
- No `RibbonBootstrapper.cs`, PluginEntry, Start Center, updater, Quantity/BQ, Workspace, Core, release/signing or LOCAL inbox edits.
- No GitHub Actions dispatch and no remote BricsCAD V25 runtime PASS claim.

## Validation plan

Re-fetch both product files immediately before writes, replace create-only ID guards with find-or-create/reconcile behavior, and strengthen the existing auto-discovered grouped-augmenter gate to reject the stale `continue`/early-return patterns while preserving exact panel IDs and active-document dispatch. Verify commit diffs and current-main ancestry after integration.

## Completion condition

Reference Wall and Project Tools existing buttons converge to current source state after reinitialization without duplication or unrelated deletion, static regression coverage is merged, and exact V25 hot-reload/render proof remains LOCAL_ONLY.