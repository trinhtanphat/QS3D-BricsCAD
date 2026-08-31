# RebarSchedule input Count integrity

## Scope

Core-only deterministic integrity for `RebarScheduleBuilder.Build(IEnumerable<RebarScheduleInput>)`. Licensed BricsCAD runtime is not applicable.

## Invariant

When a schedule input source exposes Count through any supported generic, read-only, or non-generic collection interface, all admitted Count evidence is authoritative for the full traversal. For each retained input the required order is:

`Count rebound -> MoveNext -> Count rebound -> known-count overrun / 10,000-row bound -> Current -> Count rebound -> semantic parsing / row retention`

The same Count contract is rebound at terminal traversal and before publication. Negative, oversized, conflicting, under-yield and over-yield evidence fails closed. Pure streaming enumerables without Count metadata remain supported and single-pass.

## Hostile regression

The deterministic smoke supplies counted enumerables that report Count `1` at admission and transiently report `2` on the first Count reread after either `MoveNext` or `Current`. Both must fail before the hostile input is semantically accepted; MoveNext drift must fail with zero `Current` reads. Stable counted and pure-streaming controls must still produce the expected row.

## Qualification

Run:

```text
python scripts/preflight-rebar-schedule-input-count-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Protected acceptance additionally requires current exact-head Shared CI `preflight + core`, latest-main reconciliation, expected-head merge, and exact protected-main verification.