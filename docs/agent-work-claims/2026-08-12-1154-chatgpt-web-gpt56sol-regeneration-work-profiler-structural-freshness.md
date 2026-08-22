# Work claim — Regeneration work profiler structural freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-work-profiler-structural-freshness`
- Registered: `2026-08-12T11:54:00+07:00`
- Completed: `2026-08-12T11:58:00+07:00`
- Baseline main SHA: `d1abc79155f9c4459791bca4103e236fc96e95c2`
- Priority: P2 — reject caller-controlled subset enumeration that structurally changes project element ownership without advancing `ChangeVersion`.

## Confirmed defect

`RegenerationWorkProfiler.ProfileSubset(...)` captured `ProjectState.ChangeVersion` before enumerating caller-controlled `elementIds` and rechecked it afterward. Direct mutations of `project.Elements` do not necessarily advance `ChangeVersion`. A lazy target enumerable could remove or replace a project element (including same-ID replacement) while yielding target ids; the existing version check passed and the profiler continued using the mutated ownership set.

## Delivered contract

- Snapshot unique project element ID -> instance ownership immediately before caller-controlled target enumeration.
- Keep the existing `ChangeVersion` freshness check.
- After enumeration and semantic-version validation, reject count/null/duplicate/remove/replace drift before empty-subset handling or candidate planning.
- Stable lazy subset profiling remains unchanged.
- No public API signature changes.

## Evidence

- Claim: `ebe84ffe1de0d5db52f9143a67885259d534875a`
- Plan: `d1e9dc185fdeef5665dd9f12c648431e6e1c00b8`
- Source fix: `1bfff2b62f61a6dc9bf66db7d133f1c62b1e73d2`
- Focused smoke: `04756d7556e600889610bea7d70e68a74482c7eb`
- Smoke registration: `89e41ca326767375a6c1aead36bd39ab7689c71b`
- Static preflight: `6183effd2fb88f0d1768cf79313d1a122a95b74b`

Readback on current `main` confirmed ownership snapshot before caller enumeration, semantic-version check followed by structural ownership recheck before the empty-subset path, same-ID replacement/removal smoke coverage, and the static preflight after concurrent writes.

## Validation limits

The GitHub connector session did not execute the Core smoke executable, Python preflight, GitHub Actions, or licensed BricsCAD runtime. No PASS is claimed for those execution environments.

## Excluded scope

- `DependencyImpactPlanner` structural freshness, already completed separately.
- `RegenerationEngine` / `RegenerationPreviewService` target freshness.
- Regeneration execution, UI, GitHub Actions, or licensed BricsCAD runtime qualification.
