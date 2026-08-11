# Agent work claim — Support Bundle atomic publish

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DSUPPORTBUNDLE` publish its privacy-safe text report atomically without changing bundle contents or read-only/cancel behavior.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/SupportBundleCommands.cs`
  - `scripts/preflight-support-bundle-readonly.py`
  - `scripts/preflight-support-bundle-atomic-publish.py`
  - this claim file
- Contract:
  - keep SaveFileDialog confirmation before project access;
  - keep `ProjectContextCoordinator.TryGetReadOnly` and the existing aggregate/version-only privacy payload;
  - serialize/write to a unique same-directory temp, flush durable bytes, then replace an existing destination or move into a new destination;
  - always clean leftover temp best-effort without masking the original publish error;
  - never truncate/open the selected destination before temp write succeeds;
  - reconcile the existing read-only/privacy preflight so it accepts the atomic publisher while preserving its cancel/read-only/privacy/UI ordering checks;
  - post-write UI remains best-effort;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
