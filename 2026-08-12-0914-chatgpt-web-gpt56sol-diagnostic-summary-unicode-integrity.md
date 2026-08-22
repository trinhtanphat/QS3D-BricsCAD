# Work claim — Diagnostic summary Unicode integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:14:00+07:00`
- Completed: `2026-08-12T09:18:00+07:00`
- Baseline main SHA: `627cd87ed4b5191f6664c3de6ea56491e18a32cd`
- Priority: evidence-driven remote-safe diagnostic export integrity

## Reason

`ProjectDiagnosticSummaryExporter` wrote its JSON with `new UTF8Encoding(false)`, whose default replacement fallback silently substituted malformed UTF-16 such as unpaired surrogates. Its custom JSON `Escape` helper also passed surrogate code units through unchanged. `ModelHealthIssue` intentionally accepts arbitrary diagnostic code text, so a malformed issue code could produce a summary string containing an unpaired surrogate and an exported file containing replacement-character bytes rather than the source diagnostic identity.

## Changed scope

Diagnostic JSON string values are now preflighted with a shared strict `UTF8Encoding(false, true)` before escaping, and file export uses the same strict encoder. Malformed Unicode fails closed; valid supplementary Unicode remains unchanged. Existing JSON escaping, privacy-safe output, counts/grouping, schema, atomic replacement and UTF-8-without-BOM behavior remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs`
- `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummaryUnicodeSmoke.cs`
- this claim file

## Completion

- Claim commit: `290a907ce88b171c2e28ac1f03ae4cff499ff47a`.
- Implementation commit: `b2aab53e286878438348b831cb32c0fb9ab128f4` — add shared strict UTF-8 validation for JSON string text and use that strict encoder for persisted diagnostic summaries.
- Regression commit: `e8558edf801e462085e4027967ff32397982be1b` — reject malformed high/low surrogate diagnostic codes and preserve valid supplementary Unicode through Build and strict UTF-8 file export.
- Validation actually performed:
  - fetched the implementation commit diff and confirmed only the strict encoder field, export writer encoder and `Escape` preflight changed;
  - re-fetched current exporter source and confirmed strict Unicode validation remains present;
  - re-fetched the dedicated smoke source and checked malformed + valid supplementary Unicode coverage;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25/V26 runtime PASS is claimed.

## Coordination

The recent Diagnostic Summary null-issue and undefined-severity claims were already completed and are disjoint from this Unicode/export-integrity lane. No newer overlapping claim appeared before this scope was reserved.

## Completion condition

Satisfied: current `main` fails closed on malformed Unicode before Diagnostic Summary JSON/export replacement can occur, preserves valid supplementary Unicode, focused regression coverage is present, and this claim is released as `COMPLETED`.
