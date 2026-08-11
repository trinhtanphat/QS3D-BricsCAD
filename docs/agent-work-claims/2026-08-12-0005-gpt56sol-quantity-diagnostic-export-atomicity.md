# Work claim — Quantity Settings diagnostic export atomicity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-diagnostic-export-atomicity-20260812-0005`
- Registered: `2026-08-12T00:05:00+07:00`
- Baseline main SHA observed: `51622e193c45827bf4b5b56ac738697907c8d7f6`
- Priority: P1 — prevent a failed sanitized health export from truncating a previously valid user-selected diagnostic JSON.

## Confirmed defect

`QuantityCalculationMatrixDiagnosticSnapshotExporter.Save()` currently opens the destination itself with `FileMode.Create` and serializes directly into it. If serialization or the filesystem fails after truncation, an existing diagnostic report is destroyed/left partial even though the new export did not complete.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityCalculationMatrixDiagnosticSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationMatrixDiagnosticSnapshotSmoke.cs`
- `scripts/preflight-quantity-settings-diagnostics-export.py`
- this claim file for close-out

## Contract

- Serialize to a unique temp file in the destination directory first.
- Flush the temp file before publishing it.
- If destination exists, replace it atomically without creating a secondary backup for this disposable diagnostic artifact; otherwise move the completed temp file into place.
- Always best-effort delete the temp file in `finally` when publication fails.
- Keep the public `Write(Stream, snapshot)` method unchanged for in-memory/support callers.
- Preserve the sanitized snapshot schema and all command-level path redaction/read-only behavior.

## Excluded scope

- No Quantity Settings machine-store, WPF, Core rule/deduction/matrix semantics, project persistence, CAD geometry, Ribbon/Start Center, updater/release or GitHub Actions changes.

## Validation plan

- Extend snapshot smoke to exercise `Save()` to a temporary destination and verify the resulting JSON retains expected portable fields while caller state remains unchanged.
- Extend existing diagnostic-export preflight to forbid direct `File.Open(fullPath, FileMode.Create...)` destination writes and require temp-create -> Write -> publish -> finally-cleanup ordering.
- Re-fetch current `main` before implementation/merge and preserve concurrent winners without force push.
- Source/static review only from this remote session; no GitHub Actions/native V25 runtime PASS claim.

## Coordination

The health export, diagnostics path redaction and matrix snapshot claims are completed. Current active recognition/project-save/ownership and other lanes do not own this Core snapshot exporter.

## Completion condition

A failed health snapshot export can no longer truncate an existing destination before successful serialization/publish; regression source is merged to `main` and this claim is marked `COMPLETED` with exact merge evidence.
