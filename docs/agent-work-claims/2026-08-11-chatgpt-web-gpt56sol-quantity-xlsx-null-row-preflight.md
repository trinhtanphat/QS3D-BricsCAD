# Agent Work Claim — Quantity XLSX null-row preflight

- Agent: ChatGPT Web / GPT-5.6 Sol
- Date: 2026-08-11
- Status: ACTIVE
- Branch/target: direct `main` under current `AGENTS.md` coordination policy

## Scope reservation

- `src/QS3D.Core/Export/XlsxQuantityExporter.cs`
- `tests/QS3D.Core.SmokeTests/XlsxQuantityNullRowSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (shared registry; re-read immediately before update)
- this claim file

## Explicit exclusions

- Do not change `ExportEd2` validation/parity semantics; its detail/summary rows are already validated before `ExportCore`.
- Do not touch Quantity palette/settings/viewport, Reporting builders, BricsCAD commands/UI, rebar exporters, persistence, ProjectInterchange, or other active-agent lanes.

## Verified defect

`XlsxQuantityExporter.Export(path, rows)` validates only the list reference and then calls `ExportCore`. `ExportCore` normalizes the path, creates the destination directory/temp package and calls `BuildSheet`; `BuildSheet` dereferences each row without a null guard. A null row therefore causes incidental failure only after filesystem/package work.

## Plan

1. Fail-fast scan the regular `Export` rows immediately after the existing null-list check and before `ExportCore`.
2. Reject the first null entry with `ArgumentException`, bind it to `rows`, and include the zero-based index.
3. Add a Core smoke proving the exception contract, existing destination preservation, and no missing output directory creation.
4. Register the smoke from the latest shared registry blob.
5. Re-read current `main`, inspect available commit/CI evidence, attempt runtime Core smoke only if the environment can obtain the repo, then close this claim truthfully with exact SHAs.
