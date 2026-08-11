# Work claim — Quantity Settings diagnostic export atomicity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-diagnostic-export-atomicity-20260812-0005`
- Registered: `2026-08-12T00:05:00+07:00`
- Completed: `2026-08-12T00:09:00+07:00`
- Baseline main SHA observed: `51622e193c45827bf4b5b56ac738697907c8d7f6`
- Priority: P1 — prevent a failed sanitized health export from truncating a previously valid user-selected diagnostic JSON.

## Confirmed defect

`QuantityCalculationMatrixDiagnosticSnapshotExporter.Save()` previously opened the destination itself with `FileMode.Create` and serialized directly into it. If serialization or the filesystem failed after truncation, an existing diagnostic report could be destroyed/left partial even though the new export did not complete.

## Delivered scope

- `src/QS3D.Core/Reporting/QuantityCalculationMatrixDiagnosticSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationMatrixDiagnosticSnapshotSmoke.cs`
- `scripts/preflight-quantity-settings-diagnostics-export.py`
- this claim file

## Implemented contract

- `Save()` now creates a unique same-directory temp file with `FileMode.CreateNew`.
- It serializes through the existing public `Write(Stream, snapshot)` API and calls `Flush(true)` before publication.
- Existing destinations are replaced with `File.Replace(temp, fullPath, null, true)` only after successful temp serialization; new destinations use `File.Move(temp, fullPath)`.
- A `finally` block performs best-effort temp deletion and deliberately does not mask the original publish failure.
- The public `Write(Stream, snapshot)` surface and portable sanitized snapshot schema remain unchanged.

## Regression coverage

- Snapshot smoke now saves a real temporary JSON, verifies the first portable payload, saves a second snapshot to the same destination to exercise replacement, verifies the second payload won, and confirms only the final destination remains in the directory.
- Existing diagnostic-export preflight now rejects direct `File.Open(fullPath, FileMode.Create...)` destination writes and pins temp-create -> Write -> durable flush -> replace/move -> finally cleanup ordering.
- Existing command Load -> snapshot -> save-dialog -> exporter ordering and privacy/read-only guards remain in the same preflight.

## Product integration

- Claim registration: `f2baddd0a4680acf0e4d53eb7ef4087686f0df5c`.
- PR: `#557` — `fix(quantity): publish health snapshots atomically`.
- Squash merge on `main`: `8f1300b8178fd99b666ad2de15e6210176068a67`.

## Validation actually performed

- Re-fetched the exporter, smoke and existing export preflight before implementation and preserved their existing sanitized/read-only contract.
- PR #557 was squash-merged without force push while `main` was concurrently advancing.
- Source/static review only in this remote session; the smoke/preflight were not executed from a repository checkout, so no execution PASS is claimed.
- No GitHub Actions or release workflow was dispatched. No licensed BricsCAD V25 runtime PASS is claimed.

## Coordination

No Quantity Settings machine-store, WPF, rule/deduction/matrix semantics, project persistence, CAD geometry, Ribbon/Start Center, updater or release surfaces were modified.

## Completion

Reservation released. A health snapshot is now published only after successful temp serialization/flush, so a failed export cannot truncate an existing destination first.
