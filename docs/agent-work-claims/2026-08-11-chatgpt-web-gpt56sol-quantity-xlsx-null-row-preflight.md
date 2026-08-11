# Agent Work Claim — Quantity XLSX null-row preflight

- Agent: ChatGPT Web / GPT-5.6 Sol
- Date: 2026-08-11
- Status: COMPLETE
- Branch/target: direct `main` under current `AGENTS.md` coordination policy

## Scope reservation

This claim reserved only:

- `src/QS3D.Core/Export/XlsxQuantityExporter.cs`
- `tests/QS3D.Core.SmokeTests/XlsxQuantityNullRowSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (shared registry; re-read immediately before update)
- this claim file

## Explicit exclusions preserved

- `ExportEd2` validation/parity semantics were not changed; its detail/summary rows already validate before `ExportCore`.
- No Quantity palette/settings/viewport, Reporting builder, BricsCAD command/UI, rebar exporter, persistence, ProjectInterchange, or other active-agent lane was modified.

## Verified defect and fix

`XlsxQuantityExporter.Export(path, rows)` previously validated only the list reference and then called `ExportCore`. `ExportCore` performs path normalization, destination-directory/temp-package work and later regular `BuildSheet` dereferences each row. A null inner row therefore failed only after filesystem/package work.

Regular `Export` now scans the list immediately after the existing null-list check and before `ExportCore`. The first null entry fails with `ArgumentException`, `ParamName == "rows"`, and a message containing the zero-based row index. `ExportEd2` is unchanged.

## Regression

`XlsxQuantityNullRowSmoke` verifies:

- a null inner quantity row is rejected;
- the exception identifies `rows` and index `0`;
- an existing destination sentinel is preserved;
- a missing output directory is not created, proving failure occurs before filesystem work.

The smoke is registered from the latest shared registry blob.

## Commits on `main`

- Claim registration: `c9d1c6b6228296934c4cd4075eec6547d92caad8`
- Source fix: `210cd279e331a6ce0778624290d1c8c1274af63b`
- Regression smoke: `da909dc854d07379effb62ee164d58b1c6a3442c`
- Smoke registry: `abdff1c6db767fe41a23274c760ab1fe1a175357`

## Validation evidence

- Commit diff for `210cd279e331a6ce0778624290d1c8c1274af63b` shows only the three-line regular `Export` preflight was added; ED2 code was not modified.
- Re-read current `main`: source preflight, regression smoke and `XlsxQuantityNullRowSmoke.Run();` registry call are present.
- `get_commit_combined_status` for `abdff1c6db767fe41a23274c760ab1fe1a175357` returned no automatic statuses/checks; no CI pass is claimed.
- Earlier in this same session, container checkout/runtime execution was blocked before clone by `Could not resolve host: github.com`; no runtime pass or `LOCAL_PASS` is claimed.
- This is CAD-independent Core export code and does not require BricsCAD V25/DWG validation for the source contract.
