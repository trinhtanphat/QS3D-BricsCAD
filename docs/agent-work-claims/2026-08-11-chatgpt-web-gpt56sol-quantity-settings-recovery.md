# Work claim — Quantity Settings backup recovery

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-recovery`
- Registered: `2026-08-11T20:56:30+07:00`
- Baseline main SHA: `237e6ebbcfaa90b2465e172208bcc812ff95ccab`
- Priority: continue whole-repository hardening with a verified, currently unclaimed persistence defect.

## Reserved scope

Fix Quantity Settings local-file recovery so the `.bak` produced by atomic replacement can restore a missing or unreadable/corrupt primary settings file without changing quantity-calculation semantics or weakening the existing schema validation/normalization contract.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Services/QuantitySettingsStore.cs`
- a narrowly scoped Quantity Settings recovery preflight/regression guard under `scripts/` when no unclaimed existing gate covers this behavior
- this claim file

## Excluded scope

- Core quantity report/source-provenance work
- quantity calculation semantics, quantity UI, Ribbon or command redesign
- semantic capture/bootstrap lifecycle
- updater/release/package/signing/install work
- Core mutation/persistence atomicity lanes owned by other agents
- licensed BricsCAD V25 runtime qualification or private-DWG evidence

## Validation plan

- re-read latest `main` before implementation and preserve concurrent changes
- route primary and backup through the same settings deserialization and normalization/validation path
- add a source regression gate proving backup fallback is retained
- rely on aggregate repository Python syntax discovery for the new guard
- inspect resulting commit/status evidence; do not dispatch manual GitHub Actions

## Completion condition

Verified backup-recovery defect is fixed and regression-covered in source, changes are pushed to `main`, validation limitations are recorded, and this claim is marked `COMPLETED`.
