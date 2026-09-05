# Issue 5829 — SingleFooting Family-scope editor

Lane-Key: issue-5829
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:local022-01a06ce3-20260905
Canonical carrier: agent/local022-01a06ce3-20260905/issue-5829-footing-scope-regeneration
Ownership-Key: single-footing-family-scope-native-regeneration
Base main: `256452879f653c84d5aced82a12474a1e10508dd`

## Outcome and boundary

Family / Type property rendering uses the specialized SingleFooting presenter before generic fallback. It exposes exactly six validated millimeter editors and keeps the existing `SingleFootingRegenerationService.ApplyFamilyDimensions` edit boundary. Generic families, unregistered host-free ViewModels and Instance selection keep their existing dispatch. Invalid specialized dimensions produce read-only diagnostic rows, not editable generic keys.

`BindViewModel` registers the presenter before publishing `DataContext`, including after `ClearProject` replaces the ViewModel. Registration is assignment with delegate equality, not accumulating event subscriptions. Tree/family/Add selection asks the ViewModel for Family scope and lets the same presenter render; the renderer never recursively reloads the ViewModel.

Scope: `WorkspaceViewModel.cs`, `WorkspacePanel.xaml.cs`, the two SingleFooting panel partials, this claim and the focused PowerShell/Python regression pair. No installed package, tunnel, CAD process, licensed harness output or unrelated source changes.

## Evidence

The initial executable regression extracted the unmodified production scope setter and `LoadCurrentProperties` and failed with `Family scope discarded the specialized SingleFooting presenter`. With the fix, the same routing scenario passes.

The completed regression executes actual renderer, row setter, dimension contract and edit callback code with host/document/native-operation doubles. It verifies Family/Instance dispatch, generic and empty fallback, repeated and replacement registration, six editable mm fields, H2 conversion, invalid numeric/taper refusal, missing documents, stale same-ID families on another document, duplicate families, suppressed contexts and read-only malformed-family presentation.

The working-tree candidate compiled for both V25 (`net48`) and V26 (`net8.0-windows`) with zero warnings and zero errors using the existing stable-reference build scripts in this isolated worktree. The pinned platform submodule is `fcf24893aac7fabe11017bbd5ed0072f5becd87d`. The executable scope preflight, existing SingleFooting workspace guard, Add-route guard and footing normalization regression passed. These results precede the owning agent's commit; subsequent package/runtime evidence must name the resulting pushed SHA.

Host-free evidence is not licensed native PASS. The owning LOCAL022 task must run scope-only physical Family selection, H2 edit/regeneration and save/cold-reopen on V25 and V26, bound to the exact pushed source. No parent-tree reselection workaround qualifies scope-switching correctness. Same-selection asynchronous scope resets remain runtime observations to verify, not inferred fixed behavior.
