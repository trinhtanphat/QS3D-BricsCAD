# Work claim — Diagnostic summary Unicode integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:14:00+07:00`
- Baseline main SHA: `627cd87ed4b5191f6664c3de6ea56491e18a32cd`
- Priority: evidence-driven remote-safe diagnostic export integrity

## Reason

`ProjectDiagnosticSummaryExporter` writes its JSON with `new UTF8Encoding(false)`, whose default replacement fallback silently substitutes malformed UTF-16 such as unpaired surrogates. Its custom JSON `Escape` helper also passes surrogate code units through unchanged. `ModelHealthIssue` intentionally accepts arbitrary diagnostic code text, so a malformed issue code can produce a summary string containing an unpaired surrogate and an exported file containing replacement-character bytes rather than the source diagnostic identity.

## Intended scope

Fail closed on malformed Unicode before diagnostic JSON is produced or persisted, while preserving valid supplementary Unicode, existing JSON escaping, privacy-safe output, counts/grouping, atomic replacement and UTF-8 without BOM.

## Changed surfaces

- `src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs`
- focused smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual supported runtime execution.
