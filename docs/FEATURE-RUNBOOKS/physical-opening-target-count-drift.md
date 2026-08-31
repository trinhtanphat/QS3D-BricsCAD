# Physical opening target Count-drift integrity

## Scope

Carrier `issue-5126` hardens `PhysicalOpeningCutTargetStateCodec.Normalize(IEnumerable<string>)` against caller-controlled collections whose advertised Count changes transiently while `MoveNext` or `Current` executes.

## Contract

- Generic, read-only and non-generic known Count values remain admission-time integrity evidence.
- Negative, conflicting and values above 4,096 fail before enumeration.
- A stable known Count is rebound immediately before and after caller-controlled `MoveNext`, after caller-controlled `Current`, and after traversal.
- Known over-yield fails after the unexpected `MoveNext` succeeds but before its `Current` is read.
- Terminal under-yield remains rejected.
- Pure streaming input remains supported, with item 4,097 rejected before its `Current` is read.
- Canonical-id validation, duplicate rejection, case-insensitive sort behavior, `Write` no-partial-publication behavior and the 4,096 accepted boundary remain unchanged.

## Deterministic validation

Run from the repository root:

```text
python scripts/preflight-physical-opening-target-count-drift.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The smoke uses hostile enumerables to prove that MoveNext-time drift is rejected before any `Current` read and Current-time drift is rejected immediately after exactly one caller `Current` read. It also proves known over-yield and pure-streaming item 4,097 are both rejected before an unexpected `Current` read.

## Runtime classification

`NOT_APPLICABLE` for licensed BricsCAD runtime. This carrier changes deterministic Core input-integrity behavior only and does not claim `LOCAL_PASS` evidence.
