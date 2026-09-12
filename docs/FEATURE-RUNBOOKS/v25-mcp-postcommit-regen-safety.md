# V25 MCP post-commit Regen safety (#6530)

## Live failure evidence

Official `v0.2.0-preview.6` on BricsCAD V25.2.10 terminated during authenticated `qs3d_place_single_footing` on a disposable DWG. Windows Error Reporting recorded `System.AccessViolationException` / `0xc0000005` in `SpaModeler_25.9_16.tx`, with the managed stack entering native `ads_regen()` from `Editor.Regen()` through `CadPostCommitUi.TryRegen`.

The footing call reached post-commit UI refresh from `McpDiagnosticHub.InvokeInCadContext`, which uses `DocumentCollection.ExecuteInApplicationContext`. A managed `catch (Exception)` cannot make a native access violation process-safe.

## Invariant

`CadPostCommitUi.TryRegen` must never call synchronous `Editor.Regen()` while `Application.DocumentManager.IsApplicationContext` is true. The helper returns before native refresh in application context, preserving already committed CAD/project state. Normal BricsCAD command context retains the existing synchronous Regen and bounded diagnostic fallback.

No Single Footing geometry, persistence, transport, installer, or UI-layout behavior is changed by this fix.

## Deterministic verification

```powershell
python scripts/preflight-v25-mcp-postcommit-regen-safety.py
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release
```

## LOCAL_ONLY licensed verification

Use a disposable rooted DWG with a valid persisted QS3D context and active Móng đơn Family. Call `qs3d_place_single_footing` with `confirmMutation=true` and a stable `actionId`; verify the call returns without terminating BricsCAD, inspect the created semantic/native geometry, QSAVE, restart the same DWG, and confirm context plus geometry persistence. Recheck tunnel readiness and MCP `tools/list` after restart.
