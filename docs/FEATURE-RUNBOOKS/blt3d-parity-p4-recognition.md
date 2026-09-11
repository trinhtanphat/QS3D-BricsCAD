# BLT3D parity P4 — Recognition

Issue: `#6402`
Ownership-Key: `blt3d-parity-p4-recognition-commandwired-v1`
Evidence ceiling: `CommandWired`

## Scope

P4 records the existing QS3D Recognition surface in the parity manifest and workflow registry. It reuses the production deterministic Recognition/review/B4D routes; it does not add another recognition engine, duplicate UI, or vendor-specific implementation.

The canonical feature/workflow identity is `recognition`. The host-neutral binding is a UI semantic mutation requiring ActiveDocument, Project, AtomicMutation, and Audit. The manifest remains fail-closed with exact metadata `# catalog-complete=false`.

## Existing source evidence

- `scripts/preflight-recognition-ribbon-parity.py` guards the NHẬN DẠNG command surface and routes to production Recognition workflows.
- `scripts/preflight-recognition-atomic-batch.py` guards preflight-first, live-gated, all-or-nothing Recognition apply.
- `scripts/preflight-review-recognition-project-lifecycle.py` guards project identity, generated-handle filtering, and mutation rebind behavior.
- `scripts/preflight-recognition-known-count-integrity.py` guards bounded hostile/counted Recognition input traversal.
- `scripts/preflight-b4d-recognition-mass.py` guards B4D live revalidation, generated-source exclusion, and atomic batch semantics.

## Evidence boundary

This carrier proves source-level command wiring only. It does not claim `SemanticBehaviorPass`, `SaveReopenPass`, or `V25V26ParityPass`; those require later phase/runtime evidence on exact integrated SHAs. It also does not mark the catalog complete.

## Verification

Run the new P4 source guard plus the five existing guards above. Then run repository-health, deterministic Core smoke, protected PR `preflight` + `core`, and V25 compile-reference/plugin build before merge.