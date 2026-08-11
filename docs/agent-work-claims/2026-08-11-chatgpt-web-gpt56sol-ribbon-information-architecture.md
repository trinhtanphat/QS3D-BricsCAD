# Work claim — Ribbon information architecture and command discoverability

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ribbon-ia-2000`
- Registered: `2026-08-11T20:00:00+07:00`
- Baseline main SHA: `556f463e4ddbb3d8782fb3376c6aeee12c18e08c`
- Priority: user-provided BLT3D visual reference shows dense/empty-looking ribbon states; improve QS3D command discoverability and visual grouping without changing semantic/CAD ownership behavior.

## Reserved scope

Rework only the BricsCAD ribbon information architecture so existing QS3D commands are presented as clear functional groups/panels instead of one oversized panel per tab. Preserve every existing command binding and add only source-safe discoverability metadata/labels where the current command already exists. Produce a detailed implementation plan derived from the provided desktop-reference screenshots and a focused static preflight guarding the grouped-panel contract.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs`
- `scripts/preflight-ribbon-information-architecture.py` (new)
- `docs/UI-RIBBON-INFORMATION-ARCHITECTURE-2026-08-11.md` (new)
- this claim file for close-out

## Excluded scope

- No edits to `WorkspacePanel*`, `RightPanel*`, `Theme.xaml`, modeless windows, command implementations, Core semantic/persistence services, Direct Draw/Create Similar, Room Auto, recognition, level Z-chain, quantity/reporting, installer/release or local-only gates.
- No changes to command ownership, transactions, project lifecycle, geometry generation, selection semantics or native CAD behavior.
- No BricsCAD V25 runtime PASS claim from the remote connector lane.

## Validation plan

- Re-fetch current `main` and active neighboring claims before implementation writes.
- Preserve all current ribbon command strings exactly unless source proves an invalid binding.
- Add a static preflight that verifies the grouped panel model, key tabs/panels and representative command bindings, and rejects regression to the single-panel-per-tab construction.
- Inspect final diff against latest `main`; inspect CI/workflow status metadata after push when available.
- Runtime visual fit, DPI behavior and BricsCAD ribbon rendering remain local V25 qualification.

## Coordination

Known active lanes at reservation time include Room Auto regeneration safety, Create Similar/active-family support, generated source recognition, native level Z-chain, LOCAL-013 qualification and repository audit/reporting identity hardening. This reservation owns only `RibbonBootstrapper.cs` plus its new plan/preflight and does not touch their product surfaces.

## Completion condition

Grouped ribbon panels and detailed UX plan are pushed to current `main`, the static source contract is added, final command bindings are reviewed, and this claim is marked `COMPLETED` with actual validation and commit SHAs; otherwise mark `RELEASED`/`BLOCKED` without overstating runtime proof.
