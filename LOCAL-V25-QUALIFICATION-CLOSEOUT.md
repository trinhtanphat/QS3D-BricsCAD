# Licensed BricsCAD V25 runtime qualification closeout

This document is the machine-checkable closeout layer for `docs/LOCAL-V25-QUALIFICATION.md` and `LOCAL-001`.

It does **not** turn source/static review into native runtime evidence. `licensedV25RuntimeQualified=true` is valid only after a real interactive Windows x64 run on licensed BricsCAD V25 using the exact Git SHA and exact built plugin DLL whose SHA-256 is recorded in the evidence.

## 1. Prepare the sanitized matrix evidence

Copy `docs/LOCAL-V25-INTERACTIVE-MATRIX.example.json` to a local artifact directory outside tracked source, for example:

```powershell
Copy-Item .\docs\LOCAL-V25-INTERACTIVE-MATRIX.example.json `
  .\artifacts\local-v25-qualification\interactive-matrix.json
```

Run the complete interactive/private-DWG matrix in `docs/LOCAL-V25-QUALIFICATION.md` on the candidate SHA. Populate the local JSON only from observed runtime results.

Required scenario families are:

- plugin shell/UI;
- DemandLoad;
- Direct Draw;
- Build3D/generated-host ownership Health;
- Door/Opening;
- Room/HT_PHÒNG;
- Curtain;
- rebar;
- project save/reopen/multi-DWG;
- modeless multi-DWG lifetime;
- modeless editor rollback/post-commit isolation;
- BQ/BBS/Excel reporting;
- Unicode/HiDPI;
- clean install/upgrade/uninstall;
- representative private-DWG regression.

Every family must be exactly `PASS`. `FAIL`, `NOT_TESTED`, missing fields, non-empty `knownBlockers`, non-V25 identity, false licensed/interactive/x64 attestations, SHA mismatch or plugin hash mismatch all fail closed.

Do not put raw machine paths, customer/private drawing names, ProjectIds, handles, fingerprints, proprietary DLL contents, screenshots, credentials or secrets in the JSON.

## 2. Close the exact-SHA qualification

From a clean checkout of the same candidate SHA, with every BricsCAD process closed before the run:

```powershell
.\scripts\complete-local-v25-qualification.ps1 `
  -BricsCadDir "C:\Program Files\Bricsys\BricsCAD V25 en_US" `
  -Profile "QS3D-V25-TEST" `
  -InteractiveMatrixEvidence ".\artifacts\local-v25-qualification\interactive-matrix.json"
```

The closeout wrapper first runs the existing automated exact-SHA source/build/WPF/licensed NETLOAD runtime gate. It then validates the interactive evidence against the exact SHA and SHA-256 of the DLL produced by that run.

A successful closeout updates local `artifacts/local-v25-qualification/qualification.json` with:

```text
fullInteractiveMatrixStatus = PASS
licensedV25RuntimeQualified = true
interactiveMatrixEvidenceSha256 = <sha256>
qualificationScope = ...+full-interactive-matrix
```

The wrapper does not expose `-SkipRuntime`; there is no static/source-only path to the qualified state.

## 3. Package/signing distinction

`licensedV25RuntimeQualified=true` means the licensed V25 runtime qualification itself is complete for the exact tested SHA/plugin.

Stable signed customer-release qualification remains a stricter gate. The wrapper keeps `customerReleaseQualified=false` unless the same closeout invocation also successfully builds the exact package and completes approved Authenticode signing/finalization with `-Package -SignPackage`.

Do not commit certificates, signing material, proprietary BricsCAD binaries or local package artifacts.

## 4. Promotion rule

Only after `scripts/complete-local-v25-qualification.ps1` prints `LICENSED V25 RUNTIME QUALIFICATION: PASS` may the matching `LOCAL-001` entry be changed from `IN_PROGRESS` to `PASS`, and only for the exact SHA/plugin hash represented by that successful evidence.

If any source fix is required after a runtime failure, the candidate SHA changes. Rebuild, rerun the affected runtime scenarios and regenerate the matrix evidence; evidence from an older SHA must not be reused.
