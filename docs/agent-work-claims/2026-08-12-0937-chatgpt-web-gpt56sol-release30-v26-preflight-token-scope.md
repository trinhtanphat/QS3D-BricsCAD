# Work claim — release #30 V26 preflight token-scope reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-v26-preflight-token-scope`
- Registered: `2026-08-12T09:37:00+07:00`
- Baseline main SHA: `4658163352e18be52f0fbc3e53d2242571f3ec32`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reports two V26 compatibility failures caused by over-broad/exact text checks while the actual V26 target and host-major runtime diagnostics remain correct.

## Reserved scope

Reconcile only `scripts/preflight-bricscad-v26.py` with the current V26 project/comment and shared runtime-diagnostics wording. Preserve V25/V26 project files and runtime diagnostic production source unchanged.

## Canonical evidence

- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj` targets only `<TargetFramework>net8.0-windows</TargetFramework>`; the bare text `net48` appears only in a comment explaining that the separate V25 product lane remains net48.
- The V26 gate currently forbids bare `net48` anywhere in the project file, so a harmless architecture comment causes failure.
- `RuntimeDiagnosticsCommands` remains compile-selected by `BRICSCAD_V26`, uses `ExpectedRuntimeMajor/ExpectedRuntimeLabel`, validates BrxMgd/TD_Mgd host major, and tells the user to run `QS3DRELEASECHECK plus the licensed ` + ExpectedRuntimeLabel + ` scenario suite`.
- The gate requires an obsolete exact quoted fragment beginning immediately at `licensed`, which no longer exists because the string literal now begins earlier in the sentence.

## Expected surfaces

- `scripts/preflight-bricscad-v26.py`
- this claim file for close-out

## Excluded scope

- No edits to V25/V26 csproj, RuntimeDiagnosticsCommands, runtime probe, release readiness, updater, workflow, qualification docs or package identity.
- No weakening of the required V26 `net8.0-windows` target or V25-only binary/update prohibitions.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Replace bare `net48` prohibition with executable MSBuild target-form prohibitions so comments can mention V25 without allowing V26 to target net48.
- Retain required `<TargetFramework>net8.0-windows</TargetFramework>` and all V25 environment/update identity prohibitions.
- Replace obsolete runtime sentence literal with structural wording tokens that still require `plus the licensed ` and `ExpectedRuntimeLabel + " scenario suite` alongside existing host-major checks.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active reservation for the V26 compatibility preflight.

## Completion condition

The V26 gate validates actual build/runtime semantics rather than harmless comment/string-literal boundaries, retains all host-major safety checks, is pushed to `main`, and this claim is closed with exact evidence.
