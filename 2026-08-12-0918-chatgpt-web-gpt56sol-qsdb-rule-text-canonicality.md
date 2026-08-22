# Work claim — QSDB QuantityRule text canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-rule-text-canonicality`
- Registered: `2026-08-12T09:18:00+07:00`
- Baseline main SHA: `734cdec416cc507ced64c9d6a8927872deb61a40`
- Regression commit: `0fb1b109701049ab3665d3647305bd81a024c247`
- Completed source commit: `cd398601829106d2e4c1dc9b90398cf21297b14a`
- Readback main SHA before close-out: `63ac503d6ef73cb4aa88b8c1c2cf1c4628356704`
- Priority: P1 deterministic persistence / fail-closed token integrity found during owner-requested `continue all` audit.

## Confirmed defect

`QuantityRule` has canonicalized all required textual fields with `Trim()` since at least `f497819ad7de4178e42f25c070c38ac77b850412`, and `QsdbProjectStore.Serialize(...)` persists the resulting `id`, `output`, `expression` and `version`. The previous XML validator required canonical `id` and `output` but only allowlisted `expression` and `version`, allowing padded values to be trimmed by the constructor during load and silently rewritten on save.

## Implemented contract

1. `ValidateRules(...)` now requires `expression` through `ValidateRequiredCanonicalAttribute(...)`.
2. `version` is guarded by the same non-empty/no-leading-or-trailing-whitespace contract.
3. Existing `id`, category and output validation remains unchanged.
4. Expression grammar/evaluation, output identity, provenance, preview and version semantics are unchanged.
5. Focused smoke coverage saves a canonical rule, verifies expression/version round-trip, then independently pads persisted `expression` and `version` and requires `Load()` to reject each.

## Verification

- Current-main validator readback confirmed the two new rule-text checks alongside existing id/category/output guards.
- Current-main smoke readback confirmed canonical round-trip and both padded-token cases.
- `cd398601829106d2e4c1dc9b90398cf21297b14a...main` compared as `ahead` with the source commit as merge base; later concurrent work touched unrelated claims only in that comparison window.
- Smoke source was committed but not executed from this remote connector session. Full Core smoke execution/build and GitHub Actions were not run; no PASS is fabricated.
- This is Core persistence work and makes no licensed BricsCAD runtime claim.

## Excluded

- No rule-engine/provenance/preview/UI changes.
- No QSDB names/ids/categories/numeric/timestamp/dirty/changeVersion/migration changes beyond preserving already merged contracts.
- No BricsCAD adapter or installer/release work.
