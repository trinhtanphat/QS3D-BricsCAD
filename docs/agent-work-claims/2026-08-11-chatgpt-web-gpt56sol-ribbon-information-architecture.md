# Work claim — Ribbon information architecture and command discoverability

- Status: `COMPLETED`
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

Known active lanes at reservation time included Room Auto regeneration safety, Create Similar/active-family support, generated source recognition, native level Z-chain, LOCAL-013 qualification and repository audit/reporting identity hardening. A later Start Center reservation explicitly excluded this Ribbon scope while it remained ACTIVE. This lane touched only `RibbonBootstrapper.cs` plus its new plan/preflight and did not modify neighboring product surfaces.

## Completion condition

Grouped ribbon panels and detailed UX plan are pushed to current `main`, the static source contract is added, final command bindings are reviewed, and this claim is marked `COMPLETED` with actual validation and commit SHAs; otherwise mark `RELEASED`/`BLOCKED` without overstating runtime proof.

## Completion record

- Registration commit: `d5f7cce7e266f1ed476c490958eaac155f7e4e82`.
- Implementation commit: `9f2ce05895b3ecf308c21915ddeb3bcb90ec57fc` — `feat(ribbon): group commands by workflow`.
- Implementation result: replaced the flat one-panel-per-tab presentation model with `RibbonTabSpec -> RibbonPanelSpec -> RibbonButtonSpec`; added named functional panels across all 11 QS3D tabs; retained click-time dispatch through the current `MdiActiveDocument`.
- Command preservation: a staged-source set comparison verified all 103 unique pre-change command strings remain represented after regrouping, with no missing or invented command bindings in this lane.
- Regression guard: added `scripts/preflight-ribbon-information-architecture.py`; its focused logic was exercised against the staged generated source and returned PASS before integration. The committed preflight also verifies grouped-panel structure, panel identity in native IDs, key tab/panel captions and the full pre-existing command set.
- Documentation: added `docs/UI-RIBBON-INFORMATION-ARCHITECTURE-2026-08-11.md` with screenshot review, panel map, non-goals, safety boundary and exact local V25 render/DPI checklist.
- Integration safety: immediately before the implementation tree was created, current `main` still had the pre-change `RibbonBootstrapper.cs` blob `becc1d401c32689a8f6a5b036a30acc3f1491e06`; the implementation was fast-forwarded without force. A later concurrent `main` commit is a direct child of the Ribbon implementation commit, confirming the batch remained in current ancestry.
- GitHub status metadata for the implementation SHA returned no status contexts. No GitHub Actions, release, installer, signing, C# adapter build or licensed BricsCAD V25 runtime was dispatched/executed by this lane.
- LOCAL_ONLY remains pending: exact BricsCAD V25 ribbon rendering, 1366×768/HiDPI fit, idempotent reload, tab/panel overflow and representative button dispatch must be qualified on the exact candidate SHA under the existing local V25 qualification process. No `LOCAL_PASS` is inferred here.
