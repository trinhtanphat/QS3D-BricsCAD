# Work claim — Quantity Settings backup recovery

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-recovery`
- Registered: `2026-08-11T20:56:30+07:00`
- Completed: `2026-08-11T21:08:00+07:00`
- Baseline main SHA: `237e6ebbcfaa90b2465e172208bcc812ff95ccab`
- Priority: continue whole-repository hardening with a verified, currently unclaimed persistence defect.

## Reserved scope

Fix Quantity Settings local-file recovery so the `.bak` produced by atomic replacement can restore a missing or unreadable/corrupt primary settings file without changing quantity-calculation semantics or weakening the existing schema validation/normalization contract.

## Completed changes

- `f99f8782245938d3ef09c7eb81c937533b1e37b6` — connected `Load()` to the `.bak` already produced by atomic replacement and centralized backup-path construction so write/read recovery cannot drift to different filenames.
- `b92023b25cdd45c89859aee8bb3e243cd8057b61` — preserved the existing schema-hardening invariant: an unsupported future-schema primary remains fail-closed instead of silently falling back to an older backup. Missing/corrupt ordinary primary state can still recover through the same `ReadAndValidate(...)` path used for normal settings reads.
- `de0efacf5d3a3060aee95596f42ddf1133f0de49` — added auto-discovered `scripts/preflight-quantity-settings-recovery.py`, guarding primary-first load ordering, validated backup fallback, common atomic backup naming, and future-schema fail-closed behavior.

## Validation evidence

- Re-read `QuantitySettingsStore.cs` from current `main` after the implementation commits; primary and backup both route through `ReadAndValidate(...)`, and the atomic writer consumes the same `GetBackupPath(...)` helper as the loader.
- Re-read `QuantityCalculationSettings.NormalizeAndValidate()` and the completed schema-hardening claim before finalizing recovery behavior; `SchemaVersion > CurrentSchemaVersion` remains an explicit incompatibility and is not hidden by backup fallback.
- Re-read `scripts/preflight-quantity-settings-recovery.py`; the aggregate `preflight-all.py` contract auto-discovers `preflight-*.py` guards, while the repository-health gate parses Python tooling for syntax.
- Attempted an independent local checkout/preflight run, but the available container could not resolve `github.com`, so no local execution PASS is claimed.
- GitHub reports no combined status checks and no automatic workflow runs for the regression commit, consistent with manual-only CI. No workflow dispatch was performed.
- No licensed BricsCAD V25 runtime/private DWG/proprietary managed assemblies were available; runtime qualification is not claimed.

## Coordination / exclusions respected

No edits were made to active Core quantity provenance, semantic capture/bootstrap, updater/release/package/signing/install, Core mutation atomicity, Ribbon or quantity-calculation semantics/UI lanes.

## Result

The previously unused atomic settings backup is now a real recovery path for missing/corrupt per-user Quantity Settings, while unsupported future schemas continue to fail closed. Source regression coverage is on `main`; runtime execution remains a separate local V25 qualification gate.
