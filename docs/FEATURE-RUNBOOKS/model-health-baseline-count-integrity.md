# Model health baseline Count integrity

## Scope

Core-only deterministic integrity for `ModelHealthBaselineService.Capture(ProjectState, IEnumerable<ModelHealthIssue>)`. Licensed BricsCAD runtime is not applicable.

## Invariant

If a baseline issue source exposes Count through a supported generic, read-only, or non-generic collection interface, the admitted Count remains authoritative for the full traversal. Each retained issue follows:

`Count rebound -> MoveNext -> Count rebound -> known-count overrun / HealthSummary.MaxIssueCount bound -> Current -> Count rebound -> semantic validation -> retention`

Terminal traversal rebounds Count again and verifies exact known-count yield. Negative, oversized and conflicting Count evidence continues to fail before enumeration. Pure streaming sources remain single-pass and supported up to the existing diagnostics ceiling.

## Hostile regression

The deterministic smoke uses a counted source reporting Count `1` at admission and transiently `2` on the first Count reread after either `MoveNext` or `Current`. MoveNext drift must fail before any `Current` read; Current drift must fail before the issue is retained. Stable counted and pure-streaming controls must still capture one issue.

## Qualification

Run:

```text
python scripts/preflight-model-health-baseline-count-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Protected acceptance additionally requires current exact-head Shared CI `preflight + core`, latest-main reconciliation, expected-head merge, and exact protected-main verification.
