# Work claim — Workspace canonical generated-owner selection

- Status: `COMPLETED`
- Agent: `codex-issue982-workspace-curtain-selection-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T17:00:00+07:00`
- Baseline `origin/main`: `fa79e2e5e7798cb9299bb43f21c3745d8de65507`
- Issue: `#982`
- Priority: P0 source blocker for the pending `LOCAL-002 / P10` generated Curtain panel → Workspace/Family review cell.

## Evidence

`QS3DINSPECT` forwards selected CAD snapshots to `WorkspacePanel.TryResolveSemanticSelection(...)`. That method currently rebuilds an ownership index from `SemanticReferenceHandles.GetSelectionAliases(...)`, whose generated aliases stop at `GeneratedSolidHandle` and `PhysicalOpeningCutSolidHandle`. The canonical `SemanticHandleOwnershipResolver.Resolve(...)` instead consumes `GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(...)` and already recognizes Curtain panel/frame and future generated owner slots with ambiguity protection. The Workspace path therefore rejects a generated Curtain panel as non-semantic even though the canonical ownership resolver identifies its GlassWall owner.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.MultiSelectionProperties.cs` — replace the duplicate alias scan with the canonical Core ownership resolver while retaining strict one-CAD-reference-per-semantic-owner behavior.
- focused Core regression under `tests/QS3D.Core.SmokeTests/` for generated Curtain panel/frame owner resolution, unknown handles and ambiguous ownership;
- one focused static adapter gate under `scripts/` proving Workspace delegates to the canonical resolver and preserves blank/duplicate/unknown/same-owner refusal;
- `docs/LOCAL-AGENT-INBOX.md` and `docs/CURTAIN-NATIVE-PANELS.md` — source-ready handoff wording only;
- this claim for completion evidence.

## Boundaries

- Do not add Curtain-specific owner keys to `SemanticReferenceHandles`; `GeneratedHandleOwnershipPolicy` remains authoritative.
- Preserve source-handle, generated-host and physical-opening selection behavior.
- Unknown native objects, duplicate CAD handles, ambiguous cross-owner claims and multiple selected CAD references owned by the same semantic element remain fail-closed.
- No Curtain geometry/build/Health mutation, native Undo/Redo fix (`#987`), Source Reconcile/`LOCAL-004`, private/runtime probe, release workflow or GitHub Actions changes.
- Source/static evidence may mark P10 `SOURCE_READY`; exact BricsCAD V25 Workspace/Family/Health/Release qualification remains `PENDING_LOCAL`.

## Validation plan

- focused Core smoke/regression and focused adapter gate;
- strict Core Release build/smoke;
- V25 x64 Release compile only if installed references are accessible;
- aggregate preflight and `git diff --check`;
- fetch/rebase current `origin/main`, review exact diff, push/merge without force-push, close issue `#982`, then mark this claim `COMPLETED` in a separate closeout.

## Completion evidence

- Claim-only PR `#1022` merged first at `5ad78f119b0ca1f7c6400a896942d9416794cc89`; implementation editing began only after that merge.
- Source commit `d5420f96ac13a26c1fb85591013f0acf15748574` merged through PR `#1027` at `5a6de128a7e390e7b54663603f99e1a6ad72d2eb`; issue `#982` closed from the merged PR.
- Exact merged SHA `5a6de128a7e390e7b54663603f99e1a6ad72d2eb`: focused Workspace/Curtain owner-selection and project-lifecycle gates PASS; Core Release build has 0 warnings / 0 errors; deterministic Core smoke reports `ALL PASS`; installed-reference V25 x64 Release compile has 0 warnings / 0 errors; `git diff --check` PASS.
- Exact merged SHA aggregate: 733/737 discovered source-safe preflights PASS. The four failures are pre-existing unrelated runtime/release lanes (`preflight-bricscad-v26.py`, `preflight-customer-release.py`, `preflight-runtime-diagnostics-readonly.py`, `preflight-runtime-diagnostics-truth.py`); this claim did not edit them.
- No BricsCAD runtime was launched and no GitHub Actions were run or inspected. `LOCAL-002 / P10` is `SOURCE_READY / PENDING_LOCAL`: exact interactive V25 Workspace/Family/Health/Release behavior still requires licensed local proof.
