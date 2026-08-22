# Work claim — release #30 ED2 unit read-only preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release30-ed2-unit-readonly-preflight`
- Registered: `2026-08-12T09:56:00+07:00`
- Completed: `2026-08-12T09:58:00+07:00`
- Baseline main SHA: `dae1e356c251edb699f28f3434468385d8a55c81`
- Claim commit: `aac749a812541108246233fc74aa0663eaf35108`
- Implementation commit: `3096b69658eaf9edcf7ee7dad833fa91911b2777`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reported two ED2 unit/read-only failures after ED2 adopted pre-save detached validation and DrawingUnitWorkflow generalized read-only quantity preparation across ED2 and BQ.

## Completed scope

Reconciled only `scripts/preflight-ed2-unit-export-readonly.py` with the current ED2 command ordering and shared `readOnlyQuantityPreparation` unit contract. Commands/DrawingUnitWorkflow production source remained unchanged.

## Implemented gate contract

- ED2 must resolve an existing project read-only, then run `DrawingUnitWorkflow.EnsureResolved`, then detached report/live-handle validation, then SaveFileDialog/confirmation, then the XLSX write.
- ED2 command section explicitly fails if it regains `GetOrCreate` or a mutation-context bind, and fails if XLSX write appears before confirmation.
- DrawingUnitWorkflow must retain ED2 and BQ read-only markers plus `readOnlyQuantityPreparation = readOnlyExportPreparation || readOnlyBqPreparation`.
- Resolved unit handling may persist legacy binding only when `!readOnlyQuantityPreparation`.
- Unresolved read-only quantity preparation must return false before `PromptAndPersist` and may not contain GetOrCreate/Save/Touch.
- ED2-specific unresolved unit guidance remains inside the shared read-only guard.

## Validation performed

- Verified claim commit `aac749a812541108246233fc74aa0663eaf35108` remained an ancestor of moving `main`; the intervening change was unrelated EntitySnapshot metric validation.
- Re-fetched the exact gate immediately before implementation.
- Read current Commands ED2 section and DrawingUnitWorkflow before changing the gate.
- Implementation commit `3096b69658eaf9edcf7ee7dad833fa91911b2777` is on `main`.
- No production source was changed.
- No GitHub Actions/build/release dispatch was performed and no BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed. The ED2 unit gate now matches existing-project-first/pre-save-validation and shared ED2+BQ read-only unit semantics without weakening non-mutation guarantees, and this reservation is released.
