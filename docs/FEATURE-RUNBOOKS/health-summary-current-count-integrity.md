# HealthSummary Current-time Count integrity

## Scope

Core-only deterministic diagnostics integrity for `HealthSummary(IEnumerable<ModelHealthIssue>)`. Licensed BricsCAD runtime is not applicable.

## Invariant

When the source exposes a caller-known Count through any supported collection interface, HealthSummary binds the admitted Count channels and requires them to remain stable around every caller-controlled traversal boundary. The required ordering for each retained row is:

`Count rebound -> MoveNext -> Count rebound -> overrun/public bound -> Current -> Count rebound -> retain`

The post-`Current` rebound is mandatory because a hostile enumerator can mutate or transiently falsify Count while producing `Current`, then restore it before the next loop check. No issue may be retained from such a traversal.

Pure streaming `IEnumerable<ModelHealthIssue>` inputs that expose no Count remain single-pass and supported. Existing rejection of negative, conflicting, over-limit, under-yield and over-yield Count evidence remains unchanged.

## Deterministic qualification

Run:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-health-summary-current-count-integrity.py
```

Expected hostile regression: a source whose `Current` causes one Count channel to transiently report `2` while the admitted Count is `1` must fail before the issue is retained. A stable counted source must still produce exactly one issue.
