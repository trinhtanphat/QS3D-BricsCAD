# Work claim — release #30 export regeneration order preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release30-export-regeneration-order-preflight`
- Registered: `2026-08-12T09:28:00+07:00`
- Completed: `2026-08-12T09:30:00+07:00`
- Baseline main SHA: `ed2448e545ffaf43422afe57bf02ba007cc2da64`
- Claim commit: `35e8c4f091539e5f4dbfed6595c1f110eea5eac5`
- Implementation commit: `f96cf5324be620cf464270a64e4d7ef7dc9a87f2`
- Priority: QS3D Cloud V25 Preview Build & Release #30 still failed `preflight-export-regeneration-project-context.py` for BBS CSV because this aggregate gate assumed every schedule export confirmed Save before read-only validation, while the canonical BBS CSV contract validates exportability first and writes only after confirmation.

## Completed scope

Reconciled only `scripts/preflight-export-regeneration-project-context.py`. BBS CSV now follows its current pre-save-validation contract while Curtain/Door/Material/Room retain their existing destination-first ordering. Product source was unchanged.

## Implemented contract

- Every schedule export must still require an existing project read-only, create a detached snapshot and call detached `RegenerateDirty(snapshot)`.
- Every pure export still fails if it binds `ExistingProjectMutationContext` or regenerates the live project.
- BBS CSV requires read-only lookup -> detached copy -> regeneration/validation -> Save confirmation -> `RebarCsvExporter.Export(...)`.
- BBS CSV explicitly fails if a persistent CSV exporter call appears before confirmation.
- Curtain/Door/Material/Room continue to require Save confirmation -> read-only lookup -> detached copy -> regeneration.

## Validation performed

- First claim creation raced moving `main` and returned 409; no force/overwrite was used. Current HEAD was refreshed and the claim was created successfully.
- Verified claim commit `35e8c4f091539e5f4dbfed6595c1f110eea5eac5` remained an ancestor of moving `main`; the intervening commit at that check was unrelated Grid work.
- Re-fetched the preflight immediately before implementation and read it back from `main` afterward at blob `fee26c762760914da04fffdc3b30547f00962562`.
- No product source was changed.
- No GitHub Actions/build/release dispatch was performed and no BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed. The aggregate export-regeneration gate now agrees with each current exporter lifecycle without weakening read-only/detached/write-after-confirmation safety, and this reservation is released.
