# BLT3D parity P2 — shell and project navigation

## Scope

P2 binds already-qualified QS3D shell/workspace/Ribbon and Project/Zone/Floor/Family navigation into the host-neutral parity evidence model introduced by P1. It does not replace BricsCAD's viewport, duplicate the existing QS3D workspace, or introduce a second project database.

The production UI remains the existing V25 source shared by V26 where the project already uses shared source. The P2 carrier records and verifies that the relevant workflows are discoverable and command-wired; it does not claim host-runtime parity.

## Evidence ceiling

This carrier advances the following features only to `CommandWired`:

- `shell.start` — Start Center / launcher-facing entry surface.
- `shell.workspace` — active-document QS3D workspace shell.
- `shell.ribbon` — QS3D-owned Ribbon/topbar navigation.
- `project.setup` — active-document project bootstrap/setup entry.
- `project.zone` — Zone semantic mutation workflow.
- `project.floor` — Floor/Level semantic mutation workflow.
- `project.family` — Family semantic mutation workflow.

`docs/BLT3D-PARITY-MANIFEST.tsv` remains `# catalog-complete=false`. P2 does not set any of these rows to `V25V26ParityPass`, and it does not advance `bim.authoring`; those are later qualification/domain phases.

## Canonical workflow bindings

`ParityShellProjectNavigationCatalog` reuses the existing `FeatureId` and `ParityWorkflowRegistry` contracts.

- `shell.start`: `Infrastructure`, `Ui | Launcher`, no project precondition.
- `shell.workspace`: `ReadOnly`, `Ui`, requires `ActiveDocument`.
- `shell.ribbon`: `ReadOnly`, `Ui`, no semantic precondition.
- `project.setup`: `Infrastructure`, `Ui`, requires `ActiveDocument`; bootstrap may precede creation of semantic Project state.
- `project.zone`, `project.floor`, `project.family`: `SemanticMutation`, `Ui`, and exactly `ActiveDocument | Project | AtomicMutation | Audit`.

No MCP surface is claimed by P2 unless a later carrier supplies direct evidence for that route.

## Existing production authorities

P2 reuses the current implementation rather than creating screenshot-only duplicates:

- BricsCAD owns the native viewport, DWG, selection, document lifecycle, and native transactions.
- QS3D's existing Ribbon initialization/topbar contract owns the domain navigation surface.
- QS3D's existing workspace owns Project/Zone/Floor context, model/category navigation, Family controls, properties, and footer context.
- Existing project/floor/zone/family services remain the semantic mutation authorities.

## Baseline source guards

Before P2 implementation, the following twelve existing guards passed and remain required evidence:

1. `scripts/preflight-blt3d-bim-workspace.py`
2. `scripts/preflight-blt3d-workspace.py`
3. `scripts/preflight-project-ribbon-actions.py`
4. `scripts/preflight-project-setup-floor-reference.py`
5. `scripts/preflight-family-manager-qs-quick-workflow.py`
6. `scripts/preflight-workspace-family-command-affinity.py`
7. `scripts/preflight-workspace-footer-context.py`
8. `scripts/preflight-workspace-document-context.py`
9. `scripts/preflight-project-floor-zone-canonical-reference.py`
10. `scripts/preflight-project-floor-zone-mutation-integrity.py`
11. `scripts/preflight-family-level-manager-publication.py`
12. `scripts/preflight-family-level-manager-single-instance-veto-safe.py`

The P2-specific `scripts/preflight-blt3d-parity-p2-shell-project-navigation.py` cross-checks these evidence names against the manifest, workflow catalog, and parity smoke registration points.

## Failure policy

If a required manifest row, workflow binding, safety requirement, smoke assertion, or baseline evidence reference disappears, the P2 preflight fails closed. Presentation changes must not weaken semantic mutation requirements or convert the native BricsCAD viewport into a QS3D-owned fake renderer.

## Closure

P2 can be considered source-contract complete only when its focused guard, all twelve baseline guards, Core build, deterministic smoke suite, and protected exact-head CI pass. Overall BLT3D parity remains open because the manifest is intentionally incomplete and later P3–P13 phases still own domain and host qualification.