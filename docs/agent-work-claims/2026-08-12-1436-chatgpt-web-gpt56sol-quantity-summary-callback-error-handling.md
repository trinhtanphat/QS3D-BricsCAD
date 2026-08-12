# Work claim — Quantity Summary callback error handling

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T14:36:00+07:00`
- Baseline main SHA: `17478cbd951668e7b2234ceaca14aab44dc86947`
- Priority: P1 modeless UI failure containment during owner-requested `continue all`
- Task Key: `V25-QUANTITY-SUMMARY-CALLBACK-ERROR-CONTAINMENT`

## Confirmed defect

`QuantitySummaryWindow` catches callback exceptions but several modeless UI paths concatenate `Exception.Message` directly into `MessageBox` text. Runtime/library exception details can therefore be reflected to users and can vary by dependency/runtime instead of presenting a stable local failure boundary.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs`
- `scripts/preflight-quantity-summary-callback-error-containment.py`
- this claim file for close-out

`src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.LocateSelectionFailureGuard.cs` is explicitly out of scope because its failure-handling lane is separate and already hardened.

## Contract

- contain exceptions locally in the existing Quantity Summary modeless callbacks;
- user-facing callback failure text must be stable and must not append raw `Exception.Message` details;
- preserve the existing success paths, callback operations, MessageBox titles/icons and locate-selection failure guard;
- do not broaden into QSDB, persistence, calculation semantics, Follow3D/locate behavior, or unrelated UI refactors.

## Validation plan

Add a focused static regression/preflight source that locks the absence of raw `ex.Message` reflection in the target callback file and confirms the out-of-scope locate failure guard is not part of this source change. The preflight script will be committed but not executed in this connector session. No GitHub Actions dispatch, .NET build, Python PASS, or licensed BricsCAD runtime qualification will be claimed.
