# Work claim — Quantity Summary callback error handling

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T14:36:00+07:00`
- Completed: `2026-08-12T15:02:00+07:00`
- Baseline main SHA: `17478cbd951668e7b2234ceaca14aab44dc86947`
- Priority: P1 modeless UI failure containment during owner-requested `continue all`
- Task Key: `V25-QUANTITY-SUMMARY-CALLBACK-ERROR-CONTAINMENT`

## Confirmed defect

`QuantitySummaryWindow` caught callback exceptions but several modeless UI paths concatenated `Exception.Message` directly into `MessageBox` text. Runtime/library exception details could therefore be reflected to users and vary by dependency/runtime instead of presenting a stable local failure boundary.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs`
- `scripts/preflight-quantity-summary-callback-error-containment.py`
- this claim file for close-out

`src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.LocateSelectionFailureGuard.cs` remained explicitly out of scope because its failure-handling lane is separate and already hardened.

## Implemented contract

- Existing Quantity Summary modeless callback exception boundaries remain local.
- Seven callback failure dialogs now use stable local text and no longer append raw `Exception.Message` details: view-mode change, recalculate, column visibility, ED2 launch, Excel locate launch, current-row locate, and XLSX export.
- Existing callback operations, success paths, `QS3D` MessageBox title, warning/error icons, row-resolution semantics, Follow3D/locate behavior, persistence, QSDB, and quantity calculation behavior are unchanged.
- The separate locate-selection failure guard was not modified by this lane.

## Regression / preflight evidence

A focused static preflight was added at `scripts/preflight-quantity-summary-callback-error-containment.py`. It reads only `QuantitySummaryWindow.xaml.cs`, rejects remaining `ex.Message` reflection, and pins the seven stable callback messages plus their handlers.

## Landing evidence

- Claim: `7a1f619181203460db23e64a49460f24b173bb26`
- Source fix: `d3787602078538eb0df537b69e1213fdaf7b9832`
- Source blob from the source-fix commit: `015181cb99e2b399e3857aaed173733b4b3f9b22`
- Static preflight: `a0e4591b20b2416fc47a8b22a97a2e32afd687a0`
- Preflight blob read back from `main`: `997e9c12c0ae18d4c0df3e6152c6a56fbe45d812`

## Validation boundary

Remote GitHub readback confirms the source patch and static preflight are present on `main`. Per the claim plan, the preflight script was committed but not executed in this connector session. No GitHub Actions dispatch, .NET build, Python PASS, or licensed BricsCAD runtime qualification is claimed.

## Completion condition

Completed: source and focused static regression are pushed to `main`, their exact SHAs are recorded, scope remains bounded to Quantity Summary callback failure containment, and this claim is closed.
