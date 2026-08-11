# Work claim — Material Catalog refresh lifecycle regression gate

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-refresh-20260811-1937`
- Registered: `2026-08-11T19:37:30+07:00`
- Baseline main SHA: `6c4d1775a0f55424bb6e1d4aa35c07e5682a41e7`
- Priority: lock the already-pushed Material Catalog stale-editor/reload fix against regression without broadening into another modeless lifecycle lane

## Reserved scope

Extend the existing Material Catalog project-lifecycle static preflight so a Refresh after project reload/replacement must resolve the selected material from the current canonical catalog, synchronize or clear the editor state, and only then accept the newly resolved project as the bound project. This lane is regression coverage for the already-landed Material Catalog lifecycle fix; it does not introduce new product behavior.

## Expected surfaces

- `scripts/preflight-material-catalog-project-lifecycle.py`
- read-only inspection of `src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs`
- this claim file for close-out status

## Excluded scope

- No edits to Material Catalog product/UI source unless a new defect is independently proven after this claim is published; any such expansion requires a claim update first.
- No Revision/Rebar/Door/Room Finish/Schedule Hub viewer identity work reserved by the active modeless-viewer claim.
- No Workspace multi-selection policy, Direct Draw/Create Similar, Core atomicity, Room Finish mutation-safety, agent-registration protocol, BricsCAD V25 qualification, release, signing, installer, or GitHub Actions dispatch.

## Validation plan

- Re-read the exact current Material Catalog window and preflight before editing.
- Add structural assertions that `RefreshAll` resolves a current `selectedMaterial`, calls `LoadEditor` or `ClearEditor`, and only afterward assigns `_boundProject = project`.
- Require `LoadEditor` to synchronize `_editingId`, Name, Unit and Description from the canonical material and `ClearEditor` to clear all four fields.
- Inspect the pushed commit diff and final file on current `main`; do not claim the Python preflight was executed unless an execution environment actually runs it.

## Coordination

Current active claims reserve registration protocol, Direct Draw Create Similar, Workspace multi-selection policy, modeless viewer project identity, Core mutation atomicity and Room Finish mutation safety. This lane is intentionally limited to the existing Material Catalog lifecycle preflight and does not touch those source/test surfaces.

## Completion

- Implementation commit: `85aac86b0be9ee7467ba5da94f826cfed227d411` — `test(materials): lock refresh editor lifecycle`.
- The existing preflight now checks current-material resolution, `LoadEditor`/`ClearEditor` editor synchronization, and ordering that accepts `_boundProject = project` only after editor state is synchronized or cleared.
- Validation actually performed: exact-HEAD source inspection plus pushed commit diff/truncation review; `main` was observed at `85aac86b0be9ee7467ba5da94f826cfed227d411` immediately before close-out.
- Not claimed: execution of the Python preflight, BricsCAD V25 runtime qualification, GitHub Actions, release, signing or installer work.
- No LOCAL_ONLY inbox change was needed because this batch only strengthens static regression coverage for an already-known source lifecycle contract.

## Completion condition

Completed: the existing Material Catalog lifecycle gate protects refresh/editor synchronization and rebind ordering on current `main`, the final diff was reviewed, and this claim records the actual implementation SHA and validation boundary.
