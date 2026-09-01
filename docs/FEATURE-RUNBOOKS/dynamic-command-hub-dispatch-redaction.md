# Dynamic command-hub dispatch redaction qualification

Issue: #5233  
Lane-Key: `issue-5233`  
Runtime class: `LOCAL_ONLY` for licensed BricsCAD V25 modeless/UI execution. Remote/static evidence is never `LOCAL_PASS`.

## Source contract

Domain Hub, Geometry Extensions and Rebar 3D Hub are intentionally host-global modeless command launchers. They resolve `MdiActiveDocument` at click time and must not retain a managed `Document` or become bound to the drawing that originally opened the hub.

This carrier hardens the shared dispatch boundary without changing that behavior:

- command success is reported only after `SendStringToExecute` returns;
- Domain Hub and Rebar 3D Hub no longer place caught host/native `Exception.Message` text into modeless status;
- failed dispatch uses a stable command-specific status;
- Editor diagnostics are best-effort, type-only and may not mask the original dispatch failure;
- Geometry Extensions remains the already-redacted reference behavior;
- no failure path may claim the target command itself executed successfully.

Deterministic remote acceptance includes strengthened `scripts/preflight-dynamic-command-hubs.py` and focused `scripts/preflight-dynamic-command-hub-redaction.py`, followed by fresh exact-head protected `preflight` + `core`.

## Licensed V25 matrix — DH01–DH12

Use one exact authorized plugin artifact and a disposable pair of drawings. Record ProductVersion/plugin SHA-256 before launch.

| Cell | Action | Required evidence |
| --- | --- | --- |
| DH01 | Start V25, NETLOAD exact plugin, open drawing A | Exact artifact identity and clean startup |
| DH02 | Open Domain Hub and dispatch a valid command in drawing A | Success only after BricsCAD accepts dispatch; stable status |
| DH03 | Open drawing B, leave hub open, dispatch from Domain Hub | Command targets current active drawing B; no retained-A affinity |
| DH04 | Exercise Domain Hub with no active drawing | Stable no-document status; no escape |
| DH05 | Force controlled Domain Hub `SendStringToExecute` failure | Stable failure text; no raw exception message; type-only Editor diagnostic at most |
| DH06 | Open Rebar 3D Hub and dispatch a valid command | Success only after BricsCAD accepts dispatch |
| DH07 | Switch A→B while Rebar hub remains open and dispatch | Active-document-at-click behavior preserved |
| DH08 | Exercise Rebar hub with no active drawing | Stable no-document status; no escape |
| DH09 | Force controlled Rebar dispatch failure | Stable failure text; no raw exception message; diagnostic best-effort |
| DH10 | Exercise Geometry Extensions success/failure reference path | Existing stable failure and type-only diagnostic remain intact |
| DH11 | Repeat failure then successful dispatch in each hub | Failure does not poison later dispatch or modeless status lifecycle |
| DH12 | Close hubs, QSAVE, close drawings, fresh-process reopen | No retained document/window residue; clean process ownership/cleanup |

## Verdict

`LOCAL_PASS` requires DH01–DH12 on the same exact artifact identity with sanitized evidence and cleanup. Any native/runtime defect is `RUNTIME_FAIL` or `NO_RESULT`; hosted CI cannot be promoted to licensed runtime evidence.
