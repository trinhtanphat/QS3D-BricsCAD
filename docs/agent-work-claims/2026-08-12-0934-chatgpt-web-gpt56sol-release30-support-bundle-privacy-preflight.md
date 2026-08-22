# Work claim — release #30 Support Bundle privacy preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release30-support-bundle-privacy-preflight`
- Registered: `2026-08-12T09:34:00+07:00`
- Completed: `2026-08-12T09:36:00+07:00`
- Baseline main SHA: `4721cc060f242edc67e4d2ec14cb2981ce8e6f60`
- Claim commit: `cba9c2140a986ac1c77f7187817ecacea187a992`
- Implementation commit: `1b05b963375a6f01aac1d80802dc03e8bfeb54e1`
- Priority: QS3D Cloud V25 Preview Build & Release #30 failed an older Support Bundle privacy gate after atomic publication replaced direct `File.WriteAllLines`; the stale split boundary misclassified local `ex.Message` UI/error reporting as exported bundle content.

## Completed scope

Reconciled only `scripts/preflight-support-bundle-privacy.py` with the current `PublishSupportBundle(...)` atomic publication boundary. `SupportBundleCommands.cs` production behavior and newer Support Bundle gates remained unchanged.

## Implemented contract

- Requires the current `PublishSupportBundle(dialog.FileName, lines);` boundary instead of obsolete direct destination writes.
- Pins the atomic helper, UTF-8 StreamWriter, writer/file durable flush, `File.Replace` and `File.Move` paths.
- Applies all sensitive/raw-input prohibitions only to bundle construction before the publication call.
- Keeps drawing fingerprint presence-only validation.
- Requires local `ex.Message` reporting after publication is attempted, so failures remain user-visible without becoming bundle content.
- Fails if bundle `lines` are appended after the publication call and before the atomic helper definition.

## Validation performed

- Verified claim commit `cba9c2140a986ac1c77f7187817ecacea187a992` remained an ancestor of moving `main`; intervening work was unrelated Selection Inspector/Level smoke coverage.
- Re-fetched the exact privacy gate before implementation and read it back from `main` afterward at blob `b0c6fef1ba63a0335b642c114de0f0e1dc5b3a3b`.
- Reviewed current `SupportBundleCommands.cs`: privacy-safe aggregate `lines` are finalized before `PublishSupportBundle`, while raw exception messages are only used by local reporting afterward.
- No production source was changed.
- No GitHub Actions/build/release dispatch was performed and no BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed. The legacy privacy gate now follows the current atomic publication boundary without weakening privacy protections, and this reservation is released.
