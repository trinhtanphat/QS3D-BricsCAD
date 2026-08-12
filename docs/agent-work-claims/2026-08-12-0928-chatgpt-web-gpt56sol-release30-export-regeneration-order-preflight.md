# Work claim — release #30 export regeneration order preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-export-regeneration-order-preflight`
- Registered: `2026-08-12T09:28:00+07:00`
- Baseline main SHA: `ed2448e545ffaf43422afe57bf02ba007cc2da64`
- Priority: QS3D Cloud V25 Preview Build & Release #30 still fails `preflight-export-regeneration-project-context.py` for BBS CSV because this aggregate gate assumes every schedule export confirms Save before read-only validation, while the canonical BBS CSV contract now validates exportability first and writes only after confirmation.

## Reserved scope

Reconcile only `scripts/preflight-export-regeneration-project-context.py` so BBS CSV follows its current pre-save-validation contract while Curtain/Door/Material/Room exports retain their existing destination-first ordering. Preserve all product source unchanged.

## Canonical evidence

- Run #30 passes `preflight-bbs-csv-existing-project.py`, `preflight-bbs-csv-export-freshness.py` and the shared export side-effect gate under the BBS CSV lifecycle: existing read-only project -> detached snapshot -> regeneration/build/aggregate validation -> Save confirmation -> persistent export.
- `preflight-export-regeneration-project-context.py` still applies one global order `dialog cancel -> read-only lookup -> detached copy -> regenerate` to BBS CSV and the other schedule exporters.
- Current BBS CSV source already preserves no-create/no-mutation detached regeneration and write-after-confirmation.

## Expected surfaces

- `scripts/preflight-export-regeneration-project-context.py`
- this claim file for close-out

## Excluded scope

- No edits to BBS CSV, Curtain, Door/Opening, Material Usage or Room Finish product sources.
- No change to exporter behavior, Save dialogs, detached regeneration, project binding or post-export UI.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Keep requiring existing read-only project lookup, detached snapshot and detached-only regeneration for every export.
- Keep forbidding `ExistingProjectMutationContext` and `RegenerateDirty(project)` for every pure export.
- For BBS CSV only, require read-only lookup -> detached copy -> regeneration before Save confirmation and ensure the actual exporter call remains after confirmation.
- For Curtain/Door/Material/Room keep existing Save confirmation -> read-only lookup -> detached copy -> regeneration ordering.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active reservation for this export-regeneration preflight. This is separate from completed #29 BBS/export-order claims and does not reopen product source.

## Completion condition

The aggregate export-regeneration gate agrees with each current exporter lifecycle without weakening read-only/detached/write-after-confirmation safety, is pushed to `main`, and this claim is closed with exact evidence.
