# V25 project Save native-commit truth

## Scope
This runbook qualifies the BricsCAD V25 mouse-first `Save` and `Save As` workflows in `ProjectFileUiService`. It covers the irreversible boundary where BricsCAD has already committed the DWG but QS3D sidecar finalization or success presentation later fails.

## Required invariants
1. Capture the exact active `Document` once before mutation and keep using that same document through native save and QS3D sidecar finalization.
2. Set the native-commit classification only after the BricsCAD `Save`/`SaveAs` call returns successfully and before any sidecar/finalization work.
3. Any exception before native commit remains a normal operation failure; any exception after native commit is partial success and must never be reported as if the DWG save rolled back.
4. Post-native-commit reporting is presentation-only. It must not retry, reopen, roll back, dispatch commands, mutate the project, or recapture a different active document.
5. The partial-success message is stable and redacted; raw exception messages, inner exceptions, stack traces and local paths must not be surfaced.
6. The warning presenter is best-effort and contains its own UI exceptions so a MessageBox failure cannot escape after an irreversible native commit.
7. `Save As` must verify that the source document actually moved to the requested DWG path before sidecar relinking is accepted.

## REMOTE_SAFE qualification
Run the auto-discovered guard:

`python scripts/preflight-v25-project-save-native-commit-truth.py`

Then run repository required preflight/core checks and the admitted-reference BricsCAD V25 compile on the exact candidate SHA. Remote green validates deterministic/static contracts and compile compatibility only.

## LOCAL_ONLY qualification
A licensed BricsCAD V25 runtime may inject failures after successful native `Save` and `SaveAs` and confirm that:
- the DWG remains committed;
- the UI reports partial success rather than full failure;
- no rollback/reopen/retry occurs;
- switching active documents does not redirect project finalization or status ownership;
- presenter failure cannot escape the completed native commit boundary.

Do not report `LOCAL_PASS` without executing these scenarios in a real licensed BricsCAD V25 process.
