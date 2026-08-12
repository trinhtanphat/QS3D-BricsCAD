# Work claim — release #30 Support Bundle privacy preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-support-bundle-privacy-preflight`
- Registered: `2026-08-12T09:34:00+07:00`
- Baseline main SHA: `4721cc060f242edc67e4d2ec14cb2981ce8e6f60`
- Priority: QS3D Cloud V25 Preview Build & Release #30 fails an older Support Bundle privacy gate after atomic publication replaced direct `File.WriteAllLines`; the stale split boundary now misclassifies local `ex.Message` UI/error reporting as exported bundle content.

## Reserved scope

Reconcile only `scripts/preflight-support-bundle-privacy.py` with the current `PublishSupportBundle(...)` atomic publication boundary. Preserve `SupportBundleCommands.cs` production behavior and the newer Support Bundle atomic/read-only/privacy gates unchanged.

## Canonical evidence

- Current `SupportBundleCommands.ExportSupportBundle()` builds only aggregate/version/privacy-safe `lines`, then calls `PublishSupportBundle(dialog.FileName, lines)` and `FinalizeSupportBundleUi(...)`.
- `PublishSupportBundle` writes a same-directory temp through `StreamWriter`, flushes both writer and file stream durably, then `File.Replace`/`File.Move`s the final destination and cleans leftovers.
- Local command/UI exception reporting uses `ex.Message` after/beyond the bundle-content construction boundary and does not append it to `lines`.
- Run #30 passes the adjacent atomic-publish/read-only/current privacy gates; the older gate fails because it still searches for direct `File.WriteAllLines(dialog.FileName, lines, ...)` and uses that removed call as its privacy split marker.

## Expected surfaces

- `scripts/preflight-support-bundle-privacy.py`
- this claim file for close-out

## Excluded scope

- No edits to `SupportBundleCommands.cs`, support diagnostics docs, atomic writer, privacy fields or UI behavior.
- No weakening of sensitive-input prohibitions or drawing-fingerprint presence-only rule.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Replace the obsolete direct-write requirement with `PublishSupportBundle(dialog.FileName, lines);` and pin the atomic helper signature/writer/flush/replace/move tokens.
- Define the exported-content privacy boundary as source before `PublishSupportBundle(dialog.FileName, lines);`, not before a removed `File.WriteAllLines` call.
- Keep all sensitive/raw input prohibitions on bundle-construction code, including environment identity, DWG paths, project metadata, handles, file reads and raw exception text.
- Keep drawing fingerprint presence-only validation.
- Require local `ex.Message` reporting after the publication call so export failures can still be surfaced without entering bundle content.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active reservation for this Support Bundle privacy preflight.

## Completion condition

The legacy privacy gate follows the current atomic publication boundary without weakening privacy protections, is pushed to `main`, and this claim is closed with exact evidence.
