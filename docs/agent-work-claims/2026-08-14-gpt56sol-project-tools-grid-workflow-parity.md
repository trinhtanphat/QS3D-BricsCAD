# Work claim — Project Tools Grid workflow parity

- Status: `ACTIVE`
- Agent: `gpt56sol / ChatGPT web`
- Registered: `2026-08-14T15:14:30+07:00`
- Baseline main SHA: `c1413daca35dfd611d1ba4d24b015fa4b68bc5c3`
- Priority: issue #79 / UI completeness — expose the already-implemented first-class Grid capture, naming and annotation workflow beside the planner-review controls in Project Tools.

## Reserved scope

Make the existing Project Tools Grid/reference card expose the canonical Grid lifecycle commands already shipped by the adapter: `QS3DGRID`, `QS3DGRIDNUMBER`, `QS3DGRIDANNOTATE`, `QS3DGRIDANNOTATEALL`, plus the existing planner review commands. Keep dispatch through the existing document-bound `OnCommandClick` path. Extend the focused Project Tools static gate so future UI refactors cannot silently remove these commands.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml` — Grid/reference workflow buttons and explanatory copy only.
- `scripts/preflight-project-tools.py` — exact UI wiring/command-declaration guards for the Grid workflow only.
- This claim file for closeout.

## Excluded scope

- No changes to Grid planners, naming, annotation materialization, ownership, health, geometry, Ribbon bootstrap/augmenters, or command implementations.
- No native rectangular/radial system materialization, pair-owned intersection marker materialization, Level hosting/constraints, or other LOCAL_ONLY issue #79 work.
- No BricsCAD execution, private drawings, GitHub Actions dispatch, release/tag operations, or runtime qualification.
- Do not touch the currently claimed BLT Ribbon tab-contract lane or other ACTIVE claims.

## Validation plan

- Confirm every Project Tools Grid `Tag` resolves to exactly one existing `[CommandMethod]`.
- Run/review `scripts/preflight-project-tools.py` contract statically from source.
- Re-read changed blobs and resulting main SHAs after each write.

## Completion condition

Project Tools exposes one coherent capture → number → annotate → planner-review Grid workflow through existing canonical commands, the source guard covers it, and the claim is closed without claiming licensed BricsCAD V25 runtime PASS.