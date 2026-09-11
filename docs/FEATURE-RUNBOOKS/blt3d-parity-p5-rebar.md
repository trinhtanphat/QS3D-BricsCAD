# BLT3D parity P5 — Rebar

Issue: `#6408`
Ownership-Key: `blt3d-parity-p5-rebar-commandwired-v1`
Evidence ceiling: `CommandWired`

## Scope

P5 records the existing QS3D Rebar surface in the parity manifest and workflow registry. It reuses current production Rebar/BBS workflows as read-only evidence and does not modify their command, geometry, schedule, export, or UI implementation.

The canonical feature/workflow identity is `rebar`. The host-neutral binding is a UI semantic mutation requiring ActiveDocument, Project, Selection, AtomicMutation, and Audit. The manifest remains fail-closed with exact metadata `# catalog-complete=false`.

## Existing source evidence

- `scripts/preflight-rebar-hub.py` guards the existing Rebar 3D command/hub surface and publication behavior.
- `scripts/preflight-generated-rebar-atomicity.py` guards rollback-capable, all-or-nothing generated Rebar mutations.
- `scripts/preflight-generated-rebar-audit.py` guards canonical audit events for generated Rebar/mesh replacement families.
- `scripts/preflight-rebar-selection-project-lifecycle.py` guards admitted selection and project binding order.
- `scripts/preflight-rebar-bbs-provenance.py` guards semantic Rebar provenance through BBS export paths.
- `scripts/preflight-rebar-schedule-export-active-guard.py` guards active-DWG ownership and current-row rebuild before export.

## Evidence boundary

This carrier proves source-level command wiring only. It does not claim `SemanticBehaviorPass`, `SaveReopenPass`, or `V25V26ParityPass`. It does not claim fabrication-grade detailing or structural-code compliance, and it does not mark the catalog complete.

## Verification

Run the new P5 source guard plus the six existing guards above. Then run repository-health, targeted parity smokes, deterministic Core smoke, protected PR `preflight` + `core`, and V25 compile-reference/plugin build before merge.
